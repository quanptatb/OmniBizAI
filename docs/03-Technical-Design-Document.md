# 🏗️ OmniBiz AI — Technical Design Document / System Design

> **Version**: 1.0 | **Updated**: 2026-04-25

---

## 1. Architecture Overview

### 1.1 Architecture Style: Clean Architecture + Modular Monolith

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                               │
│  ┌────────────────────────────────────────────────────────────────┐     │
│  │  ASP.NET Core MVC (.NET 10)                                    │     │
│  │  Razor Views + Layouts + Partial Views + ViewComponents        │     │
│  │  Tag Helpers + Bootstrap/custom CSS + Chart.js/ECharts         │     │
│  │  SignalR Hub + JSON endpoints for AJAX/mobile integrations     │     │
│  └──────────────────────────────┬─────────────────────────────────┘     │
├─────────────────────────────────┴───────────────────────────────────────┤
│                        APPLICATION LAYER                                │
│  ┌────────────────────────────────────────────────────────────────┐     │
│  │  Application Services / CQRS Handlers                          │     │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌────────────────┐   │     │
│  │  │ Finance  │ │ KPI/OKR  │ │ Workflow │ │ AI Copilot     │   │     │
│  │  │ Service  │ │ Service  │ │ Service  │ │ Service        │   │     │
│  │  └──────────┘ └──────────┘ └──────────┘ └────────────────┘   │     │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌────────────────┐   │     │
│  │  │ Auth     │ │ HR       │ │ Notif    │ │ Report         │   │     │
│  │  │ Service  │ │ Service  │ │ Service  │ │ Service        │   │     │
│  │  └──────────┘ └──────────┘ └──────────┘ └────────────────┘   │     │
│  └────────────────────────────────────────────────────────────────┘     │
├─────────────────────────────────────────────────────────────────────────┤
│                        DOMAIN LAYER                                     │
│  Entities, Value Objects, Domain Events, Interfaces, Business Rules     │
├─────────────────────────────────────────────────────────────────────────┤
│                        INFRASTRUCTURE LAYER                             │
│  ┌────────────┐ ┌──────────┐ ┌─────────┐ ┌──────────┐ ┌────────────┐  │
│  │ EF Core    │ │ Redis    │ │ LLM API │ │ File     │ │ Email      │  │
│  │ SQL Server │ │ Cache    │ │ (Groq)  │ │ Storage  │ │ Service    │  │
│  └────────────┘ └──────────┘ └─────────┘ └──────────┘ └────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Technology Stack

| Layer | Technology | Version | Lý do chọn |
|-------|-----------|---------|-----------|
| **Web App** | ASP.NET Core MVC + Razor Views | .NET 10 | Full-stack C#, server-side rendering, triển khai đơn giản |
| **UI Library** | Bootstrap 5 + custom CSS + Tag Helpers | 5.x | UI nhất quán, dễ tích hợp Razor |
| **Charts** | Chart.js / ECharts | Latest | Rich visualization không phụ thuộc SPA framework |
| **Backend** | ASP.NET Core MVC Controllers + API Controllers | .NET 10 | Enterprise-grade, team expertise |
| **ORM** | Entity Framework Core | 10.x | Code-first, migrations |
| **Database** | SQL Server 2022 | 2022 | Enterprise-grade, EF Core native support, JSON support |
| **Cache** | Redis | 7.x | Session, caching, rate limiting |
| **Realtime** | SignalR | Built-in | WebSocket notifications |
| **AI** | Groq / OpenAI API | Latest | LLM inference |
| **Vector DB** | Custom vector table (SQL Server) | - | RAG embeddings via cosine similarity |
| **File Storage** | Local Disk → Azure Blob | - | Simple → scalable |
| **Auth** | ASP.NET Identity + Cookie Auth, optional JWT for external API clients | Built-in | Secure MVC sessions, extensible API access |
| **Containerization** | Docker + Docker Compose | Latest | Dev parity, deployment |
| **CI/CD** | GitHub Actions | - | Free for public repos |

---

## 2. Web Application Architecture Detail

### 2.1 Project Structure (Clean Architecture)

```
OmniBizAI/
├── src/
│   ├── OmniBizAI.Domain/              # Entities, Enums, Interfaces, Events
│   │   ├── Entities/
│   │   │   ├── Common/                 # BaseEntity, AuditableEntity
│   │   │   ├── Identity/              # User, Role, Permission
│   │   │   ├── Organization/          # Department, Position, Employee
│   │   │   ├── Finance/              # Budget, PaymentRequest, Transaction, Vendor, Wallet, Category
│   │   │   ├── Performance/          # Objective, KeyResult, KPI, CheckIn, Evaluation
│   │   │   ├── Workflow/             # WorkflowTemplate, WorkflowStep, WorkflowInstance, ApprovalAction
│   │   │   ├── AI/                   # AIChatSession, AIMessage, AIGenerationHistory
│   │   │   └── Notification/         # Notification, NotificationSetting
│   │   ├── Enums/
│   │   ├── Interfaces/               # IRepository<T>, IUnitOfWork, IAIService, etc.
│   │   └── Events/                   # DomainEvent base, specific events
│   │
│   ├── OmniBizAI.Application/        # Use Cases, DTOs, Validators
│   │   ├── Common/                   # Behaviors (Validation, Logging, Caching)
│   │   ├── DTOs/
│   │   ├── Interfaces/              # IApplicationDbContext, external service interfaces
│   │   ├── Mappings/                # AutoMapper profiles
│   │   └── Features/
│   │       ├── Auth/                # Login, Register, RefreshToken
│   │       ├── Dashboard/           # GetDashboardData
│   │       ├── Finance/             # Budget, PaymentRequest, Transaction CQRS
│   │       ├── Performance/         # OKR, KPI, CheckIn CQRS
│   │       ├── Workflow/            # Template, Instance, Approval CQRS
│   │       ├── AI/                  # Chat, RiskAnalysis, Report, Insight
│   │       ├── Organization/        # Department, Employee, Position
│   │       ├── Notification/        # Send, MarkRead, GetList
│   │       └── Report/              # Generate, Export
│   │
│   ├── OmniBizAI.Infrastructure/     # External concerns
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/       # EF Fluent API configs per entity
│   │   │   ├── Migrations/
│   │   │   └── Seeders/             # SeedDataService
│   │   ├── Identity/                 # ASP.NET Identity, cookie/JWT config
│   │   ├── Services/
│   │   │   ├── AIService.cs          # LLM API integration
│   │   │   ├── FileStorageService.cs
│   │   │   ├── EmailService.cs
│   │   │   ├── CacheService.cs
│   │   │   └── NotificationService.cs
│   │   └── Repositories/            # Generic + specific repos
│   │
│   └── OmniBizAI.Web/               # ASP.NET Core MVC entry point
│       ├── Controllers/
│       │   ├── AccountController.cs
│       │   ├── DashboardController.cs
│       │   ├── BudgetController.cs
│       │   ├── PaymentRequestController.cs
│       │   ├── TransactionController.cs
│       │   ├── ObjectiveController.cs
│       │   ├── KpiController.cs
│       │   ├── CheckInController.cs
│       │   ├── WorkflowController.cs
│       │   ├── AIController.cs
│       │   ├── DepartmentController.cs
│       │   ├── EmployeeController.cs
│       │   ├── NotificationController.cs
│       │   └── ReportController.cs
│       ├── ApiControllers/          # JSON endpoints under /api/v1
│       ├── Views/
│       │   ├── Shared/              # _Layout, partial views, validation summary
│       │   ├── Account/
│       │   ├── Dashboard/
│       │   ├── Finance/
│       │   ├── Performance/
│       │   ├── Workflow/
│       │   ├── Organization/
│       │   ├── AI/
│       │   └── Settings/
│       ├── ViewModels/              # Page-specific input/output models
│       ├── ViewComponents/          # Sidebar, notifications, KPI cards
│       ├── TagHelpers/              # Reusable Razor helpers
│       ├── Areas/
│       │   └── Admin/               # Admin screens
│       ├── Hubs/
│       │   └── NotificationHub.cs
│       ├── wwwroot/
│       │   ├── css/
│       │   ├── js/
│       │   ├── lib/
│       │   └── uploads/
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   ├── RequestLoggingMiddleware.cs
│       │   └── RateLimitingMiddleware.cs
│       ├── Filters/
│       │   ├── PermissionAuthorizationFilter.cs
│       │   └── AuditLogFilter.cs
│       └── Program.cs
│
├── tests/
│   ├── OmniBizAI.UnitTests/
│   ├── OmniBizAI.IntegrationTests/
│   └── OmniBizAI.E2ETests/
│
├── docker-compose.yml
├── Dockerfile
└── README.md
```

### 2.2 Key Design Patterns

| Pattern | Usage |
|---------|-------|
| **Repository Pattern** | Data access abstraction |
| **Unit of Work** | Transaction management |
| **CQRS (Light)** | Separate Command/Query handlers |
| **Mediator** | MediatR for request handling |
| **Strategy** | AI provider switching |
| **Observer** | Domain Events → Notifications |
| **Builder** | Workflow instance creation |
| **Specification** | Complex query filtering |

### 2.3 Middleware Pipeline

```
Request → RateLimiting → ExceptionHandling → RequestLogging → Authentication 
→ Authorization → AuditLog → Controller → Service → Repository → Database
```

---

## 3. MVC Presentation Architecture Detail

### 3.1 Project Structure

```
src/OmniBizAI.Web/
├── Controllers/
│   ├── AccountController.cs
│   ├── DashboardController.cs
│   ├── Finance/
│   ├── Performance/
│   ├── Workflow/
│   ├── Organization/
│   └── AI/
├── ApiControllers/                  # JSON APIs for AJAX, charts, external clients
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _ValidationScriptsPartial.cshtml
│   │   └── Components/
│   ├── Account/
│   ├── Dashboard/
│   ├── Finance/
│   ├── Performance/
│   ├── Workflow/
│   ├── Organization/
│   └── AI/
├── ViewModels/
├── ViewComponents/
├── TagHelpers/
├── Hubs/
└── wwwroot/
    ├── css/
    ├── js/
    ├── lib/
    └── uploads/
```

### 3.2 UI State Management
- **Page State**: Strongly typed ViewModels returned from MVC controllers
- **Form State**: Razor forms + ModelState + FluentValidation / DataAnnotations
- **Interactive State**: Vanilla JavaScript modules for filters, modals, file upload, chart refresh
- **Realtime State**: SignalR client updates notifications, dashboard alerts, approval queues
- **Session State**: ASP.NET Identity cookie + Redis-backed distributed cache where needed

---

## 4. Database Design Overview

### 4.1 Database: SQL Server 2022 extension
- **Encoding**: UTF-8
- **Timezone**: UTC (convert to local in Razor views / JavaScript)
- **Naming Convention**: snake_case cho tables/columns

### 4.2 Key Design Decisions
- Soft Delete: `IsDeleted` flag + `DeletedAt` timestamp trên tất cả main entities
- Auditable: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` trên tất cả entities
- Multi-currency: Lưu `Currency` field, default VND
- JSON columns: Dùng `nvarchar(max)` với `OPENJSON` / `JSON_VALUE` cho flexible metadata
- Collation: `Vietnamese_CI_AS` cho hỗ trợ tiếng Việt

---

## 5. API Design Overview

### 5.1 API Convention
- Base URL: `/api/v1/`
- RESTful naming: plural nouns, kebab-case
- Pagination: `?page=1&pageSize=20`
- Filtering: `?status=active&departmentId=xxx`
- Sorting: `?sortBy=createdAt&sortOrder=desc`
- Response format: `{ success, data, message, errors, pagination }`

### 5.2 Authentication
- MVC pages use ASP.NET Identity cookie authentication
- JSON API endpoints use the same cookie for first-party AJAX and optional JWT Bearer tokens for external clients

---

## 6. AI Architecture

### 6.1 AI Service Design

```
User Query → Intent Detection → Data Retrieval (RAG) → Prompt Building → LLM API → Response Parsing → Render
                                      │
                                      ▼
                              ┌──────────────┐
                              │ vector search (SQL Server) DB  │
                              │ (Embeddings) │
                              └──────────────┘
```

### 6.2 RAG Pipeline
1. **Indexing**: Nightly job embed business data (budgets, KPIs, transactions) vào vector search (SQL Server)
2. **Retrieval**: Khi user hỏi → embed query → similarity search → top-K relevant documents
3. **Augmentation**: Combine retrieved data + user context + system prompt
4. **Generation**: LLM API call với augmented prompt
5. **Post-processing**: Extract citations, format numbers, render charts

### 6.3 AI Provider Abstraction

```csharp
public interface IAIProvider
{
    Task<AIResponse> ChatCompletionAsync(AIRequest request);
    Task<float[]> GetEmbeddingAsync(string text);
}

// Implementations: GroqProvider, OpenAIProvider, ClaudeProvider
// Strategy pattern: switch provider via config
```

---

## 7. Caching Strategy

| Data | Cache Type | TTL | Invalidation |
|------|-----------|-----|-------------|
| User session/permissions | Redis | 60 min | On role change |
| Dashboard aggregations | Redis | 5 min | On new transaction |
| Department list | Redis | 30 min | On CRUD |
| Budget remaining | Redis | 1 min | On transaction |
| AI embeddings | vector search (SQL Server) | Nightly rebuild | Scheduled job |

---

## 8. Third-party Integrations

| Service | Purpose | Integration Method |
|---------|---------|-------------------|
| Groq API | LLM inference (primary) | REST API |
| OpenAI API | Fallback LLM | REST API |
| SendGrid / SMTP | Email notifications | SMTP / API |
| Google OAuth | Social login | OAuth 2.0 |
| Azure Blob Storage | File storage (production) | SDK |
