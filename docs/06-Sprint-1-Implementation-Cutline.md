# OmniBizAI - Sprint 1 Implementation Cutline

> Ngày cập nhật: 2026-04-30
> Mục tiêu: khóa phạm vi code thật trong 7 ngày để bắt đầu MVP mà vẫn giữ kiến trúc product hóa, không hard-code nghiệp vụ.

## 1. Nguyên Tắc Cắt Scope

Sprint 1 phải demo được core flow end-to-end. Các năng lực tùy biến tối đa vẫn được thiết kế bằng entity/config, nhưng chưa cần làm đầy đủ UI quản trị cho mọi cấu hình.

Quy tắc:

- Làm engine/config service trước, UI cấu hình nâng cao sau.
- Dữ liệu demo và cấu hình tenant đọc từ JSON/import profile, không viết cứng trong C#.
- Nếu thiếu thời gian, giữ core flow chạy được và hạ phần cấu hình nâng cao xuống seed/profile.
- Không đưa Finance/KPI/CRM nâng cao vào sprint này; Work Management chỉ làm lõi Kanban/List/My Tasks.

## 2. Must-Have

Phải code thật trong 7 ngày:

| Hạng mục | Cách làm Sprint 1 |
|---|---|
| Auth/RBAC | Login, tenant context, permission handler đọc config seed/cache |
| Tenant/Company | Tenant Bizen, company, department, team, employee cơ bản |
| Work Management Core | Board/List/Card, My Tasks, assign, deadline, checklist, comment, attachment metadata, move task |
| Customer | Customer, contact, site, contract/default quantity |
| Dish/BOM | Dish, ingredient, unit/category config, DishBomItem |
| Menu | Tạo menu theo customer/site/shift/meal type/slot config |
| Internal Approval | Workflow step đọc từ `ApprovalWorkflowConfig` seed |
| Customer Email Token | Dev email log, token dashboard page, form góp ý/dự kiến/chốt/phát sinh |
| Quantity | Resolve quantity bằng `QuantityRuleConfig`, fallback theo tuần trước, chốt trước 09:00, phát sinh tăng/giảm hoặc tổng mới |
| Procurement | Preview/tạo/in/export PDF/XLSX giấy đi chợ từ BOM và TotalCookingQty |
| Audit tối thiểu | Ghi audit cho task create/move/assign/comment, duyệt, quantity, BOM/procurement |

## 3. Should-Have

Làm nếu Must-Have không bị chậm:

| Hạng mục | Cách làm Sprint 1 |
|---|---|
| Dashboard | Widget cơ bản: task quá hạn/sắp đến hạn, chờ duyệt, chờ khách, thiếu BOM, giấy đi chợ |
| Import staging tối thiểu | Upload CSV, lưu `ImportBatch`/`ImportStagingRow`, validate lỗi chính |
| Configuration screen nhẹ | Xem/sửa vài config quan trọng: board column, slot món, ca ăn, loại suất |
| Seed/import profile | Profile JSON cho tenant Bizen, không dùng C# constants nghiệp vụ |
| Manual QA evidence | Screenshot + checklist pass/fail |

## 4. Nice-To-Have

Chỉ làm khi còn thời gian:

| Hạng mục | Ghi chú |
|---|---|
| AI fallback | Rule-based summary có citation, không gọi provider thật nếu chưa cần |
| Configuration UI nâng cao | Role/permission, route permission, workflow editor, form editor |
| Import commit nâng cao | Duplicate resolution UI, mapping editor |
| Dashboard nâng cao | Filter, chart, drill-down, workload |

## 5. Không Làm Trong Sprint 1

- Self-service SaaS, billing/subscription.
- Workflow designer kéo thả.
- Calendar, Timeline/Gantt, Workload heatmap, KPI/OKR và SOP/document repository đầy đủ.
- Mobile app native.
- Tích hợp Lark API realtime.
- AI provider thật bắt buộc.
- Kho nâng cao, nhà cung cấp, purchase order, cost estimation.

## 6. Technology Cutline

Target chốt: `.NET 10`, ASP.NET Core MVC, EF Core 10, SQL Server.

Project phải target `net10.0`; package ASP.NET Core/Identity/EF Core dùng version 10.x thống nhất để tránh lệch runtime giữa máy dev, build và demo.

## 7. Definition Of Done Sprint 1

- Build pass trên runtime đã chốt.
- Demo chạy được: login -> mở board -> tạo/giao/move task -> My Tasks -> tạo menu -> Chị Nga/người duyệt cấu hình duyệt nội bộ -> gửi email token -> khách xem dashboard/form -> nhập số lượng -> tính và export giấy đi chợ.
- Không có role, workflow, board column, priority, slot, rule, template hoặc prompt hard-code trong service/controller/Razor.
- Có ít nhất một profile seed/import cho tenant demo.
- Có screenshot và checklist test cho core flow.
