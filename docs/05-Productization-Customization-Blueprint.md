# OmniBizAI - Productization And Customization Blueprint

> Ngày cập nhật: 2026-04-30
> Mục tiêu: biến dự án từ một MVP cho Bizen thành sản phẩm vận hành SME có Work Management + workflow nghiệp vụ có thể bán và tùy biến liên tục theo từng khách hàng.

## 1. Định Vị Sản Phẩm

OmniBizAI là nền tảng quản lý vận hành cho doanh nghiệp SME. Sản phẩm có lớp quản lý công việc theo phòng ban kiểu Trello/Jira/Asana và lớp workflow nghiệp vụ có phê duyệt, dashboard và AI advisory. Bizen Catering Services là tenant đầu tiên dùng để kiểm chứng nghiệp vụ suất ăn bằng dữ liệu Lark thật. Khi bán cho khách khác, hệ thống không đổi code lõi cho từng khách; thay vào đó cấu hình tenant, board, rule, form, workflow và import mapping.

## 2. Nguyên Tắc Thiết Kế Để Bán Được

- Không hard-code tên công ty, phòng ban, board, column, người duyệt, mã khách, bếp, ca ăn, vị trí món hoặc rule riêng của Bizen.
- Không hard-code role nghiệp vụ, permission mapping, workflow step, form field, nhãn UI, email template, dashboard widget, export template hoặc AI prompt.
- Mọi bảng nghiệp vụ chính có `TenantId`.
- Mọi rule có khả năng khác nhau theo khách hàng phải đi qua bảng cấu hình hoặc service config.
- Mọi seed/demo data phải nằm trong JSON/import profile hoặc file dữ liệu, không nằm trong C# constants.
- Dữ liệu thật đi qua staging/validation trước khi ghi vào bảng vận hành.
- Mọi thao tác quan trọng có audit log để truy vết khi sai số lượng, sai BOM hoặc phát hành giấy đi chợ.
- Demo thương mại dùng dữ liệu đã ẩn danh, trừ khi có quyền dùng dữ liệu thật.

## 2.1 Mức Tùy Biến Tối Đa

| Vùng hệ thống | Phải tùy biến được |
|---|---|
| Navigation/UI | Menu, nhãn, thứ tự tab, widget dashboard, màu trạng thái |
| RBAC | Role, permission, route-permission, scope theo phòng ban/site |
| Master data | Khách hàng, site, bếp, ca, loại suất, nhóm món, nhóm nguyên liệu, đơn vị |
| Work Management | Board, column, label, priority, task template, SLA, automation rule, view mặc định |
| Workflow | Bước duyệt, người/role duyệt, điều kiện bỏ qua, SLA, nhắc việc |
| Form | Field, thứ tự, required, option list, validation, public/private form; Bizen dùng form góp ý menu, dự kiến, chốt và phát sinh riêng |
| Quantity | Thứ tự fallback, fallback theo tuần trước, cutoff chốt 09:00, rule phát sinh tăng/giảm hoặc tổng mới, làm tròn |
| BOM/procurement | Hao hụt, gom theo bếp/ca/khách, quy đổi đơn vị, template giấy đi chợ |
| Integration/import | Mapping field, normalize rule, duplicate rule, commit strategy |
| AI | Prompt, citation policy, ngưỡng cảnh báo, template trả lời |

Code lõi chỉ cung cấp engine thực thi các cấu hình này. Nếu muốn thêm biến thể khách hàng mới mà phải sửa controller/service để đổi rule nghiệp vụ, thiết kế đó chưa đạt yêu cầu product hóa.

## 3. Các Lớp Cấu Hình Bắt Buộc

| Lớp cấu hình | Ví dụ |
|---|---|
| Tenant | Tên doanh nghiệp, logo, timezone, ngôn ngữ, email sender |
| Role/permission | Role, capability, route permission, scope theo site/phòng ban |
| Work board | Board theo phòng ban, column Kanban, label màu, priority, rule quá hạn |
| Ca ăn | Ca 1, Ca 2, S1, SN, S6, ca gãy |
| Loại suất | Suất mặn, chay, nước, mì sữa, cháo, buffet, suất quản lý |
| Vị trí món | Mặn 1-6, chay 1-3, nước 1-3, canh, xào/luộc 1-2 |
| Bếp theo món/site | Một site có thể dùng bếp khác nhau cho món mặn, canh, nước, chay |
| Rule số lượng | Ưu tiên khách nhập, lấy cùng ngày/ca/loại suất của tuần trước, lấy hợp đồng, bắt buộc nội bộ nhập |
| Workflow duyệt | Một người duyệt, nhiều bước duyệt, bỏ qua QA, thêm bếp/ops |
| BOM/procurement | Hao hụt, làm tròn, gom theo ngày/ca/bếp/khách |
| Email/form | Template gửi khách, token, dashboard menu, mapping form góp ý/dự kiến/chốt trước 09:00/phát sinh |
| UI/report/AI | Dashboard widget, export template, AI prompt, status label |

## 4. Pipeline Dữ Liệu Thật

Luồng chuẩn:

```text
Lark CSV/API
  -> ImportBatch
  -> ImportStagingRow
  -> ImportMapping
  -> ImportValidationIssue
  -> Normalize master data
  -> Commit vào bảng vận hành
```

Không map thẳng CSV vào `MenuPlan`, `DailyMealOrder` hoặc `DishBomItem`. Export Lark thực tế có ô trống, dòng lặp, mã chưa chuẩn và nhiều cột tính toán, nên cần giữ dữ liệu gốc để đối soát.

## 5. Roadmap Thương Mại Hóa

| Giai đoạn | Mục tiêu |
|---|---|
| Sprint demo | Tenant profile đầu tiên, Work Management Core, import CSV thủ công, core flow end-to-end |
| Pilot thật | Board theo phòng ban, staging import, cấu hình slot/ca/suất/bếp, audit đầy đủ |
| Bán cho khách thứ hai | Tạo tenant mới, board/workflow/import mapping riêng, không sửa code lõi |
| SaaS/on-prem | Tenant isolation, backup, phân quyền support, license/billing |
| Mở rộng | SOP repository, KPI/OKR, Calendar/Gantt/Workload, kho, nhà cung cấp, cost estimation, forecast, API tích hợp |

## 6. Checklist Trước Khi Bán

- [ ] Có tenant isolation bằng `TenantId` và filter dữ liệu.
- [ ] Có màn hình cấu hình role, permission, board/column/label, ca ăn, loại suất, slot món, bếp, workflow, form, template và AI prompt.
- [ ] Có checklist scan code chứng minh không hard-code nghiệp vụ theo tenant.
- [ ] Có import staging kèm báo lỗi và preview trước khi commit.
- [ ] Có audit log cho task create/move/assign/comment, duyệt, chốt số lượng, sửa BOM, phát hành/hủy giấy đi chợ.
- [ ] Có seed/demo data đã ẩn danh.
- [ ] Có tài liệu triển khai tenant mới.
- [ ] Có backup/restore và quy trình xử lý sự cố dữ liệu.

## 7. No-Hard-Code Audit

Trước mỗi bản demo/bán hàng, rà code theo checklist:

- Không có tên tenant/khách hàng/site/bếp thật trong controller, service, Razor hoặc migration seed C#.
- Không có `AppRoles`, role string hoặc email người duyệt trong code nghiệp vụ.
- Không có enum nghiệp vụ cho board column, priority, task label, ca ăn, loại suất, vị trí món, nhóm món, nhóm nguyên liệu hoặc workflow step.
- Không có form field, validation message, email subject/body, export layout hoặc AI prompt viết cứng trong Razor/service.
- Các giá trị demo nằm trong JSON/import profile và có thể thay bằng profile khách hàng khác.
- Mọi route/action nhạy cảm kiểm quyền qua permission config/cache.
