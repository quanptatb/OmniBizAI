# 🏗️ OmniBiz AI — Technical Design Document / System Design

> **Version**: 1.0 | **Updated**: 2026-04-24

---

## 1. Architecture Overview

### 1.1 Architecture Style: Clean Architecture + Modular Monolith

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                               │
│  ┌──────────────────────────┐  ┌──────────────────────────────────┐     │
│  │   Next.js Frontend       │  │   ASP.NET Core MVC (Server-Side)│     │
│  │   React + Tailwind CSS   │  │   Razor Views (Admin pages)     │     │
│  │   Recharts / ECharts     │  │   SignalR Hub                   │     │
│  └──────────┬───────────────┘  └──────────────┬──────────────────┘     │
│             │ REST API / SignalR                │                        │
├─────────────┴──────────────────────────────────┴────────────────────────┤
│                        APPLICATION LAYER                                │
│  ┌────────────────────────────────────────────────────────────────┐     │
│  │  ASP.NET Core Web API                                          │     │
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
| **Frontend** | Next.js + React | 14.x | SSR, routing, performance |
| **UI Library** | Tailwind CSS | 3.x | Rapid UI development |
| **Charts** | Recharts / ECharts | Latest | Rich visualization |
| **Backend** | ASP.NET Core Web API | .NET 10 | Enterprise-grade, team expertise |
| **ORM** | Entity Framework Core | 10.x | Code-first, migrations |
| **Database** | SQL Server 2022 | 2022 | Enterprise-grade, EF Core native support, JSON support |
| **Cache** | Redis | 7.x | Session, caching, rate limiting |
| **Realtime** | SignalR | Built-in | WebSocket notifications |
| **AI** | Groq / OpenAI API | Latest | LLM inference |
| **Vector DB** | Custom vector table (SQL Server) | - | RAG embeddings via cosine similarity |
| **File Storage** | Local Disk → Azure Blob | - | Simple → scalable |
| **Auth** | JWT + ASP.NET Identity | Built-in | Standard, secure |
| **Containerization** | Docker + Docker Compose | Latest | Dev parity, deployment |
| **CI/CD** | GitHub Actions | - | Free for public repos |

---

## 2. Backend Architecture Detail

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
│   │   ├── Identity/                 # JWT, Identity config
│   │   ├── Services/
│   │   │   ├── AIService.cs          # LLM API integration
│   │   │   ├── FileStorageService.cs
│   │   │   ├── EmailService.cs
│   │   │   ├── CacheService.cs
│   │   │   └── NotificationService.cs
│   │   └── Repositories/            # Generic + specific repos
│   │
│   └── OmniBizAI.WebAPI/            # Entry point
│       ├── Controllers/
│       │   ├── AuthController.cs
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
│       ├── Hubs/
│       │   └── NotificationHub.cs
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

## 3. Frontend Architecture Detail

### 3.1 Project Structure

```
frontend/
├── src/
│   ├── app/                          # Next.js App Router
│   │   ├── (auth)/                   # Auth layout group
│   │   │   ├── login/
│   │   │   └── forgot-password/
│   │   ├── (dashboard)/              # Main layout group
│   │   │   ├── layout.tsx            # Sidebar + Header + Notification
│   │   │   ├── page.tsx              # Dashboard home
│   │   │   ├── finance/
│   │   │   │   ├── budgets/
│   │   │   │   ├── payment-requests/
│   │   │   │   ├── transactions/
│   │   │   │   └── vendors/
│   │   │   ├── performance/
│   │   │   │   ├── objectives/
│   │   │   │   ├── kpis/
│   │   │   │   ├── check-ins/
│   │   │   │   └── evaluations/
│   │   │   ├── workflow/
│   │   │   │   ├── templates/
│   │   │   │   ├── approvals/
│   │   │   │   └── audit-log/
│   │   │   ├── organization/
│   │   │   │   ├── departments/
│   │   │   │   ├── employees/
│   │   │   │   └── positions/
│   │   │   ├── ai/
│   │   │   │   ├── copilot/
│   │   │   │   └── reports/
│   │   │   └── settings/
│   │   └── api/                      # API routes (BFF)
│   ├── components/
│   │   ├── ui/                       # Design system components
│   │   ├── charts/                   # Chart components
│   │   ├── forms/                    # Form components
│   │   ├── layout/                   # Sidebar, Header, etc.
│   │   └── features/                # Feature-specific components
│   ├── hooks/                        # Custom hooks
│   ├── lib/                          # Utilities, API client
│   ├── stores/                       # Zustand stores
│   └── types/                        # TypeScript types
├── public/
├── tailwind.config.ts
├── next.config.ts
└── package.json
```

### 3.2 State Management
- **Server State**: React Query (TanStack Query) — API data caching, refetching
- **Client State**: Zustand — UI state, user preferences, notification count
- **Form State**: React Hook Form + Zod validation

---

## 4. Database Design Overview

### 4.1 Database: SQL Server 2022 extension
- **Encoding**: UTF-8
- **Timezone**: UTC (convert to local on frontend)
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
- JWT Bearer Token in `Authorization` header
- Refresh Token in HttpOnly cookie

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
