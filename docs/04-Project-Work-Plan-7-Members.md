# OmniBizAI - Kế Hoạch Phân Công 7 Thành Viên Trong 1 Tuần

> Mục đích: dùng trực tiếp để tạo Trello board, chia task, theo dõi tiến độ và chuẩn bị bản demo gửi giáo viên hướng dẫn góp ý.
> Thời gian sprint: 2026-04-26 đến 2026-05-03.
> Nguyên tắc: Core MVP trước, demo highlight sau; không đưa workflow designer, multi-tenant, banking integration, RAG hoặc public REST API vào sprint này.

## 1. Mục Tiêu Sprint 1 Tuần

Mục tiêu đến hết ngày 2026-05-02:

- Chạy được Core MVP nội bộ: login -> dashboard -> tạo PR -> submit -> workflow duyệt -> transaction/budget update -> KPI check-in -> AI fallback/mock -> report preview.
- Có seed data demo tối thiểu cho 4 role: Admin, Director, Manager, Staff.
- Có evidence ban đầu: screenshot các màn hình chính, manual test checklist, bug list, demo script.
- Đóng gói bản gửi giáo viên hướng dẫn vào 2026-05-03 để xin góp ý và cải thiện.

## 2. Nguyên Tắc Chia Việc

| Nguyên tắc | Quy định |
|---|---|
| Ai cũng phải code | Tất cả 7 thành viên có card liên quan đến code, test automation, seed helper hoặc evidence script |
| Code chính | 5 dev chính: Như, Nhật, An, Bảo, Phong |
| PM vẫn code | Quân code các helper nhỏ cho seed/evidence/docs automation, không nằm trên critical backend |
| Tester vẫn code | Khánh code Playwright smoke/manual test artifact, không chỉ test thủ công |
| Review bắt buộc | Mỗi card code chuyển `Review` trước khi sang `Testing` |
| Evidence bắt buộc | Card chỉ được `Done` khi có acceptance và evidence tối thiểu |
| Scope freeze | Từ 2026-05-01 chỉ fix bug và hoàn thiện demo, không thêm feature mới |

## 3. Trello Board Workflow

Dùng đúng 7 cột sau:

| Cột | Ý nghĩa | Điều kiện vào cột | Điều kiện ra cột |
|---|---|---|---|
| Backlog | Task cần làm nhưng chưa kéo vào ngày hiện tại | Task đã được tạo, chưa cam kết làm trong ngày | PM kéo sang `Todo` khi vào daily plan |
| Todo | Task đã chốt làm trong ngày | Có owner, deadline, acceptance rõ | Owner bắt đầu làm thì kéo sang `In Progress` |
| In Progress | Đang code/thực hiện | Owner đang làm | Xong implementation thì kéo sang `Review` |
| Review | Chờ review code/tài liệu/UI | Có commit/PR hoặc artifact review | Reviewer approve thì sang `Testing`; fail thì về `In Progress` |
| Testing | Chờ QA/manual test/Playwright/evidence | Đã review xong | Pass acceptance thì sang `Done`; fail thì về `In Progress` |
| Done | Đã pass acceptance criteria | Có evidence | Không mở lại trừ khi có bug nghiêm trọng |
| Pending | Bị chặn | Thiếu dependency, lỗi môi trường, cần quyết định, chờ góp ý | Giải tỏa blocker thì về `Todo` hoặc `In Progress` |

WIP limit:

| Cột | Giới hạn |
|---|---:|
| In Progress | Tối đa 1 card/người |
| Review | Tối đa 5 card toàn team |
| Testing | Tối đa 7 card toàn team |
| Pending | Mỗi card phải có lý do và người unblock |

## 4. Lịch Deadline 1 Tuần

| Ngày | Mục tiêu chính | Kết quả cuối ngày |
|---|---|---|
| 2026-04-26 | Setup, backlog, schema, seed plan | Project build được, Trello board có card, schema/task owner rõ |
| 2026-04-27 | Auth/RBAC/layout/entity base | Login/role seed, layout shell, entity base, DB migration đầu tiên |
| 2026-04-28 | Finance PR/Budget + UI forms | Tạo PR draft, budget list, PR form, validation cơ bản |
| 2026-04-29 | Workflow/KPI/Dashboard | Submit PR tạo workflow, KPI check-in, dashboard role widgets |
| 2026-04-30 | AI mock/fallback/report/notification | AI panel mock/fallback, notification, report preview |
| 2026-05-01 | Integration, bug fixing, QA | End-to-end flow chạy nội bộ, bug list được triage |
| 2026-05-02 | Demo script, screenshots, evidence | Có screenshot, manual checklist, demo script, package gửi góp ý |
| 2026-05-03 | Buffer/freeze | Freeze bản gửi giáo viên hướng dẫn |

## 5. Phân Công Theo Thành Viên

| Thành viên | Vai trò sprint | Số card | Trọng tâm | Ghi chú |
|---|---|---:|---|---|
| Quân | PM + Docs + helper code | 7 | Backlog, demo, evidence, advisor package | Có code helper nhỏ, không block feature |
| Như | FE Dev | 7 | Layout, dashboard UI, chart, AI panel | Code chính frontend |
| Nhật | FE Dev | 7 | Auth UI, PR form, workflow/KPI/report views | Code chính frontend |
| An | BE Dev | 7 | Project foundation, Identity, RBAC, Org, Audit | Code chính backend |
| Bảo | BE Dev | 7 | Finance, PR, Budget, Transaction, Workflow integration | Code chính backend |
| Phong | BE Dev | 7 | KPI, Dashboard data, AI mock/Gemini abstraction, Notification | Code chính backend |
| Khánh | QA + test code | 7 | Test plan, Playwright, manual QA, regression evidence | Có code test scripts |

## 6. Trello Cards - Quân

| Card ID | Nhiệm vụ | Người thực hiện | Hỗ trợ | Deadline | Cột Trello | Cách thực hiện | Tiêu chí nghiệm thu | Minh chứng |
|---|---|---|---|---|---|---|---|---|
| QN-01 | Tạo Trello board và gắn label | Quân | Khánh | 2026-04-26 | Todo | Tạo board với 7 cột chuẩn, label theo module: Auth, Finance, Workflow, KPI, AI, UI, QA, Docs | Board có đủ cột, mỗi card có owner/deadline | Screenshot board |
| QN-02 | Chốt Core MVP checklist | Quân | An, Bảo, Phong | 2026-04-26 | Todo | Lấy Core MVP từ technical doc, rút thành checklist 15-20 dòng | Team thống nhất scope, không đưa after-MVP vào sprint | Checklist trong Trello/Docs |
| QN-03 | Tạo demo script 5-7 phút | Quân | Như, Nhật | 2026-04-30 | Backlog | Viết flow Staff -> Manager -> Accountant/Director -> AI -> Report | Script có role, account, route, data seed cần dùng | `docs/test-evidence/demo-script.md` hoặc nội dung trong card |
| QN-04 | Code helper tạo thư mục evidence | Quân | Khánh | 2026-04-27 | Backlog | Tạo script nhỏ hoặc checklist command để tạo `docs/test-evidence/screenshots`, `test-results`, `exports` | Chạy được local, không ghi secret | Screenshot folder/evidence checklist |
| QN-05 | Tổng hợp risk và blocker mỗi ngày | Quân | All | 2026-05-01 | Backlog | Mỗi cuối ngày cập nhật card Pending, unblock owner, quyết định cắt scope nếu cần | Mỗi Pending có lý do và next action | Daily notes |
| QN-06 | Review tài liệu advisor package | Quân | Khánh | 2026-05-02 | Backlog | Gom link technical, academic, user guide, screenshots, bug list | Package doc rõ, gửi được cho GVHD | File/link package |
| QN-07 | Freeze bản gửi GVHD | Quân | All | 2026-05-03 | Backlog | Kiểm tra README/link docs/evidence, tạo summary việc đã làm và việc cần góp ý | Có bản freeze, không còn task critical mở | Summary gửi GVHD |

## 7. Trello Cards - Như

| Card ID | Nhiệm vụ | Người thực hiện | Hỗ trợ | Deadline | Cột Trello | Cách thực hiện | Tiêu chí nghiệm thu | Minh chứng |
|---|---|---|---|---|---|---|---|---|
| NHU-01 | Shared layout MVC | Như | Nhật | 2026-04-27 | Backlog | Cập nhật layout Razor: sidebar, topbar, notification slot, profile menu | Layout hiển thị được Dashboard, Finance, KPI, AI, Reports | Screenshot layout |
| NHU-02 | Dashboard shell UI | Như | Phong | 2026-04-28 | Backlog | Tạo card grid, chart container, pending approval panel, risk alert panel | Dashboard không blank, có empty/loading state | Screenshot dashboard |
| NHU-03 | Role dashboard widgets UI | Như | Phong | 2026-04-29 | Backlog | Bind DTO/mock data vào card theo role Director/Manager/Staff | Mỗi role có ít nhất 3 widget phù hợp | Screenshot 3 role |
| NHU-04 | Chart UI finance/KPI | Như | Phong | 2026-04-29 | Backlog | Dùng ECharts/Chart.js hoặc table fallback nếu chart chưa sẵn sàng | Chart hoặc fallback table render dữ liệu seed | Screenshot chart/fallback |
| NHU-05 | AI panel UI | Như | Phong | 2026-04-30 | Backlog | Tạo panel/modal AI có input, answer, citation, confidence, fallback marker | Gọi được `/AI/ChatJson` mock hoặc hiện fallback | Screenshot AI panel |
| NHU-06 | Responsive pass mobile/desktop | Như | Nhật | 2026-05-01 | Backlog | Kiểm tra 1366px và mobile width, fix sidebar/form overflow | Không vỡ layout trên dashboard/PR/AI | Screenshot desktop/mobile |
| NHU-07 | UI polish và evidence screenshots | Như | Quân | 2026-05-02 | Backlog | Chụp UI-02, UI-03, AI panel, dashboard states | Ảnh đặt tên đúng evidence checklist | Files screenshot |

## 8. Trello Cards - Nhật

| Card ID | Nhiệm vụ | Người thực hiện | Hỗ trợ | Deadline | Cột Trello | Cách thực hiện | Tiêu chí nghiệm thu | Minh chứng |
|---|---|---|---|---|---|---|---|---|
| NHT-01 | Identity UI polish | Nhật | An | 2026-04-27 | Backlog | Chỉnh login/logout/manage UI theo layout chung, message validation rõ | Login UI đúng, sai password có message | Screenshot login |
| NHT-02 | Payment Request create form | Nhật | Bảo | 2026-04-28 | Backlog | Tạo Razor form title, department, category, vendor, budget, line items | Submit invalid hiện ModelState; valid tạo Draft | Screenshot PR form |
| NHT-03 | Payment Request details UI | Nhật | Bảo | 2026-04-28 | Backlog | Hiện items, total, status, AI risk, workflow summary | Detail đọc được PR 8M/85M | Screenshot detail |
| NHT-04 | Approval queue/detail UI | Nhật | Bảo | 2026-04-29 | Backlog | Tạo queue, timeline, approve/reject/request change buttons | Approver thấy queue, non-approver không thấy action | Screenshot approval |
| NHT-05 | KPI check-in UI | Nhật | Phong | 2026-04-29 | Backlog | Tạo KPI list/detail/check-in form, progress display | Staff submit check-in, Manager thấy pending | Screenshot KPI |
| NHT-06 | Report preview UI | Nhật | Phong | 2026-04-30 | Backlog | Tạo report filter/preview/export button disabled/enabled theo permission | Preview có empty/data state, không crash | Screenshot report |
| NHT-07 | Form validation UX pass | Nhật | Khánh | 2026-05-01 | Backlog | Kiểm tra PR/KPI/login/report form, message gần field lỗi | Lỗi required/range/status hiện rõ | Manual QA notes |

## 9. Trello Cards - An

| Card ID | Nhiệm vụ | Người thực hiện | Hỗ trợ | Deadline | Cột Trello | Cách thực hiện | Tiêu chí nghiệm thu | Minh chứng |
|---|---|---|---|---|---|---|---|---|
| AN-01 | Project foundation check | An | Quân | 2026-04-26 | Todo | Kiểm tra MVC project, Program.cs, DbContext, package, local DB config | `dotnet build` pass local, không lỗi logging/EventLog | Build screenshot/log |
| AN-02 | Identity roles/users seed | An | Khánh | 2026-04-27 | Backlog | Seed Admin, Director, Manager, Accountant, HR, Staff với password demo trong secret nội bộ | Login được 4 role chính | Seed log/screenshot |
| AN-03 | Permission/RBAC foundation | An | Bảo | 2026-04-27 | Backlog | Tạo permission claims/policies cho finance/workflow/kpi/admin/report | Staff vào Admin bị 403 | Test note |
| AN-04 | Organization entities/basic CRUD | An | Phong | 2026-04-28 | Backlog | Tạo Company/Department/Employee/Position entity + seed 7 phòng ban | Department/employee seed đúng role scope | DB screenshot/log |
| AN-05 | Data scope service | An | Bảo | 2026-04-29 | Backlog | Implement scope theo Director/Manager/Staff, helper query scoped | Staff không xem dữ liệu phòng khác | Integration/manual note |
| AN-06 | Audit service foundation | An | Bảo | 2026-04-30 | Backlog | Ghi audit cho login, PR submit, approval, export, AI query | Audit list có event chính | Screenshot audit |
| AN-07 | Security hardening pass | An | Khánh | 2026-05-01 | Backlog | Kiểm tra authorization, anti-forgery, upload rule stub, no secret logs | Mutation route nhạy cảm có auth/anti-forgery | Security checklist |

## 10. Trello Cards - Bảo

| Card ID | Nhiệm vụ | Người thực hiện | Hỗ trợ | Deadline | Cột Trello | Cách thực hiện | Tiêu chí nghiệm thu | Minh chứng |
|---|---|---|---|---|---|---|---|---|
| BAO-01 | Finance entities/schema | Bảo | An | 2026-04-27 | Backlog | Tạo Budget, PaymentRequest, PaymentRequestItem, Transaction, Vendor, Category | Migration build được, entity có status/rowversion cần thiết | Migration/log |
| BAO-02 | Budget service/list | Bảo | Nhật | 2026-04-28 | Backlog | Implement budget list/detail/utilization, seed budget >80% | Budget Marketing hiện utilization warning | Screenshot budget |
| BAO-03 | Payment Request draft/create | Bảo | Nhật | 2026-04-28 | Backlog | Implement create draft, total recalculation server-side, validation | PR 8M và 85M tạo Draft được | Screenshot/detail |
| BAO-04 | Submit PR transaction boundary | Bảo | An | 2026-04-29 | Backlog | Implement SubmitAsync: validate Draft, risk base score, committed budget, workflow start | Submit PR tạo workflow, double submit trả 409 | Test/manual note |
| BAO-05 | Workflow 2/3 cấp integration | Bảo | Phong | 2026-04-29 | Backlog | Seed PR_TWO_LEVEL/PR_THREE_LEVEL, route amount >50M hoặc budget >80% | PR 8M đi 2 cấp, PR 85M đi 3 cấp | Screenshot workflow |
| BAO-06 | Approval final creates transaction | Bảo | An | 2026-04-30 | Backlog | Final approve tạo expense transaction, update spent/committed trong transaction DB | Budget/transaction update đúng, reject/request change đúng state | Evidence transaction |
| BAO-07 | Finance bug fixing/integration | Bảo | Khánh | 2026-05-01 | Backlog | Fix bug từ QA cho PR/budget/workflow/transaction | E2E finance flow pass | Bug list closed |

## 11. Trello Cards - Phong

| Card ID | Nhiệm vụ | Người thực hiện | Hỗ trợ | Deadline | Cột Trello | Cách thực hiện | Tiêu chí nghiệm thu | Minh chứng |
|---|---|---|---|---|---|---|---|---|
| PHG-01 | KPI entities/schema | Phong | An | 2026-04-27 | Backlog | Tạo Objective, Kpi, KpiCheckIn, EvaluationPeriod tối thiểu | Migration/build pass, seed KPI Marketing trễ | DB/log |
| PHG-02 | KPI service/check-in | Phong | Nhật | 2026-04-29 | Backlog | Implement KPI list/detail, submit check-in, approve/reject, progress formula | Check-in approve mới update progress | Screenshot/test note |
| PHG-03 | Dashboard data service | Phong | Như | 2026-04-29 | Backlog | Aggregate budget, pending approval, KPI health, high-risk PR theo role | Director/Manager/Staff nhận data khác nhau | Screenshot JSON/UI |
| PHG-04 | AI provider abstraction/mock | Phong | Như | 2026-04-30 | Backlog | Implement `IAIProvider` mock mode và fallback response, không cần live Gemini trong sprint | `/AI/ChatJson` trả summary/citation/fallback | Screenshot AI |
| PHG-05 | AI risk/advisory context | Phong | Bảo | 2026-04-30 | Backlog | Build context từ PR/budget/KPI seed, output citation IDs | PR 85M có risk High và citation | AI output JSON |
| PHG-06 | Notification basic | Phong | An | 2026-04-30 | Backlog | Lưu notification DB và unread count JSON/polling fallback | Submit/approve/check-in tạo notification | Screenshot bell/list |
| PHG-07 | Report preview data | Phong | Nhật | 2026-05-01 | Backlog | Provide finance/KPI summary DTO cho report preview | Report preview có data seed hoặc empty state | Screenshot report |

## 12. Trello Cards - Khánh

| Card ID | Nhiệm vụ | Người thực hiện | Hỗ trợ | Deadline | Cột Trello | Cách thực hiện | Tiêu chí nghiệm thu | Minh chứng |
|---|---|---|---|---|---|---|---|---|
| KHA-01 | Test plan 1 tuần | Khánh | Quân | 2026-04-26 | Todo | Tạo checklist manual theo Core MVP: auth, PR, workflow, KPI, AI, report | Checklist có pass/fail/bug/evidence column | Test checklist |
| KHA-02 | Seed validation checklist | Khánh | An, Bảo, Phong | 2026-04-27 | Backlog | Kiểm tra account, role, PR 8M/85M, budget >80%, KPI trễ | Seed đủ scenario demo | Seed QA note |
| KHA-03 | Playwright smoke setup | Khánh | Nhật | 2026-04-28 | Backlog | Code smoke login/dashboard/PR create nếu app sẵn sàng; nếu chưa, tạo skeleton test | Test chạy được hoặc skip có lý do | Test output |
| KHA-04 | Manual QA finance/workflow | Khánh | Bảo | 2026-05-01 | Backlog | Test PR create/submit/approve/reject/request change/final approve | Bug được ghi card và gán owner | Bug list |
| KHA-05 | Manual QA KPI/AI/report | Khánh | Phong, Như | 2026-05-01 | Backlog | Test KPI check-in, AI fallback/citation, report preview | Pass/fail rõ từng scenario | QA sheet |
| KHA-06 | Evidence screenshot checklist | Khánh | Như, Quân | 2026-05-02 | Backlog | Đối chiếu UI-01 đến UI-10, đặt tên file đúng convention | Đủ ảnh cần gửi GVHD hoặc note missing | Screenshot folder |
| KHA-07 | Regression và release checklist | Khánh | All | 2026-05-02 | Backlog | Chạy lại flow demo, confirm bug critical đã fix, lập release notes | Có go/no-go và bug còn lại | Release checklist |

## 13. Daily Checklist

| Thời điểm | Việc cần làm | Owner |
|---|---|---|
| 08:30 | Kéo card ngày từ `Backlog` sang `Todo` | Quân |
| 09:00 | Daily 15 phút: hôm qua, hôm nay, blocker | All |
| 13:30 | Check WIP và Pending | Quân, Khánh |
| 17:00 | Review nhanh: card nào sang Review/Testing/Done | All |
| 21:00 | Cập nhật evidence và bug list | Khánh, Quân |

## 14. Definition Of Done Cho Mỗi Card

| Loại card | Done khi |
|---|---|
| Code feature | Build pass local, có UI/API/service chạy được, có reviewer approve, QA pass scenario liên quan |
| UI | Có screenshot desktop, không vỡ layout, validation/empty/error state cơ bản |
| Backend | Có validation, data scope/auth nếu cần, audit nếu là action nhạy cảm, không duplicate state transition |
| Test | Có test/manual checklist output, bug được tạo card riêng |
| Docs/evidence | Có file/screenshot/link rõ, không có secret, đúng tên convention |

## 15. Checklist Gửi Giáo Viên Hướng Dẫn

Đến 2026-05-03, package gửi GVHD gồm:

- Link repo hoặc zip bản code.
- Link 4 file docs chính, gồm file phân công này.
- Demo script 5-7 phút.
- Screenshot tối thiểu: login, dashboard, PR form, approval queue, KPI, AI, report.
- Test checklist pass/fail.
- Bug list còn tồn tại và kế hoạch sửa.
- Câu hỏi xin góp ý: scope MVP, workflow/AI positioning, UI/demo flow, mức độ đủ cho báo cáo.

## 16. Scope Không Làm Trong 1 Tuần Này

| Hạng mục | Lý do |
|---|---|
| Workflow designer UI | After MVP, tốn thời gian và không cần cho demo Core |
| Multi-tenant SaaS | Không phù hợp 1 tuần và không cần cho SME MVP |
| Banking integration | Cần API/bảo mật/đối soát thật, để sau |
| RAG/vector search | AI mock/fallback/advisory là đủ cho bản xin góp ý |
| Public REST API | Dự án MVC-first, JSON chỉ phụ trợ Razor UI |
| UI polish nâng cao | Ưu tiên flow chạy được và evidence |
