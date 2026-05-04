# OmniBizAI - Kế Hoạch Thực Hiện Nhóm 7 Người

> Ngày cập nhật: 2026-04-30
> Sprint quy trình: 2026-04-30 đến 2026-05-07
> Mục tiêu 1 tuần: hoàn thành luồng tenant Bizen từ lập menu đến giấy đi chợ, kèm nền tảng cấu hình để có thể triển khai khách hàng khác sau này.
> Cơ cấu nhóm: 1 PM kiêm BA, 5 dev gồm 2 frontend và 3 backend, 1 tester. Ai cũng phải có phần code.
> Cutline chi tiết: xem [06-Sprint-1-Implementation-Cutline.md](06-Sprint-1-Implementation-Cutline.md).

## 1. Mục Tiêu Sprint 1 Tuần

Đến hết ngày 2026-05-07, hệ thống cần demo được:

1. Đăng nhập theo role nội bộ.
2. Quản lý tenant Bizen, công ty vận hành, phòng ban, team và nhân sự cơ bản.
3. Quản lý board/list/card theo phòng ban, My Tasks, assign, deadline, checklist, comment và file metadata.
4. Quản lý khách hàng, người liên hệ và địa điểm giao.
5. Cấu hình ca ăn, loại suất, vị trí món, bếp và board column bằng JSON/import profile.
6. Import/staging dữ liệu thật từ CSV/Lark ở mức tối thiểu nếu Must-have đã ổn.
7. Tạo món ăn và BOM nguyên vật liệu.
8. Tạo menu theo khách hàng, ngày và ca ăn.
9. Gửi menu vào luồng kiểm duyệt nội bộ.
10. Nội bộ duyệt hoặc yêu cầu chỉnh sửa.
11. Gửi khách hàng email/link token gồm dashboard menu và các form góp ý/dự kiến/chốt/phát sinh.
12. Khách hàng xem menu, góp ý nếu cần và nhập số lượng dự kiến/chốt/phát sinh qua form riêng.
13. Hệ thống tự fallback dự kiến theo cùng ngày tuần trước, chốt bằng dự kiến nếu khách không gửi chốt trước 09:00.
14. Hệ thống tính BOM và preview giấy đi chợ.
15. Phát hành giấy đi chợ và export PDF/XLSX để in.
16. Dashboard hiển thị trạng thái vận hành/task nếu còn thời gian.
17. AI mock/fallback cảnh báo task trễ, số lượng bất thường và tóm tắt nguyên liệu nếu core flow đã pass.

## 2. Nguyên Tắc Chia Việc

| Nguyên tắc | Quy định |
|---|---|
| Core flow trước | Board/task -> Menu -> Chị Nga duyệt nội bộ -> email/form khách hàng -> số lượng -> BOM -> giấy đi chợ |
| Product hóa từ đầu | Mọi dữ liệu nghiệp vụ có tenant/config; không hard-code rule/label/role/flow trong code |
| Config trước UI | Sprint 1 dùng JSON/import profile + service đọc config; UI cấu hình nâng cao chỉ làm khi core flow pass |
| Ai cũng code | PM code helper/seed/docs automation; tester code Playwright hoặc test script |
| Backend không ôm UI | Backend trả service/ViewModel rõ để frontend bind nhanh |
| Frontend không viết rule nghiệp vụ | Rule số lượng/BOM nằm ở service backend |
| Evidence bắt buộc | Card xong phải có screenshot, test note hoặc log |
| Scope freeze | Từ 2026-05-06 chỉ fix bug, không thêm feature lớn |
| AI không chặn flow | AI dùng mock/fallback; core nghiệp vụ vẫn chạy khi AI lỗi |

## 2.1 Ưu Tiên Coding Sprint 1

| Tầng | Hạng mục |
|---|---|
| Must-have | Auth/RBAC, Tenant/Company/Team, Work Management Core, Customer, Dish/BOM, Menu, Internal Approval, Email token/dev log, Quantity, Procurement, PDF/XLSX giấy đi chợ |
| Should-have | Dashboard task/vận hành cơ bản, import staging tối thiểu, audit tối thiểu, config screen nhẹ |
| Nice-to-have | AI fallback nâng cao, Configuration UI nâng cao, mapping editor |

## 3. Phân Vai Nhóm

| Thành viên | Vai trò | Trọng tâm | Vẫn phải code gì |
|---|---|---|---|
| Quân | PM + BA | Scope, backlog, acceptance, demo, báo cáo | Seed/config profile checklist, docs/evidence script nhỏ |
| Như | Frontend Dev 1 | Layout, dashboard, work board, menu list/detail, procurement UI | Razor Views, partials, CSS/Bootstrap |
| Nhật | Frontend Dev 2 | Task detail/form, form khách hàng, form menu, approval, quantity UI | Razor forms, validation UI, public customer page |
| An | Backend Dev 1 | Identity, RBAC, company, team, work entities, audit | Entity, DbContext, services, seed |
| Bảo | Backend Dev 2 | Work task service, Dish, BOM, menu, internal approval, state machine | Entity, service, unit test |
| Phong | Backend Dev 3 | Customer email forms, quantity, procurement, AI fallback | Service, email/token, BOM aggregation, AI mock |
| Khánh | Tester | Test plan, regression, bug tracking, evidence | Playwright smoke, test data verifier, QA helper |

## 4. Trello Board Workflow

Tạo 7 cột:

| Cột | Ý nghĩa |
|---|---|
| Backlog | Việc cần làm, chưa kéo vào ngày hiện tại |
| Todo | Việc đã cam kết làm trong ngày |
| In Progress | Đang làm |
| Review | Chờ review code/tài liệu |
| Testing | Chờ QA hoặc tự test |
| Done | Đã pass acceptance và có evidence |
| Pending | Bị chặn, cần quyết định hoặc dependency |

WIP limit:

- Mỗi người tối đa 1 card ở In Progress.
- Review tối đa 5 card toàn team.
- Pending phải có lý do, người unblock và hạn xử lý.

## 5. Timeline 1 Tuần

| Ngày | Mục tiêu | Kết quả cuối ngày |
|---|---|---|
| 2026-04-30 | Chốt scope, schema, backlog | Trello đủ card, entity plan rõ, route flow thống nhất |
| 2026-05-01 | Foundation + company/work/customer | Build pass, runtime thống nhất, Identity/RBAC seed, phòng ban/team, board demo và khách hàng CRUD cơ bản |
| 2026-05-02 | Work board + Dish/BOM/menu + UI form | Tạo/move task, nhập BOM, tạo menu Draft được |
| 2026-05-03 | Internal approval + email token | Menu gửi duyệt nội bộ, duyệt xong gửi dashboard/form khách được |
| 2026-05-04 | Quantity rules + customer page | Khách duyệt link, nhập số lượng, fallback rule pass |
| 2026-05-05 | BOM calculation + procurement | Preview/tạo/phát hành giấy đi chợ |
| 2026-05-06 | Dashboard/Import/Audit + QA | Should-have có bao nhiêu làm bấy nhiêu, bug critical core flow được fix |
| 2026-05-07 | Demo/evidence/freeze | Demo script, screenshot, test report, bản gửi GVHD |

## 6. Card Cho Quân - PM Kiêm BA

| ID | Nhiệm vụ | Deadline | Code/Evidence | Acceptance |
|---|---|---|---|---|
| QN-01 | Chốt scope MVP và rule nghiệp vụ | 2026-04-30 | Markdown checklist trong docs/Trello | Team thống nhất không làm Finance/KPI/CRM nâng cao |
| QN-02 | Viết acceptance criteria cho từng module | 2026-04-30 | File checklist hoặc Trello template | Mỗi card có Given-When-Then ngắn |
| QN-03 | Tạo config/seed scenario demo | 2026-05-01 | JSON seed/import profile | Có tenant demo, menu cùng ngày tuần trước, BOM mẫu; không dùng constants nghiệp vụ trong code |
| QN-04 | Chuẩn hóa demo script 5-7 phút | 2026-05-05 | Docs update | Script đi từ login đến giấy đi chợ |
| QN-05 | Code helper tạo thư mục evidence | 2026-05-05 | Script nhỏ hoặc README command | Có folder screenshots, test-results, exports |
| QN-06 | Review báo cáo và link tài liệu | 2026-05-06 | Docs PR/review notes | 4 file docs đúng scope mới |
| QN-07 | Freeze package gửi GVHD | 2026-05-07 | Summary file | Có danh sách feature, bug còn lại, câu hỏi xin góp ý |

## 7. Card Cho Như - Frontend Dev 1

| ID | Nhiệm vụ | Deadline | Code/Evidence | Acceptance |
|---|---|---|---|---|
| NHU-01 | Layout nội bộ theo tenant | 2026-05-01 | `_Layout.cshtml`, nav/sidebar | Có menu Dashboard, My Tasks, Work Boards, Company, Configuration, Customers, Menu, BOM, Import, Procurement |
| NHU-02 | Dashboard UI | 2026-05-02 | Razor view/partials | Hiển thị card task quá hạn, chờ duyệt, chờ khách, thiếu BOM, giấy đi chợ |
| NHU-03 | Work board/My Tasks UI | 2026-05-02 | `WorkBoards/Details`, `MyTasks` | Xem được Kanban/List, task card, assignee, deadline |
| NHU-08 | Menu list/detail UI | 2026-05-02 | `MenuPlans/Index`, `Details` | Xem được status, dishes, timeline |
| NHU-04 | Internal approval queue UI | 2026-05-03 | Approval queue view | Người duyệt thấy danh sách và action |
| NHU-05 | Procurement preview UI | 2026-05-05 | `ProcurementPlans/Preview` | Bảng nguyên liệu không vỡ layout, có total |
| NHU-06 | AI advisory panel UI | 2026-05-06 | Partial/modal AI | Hiển thị summary, severity, recommendation, citation |
| NHU-07 | Screenshot UI evidence | 2026-05-07 | Ảnh màn hình | Có ảnh dashboard, menu, approval, procurement |

## 8. Card Cho Nhật - Frontend Dev 2

| ID | Nhiệm vụ | Deadline | Code/Evidence | Acceptance |
|---|---|---|---|---|
| NHT-01 | Customer CRUD UI | 2026-05-01 | Customer views | Tạo/sửa khách hàng và contact được |
| NHT-02 | Work task detail/form UI | 2026-05-02 | Task views/partials | Tạo task, assign, checklist, comment, attachment metadata được |
| NHT-08 | Dish/BOM form UI | 2026-05-02 | Dish/BOM views | Nhập định mức nguyên liệu theo món |
| NHT-03 | Menu create/edit form | 2026-05-02 | Menu form view | Chọn khách, ngày, ca, món; validation rõ |
| NHT-04 | Customer email preview UI | 2026-05-03 | Preview page/modal | CS thấy email/link trước khi gửi |
| NHT-05 | Public customer review page | 2026-05-04 | Public token view | Khách duyệt/yêu cầu sửa không cần login |
| NHT-06 | Quantity input UI trên form khách hàng | 2026-05-04 | Quantity forms | Có form dự kiến, chốt trước 09:00, phát sinh tăng/giảm hoặc tổng mới từ email |
| NHT-07 | Form validation pass | 2026-05-06 | Screenshot/test note | Required/range/status errors hiển thị cạnh field |

## 9. Card Cho An - Backend Dev 1

| ID | Nhiệm vụ | Deadline | Code/Evidence | Acceptance |
|---|---|---|---|---|
| AN-01 | Project foundation check | 2026-05-01 | Build log | `dotnet build` pass, cấu hình DB ổn |
| AN-02 | Identity roles/users seed | 2026-05-01 | Seed service | Login được Admin, Director, DeptManager, TeamLead, Employee, Ops, Menu, QA, Kitchen, Purchasing, CS |
| AN-03 | Organization + Work entities | 2026-05-01 | Company/Department/Team/Employee/Work entities | Migration có bảng tổ chức và work management |
| AN-04 | Company/Department services | 2026-05-02 | Services/controllers | CRUD phòng ban cơ bản chạy |
| AN-05 | RBAC policies | 2026-05-02 | Authorization setup | User sai role bị 403 |
| AN-06 | Audit service foundation | 2026-05-04 | AuditLog entity/service | Ghi audit cho task/menu submit/approve/email/quantity/procurement |
| AN-07 | Security pass | 2026-05-06 | Checklist/test note | POST có anti-forgery, public token không lộ dữ liệu ngoài scope |

## 10. Card Cho Bảo - Backend Dev 2

| ID | Nhiệm vụ | Deadline | Code/Evidence | Acceptance |
|---|---|---|---|---|
| BAO-01 | Work task service | 2026-05-02 | Service/unit test | Tạo/move/assign/comment task được |
| BAO-08 | Dish/Ingredient/BOM schema | 2026-05-02 | Entities/migration | Tạo món và BOM được |
| BAO-02 | MenuPlan schema | 2026-05-02 | Entities/migration | Menu có customer/date/shift/status/items |
| BAO-03 | Menu service create/update | 2026-05-02 | Service/controller | Tạo Draft, sửa Draft, không sửa khi locked |
| BAO-04 | Menu state machine | 2026-05-03 | Service/unit test | Không nhảy trạng thái sai |
| BAO-05 | Internal approval service | 2026-05-03 | Approval entities/service | Submit -> queue -> approve/request change |
| BAO-06 | BOM validation service | 2026-05-05 | Service/unit test | Chặn giấy đi chợ nếu món thiếu BOM |
| BAO-07 | Backend integration fixes | 2026-05-06 | Bug fixes | Flow menu/approval ổn định cho QA |

## 11. Card Cho Phong - Backend Dev 3

| ID | Nhiệm vụ | Deadline | Code/Evidence | Acceptance |
|---|---|---|---|---|
| PHG-01 | Customer/contact/contract schema | 2026-05-01 | Entities/migration | Khách hàng có contact và default qty |
| PHG-02 | Email form token service | 2026-05-03 | Token hash/expiry service | Tạo link dashboard/form, validate token, hết hạn bị chặn |
| PHG-03 | Dev email log service | 2026-05-03 | Email abstraction | Dev xem được email/link trong log/database |
| PHG-04 | Quantity resolution service | 2026-05-04 | Service/unit test | Pass rule previous week/final cutoff/extra |
| PHG-05 | Procurement calculation service | 2026-05-05 | Service/unit test | Gom nguyên liệu theo BOM và total qty |
| PHG-06 | Dashboard data service | 2026-05-06 | DTO/service | Có số liệu task quá hạn, menu chờ duyệt, chờ khách, thiếu BOM |
| PHG-07 | AI fallback/advisory service | 2026-05-06 | AI mock/fallback | Trả warning có citation khi task trễ hạn, số lượng biến động hoặc thiếu BOM |

## 12. Card Cho Khánh - Tester

| ID | Nhiệm vụ | Deadline | Code/Evidence | Acceptance |
|---|---|---|---|---|
| KHA-01 | Test plan theo core flow | 2026-04-30 | Test checklist | Có pass/fail/bug/evidence column |
| KHA-02 | Seed validation checklist | 2026-05-02 | QA notes/helper | Xác nhận có user, board, task, customer, menu, BOM, previous week qty |
| KHA-03 | Playwright smoke skeleton | 2026-05-02 | Test code | Có test login/dashboard/My Tasks hoặc skip rõ lý do |
| KHA-04 | Manual QA approval/email | 2026-05-04 | Bug list | Test duyệt nội bộ, token, public page |
| KHA-05 | Manual QA quantity/BOM | 2026-05-05 | Bug list | Test fallback số lượng và giấy đi chợ |
| KHA-06 | Regression dashboard/AI | 2026-05-06 | QA report | Dashboard và AI fallback không crash |
| KHA-07 | Release checklist/evidence | 2026-05-07 | Test report | Có go/no-go, screenshot và bug còn lại |

## 13. Dependency Map

| Feature | Phụ thuộc | Owner chính |
|---|---|---|
| Work board/task | Organization, RBAC | An, Bảo, Như, Nhật |
| Menu create | Customer, Dish | Bảo, Nhật |
| Internal approval | Menu state machine, roles | Bảo, An |
| Customer email forms | Menu InternalApproved, contact | Phong, Nhật |
| Customer submit forms + quantity | Customer form token, previous week seed | Phong, Nhật |
| Procurement | Quantity confirmed, BOM đầy đủ | Phong, Bảo, Như |
| Dashboard | Work/menu/approval/quantity/procurement data | Phong, Như |
| AI fallback | Work/dashboard/procurement/quantity context | Phong, Như |

## 14. Daily Checklist

| Thời điểm | Việc cần làm | Owner |
|---|---|---|
| 08:30 | Kéo card ngày từ Backlog sang Todo | Quân |
| 09:00 | Daily 15 phút: hôm qua, hôm nay, blocker | All |
| 13:30 | Kiểm tra dependency và Pending | Quân, Khánh |
| 17:00 | Review card đã xong implementation | Owner + reviewer |
| 21:00 | Cập nhật evidence, bug, screenshot | Khánh, Quân |

## 15. Definition Of Done

| Loại card | Done khi |
|---|---|
| Backend entity/service | Build pass, migration ổn, validation có, unit/manual test có evidence |
| Frontend view/form | Render được, validation rõ, không vỡ layout desktop, có screenshot |
| Work Management | Tạo/move/assign/comment task được, My Tasks đúng scope, có activity log |
| Approval/email | Đúng trạng thái, có audit, token hợp lệ/hết hạn xử lý đúng |
| Quantity/BOM | Pass rule test, không tính bằng dữ liệu client không tin cậy |
| Config-first | Không hard-code role, board column, label, slot, workflow, form, quantity rule, BOM rule, template hoặc prompt trong code |
| AI | Có fallback, không tự sửa dữ liệu, output có citation |
| Test | Có pass/fail, bug được gán owner, evidence lưu đúng |
| Docs | Không còn module cũ không liên quan, đúng tên đề tài cố định |

## 16. Checklist Gửi Giáo Viên Hướng Dẫn

Package ngày 2026-05-07 gồm:

- Link repository hoặc zip source.
- 4 file tài liệu chính:
  - Technical Implementation Blueprint.
  - Academic Report.
  - User Guide.
  - Project Work Plan 7 Members.
- Demo script 5-7 phút.
- Screenshot: login, dashboard, My Tasks, board, company, customer, menu, internal approval, customer review, quantity, BOM, procurement, AI.
- Test checklist pass/fail.
- Bug list còn lại.
- Câu hỏi xin góp ý:
  - Scope tenant Bizen và hướng product hóa đã đủ phù hợp với tên đề tài chưa?
  - Có cần mở rộng thêm kho/nhà cung cấp trong báo cáo không?
  - AI advisory ở mức mock/fallback có đủ cho giai đoạn demo góp ý không?
  - Bố cục báo cáo nộp trường có cần điều chỉnh theo template khoa không?

## 17. Scope Không Làm Trong Sprint Này

| Hạng mục | Lý do |
|---|---|
| Finance/Payment Request | Không phục vụ core flow suất ăn |
| KPI/OKR đầy đủ | Sprint này chỉ có số liệu task cơ bản, KPI/OKR để sau |
| Calendar/Gantt/Workload nâng cao | Sprint này chỉ cần Kanban/List/My Tasks |
| SOP/document repository đầy đủ | File đính kèm trong task đủ cho MVP, versioning để sau |
| CRM nâng cao | Chỉ cần customer/contact/hợp đồng cơ bản |
| Kho nâng cao | 1 tuần chỉ cần giấy đi chợ, tồn kho để sau |
| Supplier order tự động | Cần thêm quy trình mua hàng, để giai đoạn sau |
| Mobile app | MVC web đủ cho MVP |
| Self-service SaaS/billing | Sprint này chỉ cần tenant/config nền tảng, chưa cần tự đăng ký hoặc tính phí |
| AI provider thật bắt buộc | Mock/fallback đủ để demo decision support |
| Export PDF/XLSX nâng cao ngoài giấy đi chợ | Giấy đi chợ Sprint 1 phải xuất PDF/XLSX; các báo cáo khác để sau |
