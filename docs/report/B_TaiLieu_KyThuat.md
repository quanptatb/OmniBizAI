# TÀI LIỆU KỸ THUẬT — HỆ THỐNG OMNIBIZAI

> Phiên bản: 1.0 | Ngày cập nhật: 18/05/2026  
> Đối tượng: Lập trình viên, kỹ sư bảo trì, AI Agent

---

## 1. Tổng quan hệ thống

**OmniBizAI** là nền tảng vận hành thông minh dành cho doanh nghiệp vừa và nhỏ (SME), tích hợp đa module trên một giao diện web duy nhất:

- Quản lý vận hành (Operation Requests, Kanban Workflow)
- Quản lý tài chính (Ngân sách, Chi phí, Thu chi, Đề nghị thanh toán)
- Mua sắm & Kho vận (Procurement, PO, Nhập/Xuất kho, Cảnh báo tồn kho)
- Quản lý nhân sự (Phòng ban, Nhân viên, Chức vụ, Nghỉ phép)
- CRM (Khách hàng, Nhà cung cấp, Sản phẩm, Cơ hội bán hàng, Tương tác)
- KPI / OKR & Đánh giá hiệu suất
- Báo cáo đa chiều & AI Copilot (Gemini)
- Hệ thống thông báo, Audit Log, Sao lưu dữ liệu

Hệ thống hỗ trợ **đa tenant** (multi-tenant), phân quyền RBAC 7 vai trò, thiết kế theo Apple Design System.

---

## 2. Kiến trúc tổng thể

```
┌─────────────────────────────────────────────┐
│            Client (Browser)                 │
│   HTML / CSS / JavaScript / Bootstrap 5     │
└──────────────────┬──────────────────────────┘
                   │ HTTPS
┌──────────────────▼──────────────────────────┐
│         ASP.NET Core MVC 10                 │
│  ┌─────────────┐  ┌──────────────────────┐  │
│  │ Controllers │  │ Views (Razor .cshtml)│  │
│  └──────┬──────┘  └──────────────────────┘  │
│         │                                    │
│  ┌──────▼──────────────────────────────┐    │
│  │         Service Layer               │    │
│  │  AppServices, CrmService, HrService │    │
│  │  KpiOkrServices, ProcurementService │    │
│  │  InventoryService, CashBookService  │    │
│  │  BackupService, NotificationService │    │
│  │  SettingsService, GeminiService     │    │
│  └──────┬──────────────────────────────┘    │
│         │                                    │
│  ┌──────▼──────────────────────────────┐    │
│  │   Data Access (EF Core 10)          │    │
│  │   ApplicationDbContext              │    │
│  │   ~95 Entity Models                 │    │
│  └──────┬──────────────────────────────┘    │
└─────────┼────────────────────────────────────┘
          │
┌─────────▼────────────────────────────────────┐
│        SQL Server (OmniBizDB)                │
└──────────────────────────────────────────────┘
```

---

## 3. Công nghệ sử dụng

| Thành phần | Công nghệ | Phiên bản |
|---|---|---|
| Framework | ASP.NET Core MVC | 10.0 Preview 4 |
| Ngôn ngữ | C# | 14 |
| ORM | Entity Framework Core | 10.0 Preview 4 |
| CSDL | SQL Server | 2022+ |
| Identity | ASP.NET Core Identity | 10.0 |
| Frontend | HTML5, CSS3, JavaScript ES6+ | - |
| CSS Framework | Bootstrap | 5.3.3 |
| Icon | Font Awesome 6.5, Bootstrap Icons 1.11 | - |
| Font | Inter (Google Fonts) | Variable |
| Chart | Chart.js | 4.4.3 |
| Excel Export | ClosedXML | 0.104.2 |
| AI | Google Gemini API | - |

---

## 4. Mô hình phân lớp

### 4.1. Presentation Layer
- **Views** (`Views/`): 39 thư mục view, sử dụng Razor `.cshtml`
- **Static Assets** (`wwwroot/`): CSS (`site.css`, `kpi-okr.css`), JS (`site.js`), images
- **Layout**: `_Layout.cshtml` — Sidebar + Topbar + Content + App Launcher

### 4.2. Controller Layer
- **22 Controller files** xử lý routing và điều phối logic
- Chính: `OperationsController`, `BusinessControllers`, `CrmControllers`, `ProcurementControllers`, `HrSettingsControllers`, `AccountController`...

### 4.3. Service Layer
- **12 Service classes** chứa toàn bộ business logic
- Pattern: Controller → Service → DbContext
- Tenant isolation được thực hiện qua `ITenantContext`

### 4.4. Data Access Layer
- **`ApplicationDbContext`**: Kế thừa `IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>`
- **~95 Entity Models** trong `Models/Entities/`
- **Global Query Filter**: Soft Delete (`IsDeleted`) + Tenant Isolation (`TenantId`)
- **Delete Behavior**: `DeleteBehavior.Restrict` toàn cục (tránh cascade delete)

---

## 5. Cấu trúc thư mục source code

```
OmniBizAI/
├── Controllers/          # 22 controller files
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── Seed/
│   │   └── seed_data.sql # Master seed script
│   └── Configurations/   # EF Fluent API configs
├── Models/
│   ├── Entities/          # ~95 entity models
│   │   ├── Common/        # TenantEntity base class
│   │   └── Enums/         # DomainEnums.cs
│   └── ViewModels/        # (nếu có)
├── ViewModels/            # Request/Response models
├── Services/              # 12 service classes
├── Helpers/               # Extension methods, utilities
├── Views/                 # 39 view directories + Shared
├── wwwroot/
│   ├── css/               # site.css, kpi-okr.css
│   ├── js/                # site.js
│   └── images/
├── Properties/
├── Program.cs             # Entry point + DI configuration
├── appsettings.json       # Connection string, Gemini config
├── DESIGN.md              # Apple Design System tokens
└── docs/                  # Tài liệu dự án
```

---

## 6. Quy ước đặt tên

| Đối tượng | Quy ước | Ví dụ |
|---|---|---|
| Entity | PascalCase, số ít | `OperationRequest`, `KpiDefinition` |
| DbSet | PascalCase, số nhiều | `OperationRequests`, `KpiDefinitions` |
| Controller | `{Feature}Controller` | `OperationsController` |
| Service | `{Feature}Service` | `CrmService`, `HrService` |
| View folder | Khớp Controller name | `Views/Operations/` |
| CSS class | kebab-case | `.sidebar-nav`, `.nav-item` |
| JS function | camelCase | `toggleAppLauncher()` |
| Enum | PascalCase | `OperationStatus.InProgress` |
| FK property | `{Entity}Id` | `CustomerId`, `TenantId` |

---

## 7. Thiết kế Database

### 7.1. Tổng quan
- **Database**: `OmniBizDB` (SQL Server)
- **Số bảng**: ~95 bảng (bao gồm ASP.NET Identity tables)
- **Base Entity**: Mọi entity kế thừa từ `TenantEntity` gồm: `Id (Guid)`, `TenantId`, `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`, `IsDeleted`

### 7.2. Phân nhóm bảng

| Nhóm | Bảng chính |
|---|---|
| **Tenant & Config** | Tenants, TenantSettings, TenantModules, BusinessProfiles, SystemParameters, NumberSequences |
| **Auth / RBAC** | AspNetUsers, AspNetRoles, AspNetUserRoles, AppUsers, UserProfiles, UserTenants, UserSessions, RoleDefinitions, PermissionDefinitions, PermissionAssignments, UserRoleAssignments |
| **Organization** | OrganizationUnits, Positions, EmployeeProfiles, EmployeeDepartmentAssignments, EmployeeContracts, LeaveRequests, WorkCalendars |
| **CRM & Catalog** | Customers, CustomerContacts, CustomerSites, CrmInteractions, SalesOpportunities, Vendors, ProductCategories, ProductServices, UnitsOfMeasure |
| **Operations** | OperationRequests, OperationRequestLines, WorkItems, WorkItemAssignments, WorkItemChecklists, WorkItemComments, KanbanColumns, Attachments, Tags, EntityTags |
| **Workflow** | WorkflowDefinitions, WorkflowSteps, WorkflowTransitions, WorkflowInstances, WorkflowHistories, ApprovalTasks |
| **Finance** | ProcurementRequests, ProcurementRequestLines, PurchaseOrders, PurchaseOrderLines, GoodsReceipts, GoodsReceiptLines, GoodsIssues, GoodsIssueLines, StockAlerts, CashTransactions, PaymentRequests, PaymentRequestLines, Budgets, Expenses |
| **KPI / OKR** | KpiDefinitions, KpiTargets, KpiResults, KpiCheckIns, KpiCheckInDetails, KpiGoalComments, KpiFailReasons, KpiDepartmentAssignments, KpiEmployeeAssignments, OkrObjectives, OkrKeyResults, OkrMissionMappings, MissionVisions, EvaluationPeriods, EvaluationResults, GradingRanks, BonusRules |
| **AI / Audit** | AiInsights, AiPromptTemplates, AiProviderConfigurations, AiGenerationHistories, AuditLogs, ImportJobs, ImportStagingRows, Notifications, NotificationDeliveries |
| **Reports** | ReportDefinitions, DashboardWidgets |

---

## 8. Thiết kế phân quyền (RBAC)

### 8.1. Danh sách vai trò

| # | Role Key | Tên hiển thị | Mô tả |
|---|---|---|---|
| 1 | `SYSTEM_ADMIN` | System Admin | Quản trị toàn hệ thống, mọi tenant |
| 2 | `TENANT_ADMIN` | Tenant Admin | Quản trị viên của một tổ chức |
| 3 | `EXECUTIVE` | Ban Giám đốc | Xem báo cáo tổng hợp, phê duyệt cấp cao |
| 4 | `DEPARTMENT_MANAGER` | Trưởng phòng | Quản lý phòng ban, phê duyệt |
| 5 | `STAFF` | Nhân viên | Thao tác nghiệp vụ hàng ngày |
| 6 | `ACCOUNTANT` | Kế toán | Quản lý tài chính, thu chi |
| 7 | `AUDITOR` | Kiểm toán | Xem nhật ký hệ thống |

### 8.2. Ma trận phân quyền (tóm tắt)

| Chức năng | SYS_ADMIN | TENANT_ADMIN | EXECUTIVE | DEPT_MGR | STAFF | ACCOUNTANT | AUDITOR |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Dashboard | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Yêu cầu vận hành | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Phê duyệt | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Tài chính | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Nhân sự (Phòng ban) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| KPI / OKR | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Cài đặt hệ thống | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Sao lưu | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Nhật ký Audit | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |

---

## 9. Luồng xử lý nghiệp vụ chính

### 9.1. Luồng Yêu cầu vận hành
`Tạo yêu cầu → Gửi phê duyệt → Trưởng phòng duyệt → Phân công Work Items → Thực hiện trên Kanban → Hoàn thành`

### 9.2. Luồng Mua sắm
`Tạo Đề xuất mua sắm → Phê duyệt → Tạo Đơn mua hàng (PO) → Gửi NCC → Nhập kho (Goods Receipt) → Cập nhật tồn kho`

### 9.3. Luồng KPI
`Thiết lập KPI → Gán phòng ban/nhân viên → Check-In định kỳ → Đánh giá cuối kỳ → Xếp hạng`

### 9.4. Luồng Thu Chi
`Ghi nhận giao dịch → Phân loại (Income/Expense) → Đối soát → Báo cáo`

---

## 10. API / Route / Controller Action

Hệ thống sử dụng **MVC Routing** (không phải Web API). Route mặc định:

```
{controller=Dashboard}/{action=Index}/{id?}
```

### Danh sách Controller chính

| Controller | Route prefix | Actions chính |
|---|---|---|
| `DashboardController` | `/Dashboard` | `Index` |
| `OperationsController` | `/Operations` | `Index`, `Create`, `Edit`, `Details`, `Submit`, `Cancel` |
| `WorkflowController` | `/Workflow` | `Kanban`, `MoveCard`, `AddComment` |
| `ApprovalsController` | `/Approvals` | `MyTasks`, `Process` |
| `BusinessControllers` | `/Finance`, `/Expenses` | `Index`, `Budgets`, `PaymentRequests`, `Create`, `Edit` |
| `ProcurementControllers` | `/Procurement`, `/PurchaseOrders`, `/GoodsReceipt` | `Index`, `Create`, `Approve` |
| `CrmControllers` | `/Customers`, `/Vendors`, `/Products` | `Index`, `Dashboard`, `Create`, `Edit`, `Delete` |
| `HrSettingsControllers` | `/Organization`, `/Employees`, `/Positions`, `/Settings` | `Index`, `Create`, `Company`, `Appearance`, `Modules` |
| `OkrController` | `/Okr` | `Index`, `Dashboard`, `Create`, `Edit` |
| `KpiSetupController` | `/KpiSetup` | `Index`, `Create`, `Edit` |
| `AccountController` | `/Account` | `Login`, `Logout`, `Register`, `ForgotPassword` |

---

## 11. Bảo mật

| Lớp | Giải pháp |
|---|---|
| Authentication | ASP.NET Core Identity (Cookie-based, 8h expiry, sliding) |
| Authorization | Role-based (`[Authorize(Roles = "...")]`) |
| Password | Hash bằng ASP.NET Identity (PBKDF2) |
| CSRF | Anti-Forgery Token trên mọi form POST |
| Lockout | 5 lần thất bại → khóa 15 phút |
| Soft Delete | Dữ liệu không bao giờ bị xóa vật lý |
| Tenant Isolation | Global Query Filter theo `TenantId` |
| Audit Trail | `AuditLogs` ghi nhận mọi thao tác CRUD |

---

## 12. Sao lưu và phục hồi

- **BackupService**: Tạo backup SQL Server (`.bak`) lưu tại `App_Data/Backups/`
- **Quyền**: Chỉ `SYSTEM_ADMIN` và `TENANT_ADMIN` được thực hiện
- **Tính năng**: Tạo backup thủ công, xem danh sách, tải về, xóa file cũ

---

## 13. Cấu hình môi trường

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=OmniBizDB;Trusted_Connection=True;..."
  },
  "Gemini": {
    "ApiKey": "[YOUR_API_KEY]",
    "Model": "gemini-pro"
  }
}
```

### Cấu hình DI (Program.cs)
- 12 Scoped Services được đăng ký
- 1 Singleton (`IEmailService`)
- 1 HttpClient (`GeminiService`)
- Auto-migration on startup (`db.Database.MigrateAsync()`)
- Auto-fix password cho user seed bằng SQL

---

## 14. Hướng dẫn Build, Run, Deploy

### Yêu cầu
- .NET 10 SDK (Preview 4+)
- SQL Server 2022+
- Node.js (không bắt buộc, chỉ cần nếu dùng npm packages)

### Chạy Development
```bash
dotnet restore
dotnet ef database update
dotnet watch run
```

### Build Production
```bash
dotnet publish -c Release -o ./publish
```

---

## 15. Danh sách thư viện

| Package | Phiên bản | Mục đích |
|---|---|---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0-preview.4 | Xác thực & phân quyền |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0-preview.4 | ORM + SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0-preview.4 | EF CLI (migrations) |
| `ClosedXML` | 0.104.2 | Xuất báo cáo Excel |

---

## 16. Các vấn đề kỹ thuật cần lưu ý

1. **NU1903 Warning**: Package `Microsoft.Build.Tasks.Core` 17.7.2 có lỗ hổng bảo mật — cần theo dõi bản vá
2. **DateOnly/DateTimeOffset**: EF Core không translate được `DateOnly.FromDateTime()` trong LINQ — phải so sánh trực tiếp bằng `DateTimeOffset`
3. **Cascade Delete**: Đã tắt toàn cục (`DeleteBehavior.Restrict`) để tránh lỗi SQL Server multiple cascade paths
4. **Seed Data**: User tạo bằng SQL không có PasswordHash — `Program.cs` tự động fix bằng `UserManager.AddPasswordAsync("123")`
5. **Multi-tenant**: Mọi query phải đi qua `TenantId` filter — nếu thêm entity mới, nhớ kế thừa `TenantEntity`
