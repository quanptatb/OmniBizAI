# OmniBizAI - Bộ Tài Liệu Dự Án Catering Operations Platform

> Ngày cập nhật: 2026-04-30
> Tên đề tài cố định: **"Hệ thống vận hành thông minh cho doanh nghiệp vừa và nhỏ, hỗ trợ quản lý đa cấp và đưa ra quyết định bằng AI"**
> Phạm vi triển khai: nền tảng vận hành suất ăn/catering có thể cấu hình theo từng doanh nghiệp; **Bizen Catering Services** là tenant/case study đầu tiên dựa trên dữ liệu Lark thật.

## 1. Tài Liệu Kỹ Thuật

- File: [01-Technical-Implementation-Blueprint.md](01-Technical-Implementation-Blueprint.md)
- Dành cho: dev, tester, PM/BA.
- Nội dung: phạm vi MVP, kiến trúc ASP.NET Core MVC, SQL Server schema, tenant/config entity, service contract, route map, state machine, rule tính số lượng suất ăn, BOM nguyên vật liệu, import dữ liệu Lark và giấy đi chợ.

## 2. Tài Liệu Báo Cáo Dự Án Tốt Nghiệp

- File: [02-Academic-Report.md](02-Academic-Report.md)
- Dành cho: báo cáo nộp trường, giảng viên hướng dẫn, hội đồng chấm.
- Nội dung: bố cục báo cáo đầy đủ theo chương, lý do chọn đề tài, Bizen Catering là bối cảnh thực tế đầu tiên, cơ sở lý thuyết, phân tích yêu cầu, thiết kế, triển khai, kiểm thử, đánh giá, kết luận và phụ lục.

## 3. Tài Liệu Hướng Dẫn Sử Dụng

- File: [03-User-Guide.md](03-User-Guide.md)
- Dành cho: người dùng cuối, người demo, nhóm test.
- Nội dung: cách đăng nhập, quản lý công ty/phòng ban, khách hàng, thực đơn, duyệt nội bộ, duyệt qua email, nhập số lượng dự kiến/chốt/phát sinh, tính BOM, xuất giấy đi chợ, dashboard và AI hỗ trợ quyết định.

## 4. Tài Liệu Kế Hoạch Thực Hiện Nhóm 7 Người

- File: [04-Project-Work-Plan-7-Members.md](04-Project-Work-Plan-7-Members.md)
- Dành cho: PM/BA, 5 dev, tester.
- Nội dung: kế hoạch 1 tuần từ 2026-04-30 đến 2026-05-07, phân công đều cho 7 thành viên, ai cũng có phần code, trọng tâm hoàn thành quy trình nghiệp vụ và quản lý công ty cơ bản.

## 5. Tài Liệu Thương Mại Hóa Và Tùy Biến

- File: [05-Productization-Customization-Blueprint.md](05-Productization-Customization-Blueprint.md)
- Dành cho: founder/PM/BA/dev lead.
- Nội dung: định hướng bán sản phẩm, multi-tenant tối thiểu, cấu hình liên tục theo khách hàng, pipeline import dữ liệu thật, bảo mật dữ liệu và roadmap triển khai cho khách mới.

## 6. Sprint 1 Implementation Cutline

- File: [06-Sprint-1-Implementation-Cutline.md](06-Sprint-1-Implementation-Cutline.md)
- Dành cho: dev team, PM/BA, tester.
- Nội dung: khóa phạm vi 7 ngày theo Must-have/Should-have/Nice-to-have, chốt cách làm config bằng JSON/import profile trước khi làm UI cấu hình nâng cao, và chọn runtime `.NET 8 LTS` làm mặc định an toàn.

## Ghi Chú Bảo Trì

- MVP vẫn tập trung hoàn thành core flow suất ăn trước, nhưng database và service phải có `TenantId`, cấu hình theo tenant và import staging để có thể bán cho khách khác sau này.
- Không hard-code nghiệp vụ: role, permission, workflow, form, vị trí món, rule số lượng, BOM, email, export, dashboard và AI prompt đều phải đi qua cấu hình hoặc import profile.
- Chưa triển khai self-service SaaS, billing/subscription, marketplace plugin hoặc workflow designer kéo thả trong sprint đầu; đây là phần thương mại hóa giai đoạn sau.
- AI trong đề tài được giữ đúng tinh thần tên đề tài: AI chỉ hỗ trợ ra quyết định bằng cảnh báo, tóm tắt, gợi ý mua hàng và phát hiện bất thường; AI không tự duyệt, không tự gửi email và không tự sửa dữ liệu nghiệp vụ.
- Khi thay đổi nghiệp vụ, cập nhật tài liệu kỹ thuật trước; sau đó đồng bộ báo cáo, hướng dẫn sử dụng và kế hoạch nhóm.
