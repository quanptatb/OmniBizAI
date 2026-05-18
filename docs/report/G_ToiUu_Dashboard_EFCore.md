# Tối ưu Dashboard EF Core - OmniBizAI

## 1. Mục tiêu

Tài liệu này mô tả phương án và kết quả tối ưu tốc độ tải Dashboard trong hệ thống OmniBizAI theo hướng doanh nghiệp:

- Giảm dữ liệu đọc từ SQL Server.
- Loại bỏ nguy cơ N+1 query khi đọc dữ liệu liên quan.
- Dùng SQL aggregate và projection thay vì materialize toàn bộ entity.
- Bổ sung index phục vụ các truy vấn thống kê Dashboard.
- Thêm cache ngắn hạn để giảm tải khi nhiều người dùng cùng mở Dashboard.
- Đưa ra checklist rà soát các dashboard/report nặng còn lại.

Phạm vi triển khai chính là route `/Dashboard`, `DashboardController.Index()` và `DashboardService.GetDashboardAsync()` trong `Services/AppServices.cs`.

## 2. Hiện trạng trước tối ưu

Dashboard chính chỉ hiển thị các nhóm dữ liệu sau:

- Tổng yêu cầu vận hành.
- Yêu cầu chờ duyệt.
- Yêu cầu quá hạn.
- Người dùng đang hoạt động.
- Biểu đồ yêu cầu theo trạng thái.
- Biểu đồ xu hướng tạo mới/hoàn thành trong 6 tháng.
- 5 yêu cầu gần đây.
- 8 dòng audit log gần đây.

Service cũ có các vấn đề:

- Load toàn bộ `OperationRequests` bằng `ToListAsync()` rồi mới `Count`, `GroupBy`, tính chart trong memory.
- Query thêm `DeptWorkload`, `KpiSummaries`, `Budgets`, `Expenses` dù View `Views/Dashboard/Index.cshtml` không render các dữ liệu này.
- Tính ngân sách đã dùng bằng `budgets.Sum(b => b.Expenses.Sum(...))`, phụ thuộc navigation collection và có nguy cơ sai số liệu hoặc tạo N+1 nếu lazy loading được bật sau này.
- Recent audit join sang `AppUsers` dù `AuditLogs` đã có sẵn `UserName`.

Root cause không chỉ là N+1 cổ điển, mà là Dashboard đang materialize nhiều dữ liệu không cần thiết. Khi bảng lớn lên, thời gian tải sẽ tăng theo số dòng thay vì tăng theo số aggregate nhỏ cần hiển thị.

## 3. Thay đổi đã triển khai

### 3.1. Query read-only dùng `AsNoTracking()`

Các query Dashboard là truy vấn đọc, không chỉnh sửa entity. Vì vậy toàn bộ query chính chuyển sang `AsNoTracking()` để EF Core không tạo tracking graph không cần thiết.

Lợi ích:

- Giảm RAM request.
- Giảm chi phí CPU của Change Tracker.
- Phù hợp với dashboard, list và report.

### 3.2. Aggregate chạy trên SQL Server

Các chỉ số như tổng yêu cầu, quá hạn, số pending approval, active user, số lượng theo trạng thái, số lượng theo tháng được tính bằng `CountAsync()`, `GroupBy()`, `Select()` trên SQL.

Không còn pattern:

```csharp
var requests = await db.OperationRequests.ToListAsync();
var total = requests.Count;
```

Thay vào đó, service dùng `IQueryable` và chỉ materialize kết quả aggregate nhỏ.

### 3.3. Projection thay cho `Include()`

Dashboard không cần entity graph đầy đủ. Với recent request, service chỉ lấy đúng các cột cần render:

- `Id`
- `RequestNo`
- `Title`
- `Type`
- `Status`
- `Priority`
- `CreatedBy`
- `CreatedAt`
- `DueDate`

Không dùng `Include()` cho list/card vì `Include()` load navigation entity đầy đủ, dễ làm câu SQL nặng và kéo thừa cột.

Nguyên tắc:

- Dùng Projection cho dashboard, list, card, chart, report summary.
- Dùng `Include()` cho detail page hoặc nghiệp vụ thật sự cần entity cùng navigation để xử lý đầy đủ.

### 3.4. Monthly trend giới hạn 6 tháng

Monthly trend hiện chỉ query dữ liệu từ đầu tháng cách đây 5 tháng đến đầu tháng kế tiếp.

Service group theo `Year/Month` trên SQL, sau đó mới format label `"MM/yyyy"` trong memory. Cách này tránh dùng `.ToString()` trong LINQ-to-SQL và giúp SQL Server tận dụng index theo `CreatedAt`/`UpdatedAt`.

### 3.5. Bỏ query không render ở request đầu

Các dữ liệu chưa hiển thị trên `Views/Dashboard/Index.cshtml` không còn được query trong Dashboard chính:

- `DeptWorkload`
- `KpiSummaries`
- `TotalBudget`
- `UsedBudget`

Nếu sau này UI cần các widget này, nên thêm endpoint/widget riêng hoặc cache riêng thay vì nhét tất cả vào request `/Dashboard`.

### 3.6. Cache nhẹ bằng `IMemoryCache`

Đã đăng ký `AddMemoryCache()` trong `Program.cs` và inject `IMemoryCache` vào `DashboardService`.

Cache key:

```text
Dashboard:{TenantId}
```

TTL:

```text
30 giây
```

Dashboard snapshot được cache theo tenant, còn thông tin user như `UserFullName`, `UserRole`, `TenantName` được gắn lại sau khi lấy cache. Cách này tránh leak tên người dùng giữa các account cùng tenant.

Chiến lược này phù hợp với app đơn instance. Nếu deploy nhiều instance, có thể nâng cấp sang Redis/Distributed Cache sau.

## 4. Index phục vụ Dashboard

Các index mới được cấu hình bằng Fluent API và migration `AddDashboardPerformanceIndexes`.

### 4.1. `OperationRequests`

```text
IX_OperationRequests_Dashboard_CreatedAt
(TenantId, IsDeleted, CreatedAt)
```

Dùng cho:

- Tổng hợp request theo thời gian.
- Lấy 5 request gần nhất.
- Monthly created trend.

```text
IX_OperationRequests_Dashboard_Status_DueDate
(TenantId, IsDeleted, Status, DueDate)
```

Dùng cho:

- Đếm request quá hạn.
- Filter theo trạng thái và hạn xử lý.

```text
IX_OperationRequests_Dashboard_Status_UpdatedAt
(TenantId, IsDeleted, Status, UpdatedAt)
```

Dùng cho:

- Monthly completed trend.
- Lọc request hoàn thành theo thời gian cập nhật.

```text
IX_OperationRequests_Dashboard_OrganizationUnit
(TenantId, IsDeleted, OrganizationUnitId)
```

Dùng cho:

- Dept workload nếu widget này được bật lại sau.
- Các report vận hành theo phòng ban.

### 4.2. `ApprovalTasks`

```text
IX_ApprovalTasks_Dashboard_Status
(TenantId, IsDeleted, Status)
```

Dùng cho đếm approval task đang pending.

### 4.3. `AppUsers`

```text
IX_AppUsers_Dashboard_Status
(TenantId, IsDeleted, Status)
```

Dùng cho đếm user đang active.

### 4.4. `AuditLogs`

```text
IX_AuditLogs_Dashboard_CreatedAt
(TenantId, CreatedAt)
```

Dùng cho lấy audit log mới nhất theo tenant. Composite index này cũng thay thế tốt index đơn `TenantId` vì `TenantId` vẫn là cột đầu tiên.

## 5. Checklist rà soát các dashboard/report nặng khác

Các màn hình dưới đây vẫn nên được audit tiếp vì có pattern tương tự: kéo list lớn về memory rồi mới thống kê.

### 5.1. `/Reports/Dashboard`

Việc cần làm:

- Không load toàn bộ `OperationRequests` theo date range rồi mới group trong memory.
- Filter `dept` ngay trong SQL trước khi materialize.
- Group status, department, monthly trend bằng SQL.
- Tránh `CreatedAt.Date` trong LINQ-to-SQL; dùng khoảng `>= fromStart` và `< toExclusive`.

### 5.2. `/Reports/Finance`

Việc cần làm:

- Không load toàn bộ `Budgets`, `Expenses`, `PaymentRequests` rồi mới sum/count.
- Tính `TotalBudget`, `TotalExpense`, `PendingPaymentAmount`, `PaymentByStatus`, `ExpenseByDept`, `ExpenseTrend` bằng SQL aggregate.
- Projection department name thay vì `Include(e => e.Budget).ThenInclude(...)` nếu chỉ cần tên phòng ban.

### 5.3. `/Reports/CashFlow`

Việc cần làm:

- Không load toàn bộ `CashTransactions`.
- Tính income/expense/category/payment method/monthly trend bằng SQL.
- Thêm index theo `(TenantId, IsDeleted, Status, TransactionDate, TransactionType)` nếu dữ liệu thu chi tăng lớn.

### 5.4. `/Reports/Crm` và `/Customers/Dashboard`

Việc cần làm:

- Không load toàn bộ `Customers`, `Vendors`, `SalesOpportunities`, `CrmInteractions` nếu chỉ cần count/sum/chart.
- Top customers by revenue nên group trực tiếp trên SQL.
- Recent customers/vendors nên dùng `OrderByDescending().Take(5).Select(...)`.

### 5.5. `/Inventory`

Việc cần làm:

- Service hiện đã batch compute stock, tốt hơn N+1 từng sản phẩm.
- Có thể tối ưu thêm bằng dictionary lookup thay vì `FirstOrDefault()` lặp trên từng product.
- Khi dataset lớn, nên đẩy tính tồn kho vào SQL projection hoặc tạo bảng/materialized summary tồn kho.

### 5.6. `/Employees/Dashboard`

Việc cần làm:

- Không load toàn bộ `EmployeeProfiles.Include(User)` nếu chỉ cần count active/inactive/new month.
- Recent employees dùng projection + `Take(5)`.
- Department distribution join/group trên SQL thay vì query toàn bộ department dictionary nếu dữ liệu lớn.

### 5.7. `/Okr/Dashboard`

Việc cần làm:

- Không load toàn bộ OKR + KeyResults nếu chỉ cần progress trung bình và 5 OKR gần đây.
- Average progress có thể projection dữ liệu tối thiểu hoặc tính summary riêng.
- Recent KPI dùng projection, không cần load toàn bộ KPI rồi `Take(5)` trong memory.

## 6. Test plan

### 6.1. Kiểm thử build

Chạy:

```bash
dotnet build
```

Kỳ vọng:

- Build thành công.
- Không phát sinh lỗi compile do service/migration.

### 6.2. Kiểm thử đúng dữ liệu

Kiểm tra `/Dashboard` sau login:

- Tổng yêu cầu vận hành đúng theo tenant.
- Số yêu cầu quá hạn đúng điều kiện: `DueDate < today`, status không phải `Completed` hoặc `Cancelled`.
- Pending approvals đúng status `Pending`.
- Active users đúng status `Active`.
- Chart trạng thái hiển thị đủ status có dữ liệu.
- Chart monthly trend đủ 6 tháng gần nhất.
- Recent requests tối đa 5 dòng.
- Recent audits tối đa 8 dòng.
- Không hiển thị dữ liệu soft-deleted.
- Không leak dữ liệu tenant khác.

### 6.3. Kiểm thử hiệu năng

Seed dữ liệu lớn để đo:

- 10.000 `OperationRequests`.
- 5.000 `AuditLogs`.
- 1.000 `AppUsers`.
- 1.000 `ApprovalTasks`.

Kỳ vọng:

- `/Dashboard` không materialize toàn bộ `OperationRequests`.
- Query recent request chỉ lấy 5 dòng.
- Query recent audit chỉ lấy 8 dòng.
- Server-side processing mục tiêu dưới 500-800ms ở môi trường local với SQL Server ổn định.
- Tổng page load đạt NFR dưới 3 giây.

### 6.4. Đo trước/sau

Gợi ý bật EF command logging trong Development:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

Các chỉ số cần ghi:

- Số SQL commands khi load `/Dashboard`.
- Tổng thời gian xử lý request.
- Thời gian câu query chậm nhất.
- Số dòng trả về từ query recent/list.
- CPU/RAM nếu có công cụ profiling.

## 7. Acceptance criteria

Task đạt yêu cầu khi:

- `DashboardService.GetDashboardAsync()` không còn `ToListAsync()` toàn bộ `OperationRequests` để thống kê.
- Query dashboard read-only dùng `AsNoTracking()`.
- Dashboard dùng projection thay vì `Include()` cho dữ liệu list/card.
- Không query budget/KPI/dept workload trong request đầu nếu UI chưa render.
- Có `IMemoryCache` TTL 30 giây theo tenant.
- Có migration index phục vụ dashboard.
- `dotnet build` thành công.
- Có tài liệu review chi tiết trong `docs/report/G_ToiUu_Dashboard_EFCore.md`.

## 8. Hướng phát triển tiếp

Sau khi Dashboard chính ổn định, nên tạo backlog tối ưu cho report và dashboard còn lại theo thứ tự ưu tiên:

1. `/Reports/Executive` vì tổng hợp nhiều phân hệ.
2. `/Reports/Finance` và `/Reports/CashFlow` vì dữ liệu tài chính có thể tăng nhanh.
3. `/Inventory` vì tồn kho cần aggregate nhập/xuất.
4. `/Employees/Dashboard` và `/Okr/Dashboard` vì có nhiều navigation/collection.
5. `/Reports/Crm` vì pipeline và interaction trend dễ lớn theo thời gian.

Nếu hệ thống chạy production nhiều tenant, cân nhắc:

- Distributed cache bằng Redis.
- Query tags để tracing.
- Database-level monitoring cho slow query.
- Precomputed summary table cho các chart lớn.
