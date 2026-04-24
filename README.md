# OmniBiz AI

OmniBiz AI is a modular monolith for SME operations: finance, KPI/OKR, workflow approval, AI guardrails, dashboard, RBAC, audit-ready persistence, and a lightweight Next.js client.

The implemented database is **SQL Server** via EF Core SQL Server.

## Stack

- Backend: .NET 10, ASP.NET Core Web API, Clean Architecture layers
- Persistence: EF Core 10 + SQL Server 2022
- Auth: JWT Bearer + BCrypt password hashing + refresh tokens
- Frontend: Next.js 14 + React 18
- Tests: xUnit
- Containers: SQL Server 2022, Redis, backend, frontend

## Project Layout

```text
backend/
  src/OmniBizAI.Domain
  src/OmniBizAI.Application
  src/OmniBizAI.Infrastructure
  src/OmniBizAI.WebAPI
  tests/OmniBizAI.UnitTests
frontend/
docs/
docker-compose.yml
```

## Environment

Copy `.env.example` to `.env` and set strong values:

```powershell
Copy-Item .env.example .env
```

Required variables:

- `ConnectionStrings__DefaultConnection`
- `DB_PASSWORD` only when running the local SQL Server Docker profile
- `Jwt__Secret` minimum 32 characters
- `AllowedOrigins`
- `NEXT_PUBLIC_API_URL`

## Run Locally Without Docker

Start SQL Server locally, then:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=OmniBizDB;User Id=sa;Password=<password>;TrustServerCertificate=True;"
$env:Jwt__Secret="change-this-jwt-secret-at-least-32-characters"
dotnet ef database update --project backend\src\OmniBizAI.Infrastructure --startup-project backend\src\OmniBizAI.WebAPI
dotnet run --project backend\src\OmniBizAI.WebAPI -- --seed
dotnet run --project backend\src\OmniBizAI.WebAPI --urls http://localhost:5000
```

Frontend:

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:3000`.

## Docker

For a remote SQL Server, configure `.env` with the remote connection string first:

```env
ConnectionStrings__DefaultConnection=Server=s103-d186.interdata.vn,1433;Database=OmniBizDB;User Id=biz;Password=<password>;TrustServerCertificate=True;
```

Then run backend and frontend:

```powershell
docker compose up -d --build backend frontend
docker compose exec backend dotnet OmniBizAI.WebAPI.dll --seed
```

To use the local SQL Server container instead, set a strong `DB_PASSWORD` and enable the `local-db` profile:

```powershell
docker compose --profile local-db up -d --build sqlserver
docker compose --profile local-db up -d --build backend frontend
docker compose exec backend dotnet OmniBizAI.WebAPI.dll --seed
```

Backend API: `http://localhost:5000/api/v1`  
Frontend: `http://localhost:3000`  
Health: `http://localhost:5000/health`

## Test Accounts

All seeded accounts use:

```text
Test@123456
```

Emails:

- `admin@omnibiz.ai`
- `director@omnibiz.ai`
- `manager.mkt@omnibiz.ai`
- `manager.it@omnibiz.ai`
- `accountant@omnibiz.ai`
- `hr@omnibiz.ai`
- `staff.mkt@omnibiz.ai`
- `staff.sales@omnibiz.ai`

## Core Commands

```powershell
dotnet build backend\OmniBizAI.slnx -m:1
dotnet test backend\OmniBizAI.slnx -m:1 --no-build
npm run build --prefix frontend
```

Migration commands:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=OmniBizDB;User Id=sa;Password=<password>;TrustServerCertificate=True;"
dotnet ef migrations add <Name> --project backend\src\OmniBizAI.Infrastructure --startup-project backend\src\OmniBizAI.WebAPI --output-dir Data\Migrations
dotnet ef database update --project backend\src\OmniBizAI.Infrastructure --startup-project backend\src\OmniBizAI.WebAPI
```

## Implemented API Areas

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh-token`
- `GET /api/v1/auth/me`
- `GET/POST /api/v1/departments`
- `GET/POST /api/v1/employees`
- `GET/POST /api/v1/budgets`
- `POST /api/v1/budget-categories`
- `POST /api/v1/vendors`
- `POST /api/v1/wallets`
- `POST /api/v1/payment-requests`
- `POST /api/v1/payment-requests/{id}/submit`
- `POST /api/v1/transactions`
- `POST /api/v1/evaluation-periods`
- `POST /api/v1/objectives`
- `POST /api/v1/key-results`
- `POST /api/v1/kpis`
- `POST /api/v1/kpis/{id}/check-in`
- `POST /api/v1/check-ins/{id}/approve`
- `POST /api/v1/check-ins/{id}/reject`
- `GET /api/v1/approval-queue`
- `POST /api/v1/workflow-instances/{id}/approve`
- `POST /api/v1/workflow-instances/{id}/reject`
- `POST /api/v1/ai/chat`
- `POST /api/v1/ai/risk-analysis`
- `GET /api/v1/dashboard/overview`

## Design Notes

- SQL Server is the canonical database. EF migrations target SQL Server types and constraints.
- AI currently has a deterministic local rules fallback for risk analysis and dashboard Q&A, so the app runs without external LLM keys.
- The current MVP implements the most important P0/P1 flows and keeps interfaces ready for richer external AI, reporting, file storage, SignalR, and deeper data scoping.
