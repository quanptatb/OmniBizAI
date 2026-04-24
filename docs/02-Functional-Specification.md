# 📐 OmniBiz AI — Functional Specification

> **Version**: 1.0 | **Updated**: 2026-04-24

---

## 1. Module: Authentication & Authorization

### 1.1 Đăng nhập (Login)
- **Input**: Email, Password
- **Output**: JWT Access Token + Refresh Token
- **Flow**: User nhập email/password → Validate → Tạo JWT (chứa userId, role, departmentId, permissions) → Trả token + redirect dashboard theo role
- **Rules**:
  - Password tối thiểu 8 ký tự, có uppercase, number, special char
  - Lock account sau 5 lần sai liên tiếp (unlock sau 15 phút)
  - Access Token TTL: 60 phút, Refresh Token: 7 ngày
- **Edge Cases**:
  - Email không tồn tại → "Email hoặc mật khẩu không đúng"
  - Account bị khóa → Hiển thị thời gian còn lại
  - Token hết hạn → Auto refresh, nếu refresh token hết hạn → redirect login

### 1.2 Phân quyền RBAC
- **Roles**: Admin, Director, Manager, Accountant, HR, Staff
- **Permission Model**: Role → có nhiều Permission (module:action)
- **Data Scoping**:
  - Admin/Director: Toàn bộ công ty
  - Manager: Phòng ban mình quản lý
  - Staff/Accountant/HR: Dữ liệu cá nhân + dữ liệu được assign

---

## 2. Module: HR Basic (Organization)

### 2.1 Quản lý Phòng ban (Department)
- **CRUD**: Tạo/sửa/xóa/xem phòng ban
- **Fields**: Name, Code, ParentDepartmentId (hỗ trợ phòng ban con), ManagerId, BudgetLimit, Status
- **Rules**: Không xóa phòng ban có nhân viên active. Phòng ban con kế thừa budget parent nếu không set riêng
- **Edge Cases**: Di chuyển NV giữa phòng ban → cập nhật data scope

### 2.2 Quản lý Nhân viên (Employee)
- **CRUD**: Tạo/sửa/xóa/xem nhân viên
- **Fields**: FullName, Email, Phone, EmployeeCode, DepartmentId, PositionId, ManagerId, JoinDate, Status, Avatar
- **Rules**: Email unique. EmployeeCode auto-generate (VD: EMP-001). Soft delete (IsDeleted flag)
- **Edge Cases**: NV nghỉ việc → deactivate, giữ dữ liệu lịch sử

### 2.3 Quản lý Chức vụ (Position)
- **Fields**: Name, Level, DepartmentId, Description
- **Rules**: Level dùng cho workflow approval (level càng cao, quyền duyệt càng lớn)

---

## 3. Module: Finance

### 3.1 Quản lý Ngân sách (Budget)
- **CRUD**: Tạo/sửa ngân sách theo kỳ
- **Fields**: Name, DepartmentId, CategoryId, FiscalYear, FiscalMonth/Quarter, AllocatedAmount, SpentAmount, RemainingAmount, Status
- **Rules**:
  - RemainingAmount = AllocatedAmount - SpentAmount (auto-calculated)
  - Cảnh báo khi SpentAmount > 80% AllocatedAmount (Yellow), > 100% (Red)
  - Chỉ Director/Accountant được tạo/sửa budget
- **Edge Cases**: Điều chỉnh budget giữa kỳ → tạo BudgetAdjustment record với lý do

### 3.2 Đề nghị Thanh toán (Payment Request)
- **Input**: Title, Description, Amount, CategoryId, VendorId, DepartmentId, LineItems[], Attachments[]
- **Flow**:
  1. Staff tạo đề nghị → Status: Draft
  2. Submit → AI Risk Check tự động → Status: PendingApproval
  3. Workflow engine xác định approver(s) dựa trên rule
  4. Approver duyệt/từ chối → Status: Approved/Rejected
  5. Sau khi Approved → tạo Transaction tự động
- **Rules**:
  - Amount > 0, phải có ít nhất 1 line item
  - Mỗi line item: Description, Quantity, UnitPrice, TotalPrice
  - File đính kèm: max 5 files, mỗi file ≤ 10MB, chấp nhận PDF/JPG/PNG/XLSX
- **Edge Cases**:
  - Vượt ngân sách → AI cảnh báo nhưng vẫn cho submit (kèm flag)
  - NCC trùng trong 7 ngày → AI cảnh báo duplicate

### 3.3 Giao dịch (Transaction)
- **Fields**: Type (Income/Expense), Amount, CategoryId, DepartmentId, PaymentRequestId, TransactionDate, Reference, Note
- **Rules**: Expense tự động trừ budget. Income cộng vào wallet
- **Edge Cases**: Hủy giao dịch → reverse budget impact

### 3.4 Danh mục Chi phí (Category)
- **Fields**: Name, Code, Type (Income/Expense), ParentCategoryId, IsActive
- **Rules**: Hỗ trợ tree structure (VD: Marketing > Digital > Google Ads)

### 3.5 Nhà cung cấp (Vendor)
- **Fields**: Name, TaxCode, ContactPerson, Phone, Email, Address, BankAccount, Rating
- **Rules**: TaxCode unique nếu có

### 3.6 Ví / Tài khoản (Wallet)
- **Fields**: Name, Type (Cash/Bank/EWallet), Balance, Currency, BankName, AccountNumber
- **Rules**: Balance cập nhật real-time theo transaction

---

## 4. Module: KPI/OKR

### 4.1 Kỳ đánh giá (Evaluation Period)
- **Fields**: Name, StartDate, EndDate, Type (Monthly/Quarterly/Yearly), Status (Planning/Active/Closed)
- **Rules**: Không overlap period cùng type. Chỉ Admin/Director tạo

### 4.2 Objective (OKR)
- **Fields**: Title, Description, PeriodId, OwnerType (Company/Department/Individual), OwnerId, ParentObjectiveId, Progress, Status
- **Rules**:
  - Progress = average(KeyResults.Progress)
  - Hỗ trợ cascade: Company → Department → Individual
  - Status: Draft, Active, Completed, Cancelled
- **Edge Cases**: Xóa Objective → cascade xóa Key Results

### 4.3 Key Result
- **Fields**: Title, ObjectiveId, MetricType (Number/Percentage/Currency/Boolean), StartValue, TargetValue, CurrentValue, Weight, Progress, Unit
- **Rules**:
  - Progress = (CurrentValue - StartValue) / (TargetValue - StartValue) × 100
  - Weight tổng các KR trong 1 Objective = 100%
- **Edge Cases**: TargetValue = StartValue → Progress = 0 (tránh chia 0)

### 4.4 KPI
- **Fields**: Name, Description, PeriodId, DepartmentId, AssigneeId, Formula, TargetValue, CurrentValue, Weight, Unit, Frequency
- **Rules**: Phân bổ theo cá nhân hoặc phòng ban. Auto-calculate từ data source nếu có formula

### 4.5 Check-in
- **Fields**: KpiId/KeyResultId, CheckinDate, PreviousValue, NewValue, Progress, Note, Evidence(attachments), Status
- **Flow**:
  1. Staff tạo check-in → Status: Submitted
  2. Manager review → Approve (cập nhật CurrentValue) hoặc Reject (kèm comment)
- **Rules**: Tối đa 1 check-in/tuần/KPI. Phải có note giải thích
- **Edge Cases**: Check-in bị reject → Staff sửa và submit lại

### 4.6 Đánh giá hiệu suất (Performance Evaluation)
- **Fields**: EmployeeId, PeriodId, TotalScore, Rating (A/B/C/D/E), ReviewerId, Comments, Status
- **Rules**:
  - A: ≥90%, B: 70-89%, C: 50-69%, D: 30-49%, E: <30%
  - TotalScore = weighted average của tất cả KPI assigned

---

## 5. Module: Workflow Engine

### 5.1 Workflow Template
- **Fields**: Name, EntityType (PaymentRequest/BudgetAdjustment/KPIApproval), Steps[], Conditions[], IsActive
- **Rules**: Mỗi EntityType có thể có nhiều template, nhưng chỉ 1 active

### 5.2 Workflow Step
- **Fields**: TemplateId, StepOrder, ApproverType (Role/Position/SpecificUser), ApproverId, IsRequired, TimeoutHours
- **Rules**: Steps thực hiện tuần tự. Nếu IsRequired=false → auto-skip nếu timeout

### 5.3 Workflow Condition
- **Fields**: TemplateId, Field (Amount/BudgetPercentage/Department), Operator (>, <, =, >=, <=), Value, ThenTemplateId
- **VD**: Nếu Amount > 50,000,000 → dùng template "Duyệt 3 cấp"
- **VD**: Nếu BudgetPercentage > 80% → thêm step Director duyệt

### 5.4 Workflow Instance
- **Fields**: TemplateId, EntityType, EntityId, CurrentStepOrder, Status (Pending/InProgress/Approved/Rejected/Cancelled), CreatedBy
- **Flow**: Submit entity → Tạo instance → Notify first approver → Approve → Next step → ... → Final approve
- **Edge Cases**: Approver nghỉ phép → escalate lên cấp trên sau timeout

### 5.5 Approval Action
- **Fields**: InstanceId, StepOrder, ApproverId, Action (Approve/Reject/Comment/RequestChange), Comment, ActionDate
- **Rules**: Mỗi step chỉ 1 action. Reject ở bất kỳ step → whole instance Rejected

---

## 6. Module: AI Copilot

### 6.1 AI Chat Q&A
- **Input**: User message (tiếng Việt), Context (current page, filters, selected data)
- **Output**: AI response với data references, charts (nếu cần)
- **Flow**: User hỏi → Extract intent → Query internal data (RAG) → Build prompt with context → LLM generate → Parse response → Render with citations
- **Rules**: Chỉ trả lời dựa trên data user có quyền xem. Citation bắt buộc khi trích dẫn số liệu
- **Edge Cases**: Câu hỏi ngoài scope → "Tôi chỉ có thể trả lời về dữ liệu doanh nghiệp"

### 6.2 AI Spend Guardrail
- **Input**: PaymentRequest data
- **Output**: RiskScore (0-100), RiskFactors[], Recommendations[]
- **Checks**:
  - Vượt ngân sách (% budget remaining)
  - NCC trùng lặp trong 7 ngày
  - Amount bất thường so với lịch sử
  - Category không phù hợp với phòng ban
- **Rules**: RiskScore > 70 → flag "High Risk". Không block, chỉ cảnh báo

### 6.3 AI KPI Insight
- **Input**: KPI/OKR data của period hiện tại
- **Output**: InsightType (Warning/Suggestion/Achievement), Message, AffectedEntities[]
- **Checks**: KPI tụt > 20% so với target pace, Phòng ban chậm tiến độ, NV cần hỗ trợ

### 6.4 AI Report Writer
- **Input**: ReportType (Monthly/Quarterly), Period, Scope (Company/Department)
- **Output**: Markdown report với sections: Summary, Financial Overview, KPI Status, Risks, Recommendations
- **Rules**: Tự động include charts data. Dùng số liệu thực từ DB

### 6.5 AI Generation History
- **Fields**: UserId, Module, PromptType, InputData, OutputData, Rating, CreatedAt
- **Rules**: Lưu lại để user review và reuse. Retention: 90 ngày

---

## 7. Module: Notification

### 7.1 In-App Notification
- **Triggers**: Approval request, Approval result, KPI deadline, Budget warning, AI alert
- **Fields**: UserId, Title, Message, Type, EntityType, EntityId, IsRead, CreatedAt
- **Delivery**: SignalR realtime push + Bell icon với unread count

### 7.2 Email Notification (Optional)
- **Triggers**: Same as in-app, configurable per user
- **Rules**: Batch digest option (daily summary thay vì mỗi event)

---

## 8. Module: Reporting & Export

### 8.1 Financial Reports
- Báo cáo thu/chi theo kỳ, phòng ban, danh mục
- Budget utilization report
- Cashflow projection

### 8.2 KPI/OKR Reports
- Tiến độ OKR theo phòng ban
- KPI scorecard cá nhân
- Performance ranking

### 8.3 Export
- **Formats**: PDF, Excel (XLSX)
- **Rules**: Export theo quyền data scope. Audit log khi export

---

## 9. Cross-cutting Concerns

### 9.1 Audit Log
- **Tracked Actions**: Login/Logout, CRUD operations, Approval actions, Export, AI queries
- **Fields**: UserId, Action, EntityType, EntityId, OldValue (JSON), NewValue (JSON), IpAddress, UserAgent, Timestamp
- **Rules**: Immutable (không xóa/sửa). Retention: 1 năm. Chỉ Admin xem

### 9.2 File Management
- **Storage**: Local disk (MVP) hoặc Azure Blob / S3
- **Rules**: Virus scan disabled cho MVP. Max file size: 10MB. Allowed types: PDF, JPG, PNG, XLSX, DOCX

### 9.3 Search & Filter
- Mỗi list view hỗ trợ: Text search, Date range, Status filter, Department filter, Pagination (20 items/page), Sort (asc/desc)
