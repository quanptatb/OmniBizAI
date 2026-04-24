# OmniBiz AI Backend Audit Report

## Executive Summary
This report audits the current state of the OmniBiz AI backend (ASP.NET Core Web API) against the provided Product Requirements Document (PRD) and MVP specifications. 

Overall, the backend implements the foundational data creation paths (POST endpoints) for core entities but severely lacks data retrieval (GET), updating (PUT/PATCH), and deletion (DELETE) endpoints across all modules. Several entire modules (Notifications, Reports, Admin CRUD) are missing.

## 1. Auth / RBAC
*   **Expected**: Login, logout, refresh token, current user, role/permission management, audit log.
*   **Existing**: `POST /auth/login`, `POST /auth/refresh-token`, `GET /auth/me`. JWT middleware and role-based authorization attributes are present.
*   **Missing**: 
    *   `POST /auth/logout` (token revocation/blacklist).
    *   Role/Permission management endpoints (CRUD roles, assign permissions to users).
*   **API endpoint status**: Partially complete.
*   **Database/entity status**: Basic Identity exists.
*   **Priority**: P1 (Logout), P2 (Role management UI backend).
*   **Suggested fix**: Implement `Logout` endpoint. Implement Admin CRUD for users and roles.

## 2. Organization / HR Basic
*   **Expected**: Company, Department CRUD, Employee CRUD, Position CRUD, Org Chart.
*   **Existing**: `GET /departments`, `POST /departments`, `GET /employees`, `POST /employees`.
*   **Missing**:
    *   `GET /departments/{id}`, `PUT /departments/{id}`, `DELETE /departments/{id}`.
    *   `GET /employees/{id}`, `PUT /employees/{id}`, `DELETE /employees/{id}`.
    *   Position CRUD endpoints completely missing.
    *   Org chart / hierarchy retrieval endpoint.
*   **Priority**: P0 (Need GET by ID for detail pages, PUT for edits).
*   **Suggested fix**: Add missing standard CRUD endpoints for Departments and Employees. Implement Positions controller.

## 3. Finance
*   **Expected**: Budget CRUD, Categories, Payment Requests (w/ attachments, line items), Vendors, Wallets, Transactions.
*   **Existing**: `GET /budgets`, `POST /budgets`, `POST /budget-categories`, `POST /vendors`, `POST /wallets`, `POST /payment-requests`, `POST /payment-requests/{id}/submit`, `POST /transactions`.
*   **Missing**:
    *   `GET /budgets/{id}`, `PUT /budgets/{id}`.
    *   `GET /budget-categories` (to populate dropdowns).
    *   `GET /vendors` (to populate dropdowns).
    *   `GET /wallets` (to check balances).
    *   `GET /payment-requests`, `GET /payment-requests/{id}` (Critical for approval queue and history).
    *   `GET /transactions` (Transaction history).
    *   Attachment upload endpoint (e.g., `POST /finance/upload`).
*   **Priority**: P0 (Without GET endpoints, the frontend cannot display lists or details for workflows).
*   **Suggested fix**: Implement all missing `GET` endpoints to support frontend lists, dropdowns, and detail views.

## 4. KPI / OKR
*   **Expected**: Evaluation Period CRUD, Objective CRUD/Tree, Key Result CRUD, KPI CRUD, Check-ins, Approvals, Scorecard.
*   **Existing**: `POST /evaluation-periods`, `POST /objectives`, `POST /key-results`, `POST /kpis`, `POST /kpis/{id}/check-in`, `POST /check-ins/{id}/approve`, `POST /check-ins/{id}/reject`.
*   **Missing**:
    *   All `GET` endpoints: `GET /evaluation-periods`, `GET /objectives`, `GET /key-results`, `GET /kpis`.
    *   Endpoint for retrieving the OKR cascade/tree view.
    *   Endpoint for retrieving check-in history.
    *   Performance evaluation scorecard endpoint.
*   **Priority**: P0.
*   **Suggested fix**: Add `GET` endpoints for all entities. Create a specific endpoint for the OKR tree hierarchy.

## 5. Workflow
*   **Expected**: Template CRUD, Step CRUD, Instance creation, Approval Queue, Actions (Approve/Reject), Audit log.
*   **Existing**: `GET /approval-queue`, `POST /workflow-instances/{id}/approve`, `POST /workflow-instances/{id}/reject`.
*   **Missing**:
    *   Workflow Template and Step CRUD endpoints (currently hardcoded or missing management UI).
    *   `GET /workflow-instances/{id}` (to view workflow progress/timeline).
    *   Workflow audit log endpoint.
*   **Priority**: P1 (Need instance details for UI).
*   **Suggested fix**: Add endpoint to get workflow instance details (timeline, steps). Implement Template CRUD.

## 6. Dashboard
*   **Expected**: Overview metrics, financial summary, KPI summary, risk alerts, charts.
*   **Existing**: `GET /dashboard/overview`.
*   **Missing**:
    *   Detailed chart data endpoints (unless `overview` returns fully populated chart arrays, which is unlikely for optimal performance).
*   **Priority**: P1.
*   **Suggested fix**: Expand `DashboardController` with endpoints for specific charts (e.g., `GET /dashboard/cashflow-chart`).

## 7. Notification
*   **Expected**: Notification list, unread count, mark read, SignalR.
*   **Existing**: None. Controller is missing.
*   **Missing**: Entire Notification module API.
*   **Priority**: P1.
*   **Suggested fix**: Create `NotificationController` with GET list, mark-as-read, and unread-count endpoints.

## 8. AI Copilot
*   **Expected**: Chat sessions, chat UI, RAG, risk analysis, report generation, history.
*   **Existing**: `POST /ai/chat`, `POST /ai/risk-analysis`.
*   **Missing**:
    *   Chat session management (`GET /ai/sessions`, `POST /ai/sessions`, `GET /ai/sessions/{id}/messages`).
    *   Report generation endpoint.
*   **Priority**: P2.
*   **Suggested fix**: Add chat history/session management endpoints to support persistent conversations.

## 9. Reports / Export
*   **Expected**: Financial, KPI, Department reports, PDF/Excel export.
*   **Existing**: None.
*   **Missing**: All reporting endpoints.
*   **Priority**: P2.
*   **Suggested fix**: Create `ReportController` handling data aggregation and file generation.

## 10. Admin / System
*   **Expected**: User/Role management, system settings, audit logs.
*   **Existing**: `POST /admin/seed-data`, `GET /admin/health`.
*   **Missing**:
    *   User/Role CRUD.
    *   System audit log viewer.
*   **Priority**: P2.
*   **Suggested fix**: Add Admin endpoints for system-wide configurations and log viewing.

## Conclusion
The backend is approximately 30-40% complete regarding MVP API surface area. While core business logic (creating entities, advancing workflows) seems partially implemented, the system lacks the fundamental data retrieval APIs required to build a functional frontend dashboard and management application. 
For the frontend completion phase, extensive use of mock data or frontend-side temporary services will be required for any list or detail view, marked with `TODO_BACKEND_MISSING`.
