# OmniBizAI - Thiết kế hệ thống và catalog sơ đồ

> Tài liệu này gom các sơ đồ bắt buộc/nên có cho đồ án ASP.NET Core MVC .NET 10 + SQL Server.  
> Các sơ đồ dùng Mermaid để render trực tiếp trong Markdown. Khi cần nộp Word/PDF, có thể export từng sơ đồ thành PNG.

## 1. Catalog sơ đồ

| STT | Sơ đồ | Mục đích | Mức độ | Vị trí |
| --- | --- | --- | --- | --- |
| 1 | Use Case Diagram | Người dùng nào dùng chức năng nào | Bắt buộc | Mục 2 |
| 2 | Use Case Description | Chi tiết luồng chính/phụ/điều kiện | Bắt buộc | [04-Requirements-and-Use-Cases.md](./04-Requirements-and-Use-Cases.md) |
| 3 | ERD | Thiết kế thực thể, quan hệ nghiệp vụ | Bắt buộc | [06-Database-Design.md](./06-Database-Design.md) |
| 4 | Database Diagram | Sơ đồ bảng SQL Server thực tế | Bắt buộc | [06-Database-Design.md](./06-Database-Design.md) |
| 5 | Class Diagram | Model, Entity, ViewModel, Service | Nên có | Mục 4 |
| 6 | Sequence Diagram | Luồng xử lý chức năng quan trọng | Nên có | Mục 5 |
| 7 | Activity Diagram | Quy trình nghiệp vụ | Nên có | Mục 6 |
| 8 | Architecture Diagram | Browser -> Controller -> Service -> DbContext -> SQL Server | Bắt buộc | Mục 3 |
| 9 | Sitemap / Navigation Diagram | Trang và luồng điều hướng | Nên có | Mục 7 |
| 10 | Deployment Diagram | IIS/server, app, SQL Server, HTTPS | Nên có | Mục 8 |

## 2. Use Case Diagram

```mermaid
flowchart LR
    Admin["Tenant Admin"] --> Org(("Quản lý tổ chức"))
    Admin --> Users(("Quản lý người dùng"))
    Admin --> Roles(("Quản lý vai trò/quyền"))

    Staff["Staff"] --> CreateOps(("Tạo yêu cầu vận hành"))
    Staff --> UpdateTask(("Cập nhật công việc"))

    Manager["Department Manager"] --> Approve(("Duyệt yêu cầu"))
    Manager --> DeptReport(("Xem KPI bộ phận"))
    Manager --> Ai(("Hỏi AI"))

    Executive["Executive"] --> Dashboard(("Xem dashboard tổng hợp"))
    Executive --> Approve
    Executive --> Ai
    Executive --> Export(("Xuất báo cáo"))

    Accountant["Accountant"] --> Finance(("Quản lý chi phí/thanh toán"))
    Auditor["Auditor"] --> Audit(("Xem audit log"))
    Auditor --> Export
```

## 3. Architecture Diagram

ASP.NET Core MVC tách trách nhiệm thành Model, View và Controller. Trong dự án này, Controller nhận request, Service xử lý business logic, DbContext làm việc với SQL Server, View `.cshtml` hiển thị UI bằng Razor.

```mermaid
flowchart TB
    Browser["Browser"] --> HTTPS["HTTPS"]
    HTTPS --> MVC["ASP.NET Core MVC App"]

    subgraph App["OmniBizAI Web Application"]
        Routing["Routing / Middleware"] --> Auth["Authentication + Authorization"]
        Auth --> Controller["MVC Controllers"]
        Controller --> ViewModel["ViewModels / Input Models"]
        Controller --> Service["Application Services"]
        Service --> Domain["Domain Entities"]
        Service --> Workflow["Workflow Services"]
        Service --> AiService["AI Decision Service"]
        Service --> Export["Export Service"]
        Service --> Audit["Audit Logger"]
        Service --> DbContext["EF Core DbContext"]
        Controller --> Razor["Razor Views .cshtml"]
    end

    DbContext --> SQL[("SQL Server")]
    AiService --> Provider["AI Provider"]
    Export --> Files["Excel / CSV / PDF"]
    Audit --> SQL
```

## 4. Class Diagram

```mermaid
classDiagram
    class BaseEntity {
        +Guid Id
        +DateTimeOffset CreatedAt
        +DateTimeOffset? UpdatedAt
        +bool IsDeleted
    }

    class Tenant {
        +string Code
        +string Name
        +TenantStatus Status
    }

    class OrganizationUnit {
        +Guid TenantId
        +Guid? ParentId
        +string Code
        +string Name
        +int Level
        +Guid? ManagerUserId
    }

    class AppUser {
        +Guid TenantId
        +Guid? OrganizationUnitId
        +string FullName
        +string Email
        +UserStatus Status
    }

    class OperationRequest {
        +Guid TenantId
        +string RequestNo
        +string Type
        +string Title
        +Guid OrganizationUnitId
        +Guid CreatedByUserId
        +OperationStatus Status
        +DateOnly? DueDate
    }

    class ApprovalTask {
        +Guid TenantId
        +string TargetType
        +Guid TargetId
        +string StepCode
        +ApprovalStatus Status
        +string? DecisionNote
    }

    class AiInsight {
        +Guid TenantId
        +string ContextType
        +Guid? ContextId
        +string Question
        +string Summary
        +RiskLevel RiskLevel
    }

    class ApplicationDbContext {
        +DbSet~Tenant~ Tenants
        +DbSet~OrganizationUnit~ OrganizationUnits
        +DbSet~OperationRequest~ OperationRequests
        +DbSet~ApprovalTask~ ApprovalTasks
        +DbSet~AiInsight~ AiInsights
    }

    class OperationRequestService {
        +SearchAsync(query)
        +CreateAsync(input)
        +SubmitAsync(id)
        +CancelAsync(id, reason)
    }

    class ApprovalService {
        +GetMyPendingTasksAsync()
        +ApproveAsync(taskId, note)
        +RejectAsync(taskId, reason)
    }

    class AiDecisionService {
        +AnalyzeDashboardAsync(input)
        +AnalyzeOperationAsync(id, question)
    }

    BaseEntity <|-- Tenant
    BaseEntity <|-- OrganizationUnit
    BaseEntity <|-- OperationRequest
    BaseEntity <|-- ApprovalTask
    BaseEntity <|-- AiInsight
    Tenant "1" --> "*" OrganizationUnit
    Tenant "1" --> "*" AppUser
    OrganizationUnit "1" --> "*" AppUser
    AppUser "1" --> "*" OperationRequest
    OperationRequest "1" --> "*" ApprovalTask
    OperationRequest "1" --> "*" AiInsight
    ApplicationDbContext --> Tenant
    ApplicationDbContext --> OperationRequest
    OperationRequestService --> ApplicationDbContext
    ApprovalService --> ApplicationDbContext
    AiDecisionService --> ApplicationDbContext
```

## 5. Sequence Diagram

### 5.1. Đăng nhập

```mermaid
sequenceDiagram
    actor User as Người dùng
    participant UI as Login.cshtml
    participant Account as AccountController
    participant SignIn as SignInManager
    participant Permission as PermissionService
    participant DB as SQL Server

    User->>UI: Nhập email/password
    UI->>Account: POST /Account/Login
    Account->>SignIn: PasswordSignInAsync
    SignIn->>DB: Kiểm tra user/password
    DB-->>SignIn: Kết quả
    SignIn-->>Account: Success/Failed
    Account->>Permission: Nạp TenantId, role, permission
    Permission->>DB: Query quyền
    DB-->>Permission: Permission list
    Account-->>UI: Redirect Dashboard hoặc báo lỗi
```

### 5.2. Tạo và gửi yêu cầu vận hành

```mermaid
sequenceDiagram
    actor Staff as Staff
    participant View as Operations/Create.cshtml
    participant Controller as OperationsController
    participant Service as OperationRequestService
    participant Workflow as ApprovalService
    participant DB as SQL Server
    participant Audit as AuditLogger

    Staff->>View: Nhập yêu cầu
    View->>Controller: POST /Operations/Create
    Controller->>Service: CreateAsync(input)
    Service->>Service: Validate input + tenant scope
    Service->>DB: Insert OperationRequest
    Service->>Audit: Log Create
    Staff->>View: Bấm Gửi duyệt
    View->>Controller: POST /Operations/{id}/Submit
    Controller->>Service: SubmitAsync(id)
    Service->>Workflow: Create approval tasks
    Workflow->>DB: Insert ApprovalTask
    Service->>DB: Update status InReview
    Service->>Audit: Log Submit
    Controller-->>View: Redirect Details
```

### 5.3. AI phân tích dashboard

```mermaid
sequenceDiagram
    actor Executive as Executive
    participant UI as AiInsights.cshtml
    participant Controller as AiInsightsController
    participant Builder as BusinessContextBuilder
    participant Guard as AiPolicyGuard
    participant AI as AiDecisionService
    participant Provider as AI Provider
    participant DB as SQL Server

    Executive->>UI: Nhập câu hỏi
    UI->>Controller: POST /AiInsights/Ask
    Controller->>Builder: Build tenant-scoped context
    Builder->>DB: Query dashboard data
    DB-->>Builder: Context đã lọc quyền
    Controller->>Guard: Validate prompt/context
    Controller->>AI: AnalyzeDashboardAsync
    AI->>Provider: Send prompt
    Provider-->>AI: Summary + risks + recommendations
    AI->>DB: Save AiInsight
    Controller-->>UI: Trả kết quả
```

## 6. Activity Diagram

### 6.1. Quy trình duyệt yêu cầu

```mermaid
flowchart TB
    Start([Bắt đầu]) --> Draft["Tạo yêu cầu nháp"]
    Draft --> Validate["Validate dữ liệu"]
    Validate -->|Lỗi| Fix["Sửa dữ liệu"]
    Fix --> Validate
    Validate -->|Hợp lệ| Submit["Gửi duyệt"]
    Submit --> ManagerReview["Trưởng bộ phận duyệt"]
    ManagerReview -->|Từ chối| Reject["Trả về kèm lý do"]
    Reject --> Draft
    ManagerReview -->|Duyệt| NeedExec{"Cần lãnh đạo duyệt?"}
    NeedExec -->|Có| ExecReview["Lãnh đạo duyệt"]
    NeedExec -->|Không| Work["Thực hiện công việc"]
    ExecReview -->|Từ chối| Reject
    ExecReview -->|Duyệt| Work
    Work --> Complete["Hoàn thành"]
    Complete --> Report["Cập nhật báo cáo/KPI"]
    Report --> End([Kết thúc])
```

### 6.2. Quy trình export báo cáo

```mermaid
flowchart TB
    Start([Bắt đầu]) --> Filter["Chọn filter báo cáo"]
    Filter --> Permission["Kiểm tra quyền REPORTS_EXPORT"]
    Permission -->|Không có quyền| Deny["Hiển thị 403"]
    Permission -->|Có quyền| Validate["Validate khoảng ngày"]
    Validate -->|Không hợp lệ| Error["Hiển thị lỗi filter"]
    Validate -->|Hợp lệ| Query["Query dữ liệu theo TenantId"]
    Query --> Generate["Tạo file CSV/Excel/PDF"]
    Generate --> Audit["Ghi audit export"]
    Audit --> Download["Tải file"]
    Download --> End([Kết thúc])
```

## 7. Sitemap / Navigation Diagram

```mermaid
flowchart TB
    Login["/Account/Login"] --> Home["/Dashboard"]
    Home --> Admin["/Admin"]
    Admin --> Tenants["/Tenants"]
    Admin --> Users["/Users"]
    Admin --> Roles["/Roles"]
    Home --> Org["/OrganizationUnits"]
    Home --> Ops["/Operations"]
    Ops --> OpsCreate["/Operations/Create"]
    Ops --> OpsDetails["/Operations/Details/{id}"]
    OpsDetails --> OpsSubmit["/Operations/{id}/Submit"]
    Home --> Approvals["/Approvals/MyTasks"]
    Approvals --> ApprovalDetails["/Approvals/{id}"]
    Home --> Reports["/Reports/Dashboard"]
    Reports --> Export["/Reports/Export"]
    Home --> AI["/AiInsights"]
    Home --> Audit["/AuditLogs"]
```

## 8. Deployment Diagram

```mermaid
flowchart TB
    User["User Browser"] --> DNS["Domain / DNS"]
    DNS --> HTTPS["HTTPS 443"]
    HTTPS --> IIS["IIS Site + ASP.NET Core Hosting Bundle"]
    IIS --> Kestrel["ASP.NET Core App running on Kestrel"]

    subgraph Server["Windows Server / Hosting VM"]
        IIS
        Kestrel
        Logs["Application Logs"]
        Env["Environment Variables / .env"]
    end

    Kestrel --> SQL["SQL Server"]
    Kestrel --> AI["AI Provider API"]
    SQL --> Backup["Database Backup"]
    Kestrel --> Files["wwwroot / Uploads / Export Files"]
```

## 9. Source Code Structure

```text
Controllers/        MVC controllers, route/action handling
Data/               ApplicationDbContext, Code First configurations, migrations, seed
Models/             Code First entity, enum, validation models
Services/           Business logic, workflow, AI, export, audit
ViewModels/         Input/output models for Razor Views
Views/              .cshtml Razor pages grouped by controller
wwwroot/            Static files: CSS, JS, images, libraries
docs/               Project documents and diagrams
```

## 10. Controller/API Document mẫu

| Controller | Action | Method | Route | Input | Output |
| --- | --- | --- | --- | --- | --- |
| AccountController | Login | GET | `/Account/Login` | None | Login view |
| AccountController | Login | POST | `/Account/Login` | Email, Password | Redirect/Error |
| OperationsController | Index | GET | `/Operations` | Query filter | List view |
| OperationsController | Create | GET | `/Operations/Create` | None | Create view |
| OperationsController | Create | POST | `/Operations/Create` | OperationRequestCreateInput | Redirect/Error |
| OperationsController | Submit | POST | `/Operations/{id}/Submit` | Id | Redirect/Error |
| ApprovalsController | MyTasks | GET | `/Approvals/MyTasks` | None | Pending tasks view |
| ApprovalsController | Approve | POST | `/Approvals/{id}/Approve` | Note | Redirect/Error |
| AiInsightsController | Ask | POST | `/AiInsights/Ask` | Question, ContextType | JSON result |
| ReportsController | Export | GET/POST | `/Reports/Export` | Report filter | File |

## 11. Coding Convention

| Thành phần | Quy ước |
| --- | --- |
| Controller | Tên số nhiều theo module, hậu tố `Controller`, ví dụ `OperationsController` |
| Action | Động từ rõ nghĩa: `Index`, `Details`, `Create`, `Edit`, `Submit`, `Approve` |
| Entity | Danh từ số ít: `OperationRequest`, `ApprovalTask` |
| ViewModel | Hậu tố theo mục đích: `CreateInput`, `UpdateInput`, `ListItem`, `DetailsViewModel` |
| Service interface | Bắt đầu bằng `I`, ví dụ `IOperationRequestService` |
| Service implementation | Bỏ chữ `I`, ví dụ `OperationRequestService` |
| Permission | Viết hoa snake case: `OPERATIONS_EDIT`, `REPORTS_EXPORT` |
| Async method | Hậu tố `Async` và nhận `CancellationToken` ở service |
| Validation | Validate ở cả ViewModel attribute và service rule |

## 12. Ghi chú công nghệ ASP.NET Core MVC

- `Model`: đại diện dữ liệu/nghiệp vụ.
- `View`: file `.cshtml` dùng Razor để render HTML.
- `Controller`: nhận request, gọi service, trả View/Redirect/JSON.
- `DbContext`: làm việc với SQL Server qua EF Core Code First; `DbSet`, Fluent API mapping, migration snapshot là nguồn tạo schema.
- `Service`: xử lý business logic, validation nghiệp vụ, workflow, AI.
- `Repository`: chỉ dùng nếu dự án cần tách truy vấn phức tạp; nếu không, service có thể dùng `DbContext` trực tiếp nhưng vẫn phải tránh nhồi logic vào controller.

Tham khảo chính thức:

- Microsoft Learn: [Overview of ASP.NET Core MVC](https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-10.0)
- Microsoft Learn: [Host and deploy ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/?view=aspnetcore-10.0)
