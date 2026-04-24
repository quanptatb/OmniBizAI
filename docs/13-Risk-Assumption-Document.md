# OmniBiz AI — Risk & Assumption Document

> **Version**: 1.0 | **Updated**: 2026-04-24

---

## 1. Risk Register

### 1.1 Technical Risks

| ID    | Risk                                          | Probability | Impact | Severity    | Mitigation                                                                        | Contingency                                                  |
| ----- | --------------------------------------------- | ----------- | ------ | ----------- | --------------------------------------------------------------------------------- | ------------------------------------------------------------ |
| TR-01 | **LLM API unavailable / rate limited**  | Medium      | High   | 🔴 Critical | Multi-provider strategy (Groq + OpenAI fallback). Cache AI responses.             | Pre-generate demo responses, show cached results during demo |
| TR-02 | **AI response quality inconsistent**    | Medium      | Medium | 🟡 High     | Fine-tune prompts, use structured output format, validate against test cases      | Manual review AI outputs, curate demo Q&A pairs              |
| TR-03 | **Database performance with 61 tables** | Low         | High   | 🟡 High     | Proper indexing strategy, query optimization, Redis caching                       | Simplify aggregation queries, use materialized views         |
| TR-04 | **EF Core migration conflicts**         | Medium      | Medium | 🟡 High     | One person manages migrations at a time, rebase before merge                      | Manual SQL script as backup                                  |
| TR-05 | **Complex workflow engine bugs**        | High        | High   | 🔴 Critical | Start with simple 2-step workflow, add complexity gradually, extensive unit tests | Fallback to hardcoded approval flows                         |
| TR-06 | **SignalR WebSocket issues**            | Low         | Low    | 🟢 Medium   | Use SignalR with fallback to long-polling                                         | Polling-based notification refresh                           |
| TR-07 | **Docker image build failures**         | Low         | Medium | 🟢 Medium   | Multi-stage builds, lock dependency versions, CI validates build                  | Build locally, deploy manually                               |
| TR-08 | **pgvector performance for RAG**        | Medium      | Medium | 🟡 High     | Use HNSW index, limit vector dimensions, batch embedding                          | Reduce RAG scope, use keyword search fallback                |
| TR-09 | **Cross-browser compatibility**         | Low         | Low    | 🟢 Medium   | Test on Chrome, Firefox, Safari. Use modern CSS features with fallbacks           | Focus on Chrome for demo                                     |
| TR-10 | **API security vulnerabilities**        | Medium      | High   | 🔴 Critical | Follow OWASP guidelines, input validation, RBAC enforcement, code review          | Security scan before demo, fix critical issues               |

### 1.2 Product Risks

| ID    | Risk                                               | Probability | Impact | Severity    | Mitigation                                                              | Contingency                                     |
| ----- | -------------------------------------------------- | ----------- | ------ | ----------- | ----------------------------------------------------------------------- | ----------------------------------------------- |
| PR-01 | **Scope creep** — thêm feature ngoài MVP  | High        | High   | 🔴 Critical | Freeze feature list after Week 2, reject out-of-scope requests          | Cut non-essential features (P2), focus on P0/P1 |
| PR-02 | **Demo data không realistic**               | Medium      | High   | 🟡 High     | Seed data based on real SME scenarios, review with domain expert        | Prepare backup demo script with scripted data   |
| PR-03 | **UI/UX chưa đủ polish**                  | Medium      | Medium | 🟡 High     | Use established component library, dedicate Week 10-11 for polish       | Focus polish on demo screens only               |
| PR-04 | **Business rules phức tạp hơn dự kiến** | Medium      | Medium | 🟡 High     | Document rules early, validate with stakeholders, iterative development | Simplify rules for MVP                          |
| PR-05 | **User flow quá nhiều bước**             | Low         | Medium | 🟢 Medium   | Optimize UX with defaults, auto-fill, quick actions                     | Demo with pre-filled data                       |
| PR-06 | **Báo cáo/Dashboard dữ liệu sai**        | Medium      | High   | 🔴 Critical | Unit test all aggregation logic, cross-verify with raw data             | Manual verification before demo                 |

### 1.3 Project Management Risks

| ID    | Risk                                            | Probability | Impact | Severity | Mitigation                                                  | Contingency                                         |
| ----- | ----------------------------------------------- | ----------- | ------ | -------- | ----------------------------------------------------------- | --------------------------------------------------- |
| PM-01 | **Team member absent / unavailable**      | Medium      | High   | 🟡 High  | Cross-training, documented tasks, pair programming          | Reassign tasks, reduce scope                        |
| PM-02 | **Skill gap (unfamiliar tech)**           | Medium      | Medium | 🟡 High  | Training sessions Week 1-2, code review, documentation      | Choose simpler alternatives                         |
| PM-03 | **Integration conflicts between modules** | High        | Medium | 🟡 High  | Clear API contracts, integration tests, daily standups      | Dedicated integration sprint W10                    |
| PM-04 | **Deadline pressure causes tech debt**    | High        | Low    | 🟡 High  | Code review, enforce conventions, refactor time in schedule | Accept some tech debt for MVP, document for Phase 2 |
| PM-05 | **Communication gaps**                    | Medium      | Medium | 🟡 High  | Daily standup, shared Notion/Jira, PR reviews               | Weekly sync meetings                                |

### 1.4 Infrastructure & External Risks

| ID    | Risk                                          | Probability | Impact   | Severity    | Mitigation                                           | Contingency                                        |
| ----- | --------------------------------------------- | ----------- | -------- | ----------- | ---------------------------------------------------- | -------------------------------------------------- |
| IR-01 | **VPS/Cloud downtime during demo**      | Low         | Critical | 🔴 Critical | Health monitoring, auto-restart, backup VPS          | Run demo from localhost, pre-recorded video backup |
| IR-02 | **LLM API cost overrun**                | Medium      | Medium   | 🟡 High     | Rate limiting, token budget, use free-tier models    | Switch to cheaper model, reduce AI features        |
| IR-03 | **SSL certificate issues**              | Low         | Medium   | 🟢 Medium   | Let's Encrypt auto-renewal, test before demo         | HTTP fallback for demo (not ideal)                 |
| IR-04 | **Internet connectivity at demo venue** | Medium      | High     | 🔴 Critical | Test venue connectivity, prepare mobile hotspot      | Local demo environment, pre-recorded video         |
| IR-05 | **GitHub/npm/NuGet outage**             | Low         | Medium   | 🟢 Medium   | Local cache of dependencies, Docker images pre-built | Use cached dependencies                            |

---

## 2. Risk Severity Matrix

```
              Low Impact    Medium Impact    High Impact    Critical Impact
High Prob   │   🟢 Low    │   🟡 High     │   🔴 Critical │   🔴 Critical  │
Med Prob    │   🟢 Low    │   🟡 High     │   🟡 High     │   🔴 Critical  │
Low Prob    │   ⚪ Minimal │   🟢 Medium   │   🟡 High     │   🟡 High      │
```

### Top 5 Critical Risks to Watch

1. 🔴 **TR-01**: LLM API unavailability → Multi-provider + cached fallback
2. 🔴 **TR-05**: Workflow engine bugs → Start simple, test extensively
3. 🔴 **PR-01**: Scope creep → Feature freeze after Week 2
4. 🔴 **PR-06**: Dashboard data accuracy → Unit test all calculations
5. 🔴 **IR-01**: Demo infrastructure down → Localhost backup + video

---

## 3. Assumptions

### 3.1 Technical Assumptions

| ID    | Assumption                                           | Validation Method             | Impact if Wrong                            |
| ----- | ---------------------------------------------------- | ----------------------------- | ------------------------------------------ |
| TA-01 | Team members có kỹ năng .NET Core cơ bản        | Self-assessment survey Week 1 | Cần training sessions, delay M2           |
| TA-02 | Team members có kỹ năng React/Next.js cơ bản    | Self-assessment survey Week 1 | Assign more backend-focused tasks          |
| TA-03 | PostgreSQL 16 + pgvector đủ performance cho MVP    | Load test Week 10             | Switch to dedicated vector DB              |
| TA-04 | Groq free tier đủ cho development + demo           | Monitor usage daily           | Switch to OpenAI paid, budget $20          |
| TA-05 | Docker Desktop available trên tất cả dev machines | Verify Week 1                 | Cài đặt hỗ trợ, hoặc dùng cloud dev |
| TA-06 | .NET 10 SDK stable cho production                    | Test early Week 1             | Downgrade to .NET 8 LTS                    |
| TA-07 | Redis adequate cho caching MVP volume                | Performance test              | In-memory cache fallback                   |
| TA-08 | Single database instance đủ cho MVP                | Monitor connections           | Connection pooling tuning                  |

### 3.2 Product Assumptions

| ID    | Assumption                                           | Validation Method                    | Impact if Wrong                  |
| ----- | ---------------------------------------------------- | ------------------------------------ | -------------------------------- |
| PA-01 | SME cần quản lý tài chính + KPI tích hợp      | Market research, competitor analysis | Pivot focus to strongest module  |
| PA-02 | 6 user roles đủ cover các persona                 | Wireframe review with peers          | Add/merge roles                  |
| PA-03 | Tiếng Việt là ngôn ngữ duy nhất cho MVP        | Scope document agreement             | Add i18n framework early         |
| PA-04 | VND là currency duy nhất cho MVP                   | Scope document agreement             | Add multi-currency support       |
| PA-05 | Không cần tích hợp bank/tax/accounting thực     | Scope document agreement             | Mock integration points          |
| PA-06 | Dashboard metrics dùng 5 loại chart là đủ       | Wireframe review                     | Add more chart types             |
| PA-07 | AI Q&A bằng tiếng Việt hoạt động tốt với LLM | Test with sample queries Week 8      | Add English fallback             |
| PA-08 | Demo data realistic đủ thuyết phục hội đồng   | Review with advisor                  | Enhance data, add more scenarios |

### 3.3 Process Assumptions

| ID    | Assumption                             | Validation Method          | Impact if Wrong               |
| ----- | -------------------------------------- | -------------------------- | ----------------------------- |
| XA-01 | Team có thể họp standup hàng ngày | Schedule confirmation      | Async updates via chat        |
| XA-02 | Code review hoàn thành trong 24h     | Track PR aging             | Escalation process            |
| XA-03 | Git Flow branching strategy phù hợp  | Team feedback after Week 3 | Simplify to trunk-based       |
| XA-04 | CI/CD pipeline chạy < 10 phút        | Measure after setup        | Optimize build, parallel jobs |
| XA-05 | Demo venue có internet ổn định     | Pre-visit venue            | Prepare offline demo          |

---

## 4. Dependencies

### 4.1 External Dependencies

| Dependency           | Type                | Risk Level | Alternative                    |
| -------------------- | ------------------- | ---------- | ------------------------------ |
| Groq API             | AI inference        | Medium     | OpenAI / Claude / Ollama local |
| PostgreSQL           | Database            | Low        | SQL Server (team familiar)     |
| Redis                | Cache               | Low        | In-memory cache                |
| GitHub               | Source control + CI | Low        | GitLab / Bitbucket             |
| npm registry         | Frontend packages   | Low        | Yarn berry offline cache       |
| NuGet                | Backend packages    | Low        | Local package cache            |
| Docker Hub           | Container images    | Low        | Pre-pull images                |
| Google Fonts (Inter) | Typography          | Low        | System fonts fallback          |
| Let's Encrypt        | SSL cert            | Low        | Self-signed for demo           |

### 4.2 Internal Dependencies

```
Auth/RBAC (Week 3)
  ↓ blocks
HR Module (Week 4)  ←────→  Finance Module (Week 3-5)  ←────→  KPI Module (Week 3-5)
  ↓                            ↓                                   ↓
  └──────── all block ─────────┤                                   │
                               ↓                                   ↓
                         Workflow Engine (Week 6-7)  ←─────────────┘
                               ↓
                         Dashboard + Notifications (Week 6-7)
                               ↓
                         AI Layer (Week 8-9)    ← needs all data modules
                               ↓
                         Testing & Hardening (Week 10-11)
                               ↓
                         Demo (Week 12)
```

---

## 5. Risk Response Actions Summary

| Response Type                           | Count | Examples                                       |
| --------------------------------------- | ----- | ---------------------------------------------- |
| **Mitigate** (reduce probability) | 15    | Multi-provider AI, unit tests, code review     |
| **Contingency** (plan if happens) | 12    | Localhost demo, cached AI, simplified workflow |
| **Accept** (low impact, monitor)  | 5     | Browser compat, minor tech debt                |
| **Avoid** (eliminate source)      | 3     | Feature freeze, lock dependency versions       |
| **Transfer** (to third party)     | 1     | SSL via Let's Encrypt                          |

---

## 6. Monitoring & Escalation

### 6.1 Risk Monitoring Frequency

| Severity    | Review Frequency     | Escalation              |
| ----------- | -------------------- | ----------------------- |
| 🔴 Critical | Daily standup        | Immediate team meeting  |
| 🟡 High     | Weekly sprint review | Next standup discussion |
| 🟢 Medium   | Bi-weekly check      | Sprint retrospective    |
| ⚪ Minimal  | Monthly              | No escalation needed    |

### 6.2 Risk Escalation Path

```
Developer detects issue → Report in standup → Tech Lead assesses
→ If Critical: Emergency team meeting → Implement contingency
→ If High: Add to sprint backlog → Assign owner → Track daily
→ If Medium/Low: Log in risk register → Review at sprint review
```
