# 🗺️ OmniBiz AI — Architecture Diagram

> **Version**: 1.0 | **Updated**: 2026-04-24

---

## 1. System Architecture Overview (C4 - Context Level)

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                                    USERS                                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐     │
│  │ Director │ │ Manager  │ │Accountant│ │   HR     │ │  Staff   │ │  Admin   │     │
│  └─────┬────┘ └─────┬────┘ └─────┬────┘ └─────┬────┘ └─────┬────┘ └─────┬────┘     │
│        └────────────┴────────────┴─────────────┴────────────┴────────────┘           │
│                                        │ HTTPS                                       │
│                                        ▼                                             │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐ │
│  │                          LOAD BALANCER / REVERSE PROXY                          │ │
│  │                              (Nginx / Traefik)                                  │ │
│  └──────────────────────────────┬──────────────────────────────────────────────────┘ │
│                                 │                                                    │
│         ┌───────────────────────┼───────────────────────┐                           │
│         ▼                       ▼                       ▼                           │
│  ┌─────────────┐      ┌──────────────┐       ┌──────────────┐                      │
│  │  Next.js    │      │  ASP.NET     │       │  SignalR     │                      │
│  │  Frontend   │◄────►│  Core API    │       │  WebSocket   │                      │
│  │  (SSR/CSR)  │      │  (REST)      │       │  Hub         │                      │
│  └─────────────┘      └──────┬───────┘       └──────┬───────┘                      │
│                              │                       │                              │
│         ┌────────────────────┼───────────────────────┘                              │
│         ▼                    ▼                                                      │
│  ┌─────────────┐      ┌──────────────┐      ┌──────────────┐                       │
│  │  Redis      │      │ SQL Server   │      │  File        │                       │
│  │  Cache      │      │ + SQL Server   │      │  Storage     │                       │
│  └─────────────┘      └──────────────┘      └──────────────┘                       │
│                              │                                                      │
│                              ▼                                                      │
│                    ┌──────────────────┐                                             │
│                    │  EXTERNAL APIs   │                                             │
│                    │  ┌────────────┐  │                                             │
│                    │  │ Groq LLM   │  │                                             │
│                    │  │ OpenAI     │  │                                             │
│                    │  │ Google     │  │                                             │
│                    │  │ OAuth      │  │                                             │
│                    │  │ SMTP/Email │  │                                             │
│                    │  └────────────┘  │                                             │
│                    └──────────────────┘                                             │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Application Architecture (Container Level)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         FRONTEND CONTAINER                              │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                      Next.js App Router                           │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐   │  │
│  │  │Dashboard│ │Finance  │ │  KPI/   │ │Workflow │ │   AI    │   │  │
│  │  │  Pages  │ │ Pages   │ │  OKR    │ │ Pages   │ │ Copilot │   │  │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘   │  │
│  │  ┌─────────────────────────────────────────────────────────────┐ │  │
│  │  │              Shared Components Layer                        │ │  │
│  │  │  Charts │ Forms │ Tables │ Modals │ Sidebar │ Notifications│ │  │
│  │  └─────────────────────────────────────────────────────────────┘ │  │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐             │  │
│  │  │ React Query  │ │   Zustand    │ │  React Hook  │             │  │
│  │  │ (API State)  │ │ (UI State)   │ │  Form + Zod  │             │  │
│  │  └──────────────┘ └──────────────┘ └──────────────┘             │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │ REST API + SignalR
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         BACKEND CONTAINER                               │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                    ASP.NET Core Web API                            │  │
│  │  ┌───────────────────────────────────────────────────────────┐    │  │
│  │  │ Middleware: Auth → RateLimit → Logging → Exception       │    │  │
│  │  └───────────────────────────────────────────────────────────┘    │  │
│  │  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐        │  │
│  │  │Controllers│ │  SignalR   │ │Background │ │  Filters  │        │  │
│  │  │  (REST)   │ │   Hubs    │ │  Jobs     │ │  (Audit)  │        │  │
│  │  └─────┬─────┘ └───────────┘ └───────────┘ └───────────┘        │  │
│  │        │ MediatR                                                  │  │
│  │  ┌─────▼──────────────────────────────────────────────────────┐  │  │
│  │  │               APPLICATION SERVICES                          │  │  │
│  │  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐   │  │  │
│  │  │  │Finance │ │  KPI   │ │Workflow│ │   AI   │ │  Auth  │   │  │  │
│  │  │  │Handler │ │Handler │ │Handler │ │Handler │ │Handler │   │  │  │
│  │  │  └────────┘ └────────┘ └────────┘ └────────┘ └────────┘   │  │  │
│  │  └─────┬──────────────────────────────────────────────────────┘  │  │
│  │        │                                                          │  │
│  │  ┌─────▼──────────────────────────────────────────────────────┐  │  │
│  │  │               DOMAIN LAYER                                  │  │  │
│  │  │  Entities │ Value Objects │ Domain Events │ Business Rules  │  │  │
│  │  └─────┬──────────────────────────────────────────────────────┘  │  │
│  │        │                                                          │  │
│  │  ┌─────▼──────────────────────────────────────────────────────┐  │  │
│  │  │               INFRASTRUCTURE LAYER                          │  │  │
│  │  │  EF Core │ Redis │ AI Provider │ File Storage │ Email      │  │  │
│  │  └───────────────────────────────────────────────────────────┘   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Data Flow Diagrams

### 3.1 Payment Request Flow

```
Staff                    System                  Manager              Director          AI Service
  │                        │                       │                    │                   │
  │──Create Request──────►│                       │                    │                   │
  │                        │──AI Risk Check──────────────────────────────────────────────►│
  │                        │◄─────────Risk Score + Warnings──────────────────────────────│
  │                        │──Create Workflow──►│                       │                   │
  │                        │  Instance           │                       │                   │
  │                        │──Notify─────────►│                       │                   │
  │                        │                    │──Approve──────────►│                   │
  │                        │                    │  (if amount>50M)    │                   │
  │                        │                    │                     │──Approve──────►│  │
  │                        │◄───────────────────┴─────────────────────┘                   │
  │                        │──Create Transaction│                       │                   │
  │                        │──Update Budget────│                       │                   │
  │                        │──Update Dashboard─│                       │                   │
  │◄──Notification────────│                       │                    │                   │
```

### 3.2 AI Copilot Q&A Flow

```
User                     Frontend              Backend API            AI Service           Database
  │                        │                       │                    │                   │
  │──Ask Question────────►│                       │                    │                   │
  │  "Phòng nào vượt      │──POST /ai/chat──────►│                    │                   │
  │   ngân sách?"         │                       │──Check Permission──────────────────►│
  │                        │                       │◄──User Data Scope─────────────────│
  │                        │                       │──Query RAG──────►│                   │
  │                        │                       │                    │──Embed Query────►│
  │                        │                       │                    │◄──Similar Docs──│
  │                        │                       │                    │──Build Prompt──►│
  │                        │                       │                    │  (Context + Data)│
  │                        │                       │◄──AI Response─────│                   │
  │                        │◄──JSON Response──────│                    │                   │
  │◄──Render Answer───────│                       │                    │                   │
  │  (with citations      │                       │                    │                   │
  │   and charts)         │                       │                    │                   │
```

---

## 4. Deployment Architecture

### 4.1 Development Environment

```
Developer Machine (Docker Compose)
├── frontend-dev     (Next.js dev server, port 3000)
├── backend-dev      (ASP.NET Core, port 5000)
├── sqlserver-dev      (SQL Server 2022, port 1433)
├── redis-dev        (Redis 7, port 6379)
└── ssms          (Database management, port 8080)
```

### 4.2 Production Environment (VPS / Azure)

```
┌──────────────────────────────────────────────────────┐
│                     VPS / Azure VM                    │
│  ┌─────────────────────────────────────────────────┐ │
│  │              Docker Compose                      │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐      │ │
│  │  │  Nginx   │  │ Frontend │  │ Backend  │      │ │
│  │  │  Proxy   │─►│ Next.js  │  │ .NET API │      │ │
│  │  │  :80/443 │  │  :3000   │  │  :5000   │      │ │
│  │  └──────────┘  └──────────┘  └──────────┘      │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐      │ │
│  │  │SQL Server│  │  Redis   │  │  Volumes │      │ │
│  │  │  :1433   │  │  :6379   │  │ (files)  │      │ │
│  │  └──────────┘  └──────────┘  └──────────┘      │ │
│  └─────────────────────────────────────────────────┘ │
│                                                      │
│  ┌────────────────────────────┐                      │
│  │  Monitoring & Logging      │                      │
│  │  ┌─────────┐ ┌──────────┐ │                      │
│  │  │ Serilog │ │ Health   │ │                      │
│  │  │ + Seq   │ │ Checks   │ │                      │
│  │  └─────────┘ └──────────┘ │                      │
│  └────────────────────────────┘                      │
└──────────────────────────────────────────────────────┘
```

### 4.3 CI/CD Pipeline

```
┌──────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│ Push │───►│  GitHub   │───►│  Build   │───►│  Test    │───►│  Deploy  │
│ to   │    │  Actions  │    │  Docker  │    │  Unit +  │    │  to VPS  │
│ main │    │  Trigger  │    │  Images  │    │  Integ.  │    │  via SSH │
└──────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘
                                                                  │
                                                            ┌─────▼─────┐
                                                            │  Docker   │
                                                            │  Compose  │
                                                            │  Pull &   │
                                                            │  Restart  │
                                                            └───────────┘
```

---

## 5. Security Architecture

```
┌───────────────────────────────────────────────────────────────────────┐
│                        SECURITY LAYERS                                │
│                                                                       │
│  Layer 1: NETWORK                                                    │
│  ├── HTTPS/TLS 1.3 (Let's Encrypt)                                  │
│  ├── Firewall: Only 80/443 open                                     │
│  └── Rate Limiting: 100 req/min per IP                               │
│                                                                       │
│  Layer 2: APPLICATION                                                │
│  ├── JWT Authentication (RS256)                                      │
│  ├── RBAC Authorization (6 roles)                                    │
│  ├── Data Scope Filtering (per user context)                         │
│  ├── Input Validation (FluentValidation)                             │
│  ├── CORS Policy (whitelist origins)                                 │
│  └── CSRF Protection (anti-forgery tokens)                           │
│                                                                       │
│  Layer 3: DATA                                                       │
│  ├── Password Hashing (bcrypt, cost 12)                              │
│  ├── Sensitive Data Encryption (AES-256)                             │
│  ├── SQL Injection Prevention (EF Core parameterized queries)        │
│  ├── XSS Prevention (output encoding)                                │
│  └── Audit Log (immutable, all actions)                              │
│                                                                       │
│  Layer 4: AI SECURITY                                                │
│  ├── Prompt Injection Protection (input sanitization)                │
│  ├── Data Scope Enforcement (user can only query own data)           │
│  ├── Output Filtering (no PII in AI responses)                       │
│  └── Rate Limiting AI calls (10 req/min per user)                    │
└───────────────────────────────────────────────────────────────────────┘
```
