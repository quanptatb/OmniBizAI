# OmniBizAI - Sprint 1 Implementation Cutline

> Ngày cập nhật: 2026-04-30
> Mục tiêu: khóa phạm vi code thật trong 7 ngày để bắt đầu MVP mà vẫn giữ kiến trúc product hóa, không hard-code nghiệp vụ.

## 1. Nguyên Tắc Cắt Scope

Sprint 1 phải demo được core flow end-to-end. Các năng lực tùy biến tối đa vẫn được thiết kế bằng entity/config, nhưng chưa cần làm đầy đủ UI quản trị cho mọi cấu hình.

Quy tắc:

- Làm engine/config service trước, UI cấu hình nâng cao sau.
- Dữ liệu demo và cấu hình tenant đọc từ JSON/import profile, không viết cứng trong C#.
- Nếu thiếu thời gian, giữ core flow chạy được và hạ phần cấu hình nâng cao xuống seed/profile.
- Không đưa Finance/KPI/CRM nâng cao vào sprint này.

## 2. Must-Have

Phải code thật trong 7 ngày:

| Hạng mục | Cách làm Sprint 1 |
|---|---|
| Auth/RBAC | Login, tenant context, permission handler đọc config seed/cache |
| Tenant/Company | Tenant Bizen, company, department, employee cơ bản |
| Customer | Customer, contact, site, contract/default quantity |
| Dish/BOM | Dish, ingredient, unit/category config, DishBomItem |
| Menu | Tạo menu theo customer/site/shift/meal type/slot config |
| Internal Approval | Workflow step đọc từ `ApprovalWorkflowConfig` seed |
| Customer Email Token | Dev email log, token review page, approve/request-change action code |
| Quantity | Resolve quantity bằng `QuantityRuleConfig`, previous-day fallback |
| Procurement | Preview/tạo giấy đi chợ từ BOM và TotalCookingQty |
| Audit tối thiểu | Ghi audit cho duyệt, quantity, BOM/procurement |

## 3. Should-Have

Làm nếu Must-Have không bị chậm:

| Hạng mục | Cách làm Sprint 1 |
|---|---|
| Dashboard | Widget cơ bản: chờ duyệt, chờ khách, thiếu BOM, giấy đi chợ |
| Import staging tối thiểu | Upload CSV, lưu `ImportBatch`/`ImportStagingRow`, validate lỗi chính |
| Configuration screen nhẹ | Xem/sửa vài config quan trọng: slot món, ca ăn, loại suất |
| Seed/import profile | Profile JSON cho tenant Bizen, không dùng C# constants nghiệp vụ |
| Manual QA evidence | Screenshot + checklist pass/fail |

## 4. Nice-To-Have

Chỉ làm khi còn thời gian:

| Hạng mục | Ghi chú |
|---|---|
| AI fallback | Rule-based summary có citation, không gọi provider thật nếu chưa cần |
| Configuration UI nâng cao | Role/permission, route permission, workflow editor, form editor |
| Export PDF/XLSX | HTML print đủ cho sprint 1 |
| Import commit nâng cao | Duplicate resolution UI, mapping editor |
| Dashboard nâng cao | Filter, chart, drill-down |

## 5. Không Làm Trong Sprint 1

- Self-service SaaS, billing/subscription.
- Workflow designer kéo thả.
- Mobile app native.
- Tích hợp Lark API realtime.
- AI provider thật bắt buộc.
- Kho nâng cao, nhà cung cấp, purchase order, cost estimation.

## 6. Technology Cutline

Target ưu tiên: `.NET 8 LTS`, ASP.NET Core MVC, EF Core 8, SQL Server.

Nếu toàn bộ máy dev và môi trường chấm/demo đã sẵn sàng, có thể nâng lên `.NET 10`/EF Core 10. Tài liệu và code không được phụ thuộc API chỉ có ở .NET 10 nếu nhóm chưa chốt runtime.

## 7. Definition Of Done Sprint 1

- Build pass trên runtime đã chốt.
- Demo chạy được: login -> tạo menu -> duyệt nội bộ -> gửi token -> khách duyệt/nhập số lượng -> tính giấy đi chợ.
- Không có role, workflow, slot, rule, template hoặc prompt hard-code trong service/controller/Razor.
- Có ít nhất một profile seed/import cho tenant demo.
- Có screenshot và checklist test cho core flow.
