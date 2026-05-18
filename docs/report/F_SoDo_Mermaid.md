# TỔNG HỢP SƠ ĐỒ MERMAID — HỆ THỐNG OMNIBIZAI

> Phiên bản: 1.0 | Ngày: 18/05/2026

---

## 1. Use Case Diagram — Tổng quan hệ thống

```mermaid
graph LR
    subgraph Actors
        SA["System Admin"]
        TA["Tenant Admin"]
        EX["Ban Giám đốc"]
        DM["Trưởng phòng"]
        ST["Nhân viên"]
        AC["Kế toán"]
        AU["Kiểm toán"]
    end

    subgraph UC_VanHanh["Vận hành"]
        UC1["Tạo yêu cầu vận hành"]
        UC2["Kanban Workflow"]
        UC3["Phê duyệt"]
    end

    subgraph UC_TaiChinh["Tài chính"]
        UC4["Quản lý ngân sách"]
        UC5["Quản lý chi phí"]
        UC6["Sổ thu chi"]
        UC7["Đề nghị thanh toán"]
    end

    subgraph UC_MuaSam["Mua sắm & Kho"]
        UC8["Đề xuất mua sắm"]
        UC9["Đơn mua hàng"]
        UC10["Nhập kho"]
        UC11["Xuất kho"]
        UC12["Cảnh báo tồn kho"]
    end

    subgraph UC_NhanSu["Nhân sự"]
        UC13["Quản lý phòng ban"]
        UC14["Quản lý nhân viên"]
        UC15["Nghỉ phép"]
    end

    subgraph UC_CRM["CRM"]
        UC16["Quản lý khách hàng"]
        UC17["Quản lý NCC"]
        UC18["Sản phẩm & DV"]
    end

    subgraph UC_KPI["KPI / OKR"]
        UC19["Thiết lập KPI"]
        UC20["OKR"]
        UC21["Check-In"]
        UC22["Đánh giá"]
    end

    subgraph UC_HeThong["Hệ thống"]
        UC23["Cài đặt hệ thống"]
        UC24["Sao lưu"]
        UC25["Nhật ký Audit"]
        UC26["AI Copilot"]
        UC27["Báo cáo"]
    end

    ST --> UC1 & UC2 & UC5 & UC15 & UC21
    DM --> UC1 & UC2 & UC3 & UC5 & UC8 & UC19 & UC22
    EX --> UC3 & UC4 & UC27 & UC26
    AC --> UC4 & UC5 & UC6 & UC7
    TA --> UC13 & UC14 & UC23 & UC24
    SA --> UC23 & UC24 & UC25
    AU --> UC25
```

---

## 2. Use Case Diagram — Phân hệ Vận hành

```mermaid
graph TD
    NV["Nhân viên"] --> UC1["Tạo yêu cầu vận hành"]
    NV --> UC2["Xem danh sách yêu cầu"]
    NV --> UC3["Cập nhật yêu cầu"]
    NV --> UC4["Hủy yêu cầu"]
    NV --> UC5["Kéo thả Kanban"]
    NV --> UC6["Thêm comment"]

    TP["Trưởng phòng"] --> UC7["Phê duyệt yêu cầu"]
    TP --> UC8["Từ chối yêu cầu"]
    TP --> UC9["Gán WorkItem"]

    GD["Ban GĐ"] --> UC10["Phê duyệt cấp cao"]
```

---

## 3. Activity Diagram — Luồng Yêu cầu vận hành

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Nhân viên tạo yêu cầu]
    B --> C{Gửi phê duyệt?}
    C -- Không --> D[Lưu nháp]
    C -- Có --> E[Gửi lên Trưởng phòng]
    E --> F{Trưởng phòng xét duyệt}
    F -- Từ chối --> G[Trả về nhân viên + lý do]
    G --> B
    F -- Duyệt --> H[Tạo WorkItems]
    H --> I[Phân công trên Kanban]
    I --> J[Thực hiện công việc]
    J --> K{Hoàn thành?}
    K -- Chưa --> J
    K -- Xong --> L[Cập nhật trạng thái Done]
    L --> M([Kết thúc])
```

---

## 4. Activity Diagram — Luồng Mua sắm

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Tạo Đề xuất mua sắm]
    B --> C[Thêm dòng sản phẩm]
    C --> D{Gửi phê duyệt?}
    D -- Không --> E[Lưu nháp]
    D -- Có --> F[Trưởng phòng duyệt]
    F --> G{Duyệt?}
    G -- Từ chối --> H[Trả về + lý do]
    G -- Duyệt --> I[Tạo Đơn mua hàng PO]
    I --> J[Gửi NCC]
    J --> K[Nhận hàng - Goods Receipt]
    K --> L[Kiểm tra chất lượng]
    L --> M[Xác nhận nhập kho]
    M --> N[Cập nhật tồn kho]
    N --> O([Kết thúc])
```

---

## 5. Activity Diagram — Luồng KPI Check-In

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin thiết lập KPI]
    B --> C[Gán KPI cho phòng ban/nhân viên]
    C --> D[Nhân viên thực hiện Check-In]
    D --> E[Nhập giá trị thực tế + ghi chú]
    E --> F{Đạt mục tiêu?}
    F -- Đạt --> G[Trạng thái: On Track]
    F -- Chưa đạt --> H[Trạng thái: At Risk / Behind]
    G --> I[Cuối kỳ: Đánh giá tổng hợp]
    H --> I
    I --> J[Xếp hạng nhân viên]
    J --> K([Kết thúc])
```

---

## 6. ERD Tổng quan

```mermaid
erDiagram
    Tenant ||--o{ AppUser : has
    Tenant ||--o{ OrganizationUnit : has
    Tenant ||--o{ Customer : has
    Tenant ||--o{ Vendor : has

    OrganizationUnit ||--o{ AppUser : belongs
    OrganizationUnit ||--o{ OrganizationUnit : parent

    AppUser ||--o{ OperationRequest : creates
    AppUser ||--o{ WorkItem : assigned
    AppUser ||--o{ ApprovalTask : approves
    AppUser ||--o{ LeaveRequest : requests

    OperationRequest ||--o{ OperationRequestLine : has
    OperationRequest ||--o{ WorkItem : generates
    WorkItem ||--o{ WorkItemComment : has
    WorkItem ||--o{ WorkItemChecklist : has

    Customer ||--o{ CustomerContact : has
    Customer ||--o{ SalesOpportunity : has
    Customer ||--o{ CrmInteraction : has

    ProcurementRequest ||--o{ ProcurementRequestLine : has
    ProcurementRequest ||--o{ PurchaseOrder : generates
    PurchaseOrder ||--o{ PurchaseOrderLine : has
    PurchaseOrder ||--o{ GoodsReceipt : receives

    KpiDefinition ||--o{ KpiTarget : has
    KpiDefinition ||--o{ KpiCheckIn : tracks
    OkrObjective ||--o{ OkrKeyResult : has

    Budget ||--o{ Expense : tracks
```

---

## 7. ERD — Phân hệ Tài chính & Mua sắm

```mermaid
erDiagram
    ProcurementRequest {
        Guid Id PK
        string RequestNo
        string Title
        Guid RequestedByUserId FK
        int Status
    }
    ProcurementRequestLine {
        Guid Id PK
        Guid ProcurementRequestId FK
        Guid ProductServiceId FK
        decimal Quantity
        decimal EstimatedUnitPrice
    }
    PurchaseOrder {
        Guid Id PK
        string OrderNo
        Guid VendorId FK
        int Status
        decimal TotalAmount
    }
    GoodsReceipt {
        Guid Id PK
        string ReceiptNo
        Guid PurchaseOrderId FK
        DateOnly ReceiptDate
    }
    CashTransaction {
        Guid Id PK
        string TransactionNo
        string TransactionType
        decimal Amount
    }

    ProcurementRequest ||--o{ ProcurementRequestLine : has
    ProcurementRequest ||--o{ PurchaseOrder : generates
    PurchaseOrder ||--o{ GoodsReceipt : receives
    ProcurementRequestLine }o--|| ProductService : references
```

---

## 8. Sequence Diagram — Đăng nhập

```mermaid
sequenceDiagram
    actor User
    participant Browser
    participant AccountController
    participant SignInManager
    participant DB as Database

    User->>Browser: Nhập email + password
    Browser->>AccountController: POST /Account/Login
    AccountController->>SignInManager: PasswordSignInAsync()
    SignInManager->>DB: Verify credentials
    DB-->>SignInManager: User found + hash match
    SignInManager-->>AccountController: Success
    AccountController-->>Browser: Redirect /Dashboard
    Browser-->>User: Hiển thị Dashboard
```

---

## 9. Sequence Diagram — Tạo yêu cầu vận hành

```mermaid
sequenceDiagram
    actor NV as Nhân viên
    participant Ctrl as OperationsController
    participant Svc as OperationRequestService
    participant DB as Database
    participant Notif as NotificationService

    NV->>Ctrl: POST /Operations/Create (form data)
    Ctrl->>Svc: CreateAsync(model)
    Svc->>DB: INSERT OperationRequest
    Svc->>DB: INSERT OperationRequestLines
    DB-->>Svc: OK
    Svc->>Notif: SendAsync(to: Manager)
    Notif->>DB: INSERT Notification
    Svc-->>Ctrl: Success
    Ctrl-->>NV: Redirect to Index + Toast
```

---

## 10. Sequence Diagram — Phê duyệt

```mermaid
sequenceDiagram
    actor TP as Trưởng phòng
    participant Ctrl as ApprovalsController
    participant Svc as ApprovalService
    participant DB as Database
    participant Notif as NotificationService

    TP->>Ctrl: POST /Approvals/Process (Approve)
    Ctrl->>Svc: ProcessAsync(taskId, Approve)
    Svc->>DB: UPDATE ApprovalTask SET Status=Approved
    Svc->>DB: UPDATE OperationRequest SET Status=Approved
    Svc->>DB: INSERT WorkItems (auto-generate)
    Svc->>Notif: SendAsync(to: Requester)
    Svc-->>Ctrl: Success
    Ctrl-->>TP: Redirect + Toast "Đã duyệt"
```

---

## 11. Component Diagram

```mermaid
graph TB
    subgraph Client["Client Layer"]
        BR["Browser"]
        CSS["site.css + kpi-okr.css"]
        JS["site.js"]
    end

    subgraph Server["ASP.NET Core MVC 10"]
        CTRL["Controllers (22)"]
        SVC["Services (12)"]
        MW["Middleware: Auth, Static Files"]
    end

    subgraph Data["Data Layer"]
        EF["EF Core 10"]
        CTX["ApplicationDbContext"]
        ENT["Entity Models (95)"]
    end

    subgraph External["External"]
        SQL["SQL Server"]
        GEMINI["Google Gemini API"]
        EMAIL["SMTP Email"]
    end

    BR --> MW --> CTRL --> SVC --> EF --> SQL
    SVC --> GEMINI
    SVC --> EMAIL
```

---

## 12. Deployment Diagram

```mermaid
graph LR
    subgraph DevMachine["Developer Machine"]
        VS["Visual Studio / VS Code"]
        SDK[".NET 10 SDK"]
        SSMS["SQL Server Management Studio"]
    end

    subgraph Server["Production Server"]
        IIS["IIS / Kestrel"]
        APP["OmniBizAI.dll"]
        SQLSRV["SQL Server 2022"]
    end

    subgraph Cloud["Cloud Services"]
        GEM["Google Gemini API"]
    end

    DevMachine -->|"dotnet publish"| Server
    APP --> SQLSRV
    APP --> GEM
```

---

## 13. Package Diagram

```mermaid
graph TD
    subgraph Presentation
        Views["Views (Razor)"]
        WWW["wwwroot (CSS/JS)"]
    end

    subgraph Application
        Controllers["Controllers"]
        ViewModels["ViewModels"]
    end

    subgraph Domain
        Services["Services"]
        Entities["Models/Entities"]
        Enums["Models/Entities/Enums"]
    end

    subgraph Infrastructure
        DbContext["Data/ApplicationDbContext"]
        Configs["Data/Configurations"]
        Seed["Data/Seed"]
        Helpers["Helpers"]
    end

    Views --> Controllers
    Controllers --> Services
    Controllers --> ViewModels
    Services --> DbContext
    DbContext --> Entities
    Entities --> Enums
```

---

## 14. Sitemap / Sơ đồ giao diện

```mermaid
graph TD
    LOGIN["Đăng nhập"] --> DASH["Dashboard"]

    DASH --> VH["Vận hành"]
    VH --> VH1["Yêu cầu vận hành"]
    VH --> VH2["Kanban Workflow"]
    VH --> VH3["Phê duyệt"]

    DASH --> TC["Tài chính"]
    TC --> TC1["Tổng quan TC"]
    TC --> TC2["Ngân sách"]
    TC --> TC3["Chi phí"]
    TC --> TC4["Sổ thu chi"]
    TC --> TC5["Đề nghị thanh toán"]
    TC --> TC6["Mua sắm"]
    TC --> TC7["Đơn mua hàng"]
    TC --> TC8["Nhập kho"]
    TC --> TC9["Xuất kho"]
    TC --> TC10["Tồn kho"]

    DASH --> NS["Nhân sự"]
    NS --> NS1["Phòng ban"]
    NS --> NS2["Nhân viên"]
    NS --> NS3["Chức vụ"]
    NS --> NS4["Nghỉ phép"]

    DASH --> CRM["CRM"]
    CRM --> CRM1["Khách hàng"]
    CRM --> CRM2["NCC"]
    CRM --> CRM3["Sản phẩm"]

    DASH --> KPI["KPI/OKR"]
    KPI --> KPI1["OKR"]
    KPI --> KPI2["KPI Setup"]
    KPI --> KPI3["Check-In"]
    KPI --> KPI4["Đánh giá"]

    DASH --> SYS["Hệ thống"]
    SYS --> SYS1["Báo cáo"]
    SYS --> SYS2["AI Copilot"]
    SYS --> SYS3["Cài đặt"]
    SYS --> SYS4["Sao lưu"]
    SYS --> SYS5["Audit Log"]
```

---

## 15. Role-Permission Matrix

```mermaid
graph LR
    subgraph Roles
        R1["SYSTEM_ADMIN"]
        R2["TENANT_ADMIN"]
        R3["EXECUTIVE"]
        R4["DEPT_MANAGER"]
        R5["STAFF"]
        R6["ACCOUNTANT"]
        R7["AUDITOR"]
    end

    subgraph Permissions
        P1["Dashboard"]
        P2["Operations CRUD"]
        P3["Approval"]
        P4["Finance CRUD"]
        P5["HR Management"]
        P6["KPI/OKR"]
        P7["Settings"]
        P8["Backup"]
        P9["Audit Log"]
        P10["AI Copilot"]
    end

    R1 --> P1 & P2 & P3 & P4 & P5 & P6 & P7 & P8 & P9 & P10
    R2 --> P1 & P2 & P3 & P4 & P5 & P6 & P7 & P8
    R3 --> P1 & P3 & P4 & P6 & P10
    R4 --> P1 & P2 & P3 & P4 & P6
    R5 --> P1 & P2 & P4 & P6
    R6 --> P1 & P4
    R7 --> P1 & P9
```
