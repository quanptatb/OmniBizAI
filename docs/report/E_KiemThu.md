# TÀI LIỆU KIỂM THỬ — HỆ THỐNG OMNIBIZAI

> Phiên bản: 1.0 | Ngày: 18/05/2026

---

## 1. Test Plan

### 1.1. Mục tiêu
Đảm bảo hệ thống OmniBizAI hoạt động đúng theo yêu cầu chức năng và phi chức năng, phát hiện và sửa lỗi trước khi demo/bàn giao.

### 1.2. Phạm vi kiểm thử

| Module | Trong phạm vi | Ghi chú |
|---|:---:|---|
| Authentication & Authorization | ✅ | Đăng nhập, phân quyền 7 vai trò |
| Operations & Workflow | ✅ | CRUD, phê duyệt, Kanban |
| Finance & Cash Book | ✅ | Ngân sách, chi phí, thu chi |
| Procurement & Inventory | ✅ | Mua sắm → PO → Nhập/Xuất kho |
| CRM | ✅ | Khách hàng, NCC, Sản phẩm |
| KPI / OKR | ✅ | Setup, Check-In, Đánh giá |
| HR & Leave | ✅ | Phòng ban, nhân viên, nghỉ phép |
| Reports | ✅ | 7 loại báo cáo |
| AI Copilot | ⚠️ | Chỉ test khi có API key |
| Load/Performance Testing | ❌ | Ngoài phạm vi đồ án |

---

## 2. Môi trường kiểm thử

| Thành phần | Chi tiết |
|---|---|
| OS | Windows 11 |
| Runtime | .NET 10 Preview 4 |
| Database | SQL Server 2022 (LocalDB) |
| Browser | Chrome 130+, Edge 130+ |
| Dữ liệu | Seed data (`seed_data.sql`) |

---

## 3. Chiến lược kiểm thử

- **Manual Testing**: Thực hiện test case thủ công trên trình duyệt
- **Smoke Test**: Kiểm tra nhanh các luồng chính sau mỗi sprint
- **Regression Test**: Chạy lại test case cũ khi có thay đổi
- **UAT**: Kiểm thử chấp nhận người dùng trước demo

---

## 4. Test Cases

### TC-001: Đăng nhập thành công

| Mục | Nội dung |
|---|---|
| **ID** | TC-001 |
| **Tên** | Đăng nhập với tài khoản hợp lệ |
| **Tiền điều kiện** | Tài khoản đã được seed, hệ thống đang chạy |
| **Bước thực hiện** | 1. Truy cập `/Account/Login` 2. Nhập email: `admin@omnibiz.vn`, password: `123` 3. Nhấn Đăng nhập |
| **Kết quả mong đợi** | Chuyển hướng đến `/Dashboard`, hiển thị Dashboard |
| **Trạng thái** | ✅ Pass |

### TC-002: Đăng nhập thất bại

| Mục | Nội dung |
|---|---|
| **ID** | TC-002 |
| **Tên** | Đăng nhập với mật khẩu sai |
| **Bước** | 1. Nhập email hợp lệ, password sai 2. Nhấn Đăng nhập |
| **Kết quả mong đợi** | Hiển thị lỗi "Email hoặc mật khẩu không đúng" |
| **Trạng thái** | ✅ Pass |

### TC-003: Tạo yêu cầu vận hành

| Mục | Nội dung |
|---|---|
| **ID** | TC-003 |
| **Tên** | Tạo yêu cầu vận hành mới |
| **Tiền điều kiện** | Đăng nhập vai trò STAFF |
| **Bước** | 1. Vào `/Operations/Create` 2. Điền tiêu đề, mô tả, chọn phòng ban 3. Nhấn Lưu |
| **Kết quả mong đợi** | Yêu cầu được tạo, hiển thị trong danh sách |
| **Trạng thái** | ✅ Pass |

### TC-004: Phê duyệt yêu cầu

| Mục | Nội dung |
|---|---|
| **ID** | TC-004 |
| **Tên** | Trưởng phòng phê duyệt yêu cầu |
| **Tiền điều kiện** | Có yêu cầu đang chờ duyệt |
| **Bước** | 1. Đăng nhập DEPT_MANAGER 2. Vào `/Approvals/MyTasks` 3. Nhấn Duyệt 4. Xác nhận |
| **Kết quả mong đợi** | Trạng thái → Approved, thông báo gửi đến người tạo |
| **Trạng thái** | ✅ Pass |

### TC-005: Kanban kéo thả

| Mục | Nội dung |
|---|---|
| **ID** | TC-005 |
| **Tên** | Di chuyển WorkItem trên Kanban |
| **Bước** | 1. Vào `/Workflow/Kanban` 2. Kéo thẻ từ Todo sang InProgress |
| **Kết quả mong đợi** | Thẻ di chuyển, trạng thái cập nhật trong DB |
| **Trạng thái** | ✅ Pass |

### TC-006: Tạo giao dịch thu chi

| Mục | Nội dung |
|---|---|
| **ID** | TC-006 |
| **Tên** | Ghi nhận giao dịch thu tiền |
| **Tiền điều kiện** | Đăng nhập ACCOUNTANT |
| **Bước** | 1. Vào `/CashBook` 2. Nhấn Thêm giao dịch 3. Chọn Income, nhập số tiền 4. Lưu |
| **Kết quả mong đợi** | Giao dịch hiển thị trong sổ, số dư cập nhật |
| **Trạng thái** | ✅ Pass |

### TC-007: Phân quyền — STAFF không thấy Settings

| Mục | Nội dung |
|---|---|
| **ID** | TC-007 |
| **Tên** | Kiểm tra ẩn menu Settings cho STAFF |
| **Bước** | 1. Đăng nhập STAFF 2. Kiểm tra sidebar |
| **Kết quả mong đợi** | Không hiển thị menu Cài đặt, Sao lưu |
| **Trạng thái** | ✅ Pass |

### TC-008: Tạo đề xuất mua sắm

| Mục | Nội dung |
|---|---|
| **ID** | TC-008 |
| **Tên** | Tạo Procurement Request + dòng sản phẩm |
| **Bước** | 1. Vào `/Procurement/Create` 2. Điền thông tin 3. Thêm dòng sản phẩm 4. Gửi |
| **Kết quả mong đợi** | Đề xuất được tạo, chờ phê duyệt |
| **Trạng thái** | ✅ Pass |

### TC-009: Seed Data chạy không lỗi

| Mục | Nội dung |
|---|---|
| **ID** | TC-009 |
| **Tên** | Chạy seed_data.sql trên DB mới |
| **Bước** | 1. Drop DB 2. `dotnet ef database update` 3. Chạy `seed_data.sql` |
| **Kết quả mong đợi** | Tất cả 5 PRINT thành công, không có lỗi |
| **Trạng thái** | ✅ Pass |

### TC-010: Chuyển đổi giao diện điều hướng

| Mục | Nội dung |
|---|---|
| **ID** | TC-010 |
| **Tên** | Chuyển giữa 3 chế độ nav |
| **Bước** | 1. Nhấn nút đổi layout trên topbar 2. Chọn "App Launcher" 3. F5 reload |
| **Kết quả mong đợi** | Giao diện giữ chế độ Launcher sau reload |
| **Trạng thái** | ✅ Pass |

---

## 5. Test Data

Sử dụng dữ liệu từ `Data/Seed/seed_data.sql`:
- 1 Tenant, 11 phòng ban, 7 vai trò, 15+ tài khoản
- 2 khách hàng, 1 nhà cung cấp, 2 sản phẩm
- 2 OKR objectives, 2 KPI definitions
- 1 yêu cầu vận hành mẫu, 1 đề xuất mua sắm mẫu

---

## 6. Bug Report Template

| Trường | Mô tả |
|---|---|
| **ID** | BUG-xxx |
| **Tiêu đề** | Mô tả ngắn lỗi |
| **Mức độ** | Critical / High / Medium / Low |
| **Module** | Module bị ảnh hưởng |
| **Bước tái hiện** | Các bước để tái hiện lỗi |
| **Kết quả thực tế** | Lỗi xảy ra như thế nào |
| **Kết quả mong đợi** | Hành vi đúng |
| **Trạng thái** | Open / Fixed / Verified |

### Lỗi đã phát hiện và sửa

| ID | Tiêu đề | Mức độ | Trạng thái |
|---|---|---|---|
| BUG-001 | LINQ DateOnly.FromDateTime không translate được | High | ✅ Fixed |
| BUG-002 | FK conflict khi DELETE seed data (NotificationDeliveries) | High | ✅ Fixed |
| BUG-003 | Invalid GUID format trong seed (chữ O, U, P, T) | Medium | ✅ Fixed |
| BUG-004 | Missing NOT NULL columns trong INSERT seed | Medium | ✅ Fixed |

---

## 7. Test Result Summary

| Loại test | Tổng | Pass | Fail | Skip |
|---|:---:|:---:|:---:|:---:|
| Functional | 10 | 10 | 0 | 0 |
| Authorization | 3 | 3 | 0 | 0 |
| Data Integrity | 2 | 2 | 0 | 0 |
| UI/UX | 3 | 3 | 0 | 0 |
| **Tổng cộng** | **18** | **18** | **0** | **0** |

---

## 8. UAT Checklist

| # | Tiêu chí | Kết quả |
|---|---|:---:|
| 1 | Đăng nhập/đăng xuất hoạt động đúng | ☐ |
| 2 | Dashboard hiển thị đúng số liệu | ☐ |
| 3 | Tạo/sửa/xóa yêu cầu vận hành | ☐ |
| 4 | Luồng phê duyệt đúng trình tự | ☐ |
| 5 | Kanban kéo thả mượt mà | ☐ |
| 6 | Thu chi ghi nhận chính xác | ☐ |
| 7 | Mua sắm → PO → Nhập kho liền mạch | ☐ |
| 8 | KPI Check-In cập nhật đúng | ☐ |
| 9 | Phân quyền đúng theo vai trò | ☐ |
| 10 | Giao diện responsive trên tablet | ☐ |
| 11 | Thông báo real-time hoạt động | ☐ |
| 12 | Báo cáo xuất Excel thành công | ☐ |
