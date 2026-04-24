# 🚀 OmniBiz AI — DevOps / Deployment Document

> **Version**: 1.0 | **Updated**: 2026-04-24

---

## 1. Environment Setup

### 1.1 Environments

| Environment | Purpose | URL | Database | Deployment |
|------------|---------|-----|----------|-----------|
| **Local Dev** | Individual development | localhost:3000 / :5000 | Docker PostgreSQL | Docker Compose |
| **Staging** | Team testing, demo prep | staging.omnibiz.ai | Staging DB | Auto-deploy on `develop` |
| **Production** | Live demo, evaluation | omnibiz.ai | Production DB | Manual deploy on `main` |

### 1.2 Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| Docker Desktop | 24.x+ | Container runtime |
| Docker Compose | v2.x+ | Multi-container orchestration |
| .NET SDK | 10.0 | Backend build |
| Node.js | 20.x LTS | Frontend build |
| Git | 2.40+ | Version control |
| VS Code / Rider | Latest | IDE |

---

## 2. Docker Configuration

### 2.1 docker-compose.yml

```yaml
version: '3.8'

services:
  # ========== DATABASE ==========
  postgres:
    image: pgvector/pgvector:pg16
    container_name: omnibiz-postgres
    ports:
      - "5432:5432"
    environment:
      POSTGRES_DB: omnibiz_db
      POSTGRES_USER: omnibiz_user
      POSTGRES_PASSWORD: ${DB_PASSWORD:-OmniBiz@2026}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init.sql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U omnibiz_user -d omnibiz_db"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  # ========== CACHE ==========
  redis:
    image: redis:7-alpine
    container_name: omnibiz-redis
    ports:
      - "6379:6379"
    command: redis-server --requirepass ${REDIS_PASSWORD:-OmniBizRedis@2026}
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "--no-auth-warning", "-a", "${REDIS_PASSWORD:-OmniBizRedis@2026}", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  # ========== BACKEND ==========
  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: omnibiz-backend
    ports:
      - "5000:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: ${ENVIRONMENT:-Development}
      ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=omnibiz_db;Username=omnibiz_user;Password=${DB_PASSWORD:-OmniBiz@2026}"
      Redis__ConnectionString: "redis:6379,password=${REDIS_PASSWORD:-OmniBizRedis@2026}"
      Jwt__Secret: ${JWT_SECRET:-your-256-bit-secret-key-change-in-production}
      Jwt__Issuer: ${JWT_ISSUER:-OmniBizAI}
      Jwt__Audience: ${JWT_AUDIENCE:-OmniBizAI}
      AI__Provider: ${AI_PROVIDER:-Groq}
      AI__ApiKey: ${AI_API_KEY}
      AI__Model: ${AI_MODEL:-llama-3.3-70b-versatile}
      AllowedOrigins: ${ALLOWED_ORIGINS:-http://localhost:3000}
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    restart: unless-stopped

  # ========== FRONTEND ==========
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
      args:
        NEXT_PUBLIC_API_URL: ${API_URL:-http://localhost:5000/api/v1}
        NEXT_PUBLIC_WS_URL: ${WS_URL:-http://localhost:5000/hubs}
    container_name: omnibiz-frontend
    ports:
      - "3000:3000"
    environment:
      NEXT_PUBLIC_API_URL: ${API_URL:-http://localhost:5000/api/v1}
    depends_on:
      - backend
    restart: unless-stopped

  # ========== DB ADMIN ==========
  pgadmin:
    image: dpage/pgadmin4:latest
    container_name: omnibiz-pgadmin
    ports:
      - "8080:80"
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@omnibiz.ai
      PGADMIN_DEFAULT_PASSWORD: admin
    depends_on:
      - postgres
    profiles:
      - dev

volumes:
  postgres_data:
  redis_data:
```

### 2.2 Backend Dockerfile

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.sln ./
COPY src/OmniBizAI.Domain/*.csproj src/OmniBizAI.Domain/
COPY src/OmniBizAI.Application/*.csproj src/OmniBizAI.Application/
COPY src/OmniBizAI.Infrastructure/*.csproj src/OmniBizAI.Infrastructure/
COPY src/OmniBizAI.WebAPI/*.csproj src/OmniBizAI.WebAPI/
RUN dotnet restore

COPY src/ src/
WORKDIR /src/src/OmniBizAI.WebAPI
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "OmniBizAI.WebAPI.dll"]
```

### 2.3 Frontend Dockerfile

```dockerfile
# Stage 1: Build
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
RUN npm run build

# Stage 2: Runtime
FROM node:20-alpine AS runtime
WORKDIR /app

RUN adduser -D appuser
USER appuser

COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public

EXPOSE 3000
ENV PORT=3000 HOSTNAME="0.0.0.0"
CMD ["node", "server.js"]
```

---

## 3. CI/CD Pipeline (GitHub Actions)

### 3.1 CI Pipeline (.github/workflows/ci.yml)

```yaml
name: CI Pipeline

on:
  push:
    branches: [develop, main]
  pull_request:
    branches: [develop, main]

jobs:
  backend-build-test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: pgvector/pgvector:pg16
        env:
          POSTGRES_DB: omnibiz_test
          POSTGRES_USER: test_user
          POSTGRES_PASSWORD: test_pass
        ports: ['5432:5432']
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
        working-directory: backend
      - run: dotnet build --no-restore
        working-directory: backend
      - run: dotnet test --no-build --collect:"XPlat Code Coverage"
        working-directory: backend
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Port=5432;Database=omnibiz_test;Username=test_user;Password=test_pass"

  frontend-build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
        working-directory: frontend
      - run: npm run lint
        working-directory: frontend
      - run: npm run test
        working-directory: frontend
      - run: npm run build
        working-directory: frontend
```

### 3.2 CD Pipeline (.github/workflows/deploy.yml)

```yaml
name: Deploy

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    needs: [backend-build-test, frontend-build-test]
    steps:
      - uses: actions/checkout@v4

      - name: Build and push Docker images
        run: |
          docker build -t omnibiz-backend ./backend
          docker build -t omnibiz-frontend ./frontend

      - name: Deploy to VPS
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USER }}
          key: ${{ secrets.VPS_SSH_KEY }}
          script: |
            cd /opt/omnibiz
            git pull origin main
            docker compose pull
            docker compose up -d --build
            docker compose exec backend dotnet ef database update
            echo "Deployment completed at $(date)"
```

---

## 4. Secrets Management

### 4.1 Required Secrets (GitHub Secrets / .env)

| Secret | Description | Example |
|--------|-------------|---------|
| `DB_PASSWORD` | PostgreSQL password | `Str0ngP@ssw0rd!` |
| `REDIS_PASSWORD` | Redis password | `R3d1sP@ss!` |
| `JWT_SECRET` | JWT signing key (min 256-bit) | `base64-encoded-key` |
| `AI_API_KEY` | Groq/OpenAI API key | `gsk_xxx...` |
| `VPS_HOST` | Server IP/domain | `omnibiz.ai` |
| `VPS_USER` | SSH username | `deploy` |
| `VPS_SSH_KEY` | SSH private key | `-----BEGIN OPENSSH...` |

### 4.2 .env Template

```env
# Database
DB_PASSWORD=OmniBiz@2026
REDIS_PASSWORD=OmniBizRedis@2026

# JWT
JWT_SECRET=your-256-bit-secret-key-CHANGE-THIS-IN-PRODUCTION
JWT_ISSUER=OmniBizAI
JWT_AUDIENCE=OmniBizAI

# AI
AI_PROVIDER=Groq
AI_API_KEY=gsk_your_api_key_here
AI_MODEL=llama-3.3-70b-versatile

# URLs
API_URL=http://localhost:5000/api/v1
WS_URL=http://localhost:5000/hubs
ALLOWED_ORIGINS=http://localhost:3000

# Environment
ENVIRONMENT=Development
```

---

## 5. Database Operations

### 5.1 Migrations

```bash
# Create migration
cd backend/src/OmniBizAI.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../OmniBizAI.WebAPI

# Apply migration
dotnet ef database update --startup-project ../OmniBizAI.WebAPI

# Generate SQL script (for staging/prod)
dotnet ef migrations script --idempotent -o migration.sql --startup-project ../OmniBizAI.WebAPI

# Rollback
dotnet ef database update <PreviousMigrationName> --startup-project ../OmniBizAI.WebAPI
```

### 5.2 Backup & Restore

```bash
# Backup
docker exec omnibiz-postgres pg_dump -U omnibiz_user -d omnibiz_db -F c -f /tmp/backup.dump
docker cp omnibiz-postgres:/tmp/backup.dump ./backups/backup_$(date +%Y%m%d).dump

# Restore
docker cp ./backups/backup_20260501.dump omnibiz-postgres:/tmp/
docker exec omnibiz-postgres pg_restore -U omnibiz_user -d omnibiz_db -c /tmp/backup_20260501.dump

# Automated daily backup (cron)
0 2 * * * /opt/omnibiz/scripts/backup.sh >> /var/log/omnibiz-backup.log 2>&1
```

### 5.3 Seed Data

```bash
# Seed demo data (idempotent - safe to run multiple times)
docker exec omnibiz-backend dotnet OmniBizAI.WebAPI.dll --seed

# Or via API (Admin only)
curl -X POST http://localhost:5000/api/v1/admin/seed-data \
  -H "Authorization: Bearer <admin-token>"
```

---

## 6. Monitoring & Health Checks

### 6.1 Health Check Endpoints

| Endpoint | Checks | Response |
|----------|--------|----------|
| `/health` | Basic alive check | 200 OK |
| `/health/ready` | DB + Redis connectivity | 200 OK or 503 |
| `/health/live` | Process running | 200 OK |

### 6.2 Logging

| Component | Tool | Output |
|-----------|------|--------|
| Backend | Serilog | Console + File (/var/log/omnibiz/) |
| Frontend | Next.js built-in | Console + File |
| Nginx | Access + Error logs | /var/log/nginx/ |
| PostgreSQL | Built-in | Docker logs |

### 6.3 Monitoring Dashboard (Optional)

```yaml
# docker-compose.monitoring.yml (optional profile)
services:
  seq:
    image: datalust/seq:latest
    container_name: omnibiz-seq
    ports:
      - "5341:80"
    environment:
      ACCEPT_EULA: "Y"
    volumes:
      - seq_data:/data
    profiles:
      - monitoring
```

---

## 7. Rollback Plan

### 7.1 Rollback Procedures

| Scenario | Action |
|----------|--------|
| **Bad code deploy** | `git revert` + redeploy, or `docker compose up -d --build` with previous tag |
| **Bad migration** | Run `dotnet ef database update <PreviousMigration>` |
| **Data corruption** | Restore from latest backup |
| **Service crash** | Docker auto-restart (unless-stopped), check logs |
| **Full system failure** | Restore VPS from snapshot + restore DB backup |

### 7.2 Docker Image Tagging

```bash
# Tag releases
docker tag omnibiz-backend:latest omnibiz-backend:v1.0.0
docker tag omnibiz-frontend:latest omnibiz-frontend:v1.0.0

# Rollback to specific version
docker compose down
# Update docker-compose.yml to use :v1.0.0 tag
docker compose up -d
```

---

## 8. Quick Start Guide

### 8.1 First-time Setup

```bash
# 1. Clone repository
git clone https://github.com/team/omnibiz-ai.git
cd omnibiz-ai

# 2. Copy environment file
cp .env.example .env
# Edit .env with your AI API key

# 3. Start all services
docker compose up -d

# 4. Wait for services to be healthy
docker compose ps

# 5. Run database migrations
docker compose exec backend dotnet ef database update

# 6. Seed demo data
docker compose exec backend dotnet OmniBizAI.WebAPI.dll --seed

# 7. Access the application
# Frontend: http://localhost:3000
# Backend API: http://localhost:5000/api/v1
# PgAdmin: http://localhost:8080 (dev profile)

# 8. Login with default admin
# Email: admin@omnibiz.ai
# Password: Test@123456
```

### 8.2 Daily Development

```bash
# Start services
docker compose up -d postgres redis

# Run backend (hot reload)
cd backend/src/OmniBizAI.WebAPI
dotnet watch run

# Run frontend (hot reload)  
cd frontend
npm run dev
```
