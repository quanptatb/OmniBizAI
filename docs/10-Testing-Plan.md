# 🧪 OmniBiz AI — Testing Plan

> **Version**: 1.0 | **Updated**: 2026-04-24

---

## 1. Testing Strategy Overview

| Level | Tool | Target Coverage | Responsibility |
|-------|------|----------------|---------------|
| **Unit Test** | xUnit + Moq (.NET 10), Razor/MVC view-model tests | > 70% | All developers |
| **Integration Test** | xUnit + TestContainers | > 50% | Backend devs |
| **E2E Test** | Playwright | Critical flows | QA / DevOps |
| **API Test** | REST Client / Postman | All endpoints | Backend devs |
| **Performance Test** | k6 / Artillery | Key endpoints | DevOps |
| **Security Test** | OWASP ZAP (basic) | OWASP Top 10 | DevOps |

---

## 2. Unit Tests

### 2.1 Backend Unit Tests

#### Finance Module
| Test Case ID | Description | Input | Expected Output |
|-------------|-------------|-------|-----------------|
| FIN-UT-001 | Create budget with valid data | Valid BudgetDTO | Budget created, ID returned |
| FIN-UT-002 | Create budget with negative amount | Amount = -1000 | Validation error |
| FIN-UT-003 | Budget remaining calculation | Allocated=100M, Spent=60M | Remaining=40M, Util=60% |
| FIN-UT-004 | Budget warning at 80% threshold | Spent=85M, Allocated=100M | Warning flag = true |
| FIN-UT-005 | Create payment request with items | Valid PR + 2 items | PR created, total calculated |
| FIN-UT-006 | Payment request without items | PR with empty items[] | Validation error |
| FIN-UT-007 | Submit draft PR | PR status=Draft | Status → Submitted |
| FIN-UT-008 | Submit non-draft PR | PR status=Approved | Business rule error |
| FIN-UT-009 | Transaction updates budget | New expense 10M | Budget spent += 10M |
| FIN-UT-010 | Reverse transaction | Reverse 10M expense | Budget spent -= 10M |

#### KPI/OKR Module
| Test Case ID | Description | Input | Expected Output |
|-------------|-------------|-------|-----------------|
| KPI-UT-001 | Calculate KR progress | Start=0, Target=100, Current=75 | Progress=75% |
| KPI-UT-002 | KR progress division by zero | Start=50, Target=50 | Progress=0% |
| KPI-UT-003 | Objective progress from KRs | 3 KRs with weights | Weighted average |
| KPI-UT-004 | KPI rating calculation | Score=92% | Rating=A |
| KPI-UT-005 | KPI rating boundary | Score=90% | Rating=A (inclusive) |
| KPI-UT-006 | KPI rating boundary | Score=89% | Rating=B |
| KPI-UT-007 | Check-in updates KPI value | NewValue=80 | KPI current=80, progress updated |
| KPI-UT-008 | Reject check-in | Reject with comment | Status=Rejected, value not updated |
| KPI-UT-009 | Evaluation score calculation | OKR=85%, KPI=75%, weights 50/50 | Total=80% |
| KPI-UT-010 | Decrease direction KPI | Start=100, Target=50, Current=60 | Progress=80% |

#### Workflow Module
| Test Case ID | Description | Input | Expected Output |
|-------------|-------------|-------|-----------------|
| WF-UT-001 | Create workflow instance | Valid template + entity | Instance created, step 1 pending |
| WF-UT-002 | Approve step advances workflow | Approve step 1 of 3 | Current step → 2 |
| WF-UT-003 | Final approval completes workflow | Approve last step | Instance status=Approved |
| WF-UT-004 | Reject at any step | Reject step 2 | Instance status=Rejected |
| WF-UT-005 | Condition: amount > 50M | PR amount=80M | Route to 3-level template |
| WF-UT-006 | Condition: amount ≤ 50M | PR amount=30M | Route to 2-level template |

#### Auth Module
| Test Case ID | Description | Input | Expected Output |
|-------------|-------------|-------|-----------------|
| AUTH-UT-001 | Valid login | Correct email/password | JWT token returned |
| AUTH-UT-002 | Invalid password | Wrong password | 401 Unauthorized |
| AUTH-UT-003 | Account locked | 5th failed attempt | Account locked, 401 with lock message |
| AUTH-UT-004 | Token refresh | Valid refresh token | New access token |
| AUTH-UT-005 | Expired refresh token | Expired token | 401, redirect to login |
| AUTH-UT-006 | Permission check | Staff tries budget:delete | 403 Forbidden |
| AUTH-UT-007 | Data scope check | Manager queries other dept | Empty result / 403 |

### 2.2 MVC UI Unit/View Tests

| Test Case ID | Component | Description |
|-------------|-----------|-------------|
| UI-UT-001 | BudgetSummaryViewComponent | Renders correct amount formatting (VND) |
| UI-UT-002 | BudgetSummaryViewComponent | Shows warning color at 80%+ utilization |
| UI-UT-003 | PaymentRequestViewModel | Validates required fields |
| UI-UT-004 | PaymentRequestController | Calculates total from line items before rendering confirmation |
| UI-UT-005 | KpiProgressViewComponent | Shows correct % and color |
| UI-UT-006 | Approval action helpers | Hides or disables actions when user has no permission |
| UI-UT-007 | DashboardMetricViewComponent | Formats large numbers (e.g., 1.2 tỷ) |
| UI-UT-008 | Date range ViewModel | Validates end date > start date |
| UI-UT-009 | File upload action | Rejects files > 10MB |
| UI-UT-010 | Role-based navigation | Hides menu items for unauthorized role |

---

## 3. Integration Tests

### 3.1 API Integration Tests

| Test Case ID | Endpoint | Description | Preconditions |
|-------------|----------|-------------|--------------|
| INT-001 | POST /auth/login | Full login flow with DB | Seeded user exists |
| INT-002 | POST /payment-requests | Create PR and verify in DB | Authenticated, budget exists |
| INT-003 | POST /payment-requests/{id}/submit | Submit triggers workflow creation | PR in Draft status |
| INT-004 | POST /workflow-instances/{id}/approve | Approve updates PR status | Pending approval exists |
| INT-005 | Transaction → Budget update | Creating expense updates budget spent | Budget and wallet exist |
| INT-006 | POST /kpis/{id}/check-in | Check-in flow with approval | KPI assigned, active period |
| INT-007 | GET /dashboard/overview | Dashboard aggregates correctly | Multiple transactions, KPIs exist |
| INT-008 | POST /ai/chat | AI returns relevant response | Data seeded, AI API mocked |
| INT-009 | GET /budgets (Manager scope) | Returns only department budgets | Manager authenticated |
| INT-010 | RBAC enforcement | Staff cannot access admin endpoints | Staff authenticated |

### 3.2 Database Integration Tests

| Test Case ID | Description |
|-------------|-------------|
| DB-INT-001 | Cascade delete: Delete department → employees soft-deleted |
| DB-INT-002 | Unique constraint: Duplicate employee code rejected |
| DB-INT-003 | Foreign key: PR with non-existent department fails |
| DB-INT-004 | Soft delete filter: Deleted records excluded from queries |
| DB-INT-005 | Audit log: Create/Update/Delete operations logged |

---

## 4. End-to-End (E2E) Tests

### 4.1 Critical User Flows

| Flow ID | Flow Name | Steps | Expected Result |
|---------|-----------|-------|-----------------|
| E2E-001 | **Payment Request Full Cycle** | 1. Login as Staff → 2. Create PR with items → 3. Submit → 4. Login as Manager → 5. View approval queue → 6. Approve → 7. Verify PR status=Approved → 8. Verify transaction created → 9. Verify budget updated → 10. Verify dashboard reflects changes | All steps pass, data consistent |
| E2E-002 | **KPI Check-in Cycle** | 1. Login as Employee → 2. Navigate to KPIs → 3. Create check-in → 4. Login as Manager → 5. Review check-in → 6. Approve → 7. Verify KPI progress updated | KPI current value and progress updated |
| E2E-003 | **Budget Overspend Warning** | 1. Login as Accountant → 2. Create budget 100M → 3. Login as Staff → 4. Create PR 85M → 5. Verify AI risk warning shows → 6. Submit → 7. Login as Director → 8. See risk alert on dashboard | AI warning displayed, dashboard risk alert shown |
| E2E-004 | **Role-based Dashboard** | 1. Login as Director → Verify all-company data → 2. Login as Manager → Verify department-only data → 3. Login as Staff → Verify personal data only | Data scope correct for each role |
| E2E-005 | **AI Chat Q&A** | 1. Login as Director → 2. Open AI Copilot → 3. Ask "Phòng nào vượt ngân sách?" → 4. Verify relevant answer with data → 5. Verify citations present | AI responds with accurate, cited data |

### 4.2 E2E Test Configuration

```typescript
// playwright.config.ts
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  use: {
    baseURL: 'http://localhost:5000',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    trace: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'mobile', use: { ...devices['iPhone 13'] } },
  ],
});
```

---

## 5. Test Data

### 5.1 Seed Data for Testing

| Entity | Count | Notes |
|--------|-------|-------|
| Users | 10 | 1 per role + extras |
| Departments | 5 | IT, Marketing, Sales, HR, Finance |
| Employees | 20 | Distributed across departments |
| Budgets | 10 | 2 per department, different periods |
| Payment Requests | 15 | Various statuses (Draft, Pending, Approved, Rejected) |
| Transactions | 50 | Mix of income/expense |
| Objectives | 8 | Company + Department level |
| Key Results | 24 | 3 per objective |
| KPIs | 20 | Various assignees and statuses |
| Check-ins | 30 | Submitted, Approved, Rejected mix |
| Workflow Templates | 3 | 2-level, 3-level, budget-conditional |
| Notifications | 20 | Various types |

### 5.2 Test Accounts

| Email | Password | Role | Department |
|-------|----------|------|------------|
| admin@omnibiz.ai | Test@123456 | Admin | - |
| director@omnibiz.ai | Test@123456 | Director | - |
| manager.mkt@omnibiz.ai | Test@123456 | Manager | Marketing |
| manager.it@omnibiz.ai | Test@123456 | Manager | IT |
| accountant@omnibiz.ai | Test@123456 | Accountant | Finance |
| hr@omnibiz.ai | Test@123456 | HR | HR |
| staff.mkt@omnibiz.ai | Test@123456 | Staff | Marketing |
| staff.sales@omnibiz.ai | Test@123456 | Staff | Sales |

---

## 6. Acceptance Criteria Summary

### 6.1 Module-level Acceptance

| Module | Must Pass |
|--------|----------|
| Auth | Login/logout works, RBAC enforced, data scope correct |
| Finance | Budget CRUD, PR create/submit/approve cycle, transaction affects budget |
| KPI/OKR | OKR/KPI CRUD, check-in flow, scoring calculation correct |
| Workflow | Template-based routing, multi-level approval, condition evaluation |
| AI | Chat returns relevant answers, risk analysis runs, citations present |
| Dashboard | Data aggregates correctly, role-based views, real-time updates |
| Notifications | Sent on approval events, realtime via SignalR, mark-as-read works |

### 6.2 Non-functional Acceptance

| Criteria | Target |
|---------|--------|
| API response (P95) | < 500ms |
| Page load | < 3s |
| Concurrent users | Handle 50 simultaneous |
| Zero critical bugs at demo | 0 P0 bugs |
| Test coverage | > 70% backend |

---

## 7. Test Execution Plan

| Phase | Week | Focus | Owner |
|-------|------|-------|-------|
| Unit tests | Week 3-9 (continuous) | Write alongside feature code | Each developer |
| Integration tests | Week 6-9 | API + DB integration | Backend devs |
| E2E tests | Week 10-11 | Critical user flows | QA (Member 7) |
| Performance test | Week 10 | Dashboard + API load | DevOps |
| Security scan | Week 11 | OWASP ZAP basic scan | DevOps |
| UAT | Week 11-12 | Full system walkthrough | All team |
| Regression | Week 12 | Re-run all tests before demo | QA |

---

## 8. Bug Severity Classification

| Severity | Definition | Response Time | Example |
|----------|-----------|--------------|---------|
| P0 - Critical | System unusable, data loss | Fix immediately | Login broken, data corruption |
| P1 - High | Major feature broken | Fix within 4h | Approval flow fails, wrong calculations |
| P2 - Medium | Feature partially broken | Fix within 24h | Filter not working, UI misalignment |
| P3 - Low | Minor issue, cosmetic | Fix before demo | Typo, color mismatch, extra whitespace |
