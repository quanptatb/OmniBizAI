# 🔌 OmniBiz AI — API Specification

> **Version**: 1.0 | **Updated**: 2026-04-25  
> **Base URL**: `https://omnibiz.ai/api/v1`  
> **Format**: REST JSON endpoints inside ASP.NET Core MVC | **Auth**: ASP.NET Identity cookie for first-party UI, optional JWT Bearer for external API clients

---

## 1. Common Conventions

### 1.1 Request/Response Format

```json
// Success Response
{
  "success": true,
  "data": { ... },
  "message": "Operation successful",
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 150,
    "totalPages": 8
  }
}

// Error Response
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    { "field": "amount", "message": "Amount must be greater than 0" }
  ],
  "traceId": "abc-123"
}
```

### 1.2 Status Codes

| Code | Usage |
|------|-------|
| 200 | Success (GET, PUT, PATCH) |
| 201 | Created (POST) |
| 204 | No Content (DELETE) |
| 400 | Bad Request / Validation Error |
| 401 | Unauthorized (missing/invalid session or token) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not Found |
| 409 | Conflict (duplicate, state conflict) |
| 422 | Unprocessable Entity |
| 429 | Rate Limited |
| 500 | Internal Server Error |

### 1.3 Pagination, Filtering, Sorting

```
GET /api/v1/budgets?page=1&pageSize=20&sortBy=createdAt&sortOrder=desc
  &status=Active&departmentId=xxx&search=marketing
  &dateFrom=2026-01-01&dateTo=2026-12-31
```

### 1.4 Rate Limiting

| Endpoint Group | Limit | Window |
|---------------|-------|--------|
| General API | 100 req | 1 minute |
| Auth endpoints | 10 req | 1 minute |
| AI endpoints | 10 req | 1 minute |
| File upload | 20 req | 1 minute |
| Export/Report | 5 req | 1 minute |

---

## 2. Auth Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | `/auth/login` | Login | No |
| POST | `/auth/register` | Register (Admin only) | Admin |
| POST | `/auth/refresh-token` | Refresh optional external API JWT | No (refresh cookie) |
| POST | `/auth/logout` | Logout | Yes |
| POST | `/auth/forgot-password` | Request reset | No |
| POST | `/auth/reset-password` | Reset password | No (token) |
| GET | `/auth/me` | Get current user | Yes |
| PUT | `/auth/profile` | Update profile | Yes |
| PUT | `/auth/change-password` | Change password | Yes |

### POST `/auth/login`
```json
// Request
{ "email": "director@omnibiz.ai", "password": "P@ssw0rd123" }

// Response 200
{
  "success": true,
  "data": {
    "authMode": "cookie",
    "expiresIn": 3600,
    "accessToken": "eyJ... (optional, external API clients only)",
    "user": {
      "id": "uuid",
      "email": "director@omnibiz.ai",
      "fullName": "Nguyễn Văn A",
      "roles": ["Director"],
      "permissions": ["finance:read", "finance:approve", ...],
      "departmentId": "uuid",
      "avatarUrl": "/uploads/..."
    }
  }
}
```

---

## 3. Organization Endpoints

### Departments
| Method | Path | Description | Roles |
|--------|------|-------------|-------|
| GET | `/departments` | List departments | All |
| GET | `/departments/{id}` | Get department | All |
| POST | `/departments` | Create department | Admin, HR |
| PUT | `/departments/{id}` | Update department | Admin, HR |
| DELETE | `/departments/{id}` | Delete department | Admin |
| GET | `/departments/{id}/employees` | List employees in dept | Manager+ |
| GET | `/departments/tree` | Get org tree | All |

### Employees
| Method | Path | Description | Roles |
|--------|------|-------------|-------|
| GET | `/employees` | List employees | Manager+ |
| GET | `/employees/{id}` | Get employee detail | Self/Manager+ |
| POST | `/employees` | Create employee | Admin, HR |
| PUT | `/employees/{id}` | Update employee | Admin, HR |
| DELETE | `/employees/{id}` | Soft delete | Admin |
| GET | `/employees/{id}/history` | Employment history | HR+ |
| PUT | `/employees/{id}/status` | Change status | Admin, HR |

---

## 4. Finance Endpoints

### Budgets
| Method | Path | Description | Roles |
|--------|------|-------------|-------|
| GET | `/budgets` | List budgets | Accountant+ |
| GET | `/budgets/{id}` | Budget detail | Accountant+ |
| POST | `/budgets` | Create budget | Director, Accountant |
| PUT | `/budgets/{id}` | Update budget | Director, Accountant |
| DELETE | `/budgets/{id}` | Delete budget | Director |
| GET | `/budgets/{id}/utilization` | Budget utilization | All (scoped) |
| POST | `/budgets/{id}/adjust` | Request adjustment | Manager+ |
| GET | `/budgets/summary` | Budget summary by dept | Director |

### Payment Requests
| Method | Path | Description | Roles |
|--------|------|-------------|-------|
| GET | `/payment-requests` | List PRs | All (scoped) |
| GET | `/payment-requests/{id}` | PR detail | All (scoped) |
| POST | `/payment-requests` | Create PR | All |
| PUT | `/payment-requests/{id}` | Update (Draft only) | Creator |
| DELETE | `/payment-requests/{id}` | Delete (Draft only) | Creator |
| POST | `/payment-requests/{id}/submit` | Submit for approval | Creator |
| POST | `/payment-requests/{id}/cancel` | Cancel PR | Creator/Admin |
| GET | `/payment-requests/{id}/ai-risk` | Get AI risk analysis | All (scoped) |
| POST | `/payment-requests/{id}/attachments` | Upload attachment | Creator |
| DELETE | `/payment-requests/{id}/attachments/{fileId}` | Remove attachment | Creator |

### Transactions
| Method | Path | Description | Roles |
|--------|------|-------------|-------|
| GET | `/transactions` | List transactions | Accountant+ |
| GET | `/transactions/{id}` | Transaction detail | Accountant+ |
| POST | `/transactions` | Create transaction | Accountant |
| PUT | `/transactions/{id}` | Update transaction | Accountant |
| DELETE | `/transactions/{id}` | Reverse transaction | Director |
| GET | `/transactions/summary` | Summary by period | Director, Accountant |

### Wallets & Vendors
| Method | Path | Description |
|--------|------|-------------|
| GET/POST/PUT/DELETE | `/wallets[/{id}]` | Wallet CRUD |
| GET/POST/PUT/DELETE | `/vendors[/{id}]` | Vendor CRUD |
| GET | `/vendors/{id}/transactions` | Vendor transaction history |
| POST | `/vendors/{id}/rate` | Rate vendor |

### Categories
| Method | Path | Description |
|--------|------|-------------|
| GET | `/budget-categories` | List (tree) |
| POST/PUT/DELETE | `/budget-categories[/{id}]` | CRUD |

---

## 5. KPI/OKR Endpoints

### Objectives (OKR)
| Method | Path | Description | Roles |
|--------|------|-------------|-------|
| GET | `/objectives` | List objectives | All (scoped) |
| GET | `/objectives/{id}` | Objective detail with KRs | All (scoped) |
| POST | `/objectives` | Create objective | Manager+ |
| PUT | `/objectives/{id}` | Update | Manager+ |
| DELETE | `/objectives/{id}` | Delete | Director |
| GET | `/objectives/tree` | Objective cascade tree | Director |
| GET | `/objectives/{id}/progress` | Progress detail | All (scoped) |

### Key Results
| Method | Path | Description |
|--------|------|-------------|
| GET | `/key-results?objectiveId=xxx` | List KRs |
| POST | `/key-results` | Create KR |
| PUT | `/key-results/{id}` | Update KR |
| DELETE | `/key-results/{id}` | Delete KR |
| POST | `/key-results/{id}/check-in` | Submit check-in |

### KPIs
| Method | Path | Description |
|--------|------|-------------|
| GET | `/kpis` | List KPIs (scoped) |
| GET | `/kpis/{id}` | KPI detail |
| POST | `/kpis` | Create KPI |
| PUT | `/kpis/{id}` | Update KPI |
| DELETE | `/kpis/{id}` | Delete KPI |
| POST | `/kpis/{id}/check-in` | Submit check-in |
| GET | `/kpis/{id}/check-ins` | Check-in history |
| GET | `/kpis/dashboard` | KPI dashboard data |
| GET | `/kpis/scorecard/{employeeId}` | Employee scorecard |

### Check-ins & Evaluations
| Method | Path | Description |
|--------|------|-------------|
| GET | `/check-ins?status=Submitted` | Pending check-ins |
| POST | `/check-ins/{id}/approve` | Approve check-in |
| POST | `/check-ins/{id}/reject` | Reject check-in |
| GET | `/evaluations` | List evaluations |
| GET | `/evaluations/{id}` | Evaluation detail |
| POST | `/evaluations/{id}/submit` | Submit evaluation |
| GET | `/evaluation-periods` | List periods |
| POST/PUT | `/evaluation-periods[/{id}]` | Period CRUD |

---

## 6. Workflow Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/workflow-templates` | List templates |
| POST | `/workflow-templates` | Create template |
| PUT | `/workflow-templates/{id}` | Update template |
| GET | `/workflow-instances?status=Pending` | My pending approvals |
| GET | `/workflow-instances/{id}` | Instance detail with steps |
| POST | `/workflow-instances/{id}/approve` | Approve current step |
| POST | `/workflow-instances/{id}/reject` | Reject |
| POST | `/workflow-instances/{id}/comment` | Add comment |
| POST | `/workflow-instances/{id}/delegate` | Delegate to another |
| GET | `/approval-queue` | Approval queue for current user |

---

## 7. AI Endpoints

| Method | Path | Description | Rate Limit |
|--------|------|-------------|-----------|
| POST | `/ai/chat` | Send message | 10/min |
| GET | `/ai/sessions` | List chat sessions | 100/min |
| GET | `/ai/sessions/{id}/messages` | Get session messages | 100/min |
| POST | `/ai/risk-analysis` | Analyze entity risk | 10/min |
| POST | `/ai/kpi-insight` | Get KPI insights | 10/min |
| POST | `/ai/generate-report` | Generate report | 5/min |
| GET | `/ai/history` | AI generation history | 100/min |
| POST | `/ai/history/{id}/rate` | Rate AI response | 100/min |

### POST `/ai/chat`
```json
// Request
{
  "sessionId": "uuid | null",
  "message": "Tháng này phòng nào vượt ngân sách?",
  "context": {
    "page": "dashboard",
    "filters": { "period": "2026-04" }
  }
}

// Response 200
{
  "success": true,
  "data": {
    "sessionId": "uuid",
    "message": {
      "id": "uuid",
      "role": "assistant",
      "content": "Dựa trên dữ liệu tháng 4/2026, có 2 phòng ban vượt ngân sách:\n1. **Phòng Marketing**: Chi 85 triệu / Budget 70 triệu (121%)\n2. **Phòng IT**: Chi 45 triệu / Budget 40 triệu (112.5%)",
      "citations": [
        { "type": "budget", "id": "uuid", "label": "Budget Marketing T4" },
        { "type": "budget", "id": "uuid", "label": "Budget IT T4" }
      ],
      "chartData": {
        "type": "bar",
        "data": { ... }
      }
    }
  }
}
```

---

## 8. Dashboard Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/dashboard/overview` | Main dashboard data |
| GET | `/dashboard/financial-summary` | Revenue/Expense summary |
| GET | `/dashboard/kpi-summary` | KPI completion rates |
| GET | `/dashboard/pending-approvals` | Pending approvals count |
| GET | `/dashboard/risk-alerts` | Top risk items |
| GET | `/dashboard/cashflow` | Cashflow chart data |
| GET | `/dashboard/department-performance` | Dept comparison |

---

## 9. Notification & Report Endpoints

### Notifications
| Method | Path | Description |
|--------|------|-------------|
| GET | `/notifications` | List notifications |
| GET | `/notifications/unread-count` | Unread count |
| PUT | `/notifications/{id}/read` | Mark as read |
| PUT | `/notifications/read-all` | Mark all as read |
| GET/PUT | `/notifications/preferences` | Get/Update preferences |

### Reports & Export
| Method | Path | Description |
|--------|------|-------------|
| GET | `/reports/financial?period=xxx` | Financial report |
| GET | `/reports/kpi-scorecard?period=xxx` | KPI scorecard |
| GET | `/reports/department-performance` | Dept performance |
| POST | `/reports/export` | Export to PDF/Excel |

---

## 10. Admin Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/admin/users` | List all users |
| POST/PUT | `/admin/users[/{id}]` | User CRUD |
| GET/PUT | `/admin/roles[/{id}]` | Role management |
| GET | `/admin/audit-logs` | View audit logs |
| GET | `/admin/system-settings` | System settings |
| PUT | `/admin/system-settings` | Update settings |
| POST | `/admin/seed-data` | Seed demo data |
| GET | `/admin/health` | Health check |

---

## 11. WebSocket (SignalR)

### Hub: `/hubs/notification`

| Event | Direction | Payload |
|-------|-----------|---------|
| `ReceiveNotification` | Server→Client | `{ id, title, message, type, entityType, entityId }` |
| `ApprovalStatusChanged` | Server→Client | `{ instanceId, status, entityType, entityId }` |
| `DashboardDataUpdated` | Server→Client | `{ updateType: "transaction" \| "budget" \| "kpi" }` |
| `JoinGroup` | Client→Server | `{ groupName: "user_{userId}" }` |
