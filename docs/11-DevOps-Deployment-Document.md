# 🚀 OmniBiz AI — DevOps / Deployment Document

> **Version**: 1.0 | **Updated**: 2026-04-24

---

## 1. Environment Setup

### 1.1 Environments

| Environment | Purpose | URL | Database | Deployment |
|------------|---------|-----|----------|-----------|
| **Local Dev** | Individual development | localhost:5000 | Docker SQL Server | Docker Compose |
| **Staging** | Team testing, demo prep | staging.omnibiz.ai | Staging DB | Auto-deploy on `develop` |
| **Production** | Live demo, evaluation | omnibiz.ai | Production DB | Manual deploy on `main` |

### 1.2 Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| Docker Desktop | 24.x+ | Container runtime |
| Docker Compose | v2.x+ | Multi-container orchestration |
| .NET SDK | 10.0 | ASP.NET Core MVC build |
| Git | 2.40+ | Version control |
| VS Code / Rider | Latest | IDE |

---

## 2. Docker Configuration

### 2.1 docker-compose.yml

```yaml
version: '3.8'

services:
  # ========== DATABASE ==========
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: omnibiz-sqlserver
    ports:
      - "1433:1433"
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: ${DB_PASSWORD:-OmniBiz@2026!}
      MSSQL_PID: Developer
    volumes:
      - sqlserver_data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${DB_PASSWORD:-OmniBiz@2026!}' -Q 'SELECT 1' -C -No"]
      interval: 10s
      timeout: 5s
      retries: 10
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

  # ========== WEB APP ==========
  web:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: omnibiz-web
    ports:
      - "5000:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: ${ENVIRONMENT:-Development}
      ConnectionStrings__DefaultConnection: "Server=sqlserver,1433;Database=OmniBizDB;User Id=sa;Password=${DB_PASSWORD:-OmniBiz@2026!};TrustServerCertificate=True;"
      Redis__ConnectionString: "redis:6379,password=${REDIS_PASSWORD:-OmniBizRedis@2026}"
      Jwt__Secret: ${JWT_SECRET:-your-256-bit-secret-key-change-in-production}
      Jwt__Issuer: ${JWT_ISSUER:-OmniBizAI}
      Jwt__Audience: ${JWT_AUDIENCE:-OmniBizAI}
      AI__Provider: ${AI_PROVIDER:-Groq}
      AI__ApiKey: ${AI_API_KEY}
      AI__Model: ${AI_MODEL:-llama-3.3-70b-versatile}
      AllowedHosts: ${ALLOWED_HOSTS:-localhost}
    depends_on:
      sqlserver:
        condition: service_healthy
      redis:
        condition: service_healthy
    restart: unless-stopped

  # ========== DB ADMIN (Azure Data Studio web — optional) ==========
  # Note: Use Azure Data Studio or SSMS installed locally to manage SQL Server
  # Connect to localhost,1433 with sa / ${DB_PASSWORD}

volumes:
  sqlserver_data:
  redis_data:
```

### 2.2 ASP.NET Core MVC Dockerfile

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.sln ./
COPY src/OmniBizAI.Domain/*.csproj src/OmniBizAI.Domain/
COPY src/OmniBizAI.Application/*.csproj src/OmniBizAI.Application/
COPY src/OmniBizAI.Infrastructure/*.csproj src/OmniBizAI.Infrastructure/
COPY src/OmniBizAI.Web/*.csproj src/OmniBizAI.Web/
RUN dotnet restore

COPY src/ src/
WORKDIR /src/src/OmniBizAI.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "OmniBizAI.Web.dll"]
```

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
  web-build-test:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: 'Y'
          MSSQL_SA_PASSWORD: 'Test@Pass123!'
          MSSQL_PID: Developer
        ports: ['1433:1433']
        options: >-
          --health-cmd "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Test@Pass123!' -Q 'SELECT 1' -C -No"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 10
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
          ConnectionStrings__DefaultConnection: "Server=localhost,1433;Database=OmniBizTest;User Id=sa;Password=Test@Pass123!;TrustServerCertificate=True;"

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
    steps:
      - uses: actions/checkout@v4

      - name: Build and push Docker images
        run: |
          docker build -t omnibiz-web ./backend

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
            docker compose exec web dotnet ef database update
            echo "Deployment completed at $(date)"
```

---

## 4. Secrets Management

### 4.1 Required Secrets (GitHub Secrets / .env)

| Secret | Description | Example |
|--------|-------------|---------|
| `DB_PASSWORD` | SQL Server password | `Str0ngP@ssw0rd!` |
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
WS_URL=http://localhost:5000/hubs
ALLOWED_HOSTS=localhost

# Environment
ENVIRONMENT=Development
```

---

## 5. Database Operations

### 5.1 Migrations

```bash
# Create migration
cd backend/src/OmniBizAI.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../OmniBizAI.Web

# Apply migration
dotnet ef database update --startup-project ../OmniBizAI.Web

# Generate SQL script (for staging/prod)
dotnet ef migrations script --idempotent -o migration.sql --startup-project ../OmniBizAI.Web

# Rollback
dotnet ef database update <PreviousMigrationName> --startup-project ../OmniBizAI.Web
```

### 5.2 Backup & Restore

```bash
# Backup (SQL Server .bak)
docker exec omnibiz-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'OmniBiz@2026!' -C -No \
  -Q "BACKUP DATABASE [OmniBizDB] TO DISK='/var/opt/mssql/backup/OmniBizDB_$(date +%Y%m%d).bak'"
docker cp omnibiz-sqlserver:/var/opt/mssql/backup/ ./backups/

# Restore
docker cp ./backups/OmniBizDB_20260501.bak omnibiz-sqlserver:/var/opt/mssql/backup/
docker exec omnibiz-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'OmniBiz@2026!' -C -No \
  -Q "RESTORE DATABASE [OmniBizDB] FROM DISK='/var/opt/mssql/backup/OmniBizDB_20260501.bak' WITH REPLACE"

# Automated daily backup (cron)
0 2 * * * /opt/omnibiz/scripts/backup-sqlserver.sh >> /var/log/omnibiz-backup.log 2>&1
```

### 5.3 Seed Data

```bash
# Seed demo data (idempotent - safe to run multiple times)
docker exec omnibiz-web dotnet OmniBizAI.Web.dll --seed

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
| ASP.NET Core MVC app | Serilog | Console + File (/var/log/omnibiz/) |
| Nginx | Access + Error logs | /var/log/nginx/ |
| SQL Server | Built-in | Docker logs |

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
docker tag omnibiz-web:latest omnibiz-web:v1.0.0

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
docker compose exec web dotnet ef database update

# 6. Seed demo data
docker compose exec web dotnet OmniBizAI.Web.dll --seed

# 7. Access the application
# Web app: http://localhost:5000
# JSON endpoints: http://localhost:5000/api/v1
# SQL Server: localhost,1433 (use SSMS or Azure Data Studio)

# 8. Login with default admin
# Email: admin@omnibiz.ai
# Password: Test@123456
```

### 8.2 Daily Development

```bash
# Start services
docker compose up -d sqlserver redis

# Run ASP.NET Core MVC app (hot reload)
cd backend/src/OmniBizAI.Web
dotnet watch run
```
