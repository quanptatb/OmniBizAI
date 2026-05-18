# BÁO CÁO DỰ ÁN TỐT NGHIỆP

# XÂY DỰNG HỆ THỐNG QUẢN LÝ VẬN HÀNH THÔNG MINH CHO DOANH NGHIỆP VỪA VÀ NHỎ — OMNIBIZAI

---

> **Trường:** [TÊN_TRƯỜNG]
> **Khoa:** [TÊN_KHOA]
> **Ngành:** [TÊN_NGÀNH]
> **Học kỳ:** [HỌC_KỲ]
> **Giảng viên hướng dẫn:** [TÊN_GVHD]

---

## THEO DÕI PHIÊN BẢN

| Phiên bản | Ngày      | Nội dung thay đổi    | Tác giả     |
| ----------- | ---------- | ----------------------- | ------------- |
| 1.0         | 18/05/2026 | Phiên bản đầu tiên | Nhóm dự án |

---

## MỤC LỤC

- Chương 1: Giới thiệu
- Chương 2: Phân tích
- Chương 3: Thiết kế
- Chương 4: Thực thi
- Chương 5: Kiểm thử
- Chương 6: Hướng dẫn sử dụng
- Chương 7: Tổng kết và đánh giá
- Phụ lục A–D

---

## QUY ƯỚC TÀI LIỆU

| Ký hiệu          | Ý nghĩa                    |
| ------------------ | ---------------------------- |
| `[TÊN]`         | Placeholder cần thay thế   |
| **In đậm** | Thuật ngữ quan trọng      |
| `Code`           | Mã nguồn, lệnh, tên file |
| ✅                 | Hoàn thành                 |
| ⚠️               | Cần lưu ý                 |

---

## BẢNG CHÚ GIẢI THUẬT NGỮ

| Thuật ngữ | Giải thích                                                 |
| ----------- | ------------------------------------------------------------ |
| SME         | Small and Medium Enterprise — Doanh nghiệp vừa và nhỏ   |
| MVC         | Model-View-Controller — Kiến trúc phân tầng             |
| EF Core     | Entity Framework Core — ORM của .NET                       |
| RBAC        | Role-Based Access Control — Phân quyền theo vai trò      |
| CRUD        | Create, Read, Update, Delete                                 |
| OKR         | Objectives and Key Results                                   |
| KPI         | Key Performance Indicator                                    |
| CRM         | Customer Relationship Management                             |
| PO          | Purchase Order — Đơn mua hàng                            |
| GR/GI       | Goods Receipt / Goods Issue — Nhập/Xuất kho               |
| Tenant      | Đơn vị tổ chức (công ty) trong hệ thống multi-tenant |
| Soft Delete | Xóa mềm — đánh dấu IsDeleted thay vì xóa vật lý    |

---

## DANH SÁCH THÀNH VIÊN

| STT | Họ và tên     | MSSV  | Vai trò       |
| --- | ---------------- | ----- | -------------- |
| 1   | [Thành viên 1] | [...] | Nhóm trưởng |
| 2   | [Thành viên 2] | [...] | Thành viên   |
| 3   | [Thành viên 3] | [...] | Thành viên   |
| 4   | [Thành viên 4] | [...] | Thành viên   |

---

## LỜI CẢM ƠN

Nhóm chúng em xin chân thành cảm ơn thầy/cô [TÊN_GVHD] đã tận tình hướng dẫn trong suốt quá trình thực hiện dự án. Cảm ơn các thầy cô trong khoa [TÊN_KHOA] đã truyền đạt kiến thức nền tảng. Cảm ơn gia đình và bạn bè đã luôn động viên, hỗ trợ.

---

## LỜI MỞ ĐẦU

Trong bối cảnh chuyển đổi số đang diễn ra mạnh mẽ tại Việt Nam, các doanh nghiệp vừa và nhỏ (SME) đang đối mặt với thách thức lớn trong việc số hóa quy trình vận hành. Phần lớn SME vẫn quản lý bằng Excel, giấy tờ hoặc các phần mềm rời rạc, dẫn đến thiếu đồng bộ dữ liệu và khó ra quyết định kịp thời.

Dự án **OmniBizAI** ra đời nhằm cung cấp một giải pháp toàn diện, tích hợp đa module trên nền tảng web, giúp SME quản lý vận hành, tài chính, nhân sự, CRM và đo lường hiệu suất (KPI/OKR) trên một hệ thống duy nhất, với sự hỗ trợ của trí tuệ nhân tạo (AI Copilot).

---

## TÓM TẮT NỘI DUNG DỰ ÁN

OmniBizAI là hệ thống web application được xây dựng trên nền tảng ASP.NET Core MVC 10 với SQL Server, thiết kế theo kiến trúc multi-tenant, phân quyền RBAC 7 vai trò. Hệ thống bao gồm các module: Vận hành (Operations, Kanban, Approvals), Tài chính (Budget, Expense, CashBook), Mua sắm & Kho vận (Procurement, PO, GR/GI, Inventory), Nhân sự (Organization, Employee, Leave), CRM (Customer, Vendor, Product), KPI/OKR & Đánh giá, Báo cáo đa chiều, và AI Copilot tích hợp Google Gemini.

---

# CHƯƠNG 1: GIỚI THIỆU

## 1.1. Bối cảnh — Hiện trạng

Theo số liệu của Tổng cục Thống kê, Việt Nam có hơn 900.000 doanh nghiệp đang hoạt động, trong đó 98% là SME. Tuy nhiên, mức độ ứng dụng CNTT trong quản lý vận hành của nhóm này còn rất thấp:

- **70%** SME vẫn sử dụng Excel hoặc giấy tờ để quản lý
- **Chỉ 15%** có phần mềm ERP nhưng thường đắt đỏ, phức tạp
- **Thiếu tích hợp**: Nhân sự dùng 1 phần mềm, tài chính dùng 1 phần mềm khác, CRM riêng → dữ liệu phân mảnh

## 1.2. Lý do chọn đề tài

- Nhu cầu thực tế lớn từ SME Việt Nam
- Các giải pháp hiện tại (SAP, Odoo, Base.vn) quá phức tạp hoặc đắt cho SME
- Cơ hội ứng dụng AI (Gemini) vào phân tích dữ liệu doanh nghiệp
- Phù hợp để áp dụng kiến thức ASP.NET MVC, EF Core, SQL Server đã học

## 1.3. Mục tiêu dự án

1. Xây dựng hệ thống web tích hợp đa module phục vụ vận hành SME
2. Thiết kế kiến trúc multi-tenant, RBAC, đảm bảo bảo mật
3. Tích hợp AI Copilot hỗ trợ phân tích và đề xuất
4. Giao diện đẹp, hiện đại theo Apple Design System
5. Cung cấp bộ báo cáo đa chiều phục vụ ra quyết định

## 1.4. Phạm vi dự án

**Trong phạm vi:**

- 8 module nghiệp vụ chính (Vận hành, Tài chính, Mua sắm, Kho, HR, CRM, KPI/OKR, Báo cáo)
- AI Copilot (hỏi đáp dựa trên dữ liệu)
- Hệ thống thông báo, audit log, sao lưu
- Giao diện responsive, hỗ trợ đa chế độ điều hướng

**Ngoài phạm vi:**

- Mobile app native (iOS/Android)
- Thanh toán trực tuyến
- Tích hợp API bên thứ 3 (ngân hàng, logistics)

## 1.5. Đối tượng sử dụng

| # | Đối tượng              | Mô tả                             |
| - | -------------------------- | ----------------------------------- |
| 1 | Chủ doanh nghiệp / Admin | Quản lý toàn bộ hệ thống      |
| 2 | Ban Giám đốc            | Xem báo cáo, phê duyệt cấp cao |
| 3 | Trưởng phòng            | Quản lý nhóm, phê duyệt        |
| 4 | Nhân viên                | Thực hiện nghiệp vụ hàng ngày |
| 5 | Kế toán                  | Quản lý tài chính, thu chi      |
| 6 | Kiểm toán                | Giám sát nhật ký hệ thống     |
| 7 | Quản trị hệ thống      | Quản lý kỹ thuật, cài đặt    |

## 1.6. Nguồn lực — Kế hoạch

- **Nhân lực:** 4 thành viên
- **Thời gian:** 16 tuần (8 sprints × 2 tuần)
- **Công cụ:** Visual Studio, SQL Server, Git/GitHub
- **Chi tiết timeline:** Xem tài liệu Quản lý Dự án (File D)

## 1.7. Phương pháp phát triển phần mềm

Áp dụng **Agile/Scrum** với sprint 2 tuần:

- Sprint Planning → Daily Standup → Sprint Review → Retrospective
- Backlog quản lý trên GitHub Issues
- Code review qua Pull Request

## 1.8. Khảo sát hệ thống tương tự

| Hệ thống                 | Ưu điểm            | Nhược điểm                        |
| -------------------------- | --------------------- | ------------------------------------- |
| **SAP Business One** | Đầy đủ module ERP | Quá đắt cho SME, phức tạp        |
| **Odoo**             | Open-source, modular  | UI phức tạp, cần tùy biến nhiều |
| **Base.vn**          | Giao diện Việt hóa | Thiếu module tài chính, KPI sâu   |
| **1Office**          | Phù hợp SME Việt   | Không có AI, KPI/OKR hạn chế      |

**OmniBizAI** khác biệt ở: tích hợp AI Copilot, module KPI/OKR đầy đủ, giao diện Apple Design, multi-tenant, mã nguồn mở cho SME.

---

# CHƯƠNG 2: PHÂN TÍCH

## 2.1. Yêu cầu người dùng

1. Quản lý yêu cầu công việc theo quy trình phê duyệt
2. Theo dõi tiến độ công việc trực quan (Kanban)
3. Quản lý thu chi, ngân sách, đề nghị thanh toán
4. Quản lý mua sắm từ đề xuất đến nhập kho
5. Quản lý khách hàng, nhà cung cấp, sản phẩm
6. Thiết lập và theo dõi KPI/OKR
7. Xem báo cáo tổng hợp đa chiều
8. Phân quyền chặt chẽ theo vai trò

## 2.2. Yêu cầu chức năng

| #  | Mã   | Yêu cầu                                      | Mức ưu tiên |
| -- | ----- | ---------------------------------------------- | -------------- |
| 1  | FR-01 | Đăng nhập / Đăng xuất / Quên mật khẩu | Cao            |
| 2  | FR-02 | CRUD Yêu cầu vận hành + phê duyệt        | Cao            |
| 3  | FR-03 | Kanban workflow drag-drop                      | Cao            |
| 4  | FR-04 | Quản lý ngân sách, chi phí                | Cao            |
| 5  | FR-05 | Sổ thu chi (CashBook)                         | Cao            |
| 6  | FR-06 | Đề xuất mua sắm → PO → Nhập/Xuất kho   | Cao            |
| 7  | FR-07 | Cảnh báo tồn kho                            | Trung bình    |
| 8  | FR-08 | CRUD Khách hàng, NCC, Sản phẩm             | Cao            |
| 9  | FR-09 | KPI Setup, Check-In, Đánh giá               | Cao            |
| 10 | FR-10 | OKR Objectives & Key Results                   | Cao            |
| 11 | FR-11 | Báo cáo đa chiều (7 loại)                 | Trung bình    |
| 12 | FR-12 | AI Copilot phân tích dữ liệu               | Trung bình    |
| 13 | FR-13 | Quản lý phòng ban, nhân viên              | Cao            |
| 14 | FR-14 | Nghỉ phép                                    | Trung bình    |
| 15 | FR-15 | Thông báo real-time                          | Trung bình    |
| 16 | FR-16 | Sao lưu dữ liệu                             | Thấp          |
| 17 | FR-17 | Cài đặt giao diện / branding               | Thấp          |

## 2.3. Yêu cầu phi chức năng

| #      | Yêu cầu   | Mô tả                                                |
| ------ | ----------- | ------------------------------------------------------ |
| NFR-01 | Hiệu suất | Trang tải < 3 giây trên kết nối ổn định        |
| NFR-02 | Bảo mật   | Mã hóa mật khẩu, CSRF protection, tenant isolation |
| NFR-03 | Khả dụng  | Hoạt động 24/7 trên trình duyệt hiện đại      |
| NFR-04 | Mở rộng   | Kiến trúc multi-tenant, dễ thêm module             |
| NFR-05 | Giao diện  | Responsive, đẹp, Apple Design System                 |
| NFR-06 | Dữ liệu   | Soft delete, audit trail đầy đủ                    |

## 2.4. Danh sách tác nhân

| # | Tác nhân         | Mô tả                     |
| - | ------------------ | --------------------------- |
| 1 | System Admin       | Quản trị toàn hệ thống |
| 2 | Tenant Admin       | Quản trị tổ chức        |
| 3 | Executive          | Ban giám đốc             |
| 4 | Department Manager | Trưởng phòng             |
| 5 | Staff              | Nhân viên                 |
| 6 | Accountant         | Kế toán                   |
| 7 | Auditor            | Kiểm toán                 |

## 2.5. Danh sách Use Case (tóm tắt)

*(Chi tiết sơ đồ: xem File F — Sơ đồ Mermaid, mục 1–2)*

| #     | Use Case                    | Tác nhân chính  |
| ----- | --------------------------- | ------------------ |
| UC-01 | Đăng nhập / Đăng xuất | Tất cả           |
| UC-02 | Tạo yêu cầu vận hành   | Staff, Manager     |
| UC-03 | Phê duyệt yêu cầu       | Manager, Executive |
| UC-04 | Quản lý Kanban            | Staff, Manager     |
| UC-05 | Quản lý ngân sách       | Accountant, Admin  |
| UC-06 | Ghi nhận thu chi           | Accountant         |
| UC-07 | Đề xuất mua sắm         | Staff, Manager     |
| UC-08 | Tạo đơn mua hàng        | Manager            |
| UC-09 | Nhập/Xuất kho             | Staff              |
| UC-10 | Quản lý khách hàng      | Staff, Manager     |
| UC-11 | Thiết lập KPI             | Admin, Manager     |
| UC-12 | KPI Check-In                | Staff              |
| UC-13 | Đánh giá hiệu suất     | Manager            |
| UC-14 | Xem báo cáo               | Executive, Manager |
| UC-15 | AI Copilot                  | Tất cả           |
| UC-16 | Quản lý phòng ban        | Admin              |
| UC-17 | Nghỉ phép                 | Staff              |
| UC-18 | Cài đặt hệ thống       | Admin              |
| UC-19 | Sao lưu                    | Admin              |
| UC-20 | Xem audit log               | Auditor, Admin     |

## 2.6–2.9. Sơ đồ Use Case, Activity, ERD

> Xem chi tiết tại **File F — Sơ đồ Mermaid** (`docs/report/F_SoDo_Mermaid.md`), bao gồm:
>
> - Sơ đồ Use Case tổng (Mục 1) và phân hệ Vận hành (Mục 2)
> - Activity Diagram: Luồng vận hành (Mục 3), Mua sắm (Mục 4), KPI (Mục 5)
> - ERD tổng quan (Mục 6) và ERD Tài chính (Mục 7)

---

# CHƯƠNG 3: THIẾT KẾ

## 3.1. Kiến trúc hệ thống

Hệ thống áp dụng kiến trúc **MVC 3 lớp** mở rộng thêm Service Layer:

```
Browser → Controller → Service → DbContext (EF Core) → SQL Server
```

*(Chi tiết sơ đồ kiến trúc: xem File B — Tài liệu Kỹ thuật, mục 2)*

## 3.2. Thiết kế cơ sở dữ liệu

- **DBMS:** SQL Server 2022
- **Database name:** OmniBizDB
- **Số bảng:** ~95 (bao gồm ASP.NET Identity)
- **Base Entity:** `TenantEntity` với các trường: Id, TenantId, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted

## 3.3–3.4. Danh sách bảng & Đặc tả

*(Chi tiết: xem File B — Tài liệu Kỹ thuật, mục 7)*

Tổng hợp theo nhóm:

- Tenant & Config: 6 bảng
- Auth / RBAC: 11 bảng
- Organization & HR: 7 bảng
- CRM & Catalog: 9 bảng
- Operations & Work: 11 bảng
- Workflow & Approval: 6 bảng
- Finance & Procurement: 14 bảng
- KPI / OKR: 17 bảng
- AI / Audit / Notification: 9 bảng
- Reports: 2 bảng

## 3.5. Thiết kế giao diện người dùng

Giao diện được thiết kế theo **Apple Design System** với các đặc điểm:

- Font: Inter (Google Fonts)
- Màu chủ đạo: Apple Blue (#0071e3)
- Layout: Sidebar (dark) + Topbar (frosted glass) + Content area
- 3 chế độ điều hướng: Classic Sidebar, Card Sidebar, App Launcher
- Responsive: Hỗ trợ desktop, tablet

## 3.6. Sơ đồ tổ chức giao diện

*(Xem File F — Sơ đồ Mermaid, mục 14 — Sitemap)*

## 3.7. Thiết kế phân quyền

7 vai trò RBAC với ma trận phân quyền chi tiết.
*(Xem File B — Tài liệu Kỹ thuật, mục 8)*

## 3.8. Thiết kế bảo mật

| Lớp             | Giải pháp                              |
| ---------------- | ---------------------------------------- |
| Authentication   | ASP.NET Identity, Cookie 8h, Sliding     |
| Authorization    | Role-based, Controller-level             |
| Data Protection  | CSRF Token, Soft Delete                  |
| Tenant Isolation | Global Query Filter                      |
| Audit            | AuditLog table ghi nhận mọi thay đổi |
| Lockout          | 5 lần sai → khóa 15 phút             |

---

# CHƯƠNG 4: THỰC THI

## 4.1. Công nghệ sử dụng

| Thành phần | Công nghệ                         |
| ------------ | ----------------------------------- |
| Backend      | ASP.NET Core MVC 10, C# 14          |
| ORM          | Entity Framework Core 10            |
| Database     | SQL Server 2022                     |
| Frontend     | HTML5, CSS3, JS ES6+, Bootstrap 5.3 |
| AI           | Google Gemini API                   |
| Excel        | ClosedXML 0.104.2                   |

## 4.2. Môi trường phát triển

- **OS:** Windows 11
- **IDE:** Visual Studio 2022 / JetBrains Rider
- **VCS:** Git + GitHub
- **DB Tool:** SQL Server Management Studio

## 4.3. Tổ chức mã nguồn

*(Xem File B — Tài liệu Kỹ thuật, mục 5)*

## 4.4. Package / Module hệ thống

| #  | Module      | Controllers                                             | Services                                            |
| -- | ----------- | ------------------------------------------------------- | --------------------------------------------------- |
| 1  | Core        | AccountController, DashboardController                  | AppServices                                         |
| 2  | Operations  | OperationsController, WorkflowController                | OperationRequestService, WorkKanbanService          |
| 3  | Approvals   | ApprovalsController                                     | ApprovalService                                     |
| 4  | Finance     | BusinessControllers, CashBookController                 | CashBookService                                     |
| 5  | Procurement | ProcurementControllers, InventoryController             | ProcurementService, InventoryService                |
| 6  | CRM         | CrmControllers, SalesOpportunityController              | CrmService                                          |
| 7  | HR          | HrSettingsControllers, LeaveController                  | HrService                                           |
| 8  | KPI/OKR     | KpiSetupController, OkrController, KpiCheckInController | KpiOkrServices                                      |
| 9  | AI          | (trong BusinessControllers)                             | GeminiService, AiInsightService                     |
| 10 | System      | (trong HrSettingsControllers)                           | SettingsService, BackupService, NotificationService |

## 4.5. Thư viện sử dụng

*(Xem File B — Tài liệu Kỹ thuật, mục 15)*

## 4.6. Đặc tả chức năng đã triển khai

Tổng cộng **22 Controller files**, **12 Service classes**, **39 View directories**, **~95 Entity models** đã được triển khai đầy đủ.

## 4.7. Sequence Diagrams

*(Xem File F — Sơ đồ Mermaid, mục 8–10: Đăng nhập, Tạo yêu cầu, Phê duyệt)

# CHƯƠNG 5: KIỂM THỬ

## 5.1. Kế hoạch kiểm thử

Kiểm thử được thực hiện thủ công (Manual Testing) theo phương pháp Black-box, tập trung vào chức năng và phân quyền.

## 5.2. Phạm vi kiểm thử

Toàn bộ 8 module nghiệp vụ + Authentication + Authorization. Không bao gồm performance testing.

## 5.3. Chiến lược kiểm thử

- **Smoke Test** sau mỗi sprint
- **Regression Test** khi có thay đổi lớn
- **UAT** trước khi demo

## 5.4. Test Case (tóm tắt)

| ID     | Tên test case                           | Module      | Kết quả |
| ------ | ---------------------------------------- | ----------- | --------- |
| TC-001 | Đăng nhập thành công                | Auth        | ✅ Pass   |
| TC-002 | Đăng nhập thất bại (sai password)   | Auth        | ✅ Pass   |
| TC-003 | Tạo yêu cầu vận hành                | Operations  | ✅ Pass   |
| TC-004 | Phê duyệt yêu cầu                    | Approvals   | ✅ Pass   |
| TC-005 | Kanban kéo thả                         | Workflow    | ✅ Pass   |
| TC-006 | Ghi nhận giao dịch thu chi             | CashBook    | ✅ Pass   |
| TC-007 | Phân quyền STAFF không thấy Settings | Auth        | ✅ Pass   |
| TC-008 | Tạo đề xuất mua sắm                 | Procurement | ✅ Pass   |
| TC-009 | Seed data chạy không lỗi              | Data        | ✅ Pass   |
| TC-010 | Chuyển đổi 3 chế độ nav            | UI          | ✅ Pass   |

*(Chi tiết từng test case: xem File E — Tài liệu Kiểm thử)*

## 5.5. Kết quả kiểm thử

| Loại           |    Tổng    |     Pass     |    Fail    |
| --------------- | :----------: | :----------: | :---------: |
| Functional      |      10      |      10      |      0      |
| Authorization   |      3      |      3      |      0      |
| UI/UX           |      3      |      3      |      0      |
| Data            |      2      |      2      |      0      |
| **Tổng** | **18** | **18** | **0** |

## 5.6. Đánh giá lỗi và hướng khắc phục

| Lỗi                           | Nguyên nhân                              | Khắc phục                    |
| ------------------------------ | ------------------------------------------ | ------------------------------ |
| LINQ DateOnly không translate | EF Core không hỗ trợ DateOnly trong SQL | Chuyển sang DateTimeOffset    |
| FK conflict khi seed           | Thứ tự DELETE không đúng              | Thêm SET NULL trước DELETE  |
| Invalid GUID trong seed        | Ký tự chữ cái đầu không hợp lệ    | Sửa thành hex hợp lệ (A-F) |
| NULL column khi INSERT         | Thiếu cột bắt buộc                     | Bổ sung đầy đủ cột       |

---

# CHƯƠNG 6: HƯỚNG DẪN SỬ DỤNG

*(Tóm tắt — chi tiết đầy đủ: xem File C — Hướng dẫn Sử dụng)*

## 6.1. Hướng dẫn đăng nhập

Truy cập hệ thống → Nhập email + password → Nhấn Đăng nhập → Dashboard

## 6.2. Hướng dẫn theo từng vai trò

| Vai trò        | Chức năng chính                              |
| --------------- | ----------------------------------------------- |
| Admin           | Phòng ban, Tài khoản, Cài đặt, Sao lưu   |
| Trưởng phòng | Phê duyệt, Kanban, KPI                        |
| Nhân viên     | Yêu cầu vận hành, Check-In KPI, Nghỉ phép |
| Kế toán       | Thu chi, Ngân sách, Đề nghị thanh toán    |

## 6.3. Chức năng chính

- **Vận hành:** Tạo yêu cầu → Gửi duyệt → Kanban → Hoàn thành
- **Tài chính:** Ngân sách → Chi phí → Thu chi → Báo cáo
- **Mua sắm:** Đề xuất → PO → Nhập kho → Tồn kho
- **KPI/OKR:** Setup → Gán → Check-In → Đánh giá

## 6.4. Xử lý lỗi thường gặp

| Lỗi                    | Giải pháp                               |
| ----------------------- | ----------------------------------------- |
| Đăng nhập thất bại | Kiểm tra email/password, liên hệ Admin |
| Không thấy menu       | Vai trò chưa được gán quyền        |
| Trang trắng            | Tài khoản chưa gán tenant             |

---

# CHƯƠNG 7: TỔNG KẾT VÀ ĐÁNH GIÁ

## 7.1. Kết quả đạt được

1. ✅ Xây dựng thành công hệ thống web tích hợp 8+ module nghiệp vụ
2. ✅ Kiến trúc multi-tenant, RBAC 7 vai trò
3. ✅ Tích hợp AI Copilot (Google Gemini)
4. ✅ Giao diện Apple Design System, 3 chế độ điều hướng
5. ✅ Database ~95 bảng, seed data đầy đủ 105+ nhân viên
6. ✅ Bộ tài liệu hoàn chỉnh (Kỹ thuật, Hướng dẫn, Kiểm thử, Quản lý)

## 7.2. Mức độ hoàn thành

| Module                  | Trạng thái | %    |
| ----------------------- | ------------ | ---- |
| Authentication & RBAC   | Hoàn thành | 100% |
| Operations & Workflow   | Hoàn thành | 100% |
| Finance & CashBook      | Hoàn thành | 100% |
| Procurement & Inventory | Hoàn thành | 100% |
| CRM                     | Hoàn thành | 100% |
| KPI / OKR               | Hoàn thành | 100% |
| HR & Leave              | Hoàn thành | 95%  |
| Reports                 | Hoàn thành | 100% |
| AI Copilot              | Hoàn thành | 90%  |
| UI / UX                 | Hoàn thành | 100% |

## 7.3. Khó khăn

1. **Công nghệ mới:** .NET 10 đang ở bản Preview, tài liệu hạn chế
2. **Multi-tenant phức tạp:** Đảm bảo data isolation trên mọi query
3. **Cascade Delete:** SQL Server không cho phép multiple cascade paths
4. **Seed Data:** Phải mapping chính xác ~95 bảng với FK constraints
5. **Thời gian hạn chế:** 4 thành viên, 16 tuần cho hệ thống lớn

## 7.4. Giải pháp

1. Pin phiên bản .NET Preview, test kỹ trước khi dùng API mới
2. Sử dụng Global Query Filter + ITenantContext interface
3. Tắt cascade delete toàn cục: `DeleteBehavior.Restrict`
4. Viết seed script SQL với thứ tự INSERT/DELETE chặt chẽ
5. Áp dụng Agile/Scrum, ưu tiên module theo độ quan trọng

## 7.5. Bài học kinh nghiệm

- Kiến trúc multi-tenant nên thiết kế từ đầu, rất khó bổ sung sau
- EF Core Global Query Filter là công cụ mạnh cho tenant isolation
- Apple Design System giúp giao diện chuyên nghiệp nhưng đòi hỏi CSS discipline
- AI integration (Gemini) cần có fallback khi API không khả dụng
- Seed data nên viết incrementally, test từng phần

## 7.6. Hướng phát triển

1. **Mobile App:** Phát triển ứng dụng React Native / Flutter
2. **Cloud Deployment:** Deploy lên Azure / AWS với CI/CD pipeline
3. **Backup to Cloud:** Sao lưu lên Azure Blob Storage / S3
4. **Background Jobs:** Tích hợp Hangfire cho tác vụ nền (email, cleanup)
5. **Advanced AI:** Tích hợp RAG (Retrieval-Augmented Generation) cho AI chính xác hơn
6. **Multi-language:** Hỗ trợ đa ngôn ngữ (i18n)
7. **API Gateway:** Cung cấp REST API cho tích hợp bên thứ 3

---

# PHỤ LỤC A: ĐẶC TẢ USE CASE

*(Xem chi tiết tại File F — Sơ đồ Mermaid, mục 1–2)*

Hệ thống có 20 use case chính phân bổ cho 7 tác nhân. Mỗi use case được đặc tả với: Tên, Tác nhân, Tiền điều kiện, Luồng chính, Luồng thay thế, Hậu điều kiện.

---

# PHỤ LỤC B: ĐẶC TẢ CƠ SỞ DỮ LIỆU

*(Xem chi tiết tại File B — Tài liệu Kỹ thuật, mục 7)*

Database OmniBizDB gồm ~95 bảng, phân thành 10 nhóm chức năng. Mọi entity kế thừa từ `TenantEntity` đảm bảo multi-tenant isolation.

---

# PHỤ LỤC C: TEST CASE

*(Xem chi tiết tại File E — Tài liệu Kiểm thử, mục 4)*

10 test case chính bao phủ các luồng nghiệp vụ quan trọng. Tổng cộng 18 test case đều Pass.

---

# PHỤ LỤC D: MERMAID DIAGRAM SOURCE

*(Xem toàn bộ source code sơ đồ tại File F — `docs/report/F_SoDo_Mermaid.md`)*

Bao gồm 15 sơ đồ: Use Case (2), Activity (3), ERD (2), Sequence (3), Component (1), Deployment (1), Package (1), Sitemap (1), Role-Permission (1).

---

> **Ghi chú:** Các placeholder `[TÊN_TRƯỜNG]`, `[TÊN_GVHD]`, `[THÀNH VIÊN]`... cần được thay thế bằng thông tin thực tế trước khi nộp.
>
