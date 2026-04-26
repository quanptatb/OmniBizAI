<div align="center">

# OmniBizAI

### Nền tảng quản trị vận hành SME với ASP.NET Core MVC, Workflow, KPI và AI Decision Support

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-5C2D91?style=for-the-badge)](https://learn.microsoft.com/aspnet/core/mvc/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-EF%20Core-CC2927?style=for-the-badge&logo=microsoftsqlserver)](https://learn.microsoft.com/ef/core/)
[![Status](https://img.shields.io/badge/status-MVP%20planning%20%2B%20MVC%20foundation-blue?style=for-the-badge)](#trang-thai-du-an)

**OmniBizAI** là đồ án xây dựng một hệ thống quản trị vận hành cho doanh nghiệp vừa và nhỏ, tập trung vào luồng tài chính, phê duyệt, KPI, dashboard, báo cáo và AI hỗ trợ ra quyết định.

</div>

---

## Mục Lục

- [Tổng quan](#tong-quan)
- [Điểm nổi bật](#diem-noi-bat)
- [Trạng thái dự án](#trang-thai-du-an)
- [Kiến trúc](#kien-truc)
- [Công nghệ sử dụng](#cong-nghe-su-dung)
- [Cài đặt và chạy local](#cai-dat-va-chay-local)
- [Hướng dẫn sử dụng nhanh](#huong-dan-su-dung-nhanh)
- [Tài liệu dự án](#tai-lieu-du-an)
- [Kế hoạch triển khai 1 tuần](#ke-hoach-trien-khai-1-tuan)
- [Quy tắc bảo mật](#quy-tac-bao-mat)

---

## Tổng Quan

OmniBizAI hướng đến bài toán phổ biến của các doanh nghiệp vừa và nhỏ:

| Vấn đề | Cách OmniBizAI xử lý |
|---|---|
| Dữ liệu tài chính, KPI và phê duyệt bị rời rạc | Gom vào một web app MVC có phân quyền theo vai trò |
| Quy trình duyệt chi thiếu minh bạch | Workflow 2/3 cấp, timeline và audit log |
| Khó kiểm soát ngân sách | Budget utilization, cảnh báo vượt ngưỡng và dashboard |
| KPI khó theo dõi liên tục | KPI/OKR, check-in, duyệt tiến độ và scorecard |
| Báo cáo thủ công mất thời gian | Report preview/export và evidence phục vụ bảo vệ đồ án |
| AI dễ bị overclaim | AI chỉ là decision support layer, không tự duyệt, không tự sửa dữ liệu |

---

## Điểm Nổi Bật

| Nhóm | Nội dung |
|---|---|
| MVC-first | Giao diện chính dùng ASP.NET Core MVC và Razor Views, không phải public REST API |
| Modular MVC Monolith | Một project duy nhất nhưng chia rõ Controller, Service, ViewModel, Entity, Data |
| Role-based workflow | Director, Manager, Accountant, HR, Staff, Admin có phạm vi dữ liệu khác nhau |
| Finance core flow | Payment Request, Budget, Transaction, Vendor và workflow duyệt |
| KPI/OKR | Objective, KPI, check-in, progress và đánh giá |
| AI advisory | AI dùng rule + retrieval + summarization, không predictive AI, không ML training |
| Defense-ready docs | Có Technical Blueprint, Academic Report, User Guide và Work Plan |

---

## Trạng Thái Dự Án

> Trạng thái hiện tại: **ASP.NET Core MVC foundation + Identity + bộ tài liệu triển khai chi tiết**.

Đã có trong source:

- Project ASP.NET Core MVC `.NET 10`.
- ASP.NET Core Identity + EF Core SQL Server.
- Cấu trúc thư mục MVC cơ bản.
- Bộ tài liệu dự án đã tách thành nhiều file.
- Kế hoạch phân công 7 thành viên trong 1 tuần.

Đang/chuẩn bị triển khai theo tài liệu:

- Finance: Budget, Payment Request, Transaction.
- Workflow: duyệt 2 cấp/3 cấp.
- KPI/OKR: KPI, check-in, progress.
- Dashboard theo role.
- AI mock/fallback và AI advisory.
- Report preview/export.
- Evidence cho báo cáo tốt nghiệp.

---

## Kiến Trúc

```text
User
  |
  v
ASP.NET Core MVC + Razor Views
  |
  v
Controllers -> Services -> EF Core DbContext -> SQL Server
                  |
                  +-> AI Provider abstraction
                  +-> Notification
                  +-> Report/Export
                  +-> Audit
```

Nguyên tắc chính:

- Controller mỏng, không chứa business rule phức tạp.
- Service xử lý nghiệp vụ, validation, transaction boundary và data scope.
- Razor View chỉ hiển thị ViewModel, không truy vấn EF trực tiếp.
- JSON endpoint chỉ dùng phụ trợ cho AJAX/widget/AI chat.
- AI không quyết định workflow, không tự duyệt, không tự sửa dữ liệu.

---

## Công Nghệ Sử Dụng

| Thành phần | Công nghệ |
|---|---|
| Framework | ASP.NET Core MVC |
| Runtime | .NET 10 |
| UI | Razor Views, Bootstrap mặc định của template MVC |
| Auth | ASP.NET Core Identity cookie |
| Database | SQL Server |
| ORM | Entity Framework Core |
| AI định hướng | Gemini qua provider abstraction |
| Testing định hướng | xUnit, Playwright, k6 smoke |
| Kiến trúc | Modular MVC Monolith 1 project |

---

## Cài Đặt Và Chạy Local

### 1. Yêu cầu môi trường

- .NET SDK 10.x.
- SQL Server hoặc LocalDB.
- Visual Studio 2026/Preview, Rider hoặc VS Code.
- EF Core tools nếu cần chạy migration:

```powershell
dotnet tool install --global dotnet-ef
```

### 2. Clone và restore

```powershell
git clone <repository-url>
cd OmniBizAI
dotnet restore
```

### 3. Cấu hình connection string an toàn

Không đưa connection string thật, password thật hoặc API key thật vào repository.

Khuyến nghị nhanh cho local: copy file mẫu `.env.example` thành `.env`, sau đó điền password/API key thật vào `.env`.

```powershell
Copy-Item .env.example .env
```

File `.env` đã được ignore bởi git. Ứng dụng sẽ tự load `.env` khi chạy local.

Có thể dùng user secrets nếu không muốn dùng `.env`:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=OmniBizDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Hoặc dùng biến môi trường:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=(localdb)\mssqllocaldb;Database=OmniBizDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

### 4. Tạo database

```powershell
dotnet ef database update
```

### 5. Chạy ứng dụng

```powershell
dotnet run
```

Theo `launchSettings.json`, app có thể chạy tại:

- `http://localhost:5170`
- `https://localhost:7016`

---

## Hướng Dẫn Sử Dụng Nhanh

### Với khách ghé source dự án

1. Đọc phần [Trạng thái dự án](#trang-thai-du-an) để hiểu source hiện đang ở giai đoạn nào.
2. Mở [Technical Blueprint](docs/01-Technical-Implementation-Blueprint.md) nếu muốn xem thiết kế kỹ thuật.
3. Mở [Academic Report](docs/02-Academic-Report.md) nếu muốn xem khung báo cáo tốt nghiệp.
4. Mở [User Guide](docs/03-User-Guide.md) nếu muốn hiểu cách demo/người dùng thao tác.
5. Mở [Work Plan](docs/04-Project-Work-Plan-7-Members.md) nếu muốn xem phân công team và Trello cards.

### Với người chạy app local

1. Cấu hình database.
2. Chạy migration.
3. Chạy app bằng `dotnet run`.
4. Truy cập `https://localhost:7016` hoặc `http://localhost:5170`.
5. Tạo/seed tài khoản demo theo kế hoạch trong tài liệu.
6. Demo mục tiêu sau khi implement Core MVP:
   - Login theo role.
   - Staff tạo Payment Request.
   - Manager/Director duyệt.
   - Accountant kiểm tra transaction/budget.
   - Staff check-in KPI.
   - Director hỏi AI về rủi ro.
   - Xuất report/evidence.

---

## Tài Liệu Dự Án

| File | Dành cho | Nội dung |
|---|---|---|
| [00-Project-Implementation-Blueprint.md](docs/00-Project-Implementation-Blueprint.md) | Tất cả | Mục lục điều hướng bộ tài liệu |
| [01-Technical-Implementation-Blueprint.md](docs/01-Technical-Implementation-Blueprint.md) | Dev, tester, PM | Kiến trúc, module, service contract, database, route, seed, test |
| [02-Academic-Report.md](docs/02-Academic-Report.md) | Báo cáo tốt nghiệp | Problem statement, methodology, evaluation, discussion, limitation |
| [03-User-Guide.md](docs/03-User-Guide.md) | Người dùng/demo | Hướng dẫn thao tác theo vai trò |
| [04-Project-Work-Plan-7-Members.md](docs/04-Project-Work-Plan-7-Members.md) | Team triển khai | Trello board, deadline, task, owner, acceptance criteria |

---

## Kế Hoạch Triển Khai 1 Tuần

Sprint mục tiêu: `2026-04-26` đến `2026-05-03`.

Board Trello dùng 7 cột:

| Cột | Ý nghĩa |
|---|---|
| Backlog | Task cần làm nhưng chưa kéo vào ngày hiện tại |
| Todo | Task đã chốt làm trong ngày |
| In Progress | Đang code/thực hiện |
| Review | Chờ review |
| Testing | Chờ QA/test/evidence |
| Done | Đã pass acceptance criteria |
| Pending | Bị chặn hoặc chờ quyết định |

Phân công chi tiết nằm tại:

- [docs/04-Project-Work-Plan-7-Members.md](docs/04-Project-Work-Plan-7-Members.md)

---

## Quy Tắc Bảo Mật

- Không commit password thật, API key thật, connection string thật.
- Không log password, token, API key hoặc dữ liệu nhạy cảm.
- AI prompt không được chứa secret hoặc dữ liệu ngoài quyền người dùng.
- File upload phải được kiểm tra type/size/signature khi triển khai module upload.
- Các action thay đổi dữ liệu phải có authorization và anti-forgery.

---

## Ghi Chú Cho Người Chấm Hoặc Người Review

OmniBizAI được thiết kế theo hướng **dễ demo, dễ kiểm thử, dễ bảo vệ** trước khi mở rộng thành hệ thống lớn hơn.

Điểm cần đánh giá:

- Kiến trúc MVC 1 project có giữ được ranh giới module không.
- Workflow có deterministic và audit được không.
- Data scope có ngăn truy cập ngoài quyền không.
- AI có được trình bày đúng vai trò decision support không.
- Evidence cuối cùng có screenshot, test result và demo flow thật không.

---

## License

Tài liệu và mã nguồn phục vụ mục đích học tập, demo và đồ án tốt nghiệp. Nếu dùng lại cho mục đích khác, cần kiểm tra lại license, dữ liệu seed và secret cấu hình.
