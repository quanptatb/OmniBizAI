# 📏 OmniBiz AI — Development Guidelines

> **Version**: 1.0 | **Updated**: 2026-04-24

---

## 1. Coding Convention

### 1.1 C# / ASP.NET Core MVC 10

| Item | Convention | Example |
|------|-----------|---------|
| Namespace | PascalCase, match folder | `OmniBizAI.Application.Features.Finance` |
| Class | PascalCase, noun | `PaymentRequestService` |
| Interface | I + PascalCase | `IPaymentRequestService` |
| Method | PascalCase, verb | `CreatePaymentRequestAsync()` |
| Property | PascalCase | `TotalAmount` |
| Private field | _camelCase | `_paymentRequestRepository` |
| Constant | PascalCase | `MaxFileSize` |
| Enum | PascalCase | `PaymentStatus.Approved` |
| Async method | suffix `Async` | `GetBudgetByIdAsync()` |
| Controller route | kebab-case | `[Route("payment-requests")]` |

### 1.2 Razor / MVC UI

| Item | Convention | Example |
|------|-----------|---------|
| Razor View | Match action name | `Views/Finance/PaymentRequests.cshtml` |
| Partial View | `_` prefix | `_PaymentRequestForm.cshtml` |
| ViewModel | PascalCase + `ViewModel` suffix | `PaymentRequestFormViewModel` |
| ViewComponent | PascalCase + `ViewComponent` suffix | `BudgetSummaryViewComponent` |
| Tag Helper | PascalCase class, kebab-case tag | `<status-badge>` |
| JavaScript module | camelCase | `paymentRequestForm.js` |
| Constant | UPPER_SNAKE_CASE | `MAX_FILE_SIZE` |
| CSS class | kebab-case | `payment-request-form` |
| Event handler | prefix `handle/on` | `handleSubmit`, `onClick` |
| Boolean property | prefix `Is/Has/Can` | `IsLoading`, `HasError` |

### 1.3 Database

| Item | Convention | Example |
|------|-----------|---------|
| Table | snake_case, plural | `payment_requests` |
| Column | snake_case | `total_amount` |
| Primary Key | `id` | `id uuid PK` |
| Foreign Key | `{entity}_id` | `department_id` |
| Index | `IX_{table}_{columns}` | `IX_payment_requests_status` |
| Migration | `YYYYMMDDHHMMSS_Name` | `20260501_AddBudgetTable` |

---

## 2. Git Branching Strategy

### 2.1 Branch Model (Git Flow Simplified)

```
main ──────────────────────────────────────────────────── (production)
  │
  └── develop ─────────────────────────────────────────── (integration)
        │
        ├── feature/FIN-001-budget-crud ──────────────── (feature)
        ├── feature/KPI-003-checkin-system ───────────── (feature)
        ├── feature/AI-001-chat-qa ───────────────────── (feature)
        │
        ├── bugfix/FIN-002-amount-validation ─────────── (bugfix)
        │
        └── hotfix/SEC-001-jwt-expiry ────────────────── (hotfix → main)
```

### 2.2 Branch Naming

| Type | Format | Example |
|------|--------|---------|
| Feature | `feature/{ID}-{short-desc}` | `feature/FIN-001-budget-crud` |
| Bugfix | `bugfix/{ID}-{short-desc}` | `bugfix/FIN-002-amount-calc` |
| Hotfix | `hotfix/{ID}-{short-desc}` | `hotfix/SEC-001-jwt-fix` |
| Release | `release/v{version}` | `release/v1.0.0` |

### 2.3 Branch Rules

- `main`: Protected. Merge via PR only. Requires 1 review + CI pass
- `develop`: Protected. Merge via PR only. Requires CI pass
- Feature branches: Created from `develop`, merged back to `develop`
- Hotfix: Created from `main`, merged to both `main` and `develop`

---

## 3. Commit Convention (Conventional Commits)

### 3.1 Format

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### 3.2 Types

| Type | Usage | Example |
|------|-------|---------|
| `feat` | New feature | `feat(finance): add budget CRUD API` |
| `fix` | Bug fix | `fix(auth): correct JWT expiry calculation` |
| `docs` | Documentation | `docs(api): update payment request spec` |
| `style` | Code style (no logic change) | `style(ui): fix button alignment` |
| `refactor` | Refactoring | `refactor(workflow): simplify step logic` |
| `test` | Add/update tests | `test(kpi): add check-in unit tests` |
| `chore` | Maintenance | `chore(deps): upgrade EF Core to 10.1` |
| `perf` | Performance | `perf(dashboard): cache aggregation query` |
| `ci` | CI/CD changes | `ci: add docker build step` |

### 3.3 Scopes

`auth`, `finance`, `kpi`, `workflow`, `ai`, `hr`, `dashboard`, `notification`, `report`, `ui`, `api`, `db`, `infra`, `deps`

### 3.4 Examples

```
feat(finance): implement payment request creation with line items

- Add PaymentRequestController with POST endpoint
- Add CreatePaymentRequestCommand with FluentValidation
- Add PaymentRequestItem entity and mapping
- Auto-generate request number (PR-YYYY-XXXX)

Closes #42

---

fix(kpi): prevent division by zero in progress calculation

When target_value equals start_value, progress was NaN.
Now returns 0% in that edge case.

Fixes #67
```

---

## 4. Code Review Process

### 4.1 PR Requirements

| Requirement | Detail |
|------------|--------|
| Title | Follow commit convention format |
| Description | What changed, why, how to test |
| Linked issue | Reference GitHub issue/task |
| Size | Max 400 lines changed (split if larger) |
| Self-review | Author reviews own diff before requesting |
| Tests | New/updated tests for logic changes |
| Screenshots | Required for UI changes |

### 4.2 Review Checklist

- [ ] Code follows project conventions
- [ ] No hardcoded values (use constants/config)
- [ ] Error handling is appropriate
- [ ] No sensitive data in logs
- [ ] Database queries are efficient (no N+1)
- [ ] API endpoints follow REST conventions
- [ ] Input validation is present
- [ ] Authorization checks exist
- [ ] Tests cover happy path + edge cases

### 4.3 Review Flow

```
Author creates PR → Self-review → Request reviewer (1 required)
→ Reviewer reviews (within 24h) → Comments/Approve/Request Changes
→ Author addresses feedback → Re-request review → Approve → Merge
```

---

## 5. Folder Structure

### 5.1 Full-stack ASP.NET Core MVC 10

```
src/
├── OmniBizAI.Domain/           # Zero dependencies
│   ├── Entities/                # Domain entities
│   ├── Enums/                   # Domain enums
│   ├── Interfaces/              # Repository & service interfaces
│   ├── Events/                  # Domain events
│   └── Exceptions/              # Domain exceptions
│
├── OmniBizAI.Application/       # Depends on: Domain
│   ├── Common/
│   │   ├── Behaviors/           # MediatR behaviors (validation, logging)
│   │   ├── Interfaces/          # Application interfaces
│   │   ├── Models/              # Shared DTOs
│   │   └── Mappings/            # AutoMapper profiles
│   └── Features/
│       └── {Module}/            # One folder per module
│           ├── Commands/        # Create, Update, Delete
│           ├── Queries/         # GetById, GetList
│           └── DTOs/            # Module-specific DTOs
│
├── OmniBizAI.Infrastructure/    # Depends on: Domain, Application
│   ├── Data/                    # EF Core context, configs, migrations
│   ├── Identity/                # ASP.NET Identity config
│   ├── Services/                # External service implementations
│   └── Repositories/            # Repository implementations
│
└── OmniBizAI.Web/               # Depends on: All layers
    ├── Controllers/             # MVC controllers + JSON endpoints
    ├── Areas/                   # Admin and module areas
    ├── Views/                   # Razor views
    ├── ViewModels/              # Page-specific view models
    ├── ViewComponents/          # Reusable server-rendered widgets
    ├── TagHelpers/              # Reusable Razor helpers
    ├── wwwroot/                 # CSS, JS, static assets
    ├── Hubs/                    # SignalR hubs
    ├── Middleware/               # Custom middleware
    ├── Filters/                 # Action filters
    └── Extensions/              # Service registration extensions
```

### 5.2 MVC UI Assets

```
src/OmniBizAI.Web/
├── Views/
│   ├── Shared/                  # _Layout, partials, validation scripts
│   ├── Account/
│   ├── Dashboard/
│   ├── Finance/
│   ├── Performance/
│   ├── Workflow/
│   ├── Organization/
│   └── AI/
├── ViewModels/                  # Strongly typed page models
├── ViewComponents/              # Dashboard cards, nav, notification widgets
├── TagHelpers/                  # Status badges, money/date display helpers
└── wwwroot/
    ├── css/                     # Site styles and UI framework overrides
    ├── js/                      # Lightweight page scripts
    └── lib/                     # Vendored browser libraries
```

---

## 6. Error Handling

### 6.1 Backend Error Strategy

```csharp
// Domain exceptions (throw from domain/application layer)
public class NotFoundException : Exception { }
public class BusinessRuleException : Exception { }
public class ForbiddenException : Exception { }

// Global exception middleware catches and maps to HTTP status codes
// NotFoundException → 404
// BusinessRuleException → 422
// ForbiddenException → 403
// ValidationException → 400
// Unhandled → 500 (log full stack, return generic message)
```

### 6.2 MVC UI Error Strategy

- **Controller errors**: Return typed error view or JSON problem details based on request type
- **Form validation**: DataAnnotations/FluentValidation → `ModelState` inline messages
- **Network errors**: Global interceptor → "Connection lost" banner
- **404 pages**: MVC `NotFound` view
- **Unhandled UI errors**: Exception middleware → generic error view with correlation ID

---

## 7. Logging Standards

### 7.1 Log Levels

| Level | Usage | Example |
|-------|-------|---------|
| `Debug` | Development only | Query parameters, internal state |
| `Information` | Normal operations | "Payment request PR-001 created" |
| `Warning` | Potential issues | "Budget 80% utilized" |
| `Error` | Recoverable errors | "AI API call failed, using fallback" |
| `Critical` | System failures | "Database connection lost" |

### 7.2 Structured Logging (Serilog)

```csharp
_logger.LogInformation(
    "Payment request {RequestNumber} created by {UserId} for {Amount} {Currency}",
    request.RequestNumber, userId, request.TotalAmount, request.Currency);
```

### 7.3 Sensitive Data Rules

**NEVER LOG**: Passwords, tokens, API keys, personal ID numbers, bank details  
**MASK**: Email (show first 3 chars), Phone (show last 4 digits)

---

## 8. Environment Configuration

| Variable | Dev | Staging | Production |
|----------|-----|---------|-----------|
| `ASPNETCORE_ENVIRONMENT` | Development | Staging | Production |
| `ConnectionStrings__Default` | localhost | staging-db | prod-db |
| `Redis__Connection` | localhost:6379 | staging-redis | prod-redis |
| `Jwt__Secret` | dev-secret | *** | *** |
| `AI__Provider` | Groq | Groq | Groq |
| `AI__ApiKey` | *** | *** | *** |
| `Logging__Level` | Debug | Information | Warning |
| `AllowedHosts` | localhost | staging.omnibiz.ai | omnibiz.ai |
