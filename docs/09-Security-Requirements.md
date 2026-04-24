# 🔒 OmniBiz AI — Security Requirements

> **Version**: 1.0 | **Updated**: 2026-04-24

---

## 1. Authentication

### 1.1 Primary Authentication: JWT

| Item | Specification |
|------|--------------|
| Algorithm | RS256 (RSA asymmetric) |
| Access Token TTL | 60 minutes |
| Refresh Token TTL | 7 days |
| Token Storage | Access: Memory/localStorage, Refresh: HttpOnly cookie |
| Token Payload | userId, email, roles[], departmentId, permissions[], exp, iat |

### 1.2 Password Policy

| Rule | Requirement |
|------|------------|
| Min length | 8 characters |
| Complexity | At least 1 uppercase, 1 lowercase, 1 digit, 1 special char |
| Hashing | bcrypt with cost factor 12 |
| History | Cannot reuse last 5 passwords |
| Expiry | No forced expiry (NIST recommendation) |
| Account lockout | Lock after 5 consecutive failed attempts for 15 minutes |

### 1.3 OAuth 2.0 (Optional)
- Provider: Google OAuth
- Flow: Authorization Code with PKCE
- Scope: `openid email profile`
- Auto-create user account on first login if email matches company domain

### 1.4 Session Management
- Single session per device (new login invalidates old session)
- Session tracking: IP, User-Agent, started_at
- Idle timeout: 30 minutes (configurable)
- Absolute timeout: 12 hours

---

## 2. Authorization (RBAC)

### 2.1 Role Hierarchy

```
Admin (Level 1)
  └── Director (Level 2)
        └── Manager (Level 3)
              ├── Accountant (Level 4)
              ├── HR (Level 4)
              └── Staff (Level 5)
```

### 2.2 Permission Matrix

| Permission | Admin | Director | Manager | Accountant | HR | Staff |
|-----------|-------|----------|---------|------------|-----|-------|
| **Users** | | | | | | |
| user:create | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ |
| user:read (all) | ✅ | ✅ | 🔵 dept | ❌ | ✅ | ❌ |
| user:update | ✅ | ❌ | ❌ | ❌ | ✅ | 🔵 self |
| user:delete | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Finance** | | | | | | |
| budget:create | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ |
| budget:read | ✅ | ✅ | 🔵 dept | ✅ | ❌ | ❌ |
| budget:update | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ |
| budget:delete | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| payment_request:create | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| payment_request:approve | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| transaction:create | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| transaction:read | ✅ | ✅ | 🔵 dept | ✅ | ❌ | 🔵 own |
| **KPI/OKR** | | | | | | |
| objective:create | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| kpi:create | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| kpi:read | ✅ | ✅ | 🔵 dept | ❌ | ❌ | 🔵 own |
| checkin:create | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| checkin:approve | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| evaluation:read | ✅ | ✅ | 🔵 dept | ❌ | ✅ | 🔵 own |
| **Workflow** | | | | | | |
| workflow:manage | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| approval:action | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| **AI** | | | | | | |
| ai:chat | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ai:report | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **System** | | | | | | |
| audit:read | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| settings:manage | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| report:export | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |

🔵 = Scoped (only within allowed data scope)

### 2.3 Data Scoping Rules

| Role | Data Scope |
|------|-----------|
| Admin | All company data |
| Director | All company data |
| Manager | Own department + sub-departments |
| Accountant | All finance data (read), own department for others |
| HR | All employee data, own performance data |
| Staff | Own data only (own PRs, own KPIs, own check-ins) |

**Implementation**: Data scope filter applied at repository level via `IDataScopeService`

---

## 3. Data Privacy & Protection

### 3.1 Sensitive Data Classification

| Level | Data Type | Handling |
|-------|----------|---------|
| **Critical** | Passwords, API Keys, Tokens | Hashed/Encrypted, never logged |
| **High** | Bank accounts, Tax codes, Salary | Encrypted at rest (AES-256), masked in UI |
| **Medium** | Email, Phone, Address | Access-controlled, partial masking in lists |
| **Low** | Department name, KPI name | Standard access control |

### 3.2 Encryption

| Type | Method | Usage |
|------|--------|-------|
| Password hashing | bcrypt (cost 12) | User passwords |
| Data at rest | AES-256-GCM | Bank accounts, sensitive fields |
| Data in transit | TLS 1.3 | All HTTP traffic (HTTPS) |
| Database | PostgreSQL native encryption | pg_crypto extension |
| Backup | GPG encryption | Database backup files |

### 3.3 Data Masking

```
Email: ngu***@omnibiz.ai
Phone: ********4567
Bank Account: ****-****-****-1234
Tax Code: *******89
```

---

## 4. Audit Log

### 4.1 Tracked Events

| Category | Events |
|----------|--------|
| Authentication | Login, Logout, Failed Login, Password Change, Token Refresh |
| Data Mutation | Create, Update, Delete (all entities) |
| Approval | Approve, Reject, Delegate, Comment |
| Access | View sensitive data, Export report |
| AI | AI query, AI report generation |
| System | Setting change, Role change, Permission change |

### 4.2 Audit Record Structure

```json
{
  "id": 12345,
  "userId": "uuid",
  "userEmail": "admin@omnibiz.ai",
  "action": "Update",
  "entityType": "PaymentRequest",
  "entityId": "uuid",
  "entityName": "PR-2026-0042",
  "oldValues": { "status": "Draft", "amount": 50000000 },
  "newValues": { "status": "Submitted", "amount": 80000000 },
  "changesSummary": "Status changed from Draft to Submitted, Amount changed from 50M to 80M",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "requestPath": "PUT /api/v1/payment-requests/uuid",
  "createdAt": "2026-04-24T10:30:00Z"
}
```

### 4.3 Audit Rules
- Immutable: Audit logs CANNOT be modified or deleted
- Retention: 1 year minimum
- Access: Admin only
- Partitioning: By month for performance
- Export: CSV/JSON format for compliance

---

## 5. OWASP Top 10 Mitigations

| # | Risk | Mitigation |
|---|------|-----------|
| A01 | Broken Access Control | RBAC + Data Scoping + Permission middleware + audit log |
| A02 | Cryptographic Failures | TLS 1.3, bcrypt, AES-256 encryption, no plaintext secrets |
| A03 | Injection | EF Core parameterized queries, input validation, output encoding |
| A04 | Insecure Design | Threat modeling, secure defaults, principle of least privilege |
| A05 | Security Misconfiguration | Hardened Docker images, no default credentials, CORS whitelist |
| A06 | Vulnerable Components | Dependabot alerts, regular dependency updates, SCA scanning |
| A07 | Authentication Failures | Account lockout, rate limiting, secure password policy, MFA option |
| A08 | Data Integrity Failures | CSRF tokens, signed JWTs, integrity checks on file uploads |
| A09 | Logging & Monitoring | Structured logging (Serilog), audit log, health checks, alerting |
| A10 | Server-Side Request Forgery | Input validation, URL whitelist for external calls, no user-controlled URLs |

---

## 6. Input Validation

### 6.1 Validation Rules

| Field Type | Rules |
|-----------|-------|
| String | Max length, no HTML/script tags, trim whitespace |
| Email | RFC 5322 format, max 255 chars |
| Phone | Digits only, 10-15 chars, optional +prefix |
| Amount | Decimal > 0, max 18 digits, 2 decimal places |
| Date | Valid date, reasonable range (not year 1900 or 3000) |
| File | Size ≤ 10MB, allowed MIME types, content-type validation |
| UUID | Valid UUID v4 format |
| URL | Valid URL format, HTTPS only for external links |

### 6.2 Server-Side Validation

```csharp
public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequestCommand>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(999_999_999_999.99m);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one line item required");
        RuleForEach(x => x.Items).ChildRules(item => {
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
```

---

## 7. API Security

| Measure | Implementation |
|---------|---------------|
| Rate Limiting | AspNetCoreRateLimit: 100/min general, 10/min auth & AI |
| CORS | Whitelist specific origins only |
| HTTPS Only | HSTS header, redirect HTTP→HTTPS |
| Content Security Policy | Strict CSP headers |
| Anti-CSRF | Antiforgery tokens for state-changing operations |
| Request Size Limit | Max 50MB (file upload), 1MB (JSON body) |
| Response Headers | X-Content-Type-Options, X-Frame-Options, Referrer-Policy |
| API Versioning | URL versioning `/api/v1/` |

---

## 8. AI Security

| Risk | Mitigation |
|------|-----------|
| Prompt Injection | Input sanitization, system prompt hardening, output filtering |
| Data Leakage | User can only query data within their data scope |
| PII in AI Response | Post-processing filter to mask sensitive data |
| API Key Exposure | Keys in environment variables, never in code/logs |
| Cost Control | Rate limiting AI calls (10/min/user), token budget per request |
| Model Output Safety | Content filtering, refusal patterns for off-topic queries |

---

## 9. Infrastructure Security

| Area | Measure |
|------|---------|
| Docker | Non-root user, read-only filesystem where possible, no latest tag |
| Secrets | Environment variables via Docker secrets / Azure Key Vault |
| Database | Encrypted connections (SSL), strong passwords, limited network access |
| Backup | Encrypted, daily automated, stored separately, tested monthly |
| Monitoring | Health checks, failed login alerts, unusual traffic alerts |
| Updates | Automated Dependabot, monthly security patch review |
