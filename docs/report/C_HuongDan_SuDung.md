# HƯỚNG DẪN SỬ DỤNG — HỆ THỐNG OMNIBIZAI

> Phiên bản: 1.0 | Ngày: 18/05/2026  
> Đối tượng: Người dùng cuối (End-user)

---

## 1. Giới thiệu hệ thống

**OmniBizAI** là hệ thống quản lý vận hành thông minh dành cho doanh nghiệp vừa và nhỏ. Hệ thống tích hợp các module: Vận hành, Tài chính, Mua sắm, Kho vận, Nhân sự, CRM, KPI/OKR, Báo cáo và AI Copilot trên một giao diện web duy nhất.

---

## 2. Yêu cầu thiết bị / trình duyệt

| Yêu cầu | Chi tiết |
|---|---|
| Trình duyệt | Chrome 90+, Firefox 90+, Edge 90+, Safari 15+ |
| Độ phân giải | Tối thiểu 1280×720, khuyến nghị 1920×1080 |
| Kết nối | Internet ổn định |
| Thiết bị | PC, Laptop, Tablet (responsive) |

---

## 3. Hướng dẫn đăng nhập

**Tên chức năng:** Đăng nhập hệ thống  
**Mục đích:** Xác thực người dùng để truy cập hệ thống  
**Vai trò:** Tất cả người dùng  
**Đường dẫn:** `/Account/Login`

**Các bước thực hiện:**
1. Mở trình duyệt, truy cập địa chỉ hệ thống
2. Nhập **Email** và **Mật khẩu**
3. Nhấn nút **Đăng nhập**
4. Hệ thống chuyển đến trang Dashboard

**Kết quả mong đợi:** Đăng nhập thành công, hiển thị Dashboard  
**Lưu ý:**
- Sai mật khẩu 5 lần liên tiếp → tài khoản bị khóa 15 phút
- Tài khoản demo có sẵn trên trang đăng nhập (Quick Login)
- Mật khẩu mặc định cho tài khoản seed: `123`

---

## 4. Hướng dẫn đổi mật khẩu

**Tên chức năng:** Đổi mật khẩu  
**Mục đích:** Thay đổi mật khẩu cá nhân  
**Vai trò:** Tất cả  
**Đường dẫn:** `/Profile`

**Các bước:**
1. Nhấn vào avatar góc trên bên phải → chọn **Hồ sơ cá nhân**
2. Chọn tab **Đổi mật khẩu**
3. Nhập mật khẩu cũ, mật khẩu mới, xác nhận mật khẩu mới
4. Nhấn **Lưu**

**Kết quả:** Mật khẩu được cập nhật thành công

---

## 5. Giao diện chung

### 5.1. Thanh điều hướng (Sidebar)
- Nằm bên trái, chứa tất cả menu chức năng được phân nhóm
- Có thể thu gọn bằng nút hamburger trên topbar

### 5.2. Chuyển đổi giao diện điều hướng
Hệ thống hỗ trợ 3 chế độ hiển thị:
1. **Sidebar Cơ bản**: Menu dọc dạng danh sách (mặc định)
2. **Sidebar dạng Thẻ**: Menu dọc dạng lưới card
3. **App Launcher**: Ẩn sidebar, mở menu toàn màn hình

Nhấn biểu tượng **ô vuông** trên thanh topbar để chuyển đổi. Hệ thống tự ghi nhớ lựa chọn.

### 5.3. Thông báo
- Biểu tượng chuông trên topbar hiển thị số thông báo chưa đọc
- Nhấn vào để xem danh sách thông báo gần đây

---

## 6. Hướng dẫn cho Admin (TENANT_ADMIN / SYSTEM_ADMIN)

### 6.1. Quản lý phòng ban

**Tên chức năng:** Quản lý cơ cấu tổ chức  
**Mục đích:** Tạo, sửa, xóa phòng ban theo cấu trúc cây  
**Đường dẫn:** `/Organization`

**Các bước tạo phòng ban:**
1. Vào menu **Nhân sự** → **Phòng ban**
2. Nhấn **Thêm phòng ban**
3. Điền: Mã, Tên, Phòng ban cha, Trưởng phòng
4. Nhấn **Lưu**

**Kết quả:** Phòng ban mới xuất hiện trong sơ đồ tổ chức

### 6.2. Quản lý tài khoản người dùng

**Đường dẫn:** `/Users`

**Các bước tạo tài khoản:**
1. Vào menu **Nhân sự** → **Tài khoản**
2. Nhấn **Thêm người dùng**
3. Điền thông tin: Họ tên, Email, Phòng ban, Vai trò
4. Nhấn **Lưu**

**Lưu ý:** Mật khẩu mặc định sẽ được gán tự động

### 6.3. Cài đặt công ty

**Đường dẫn:** `/Settings/Company`

Cho phép cập nhật: Tên công ty, Logo, Mã số thuế, Địa chỉ, Thông tin liên hệ

### 6.4. Giao diện & Branding

**Đường dẫn:** `/Settings/Appearance`

Cho phép tùy chỉnh: Tên thương hiệu, Tagline, Icon logo, Màu chủ đạo

### 6.5. Sao lưu dữ liệu

**Đường dẫn:** `/Backup`

**Các bước:**
1. Vào **Hệ thống** → **Sao lưu dữ liệu**
2. Nhấn **Tạo bản sao lưu mới**
3. Chờ hệ thống xử lý
4. Tải về file `.bak` khi cần

---

## 7. Hướng dẫn cho Trưởng phòng (DEPARTMENT_MANAGER)

### 7.1. Phê duyệt yêu cầu

**Đường dẫn:** `/Approvals/MyTasks`

**Các bước:**
1. Vào menu **Phê duyệt**
2. Xem danh sách yêu cầu chờ duyệt
3. Nhấn **Duyệt** hoặc **Từ chối**
4. Nếu từ chối, nhập lý do bắt buộc
5. Nhấn **Xác nhận**

**Kết quả:** Yêu cầu được cập nhật trạng thái, thông báo gửi đến người tạo

### 7.2. Quản lý Kanban

**Đường dẫn:** `/Workflow/Kanban`

**Thao tác:**
- Kéo thả thẻ công việc giữa các cột (Todo → In Progress → Done)
- Nhấn vào thẻ để xem chi tiết, thêm comment, cập nhật checklist

---

## 8. Hướng dẫn cho Nhân viên (STAFF)

### 8.1. Tạo yêu cầu vận hành

**Đường dẫn:** `/Operations/Create`

**Các bước:**
1. Vào menu **Yêu cầu vận hành** → **Tạo mới**
2. Điền: Tiêu đề, Mô tả, Độ ưu tiên, Phòng ban
3. Thêm dòng chi tiết (nếu cần)
4. Nhấn **Lưu nháp** hoặc **Gửi phê duyệt**

**Kết quả:** Yêu cầu được tạo và gửi đến Trưởng phòng

### 8.2. KPI Check-In

**Đường dẫn:** `/KpiCheckIn`

**Các bước:**
1. Vào menu **KPI / OKR** → **Check-In**
2. Chọn kỳ check-in
3. Nhập giá trị thực tế cho từng KPI
4. Thêm ghi chú / bằng chứng
5. Nhấn **Gửi Check-In**

### 8.3. Xin nghỉ phép

**Đường dẫn:** `/Leave`

**Các bước:**
1. Vào menu **Nhân sự** → **Nghỉ phép**
2. Nhấn **Tạo đơn nghỉ phép**
3. Chọn loại nghỉ, ngày bắt đầu, ngày kết thúc, lý do
4. Nhấn **Gửi**

---

## 9. Hướng dẫn cho Kế toán (ACCOUNTANT)

### 9.1. Quản lý sổ thu chi

**Đường dẫn:** `/CashBook`

**Các bước ghi nhận giao dịch:**
1. Vào menu **Tài chính** → **Sổ thu chi**
2. Nhấn **Thêm giao dịch**
3. Chọn loại: Thu (Income) hoặc Chi (Expense)
4. Điền: Số tiền, Danh mục, Mô tả, Ngày
5. Nhấn **Lưu**

### 9.2. Quản lý ngân sách

**Đường dẫn:** `/Finance/Budgets`

Cho phép: Tạo ngân sách theo phòng ban/kỳ, theo dõi thực chi so với kế hoạch

### 9.3. Đề nghị thanh toán

**Đường dẫn:** `/Finance/PaymentRequests`

Cho phép: Tạo, xem, duyệt đề nghị thanh toán cho nhà cung cấp

---

## 10. Các lỗi thường gặp

| # | Lỗi | Nguyên nhân | Cách xử lý |
|---|---|---|---|
| 1 | Đăng nhập thất bại | Sai email hoặc mật khẩu | Kiểm tra lại thông tin, liên hệ Admin nếu bị khóa |
| 2 | Trang trắng sau đăng nhập | Tài khoản chưa được gán tenant | Liên hệ Admin để gán tenant |
| 3 | Không thấy menu | Vai trò không có quyền | Liên hệ Admin để cấp quyền phù hợp |
| 4 | Lỗi khi lưu dữ liệu | Thiếu trường bắt buộc | Kiểm tra các trường có dấu (*) |
| 5 | Không nhận thông báo | Trình duyệt chặn notification | Kiểm tra cài đặt trình duyệt |

---

## 11. Câu hỏi thường gặp (FAQ)

**Q: Tôi quên mật khẩu, phải làm sao?**  
A: Nhấn "Quên mật khẩu" trên trang đăng nhập, nhập email để nhận link đặt lại. Hoặc liên hệ Admin.

**Q: Làm sao để xem báo cáo?**  
A: Vào menu Báo cáo, chọn loại báo cáo phù hợp (Ban GĐ, Vận hành, Tài chính, Nhân sự, KPI/OKR...).

**Q: Dữ liệu có bị mất khi xóa không?**  
A: Không. Hệ thống sử dụng Soft Delete — dữ liệu chỉ bị ẩn, không bị xóa vật lý.

**Q: Hệ thống hỗ trợ bao nhiêu người dùng?**  
A: Không giới hạn. Hiệu suất phụ thuộc vào cấu hình server.

---

## 12. Thông tin hỗ trợ

| Kênh | Thông tin |
|---|---|
| Email | support@omnibiz.vn |
| Hotline | 1900-xxxx |
| Tài liệu | `/docs/report/` trong source code |
| GitHub | [Link repository] |
