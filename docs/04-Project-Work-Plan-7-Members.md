# OmniBizAI - Ke Hoach Phan Cong 7 Thanh Vien Trong 1 Tuan

> Muc dich: dung truc tiep de tao Trello board, chia task, theo doi tien do va chuan bi ban demo gui giao vien huong dan gop y.
> Thoi gian sprint: 2026-04-26 den 2026-05-03.
> Nguyen tac: Core MVP truoc, demo highlight sau; khong dua workflow designer, multi-tenant, banking integration, RAG hoac public REST API vao sprint nay.

## 1. Muc Tieu Sprint 1 Tuan

Muc tieu den het ngay 2026-05-02:

- Chay duoc Core MVP noi bo: login -> dashboard -> tao PR -> submit -> workflow duyet -> transaction/budget update -> KPI check-in -> AI fallback/mock -> report preview.
- Co seed data demo toi thieu cho 4 role: Admin, Director, Manager, Staff.
- Co evidence ban dau: screenshot cac man hinh chinh, manual test checklist, bug list, demo script.
- Dong goi ban gui giao vien huong dan vao 2026-05-03 de xin gop y va cai thien.

## 2. Nguyen Tac Chia Viec

| Nguyen tac | Quy dinh |
|---|---|
| Ai cung phai code | Tat ca 7 thanh vien co card lien quan den code, test automation, seed helper hoac evidence script |
| Code chinh | 5 dev chinh: Nhu, Nhat, An, Bao, Phong |
| PM van code | Quan code cac helper nho cho seed/evidence/docs automation, khong nam tren critical backend |
| Tester van code | Khanh code Playwright smoke/manual test artifact, khong chi test thu cong |
| Review bat buoc | Moi card code chuyen `Review` truoc khi sang `Testing` |
| Evidence bat buoc | Card chi duoc `Done` khi co acceptance va evidence toi thieu |
| Scope freeze | Tu 2026-05-01 chi fix bug va hoan thien demo, khong them feature moi |

## 3. Trello Board Workflow

Dung dung 7 cot sau:

| Cot | Y nghia | Dieu kien vao cot | Dieu kien ra cot |
|---|---|---|---|
| Backlog | Task can lam nhung chua keo vao ngay hien tai | Task da duoc tao, chua cam ket lam trong ngay | PM keo sang `Todo` khi vao daily plan |
| Todo | Task da chot lam trong ngay | Co owner, deadline, acceptance ro | Owner bat dau lam thi keo sang `In Progress` |
| In Progress | Dang code/thuc hien | Owner dang lam | Xong implementation thi keo sang `Review` |
| Review | Cho review code/tai lieu/UI | Co commit/PR hoac artifact review | Reviewer approve thi sang `Testing`; fail thi ve `In Progress` |
| Testing | Cho QA/manual test/Playwright/evidence | Da review xong | Pass acceptance thi sang `Done`; fail thi ve `In Progress` |
| Done | Da pass acceptance criteria | Co evidence | Khong mo lai tru khi co bug nghiem trong |
| Pending | Bi chan | Thieu dependency, loi moi truong, can quyet dinh, cho gop y | Giai toa blocker thi ve `Todo` hoac `In Progress` |

WIP limit:

| Cot | Gioi han |
|---|---:|
| In Progress | Toi da 1 card/ngui |
| Review | Toi da 5 card toan team |
| Testing | Toi da 7 card toan team |
| Pending | Moi card phai co ly do va nguoi unblock |

## 4. Lich Deadline 1 Tuan

| Ngay | Muc tieu chinh | Ket qua cuoi ngay |
|---|---|---|
| 2026-04-26 | Setup, backlog, schema, seed plan | Project build duoc, Trello board co card, schema/task owner ro |
| 2026-04-27 | Auth/RBAC/layout/entity base | Login/role seed, layout shell, entity base, DB migration dau tien |
| 2026-04-28 | Finance PR/Budget + UI forms | Tao PR draft, budget list, PR form, validation co ban |
| 2026-04-29 | Workflow/KPI/Dashboard | Submit PR tao workflow, KPI check-in, dashboard role widgets |
| 2026-04-30 | AI mock/fallback/report/notification | AI panel mock/fallback, notification, report preview |
| 2026-05-01 | Integration, bug fixing, QA | End-to-end flow chay noi bo, bug list duoc triage |
| 2026-05-02 | Demo script, screenshots, evidence | Co screenshot, manual checklist, demo script, package gui gop y |
| 2026-05-03 | Buffer/freeze | Freeze ban gui giao vien huong dan |

## 5. Phan Cong Theo Thanh Vien

| Thanh vien | Vai tro sprint | So card | Trong tam | Ghi chu |
|---|---|---:|---|---|
| Quan | PM + Docs + helper code | 7 | Backlog, demo, evidence, advisor package | Co code helper nho, khong block feature |
| Nhu | FE Dev | 7 | Layout, dashboard UI, chart, AI panel | Code chinh frontend |
| Nhat | FE Dev | 7 | Auth UI, PR form, workflow/KPI/report views | Code chinh frontend |
| An | BE Dev | 7 | Project foundation, Identity, RBAC, Org, Audit | Code chinh backend |
| Bao | BE Dev | 7 | Finance, PR, Budget, Transaction, Workflow integration | Code chinh backend |
| Phong | BE Dev | 7 | KPI, Dashboard data, AI mock/Gemini abstraction, Notification | Code chinh backend |
| Khanh | QA + test code | 7 | Test plan, Playwright, manual QA, regression evidence | Co code test scripts |

## 6. Trello Cards - Quan

| Card ID | Task | Owner | Support | Deadline | Trello Column | How to implement | Acceptance criteria | Evidence |
|---|---|---|---|---|---|---|---|---|
| QN-01 | Tao Trello board va gan label | Quan | Khanh | 2026-04-26 | Todo | Tao board voi 7 cot chuan, label theo module: Auth, Finance, Workflow, KPI, AI, UI, QA, Docs | Board co du cot, moi card co owner/deadline | Screenshot board |
| QN-02 | Chot Core MVP checklist | Quan | An, Bao, Phong | 2026-04-26 | Todo | Lay Core MVP tu technical doc, rut thanh checklist 15-20 dong | Team thong nhat scope, khong dua after-MVP vao sprint | Checklist trong Trello/Docs |
| QN-03 | Tao demo script 5-7 phut | Quan | Nhu, Nhat | 2026-04-30 | Backlog | Viet flow Staff -> Manager -> Accountant/Director -> AI -> Report | Script co role, account, route, data seed can dung | `docs/test-evidence/demo-script.md` hoac noi dung trong card |
| QN-04 | Code helper tao thu muc evidence | Quan | Khanh | 2026-04-27 | Backlog | Tao script nho hoac checklist command de tao `docs/test-evidence/screenshots`, `test-results`, `exports` | Chay duoc local, khong ghi secret | Screenshot folder/evidence checklist |
| QN-05 | Tong hop risk va blocker moi ngay | Quan | All | 2026-05-01 | Backlog | Moi cuoi ngay cap nhat card Pending, unblock owner, quyet dinh cat scope neu can | Moi Pending co ly do va next action | Daily notes |
| QN-06 | Review tai lieu advisor package | Quan | Khanh | 2026-05-02 | Backlog | Gom link technical, academic, user guide, screenshots, bug list | Package doc ro, gui duoc cho GVHD | File/link package |
| QN-07 | Freeze ban gui GVHD | Quan | All | 2026-05-03 | Backlog | Kiem tra README/link docs/evidence, tao summary viec da lam va viec can gop y | Co ban freeze, khong con task critical mo | Summary gui GVHD |

## 7. Trello Cards - Nhu

| Card ID | Task | Owner | Support | Deadline | Trello Column | How to implement | Acceptance criteria | Evidence |
|---|---|---|---|---|---|---|---|---|
| NHU-01 | Shared layout MVC | Nhu | Nhat | 2026-04-27 | Backlog | Cap nhat layout Razor: sidebar, topbar, notification slot, profile menu | Layout hien duoc Dashboard, Finance, KPI, AI, Reports | Screenshot layout |
| NHU-02 | Dashboard shell UI | Nhu | Phong | 2026-04-28 | Backlog | Tao card grid, chart container, pending approval panel, risk alert panel | Dashboard khong blank, co empty/loading state | Screenshot dashboard |
| NHU-03 | Role dashboard widgets UI | Nhu | Phong | 2026-04-29 | Backlog | Bind DTO/mock data vao card theo role Director/Manager/Staff | Moi role co it nhat 3 widget phu hop | Screenshot 3 role |
| NHU-04 | Chart UI finance/KPI | Nhu | Phong | 2026-04-29 | Backlog | Dung ECharts/Chart.js hoac table fallback neu chart chua san sang | Chart hoac fallback table render du lieu seed | Screenshot chart/fallback |
| NHU-05 | AI panel UI | Nhu | Phong | 2026-04-30 | Backlog | Tao panel/modal AI co input, answer, citation, confidence, fallback marker | Goi duoc `/AI/ChatJson` mock hoac hien fallback | Screenshot AI panel |
| NHU-06 | Responsive pass mobile/desktop | Nhu | Nhat | 2026-05-01 | Backlog | Kiem tra 1366px va mobile width, fix sidebar/form overflow | Khong vo layout tren dashboard/PR/AI | Screenshot desktop/mobile |
| NHU-07 | UI polish va evidence screenshots | Nhu | Quan | 2026-05-02 | Backlog | Chup UI-02, UI-03, AI panel, dashboard states | Anh dat ten dung evidence checklist | Files screenshot |

## 8. Trello Cards - Nhat

| Card ID | Task | Owner | Support | Deadline | Trello Column | How to implement | Acceptance criteria | Evidence |
|---|---|---|---|---|---|---|---|---|
| NHT-01 | Identity UI polish | Nhat | An | 2026-04-27 | Backlog | Chinh login/logout/manage UI theo layout chung, message validation ro | Login UI dung, sai password co message | Screenshot login |
| NHT-02 | Payment Request create form | Nhat | Bao | 2026-04-28 | Backlog | Tao Razor form title, department, category, vendor, budget, line items | Submit invalid hien ModelState; valid tao Draft | Screenshot PR form |
| NHT-03 | Payment Request details UI | Nhat | Bao | 2026-04-28 | Backlog | Hien items, total, status, AI risk, workflow summary | Detail doc duoc PR 8M/85M | Screenshot detail |
| NHT-04 | Approval queue/detail UI | Nhat | Bao | 2026-04-29 | Backlog | Tao queue, timeline, approve/reject/request change buttons | Approver thay queue, non-approver khong thay action | Screenshot approval |
| NHT-05 | KPI check-in UI | Nhat | Phong | 2026-04-29 | Backlog | Tao KPI list/detail/check-in form, progress display | Staff submit check-in, Manager thay pending | Screenshot KPI |
| NHT-06 | Report preview UI | Nhat | Phong | 2026-04-30 | Backlog | Tao report filter/preview/export button disabled/enabled theo permission | Preview co empty/data state, khong crash | Screenshot report |
| NHT-07 | Form validation UX pass | Nhat | Khanh | 2026-05-01 | Backlog | Kiem tra PR/KPI/login/report form, message gan field loi | Loi required/range/status hien ro | Manual QA notes |

## 9. Trello Cards - An

| Card ID | Task | Owner | Support | Deadline | Trello Column | How to implement | Acceptance criteria | Evidence |
|---|---|---|---|---|---|---|---|---|
| AN-01 | Project foundation check | An | Quan | 2026-04-26 | Todo | Kiem tra MVC project, Program.cs, DbContext, package, local DB config | `dotnet build` pass local, khong loi logging/EventLog | Build screenshot/log |
| AN-02 | Identity roles/users seed | An | Khanh | 2026-04-27 | Backlog | Seed Admin, Director, Manager, Accountant, HR, Staff voi password demo trong secret noi bo | Login duoc 4 role chinh | Seed log/screenshot |
| AN-03 | Permission/RBAC foundation | An | Bao | 2026-04-27 | Backlog | Tao permission claims/policies cho finance/workflow/kpi/admin/report | Staff vao Admin bi 403 | Test note |
| AN-04 | Organization entities/basic CRUD | An | Phong | 2026-04-28 | Backlog | Tao Company/Department/Employee/Position entity + seed 7 phong ban | Department/employee seed dung role scope | DB screenshot/log |
| AN-05 | Data scope service | An | Bao | 2026-04-29 | Backlog | Implement scope theo Director/Manager/Staff, helper query scoped | Staff khong xem du lieu phong khac | Integration/manual note |
| AN-06 | Audit service foundation | An | Bao | 2026-04-30 | Backlog | Ghi audit cho login, PR submit, approval, export, AI query | Audit list co event chinh | Screenshot audit |
| AN-07 | Security hardening pass | An | Khanh | 2026-05-01 | Backlog | Kiem tra authorization, anti-forgery, upload rule stub, no secret logs | Mutation route nhay cam co auth/anti-forgery | Security checklist |

## 10. Trello Cards - Bao

| Card ID | Task | Owner | Support | Deadline | Trello Column | How to implement | Acceptance criteria | Evidence |
|---|---|---|---|---|---|---|---|---|
| BAO-01 | Finance entities/schema | Bao | An | 2026-04-27 | Backlog | Tao Budget, PaymentRequest, PaymentRequestItem, Transaction, Vendor, Category | Migration build duoc, entity co status/rowversion can thiet | Migration/log |
| BAO-02 | Budget service/list | Bao | Nhat | 2026-04-28 | Backlog | Implement budget list/detail/utilization, seed budget >80% | Budget Marketing hien utilization warning | Screenshot budget |
| BAO-03 | Payment Request draft/create | Bao | Nhat | 2026-04-28 | Backlog | Implement create draft, total recalculation server-side, validation | PR 8M va 85M tao Draft duoc | Screenshot/detail |
| BAO-04 | Submit PR transaction boundary | Bao | An | 2026-04-29 | Backlog | Implement SubmitAsync: validate Draft, risk base score, committed budget, workflow start | Submit PR tao workflow, double submit tra 409 | Test/manual note |
| BAO-05 | Workflow 2/3 cap integration | Bao | Phong | 2026-04-29 | Backlog | Seed PR_TWO_LEVEL/PR_THREE_LEVEL, route amount >50M hoac budget >80% | PR 8M di 2 cap, PR 85M di 3 cap | Screenshot workflow |
| BAO-06 | Approval final creates transaction | Bao | An | 2026-04-30 | Backlog | Final approve tao expense transaction, update spent/committed trong transaction DB | Budget/transaction update dung, reject/request change dung state | Evidence transaction |
| BAO-07 | Finance bug fixing/integration | Bao | Khanh | 2026-05-01 | Backlog | Fix bug tu QA cho PR/budget/workflow/transaction | E2E finance flow pass | Bug list closed |

## 11. Trello Cards - Phong

| Card ID | Task | Owner | Support | Deadline | Trello Column | How to implement | Acceptance criteria | Evidence |
|---|---|---|---|---|---|---|---|---|
| PHG-01 | KPI entities/schema | Phong | An | 2026-04-27 | Backlog | Tao Objective, Kpi, KpiCheckIn, EvaluationPeriod toi thieu | Migration/build pass, seed KPI Marketing tre | DB/log |
| PHG-02 | KPI service/check-in | Phong | Nhat | 2026-04-29 | Backlog | Implement KPI list/detail, submit check-in, approve/reject, progress formula | Check-in approve moi update progress | Screenshot/test note |
| PHG-03 | Dashboard data service | Phong | Nhu | 2026-04-29 | Backlog | Aggregate budget, pending approval, KPI health, high-risk PR theo role | Director/Manager/Staff nhan data khac nhau | Screenshot JSON/UI |
| PHG-04 | AI provider abstraction/mock | Phong | Nhu | 2026-04-30 | Backlog | Implement `IAIProvider` mock mode va fallback response, khong can live Gemini trong sprint | `/AI/ChatJson` tra summary/citation/fallback | Screenshot AI |
| PHG-05 | AI risk/advisory context | Phong | Bao | 2026-04-30 | Backlog | Build context tu PR/budget/KPI seed, output citation IDs | PR 85M co risk High va citation | AI output JSON |
| PHG-06 | Notification basic | Phong | An | 2026-04-30 | Backlog | Luu notification DB va unread count JSON/polling fallback | Submit/approve/check-in tao notification | Screenshot bell/list |
| PHG-07 | Report preview data | Phong | Nhat | 2026-05-01 | Backlog | Provide finance/KPI summary DTO cho report preview | Report preview co data seed hoac empty state | Screenshot report |

## 12. Trello Cards - Khanh

| Card ID | Task | Owner | Support | Deadline | Trello Column | How to implement | Acceptance criteria | Evidence |
|---|---|---|---|---|---|---|---|---|
| KHA-01 | Test plan 1 tuan | Khanh | Quan | 2026-04-26 | Todo | Tao checklist manual theo Core MVP: auth, PR, workflow, KPI, AI, report | Checklist co pass/fail/bug/evidence column | Test checklist |
| KHA-02 | Seed validation checklist | Khanh | An, Bao, Phong | 2026-04-27 | Backlog | Kiem tra account, role, PR 8M/85M, budget >80%, KPI tre | Seed du scenario demo | Seed QA note |
| KHA-03 | Playwright smoke setup | Khanh | Nhat | 2026-04-28 | Backlog | Code smoke login/dashboard/PR create neu app san sang; neu chua, tao skeleton test | Test chay duoc hoac skip co ly do | Test output |
| KHA-04 | Manual QA finance/workflow | Khanh | Bao | 2026-05-01 | Backlog | Test PR create/submit/approve/reject/request change/final approve | Bug duoc ghi card va gan owner | Bug list |
| KHA-05 | Manual QA KPI/AI/report | Khanh | Phong, Nhu | 2026-05-01 | Backlog | Test KPI check-in, AI fallback/citation, report preview | Pass/fail ro tung scenario | QA sheet |
| KHA-06 | Evidence screenshot checklist | Khanh | Nhu, Quan | 2026-05-02 | Backlog | Doi chieu UI-01 den UI-10, dat ten file dung convention | Du anh can gui GVHD hoac note missing | Screenshot folder |
| KHA-07 | Regression va release checklist | Khanh | All | 2026-05-02 | Backlog | Chay lai flow demo, confirm bug critical da fix, lap release notes | Co go/no-go va bug con lai | Release checklist |

## 13. Daily Checklist

| Thoi diem | Viec can lam | Owner |
|---|---|---|
| 08:30 | Keo card ngay tu `Backlog` sang `Todo` | Quan |
| 09:00 | Daily 15 phut: hom qua, hom nay, blocker | All |
| 13:30 | Check WIP va Pending | Quan, Khanh |
| 17:00 | Review nhanh: card nao sang Review/Testing/Done | All |
| 21:00 | Cap nhat evidence va bug list | Khanh, Quan |

## 14. Definition Of Done Cho Moi Card

| Loai card | Done khi |
|---|---|
| Code feature | Build pass local, co UI/API/service chay duoc, co reviewer approve, QA pass scenario lien quan |
| UI | Co screenshot desktop, khong vo layout, validation/empty/error state co ban |
| Backend | Co validation, data scope/auth neu can, audit neu la action nhay cam, khong duplicate state transition |
| Test | Co test/manual checklist output, bug duoc tao card rieng |
| Docs/evidence | Co file/screenshot/link ro, khong co secret, dung ten convention |

## 15. Checklist Goi Giao Vien Huong Dan

Den 2026-05-03, package gui GVHD gom:

- Link repo hoac zip ban code.
- Link 4 file docs chinh, gom file phan cong nay.
- Demo script 5-7 phut.
- Screenshot toi thieu: login, dashboard, PR form, approval queue, KPI, AI, report.
- Test checklist pass/fail.
- Bug list con ton tai va ke hoach sua.
- Cau hoi xin gop y: scope MVP, workflow/AI positioning, UI/demo flow, muc do du cho bao cao.

## 16. Scope Khong Lam Trong 1 Tuan Nay

| Hang muc | Ly do |
|---|---|
| Workflow designer UI | After MVP, ton thoi gian va khong can cho demo Core |
| Multi-tenant SaaS | Khong phu hop 1 tuan va khong can cho SME MVP |
| Banking integration | Can API/bao mat/doi soat that, de sau |
| RAG/vector search | AI mock/fallback/advisory la du cho ban xin gop y |
| Public REST API | Du an MVC-first, JSON chi phu tro Razor UI |
| UI polish nang cao | Uu tien flow chay duoc va evidence |
