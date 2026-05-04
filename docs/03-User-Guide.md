# OmniBizAI - Hướng Dẫn Sử Dụng Hệ Thống

> Ngày cập nhật: 2026-04-30
> Phạm vi hướng dẫn: OmniBizAI Catering Operations Platform; Bizen Catering Services là tenant demo đầu tiên.

## 1. Tổng Quan

OmniBizAI hỗ trợ doanh nghiệp suất ăn/catering quản lý quy trình vận hành theo khách hàng doanh nghiệp. Tenant Bizen dùng dữ liệu Lark thật làm cấu hình và dữ liệu demo đầu tiên. Người dùng nội bộ đăng nhập để tạo menu, duyệt nội bộ, gửi khách hàng duyệt qua email, nhập/chốt số lượng, tính nguyên vật liệu theo BOM và xuất giấy đi chợ.

Khách hàng không cần tài khoản. Khách hàng nhận email, mở link duyệt menu và nhập số lượng dự kiến/chốt/phát sinh.

## 2. Vai Trò Người Dùng

| Vai trò | Mục tiêu sử dụng |
|---|---|
| Admin | Quản lý tài khoản, phòng ban, nhân sự, cấu hình |
| Director | Xem dashboard, cảnh báo, báo cáo và AI advisory |
| OperationsManager | Điều phối menu, duyệt vận hành, chốt số lượng |
| MenuPlanner | Tạo menu, quản lý món ăn và BOM |
| KitchenLead | Kiểm tra khả năng sản xuất, xem giấy đi chợ |
| QAReviewer | Kiểm duyệt chất lượng thực đơn |
| PurchasingStaff | Preview, tạo và phát hành giấy đi chợ |
| CustomerService | Quản lý khách hàng, gửi email duyệt, theo dõi phản hồi |
| CustomerContact | Duyệt menu và nhập số lượng qua email |

## 3. Đăng Nhập Và Đăng Xuất

### 3.1 Đăng nhập

1. Mở địa chỉ hệ thống do nhóm triển khai cung cấp.
2. Nhập email và mật khẩu.
3. Bấm **Đăng nhập**.
4. Hệ thống chuyển đến dashboard theo vai trò.

Nếu nhập sai thông tin, hệ thống hiển thị thông báo lỗi. Nếu tài khoản không có quyền vào một chức năng, hệ thống hiển thị trang 403 hoặc ẩn nút thao tác.

### 3.2 Đăng xuất

1. Bấm tên người dùng ở góc trên.
2. Chọn **Đăng xuất**.
3. Hệ thống quay lại màn hình đăng nhập.

## 4. Quản Lý Tenant Và Công Ty Vận Hành

### 4.1 Xem thông tin công ty

1. Vào menu **Công ty**.
2. Xem tên công ty, địa chỉ, email, số điện thoại.
3. Admin có thể chỉnh sửa thông tin nếu cần.

### 4.2 Quản lý phòng ban

1. Vào **Công ty > Phòng ban**.
2. Bấm **Tạo phòng ban** nếu cần thêm mới.
3. Nhập mã phòng ban, tên phòng ban và phòng ban cha nếu có.
4. Bấm **Lưu**.

Các phòng ban trong template demo:

- Ban giám đốc.
- Vận hành.
- Kế hoạch thực đơn/R&D.
- Bếp sản xuất.
- Kiểm soát chất lượng.
- Thu mua.
- Chăm sóc khách hàng.
- Quản trị hệ thống.

### 4.3 Quản lý nhân sự

1. Vào **Công ty > Nhân sự**.
2. Chọn **Tạo nhân sự**.
3. Nhập họ tên, email, phòng ban, chức vụ và role hệ thống.
4. Bấm **Lưu**.

Nhân sự sau khi được gán role sẽ chỉ thấy các chức năng phù hợp với quyền của mình.

### 4.4 Quản lý bếp sản xuất

1. Vào **Công ty > Bếp sản xuất**.
2. Bấm **Tạo bếp**.
3. Nhập mã bếp (ví dụ: `KIT-01`), tên bếp, địa chỉ và ghi chú.
4. Bấm **Lưu**.

Mỗi bếp đại diện cho một đơn vị sản xuất thực tế. Tenant có thể chọn một bếp chính cho cả menu hoặc cấu hình bếp theo site/loại suất/vị trí món để hệ thống tự suy ra bếp khi tạo giấy đi chợ.

### 4.5 Cấu hình vận hành theo tenant

Admin vào **Cấu hình** để quản lý:

- Role, permission và quyền theo màn hình/chức năng.
- Ca ăn và tên gọi khác.
- Loại suất ăn.
- Vị trí món trong thực đơn.
- Bếp theo site, loại suất hoặc vị trí món.
- Bước duyệt nội bộ.
- Rule fallback số lượng.
- Form khách hàng, field và validation.
- Email template, export template, dashboard widget và AI prompt.

Các cấu hình này giúp triển khai khách hàng mới mà không cần sửa code. Nếu một yêu cầu mới chỉ thay đổi rule, nhãn, form, quyền, template hoặc thứ tự workflow thì phải xử lý bằng cấu hình.

### 4.6 Import dữ liệu thật

Admin/Ops vào **Dữ liệu > Import** để nạp CSV hoặc dữ liệu từ Lark:

1. Chọn nguồn dữ liệu và profile mapping.
2. Upload file hoặc chọn kết nối đã cấu hình.
3. Bấm **Validate** để xem lỗi thiếu mã, thiếu đơn vị, trùng dữ liệu hoặc thiếu mapping.
4. Sửa lỗi trong file nguồn hoặc cập nhật mapping.
5. Bấm **Commit** khi preview đã đúng.

Dữ liệu import không ghi thẳng vào menu/BOM/số lượng. Hệ thống luôn lưu staging để có thể kiểm tra lại khi phát hiện sai lệch.

## 5. Quản Lý Khách Hàng

### 5.1 Tạo khách hàng doanh nghiệp

1. Vào **Khách hàng**.
2. Bấm **Tạo khách hàng**.
3. Nhập tên công ty khách hàng, mã số thuế nếu có, địa chỉ và ghi chú.
4. Nhập số lượng mặc định nếu hợp đồng thường có số suất cố định.
5. Bấm **Lưu**.

### 5.2 Thêm người liên hệ

1. Mở chi tiết khách hàng.
2. Chọn tab **Người liên hệ**.
3. Bấm **Thêm người liên hệ**.
4. Nhập họ tên, email, số điện thoại và chức danh.
5. Chọn người liên hệ chính nếu đây là người nhận email duyệt menu.
6. Bấm **Lưu**.

### 5.3 Thêm địa điểm giao

1. Mở chi tiết khách hàng.
2. Chọn tab **Địa điểm giao**.
3. Bấm **Thêm địa điểm**.
4. Nhập tên địa điểm, địa chỉ và ghi chú giao nhận.
5. Bấm **Lưu**.

## 6. Quản Lý Món Ăn Và BOM

### 6.1 Tạo món ăn

1. Vào **Thực đơn > Món ăn**.
2. Bấm **Tạo món**.
3. Nhập mã món, tên món, loại món và ghi chú dị ứng nếu có.
4. Bấm **Lưu**.

Loại món gồm món chính, món phụ, canh, tráng miệng, đồ uống hoặc loại khác.

### 6.2 Nhập BOM cho món

1. Mở chi tiết món ăn.
2. Chọn tab **BOM nguyên vật liệu**.
3. Bấm **Thêm nguyên liệu**.
4. Chọn nguyên liệu.
5. Nhập định mức cho một suất.
6. Nhập tỷ lệ hao hụt nếu có.
7. Bấm **Lưu**.

Ví dụ:

| Món | Nguyên liệu | Định mức/suất | Hao hụt |
|---|---|---:|---:|
| Cơm gà sốt nấm | Gạo | 0.120 kg | 3% |
| Cơm gà sốt nấm | Thịt gà | 0.160 kg | 8% |
| Canh rau | Rau cải | 0.070 kg | 10% |

Nếu món chưa có BOM, hệ thống sẽ không cho phát hành giấy đi chợ.

## 7. Lập Và Xuất Menu

### 7.1 Tạo menu

1. Vào **Thực đơn > Kế hoạch menu**.
2. Bấm **Tạo menu**.
3. Chọn khách hàng.
4. Chọn hợp đồng hoặc địa điểm giao (site ăn) nếu có.
5. Chọn **bếp chính** nếu tenant yêu cầu; nếu đã có cấu hình bếp theo site/món, hệ thống có thể tự gợi ý.
6. Chọn ngày phục vụ.
7. Chọn ca ăn theo cấu hình tenant.
8. Giao món cho từng vị trí trong thực đơn:

| Vị trí | Mô tả | Bắt buộc |
|---|---|---:|
| Món mặn 1-6 | Món mặn chính | Tùy cấu hình |
| Món chay 1, 2, 3 | Món chay cho thực khách | Tùy ca ăn |
| Món nước 1, 2, 3 | Canh, nước dùng | Tùy ca ăn |
| Món canh | Canh chính | Không |
| Món rau xào/luộc 1-2 | Rau trong suất ăn | Không |
| Món tráng miệng | Trái cây, chè | Không |
| Món buffet | Món buffet tự chọn | Không |
| Cơm trắng | Cơm trắng kèm suất | Không |
| Món cháo | Cháo | Không |
| Món ăn sáng | Dành cho ca sáng | Không |
| Món mì/sữa | Mì, sữa | Không |

9. Nhập ghi chú nếu cần.
10. Bấm **Lưu nháp**.

Hệ thống tự động tính: tuần, thứ, tháng và thứ tự tuần trong tháng từ ngày phục vụ.

Số lượng vị trí hiển thị phụ thuộc cấu hình tenant. Với tenant Bizen, hệ thống seed bộ slot từ dữ liệu Lark; khách hàng khác có thể có ít hoặc nhiều vị trí hơn.

Kết quả: menu ở trạng thái **Draft**.

### 7.2 Xem và xuất menu

1. Mở chi tiết menu.
2. Kiểm tra khách hàng, ngày, ca ăn và danh sách món.
3. Bấm **Xuất menu** hoặc **In menu** nếu cần gửi bản xem trước nội bộ.

Menu chỉ nên gửi khách hàng sau khi đã được duyệt nội bộ.

## 8. Nội Bộ Kiểm Duyệt Menu

### 8.1 Gửi duyệt nội bộ

1. Mở menu trạng thái **Draft**.
2. Bấm **Gửi duyệt nội bộ**.
3. Hệ thống chuyển menu sang trạng thái **InternalReview**.
4. Người duyệt nhận thông báo trong hàng chờ.

### 8.2 Duyệt nội bộ

1. Người duyệt vào **Duyệt nội bộ > Hàng chờ**.
2. Mở menu cần duyệt.
3. Kiểm tra danh sách món, ghi chú dị ứng và khả năng sản xuất.
4. Chọn **Duyệt** nếu đạt yêu cầu.
5. Nhập bình luận nếu cần.
6. Bấm **Xác nhận**.

Kết quả: nếu tất cả bước duyệt đã xong, menu chuyển sang **InternalApproved**.

### 8.3 Yêu cầu chỉnh sửa

1. Mở menu trong hàng chờ duyệt.
2. Chọn **Yêu cầu chỉnh sửa**.
3. Nhập lý do rõ ràng.
4. Bấm **Gửi yêu cầu**.

Kết quả: menu chuyển sang **InternalChangeRequested**. Người lập menu chỉnh sửa và gửi duyệt lại.

## 9. Khách Hàng Duyệt Qua Email

### 9.1 Gửi email duyệt menu

1. CS hoặc Ops mở menu đã **InternalApproved**.
2. Bấm **Gửi khách duyệt**.
3. Chọn người liên hệ nhận email.
4. Kiểm tra nội dung email preview.
5. Bấm **Gửi email**.

Kết quả: hệ thống tạo link duyệt có thời hạn và menu chuyển sang **SentToCustomer**.

### 9.2 Khách hàng duyệt menu và gửi số lượng

1. Khách hàng mở email.
2. Bấm link **Xem và duyệt thực đơn**.
3. Trang duyệt hiển thị menu, ngày, ca ăn và khách hàng.
4. Khách chọn **Duyệt menu** nếu đồng ý.
5. Khách nhập số lượng dự kiến, số lượng chốt và phát sinh nếu đã có dữ liệu.
6. Nếu chưa có số lượng, khách có thể để trống; hệ thống sẽ áp dụng rule fallback ở mục 10.
7. Khách có thể nhập ghi chú.
8. Bấm **Gửi xác nhận**.

Kết quả: nếu hệ thống resolve đủ dữ liệu số lượng, menu chuyển sang **QuantityConfirmed**. Nếu thiếu dữ liệu fallback bắt buộc, menu chuyển sang **QuantityOpen** để CS/Ops nhập bổ sung.

### 9.3 Khách hàng yêu cầu chỉnh sửa

1. Khách mở link email.
2. Bấm **Yêu cầu chỉnh sửa**.
3. Nhập lý do, ví dụ đổi món cay hoặc món có dị ứng.
4. Bấm **Gửi yêu cầu**.

Kết quả: menu chuyển sang **CustomerChangeRequested**. Nội bộ chỉnh sửa và gửi duyệt lại.

## 10. Nhập Số Lượng Dự Kiến, Chốt Và Phát Sinh

### 10.1 Khách hàng nhập số lượng

Trên cùng trang duyệt menu từ email, khách có thể nhập:

- **Số lượng dự kiến**: số suất ước tính.
- **Số lượng chốt**: số suất chính thức.
- **Phát sinh**: số lượng bổ sung hoặc tổng mới.
- **Cách xử lý phát sinh**:
  - Không có phát sinh.
  - Cộng thêm vào số lượng chốt.
  - Dùng làm tổng số lượng mới.

### 10.2 Rule tự động của hệ thống

Nếu khách không nhập số lượng dự kiến:

- Hệ thống lấy số lượng nấu của ngày trước đó cùng khách hàng, cùng địa điểm và cùng ca ăn.
- Nếu không có dữ liệu ngày trước, hệ thống lấy số lượng mặc định trong hợp đồng.

Nếu khách không nhập số lượng chốt:

- Hệ thống lấy số lượng dự kiến làm số lượng chốt.

Nếu khách nhập phát sinh:

- Chọn **Cộng thêm vào chốt**: tổng nấu = số lượng chốt + số phát sinh.
- Chọn **Dùng làm tổng mới**: tổng nấu = số phát sinh được nhập.

### 10.3 Ví dụ

| Tình huống | Dự kiến | Chốt | Phát sinh | Mode | Tổng nấu |
|---|---:|---:|---:|---|---:|
| Khách nhập đủ | 500 | 520 | 0 | Không có | 520 |
| Không nhập dự kiến | Lấy ngày trước: 520 | 520 | 0 | Không có | 520 |
| Không nhập chốt | 520 | Tự bằng dự kiến | 0 | Không có | 520 |
| Phát sinh cộng thêm | 520 | 520 | 30 | Cộng thêm | 550 |
| Phát sinh tổng mới | 520 | 520 | 600 | Tổng mới | 600 |

### 10.4 Nội bộ chỉnh số lượng

Ops hoặc CS có thể nhập/sửa số lượng thay khách trong trường hợp khách xác nhận qua điện thoại. Khi sửa thay khách, bắt buộc nhập ghi chú để audit.

## 11. Tính BOM Và Xuất Giấy Đi Chợ

### 11.1 Xem trước giấy đi chợ

1. Vào **Giấy đi chợ**.
2. Bấm **Tạo giấy đi chợ**.
3. Chọn ngày phục vụ.
4. Chọn ca ăn.
5. Chọn các menu đã chốt số lượng.
6. Bấm **Xem trước**.

Hệ thống hiển thị danh sách nguyên liệu, định mức, hao hụt và tổng cần mua.

### 11.2 Phát hành giấy đi chợ

1. Kiểm tra các dòng nguyên liệu.
2. Nếu có lỗi món thiếu BOM, quay lại bổ sung BOM trước.
3. Nếu dữ liệu đúng, bấm **Phát hành**.
4. Hệ thống chuyển giấy đi chợ sang trạng thái **Issued**.

Sau khi phát hành, PurchasingStaff và KitchenLead có thể xem để chuẩn bị mua hàng và sản xuất.

### 11.3 In giấy đi chợ

1. Mở chi tiết giấy đi chợ.
2. Bấm **In** hoặc dùng chức năng in của trình duyệt.
3. Kiểm tra ngày, ca ăn, tổng suất và nguyên liệu.

## 12. Dashboard Và AI Advisory

### 12.1 Dashboard vận hành

Dashboard hiển thị:

- Menu đang chờ duyệt nội bộ.
- Menu đã gửi khách nhưng chưa phản hồi.
- Menu đã duyệt nhưng chưa chốt số lượng.
- Món thiếu BOM.
- Giấy đi chợ hôm nay.
- Cảnh báo số lượng biến động.

### 12.2 Hỏi AI

1. Vào **AI Assistant** hoặc bấm panel AI trên dashboard.
2. Chọn ngữ cảnh: menu, số lượng hoặc giấy đi chợ.
3. Nhập câu hỏi.
4. Bấm **Gửi**.

Câu hỏi gợi ý:

- "Hôm nay khách nào tăng số lượng nhiều nhất?"
- "Giấy đi chợ ca trưa có nguyên liệu nào cần ưu tiên?"
- "Menu nào chưa đủ BOM để phát hành?"
- "Vì sao tổng thịt gà hôm nay tăng so với ngày trước?"

AI chỉ đưa gợi ý. Người dùng vẫn phải tự kiểm tra và quyết định.

## 13. Demo Script 5-7 Phút

1. Đăng nhập bằng tài khoản Admin hoặc Ops.
2. Mở dashboard, giới thiệu các card vận hành.
3. Vào khách hàng, mở ABC Factory.
4. Tạo menu trưa ngày 2026-05-01.
5. Gửi duyệt nội bộ.
6. Đăng nhập QA hoặc KitchenLead để duyệt.
7. Đăng nhập CS, gửi email duyệt khách.
8. Mở link khách hàng, duyệt menu và nhập số lượng.
9. Minh họa rule: không nhập dự kiến để hệ thống lấy ngày trước, không nhập chốt để chốt bằng dự kiến, nhập phát sinh cộng thêm 30.
10. Vào giấy đi chợ, preview nguyên liệu.
11. Phát hành giấy đi chợ.
12. Mở AI advisory để xem cảnh báo/tóm tắt.

## 14. Lỗi Thường Gặp

| Lỗi | Nguyên nhân | Cách xử lý |
|---|---|---|
| Không đăng nhập được | Sai email/mật khẩu hoặc chưa seed user | Kiểm tra tài khoản demo |
| Không thấy nút duyệt | Tài khoản không có role duyệt | Đổi đúng user hoặc gán quyền |
| Không gửi được email | SMTP chưa cấu hình | Dùng dev email log hoặc cấu hình SMTP |
| Link khách hết hạn | Token quá hạn | CS gửi lại email duyệt |
| Không tạo được giấy đi chợ | Menu chưa chốt số lượng hoặc món thiếu BOM | Chốt số lượng/bổ sung BOM |
| AI không trả lời | Chưa cấu hình provider | Hệ thống dùng fallback; kiểm tra log |

## 15. Checklist Trước Khi Demo

- [ ] Có tài khoản demo cho Admin, Ops, MenuPlanner, QA, KitchenLead, Purchasing, CS.
- [ ] Có khách hàng ABC Factory và người liên hệ có email.
- [ ] Có tenant demo và cấu hình ca ăn, loại suất, vị trí món, bếp.
- [ ] Import staging có ít nhất một file CSV/Lark sample và hiển thị lỗi validation nếu có.
- [ ] Có menu ngày trước để test fallback số lượng.
- [ ] Có ít nhất 10 món và BOM cho các món demo.
- [ ] Có ít nhất một món thiếu BOM để demo cảnh báo nếu cần.
- [ ] Gửi email ở dev mode có thể xem được token/link.
- [ ] Giấy đi chợ preview được số liệu.
- [ ] AI fallback trả về tóm tắt có citation.
