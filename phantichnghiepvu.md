# Phân tích nghiệp vụ – Khâu Vận Hành (Operations)

Hệ thống OmniBizAI có **4 module con** phục vụ khâu vận hành, liên kết chặt với nhau:

```
┌──────────────────────┐   ┌──────────────────────┐
│ 1. Operation Requests│   │ 2. Operation Plans   │
│   (Yêu cầu vận hành) │   │   (Kế hoạch vận hành)│
└──────────────────────┘   └──────────────────────┘
┌──────────────────────┐   ┌──────────────────────┐
│ 3. Resource Mgmt     │   │ 4. Maintenance       │
│  (Quản lý tài nguyên)│   │  (Bảo trì thiết bị)  │
└──────────────────────┘   └──────────────────────┘
```

---

## 1) Operation Requests — Yêu cầu vận hành

**File:** `Controllers/OperationsController.cs`, `Views/Operations/`

Đây là **luồng xử lý yêu cầu công việc phát sinh** (sửa chữa, hỗ trợ, mua sắm gấp, dịch vụ nội bộ…).

### Vòng đời yêu cầu (state machine)

```
Draft ──Submit──▶ Pending ──Approve──▶ InProgress ──Complete──▶ Done
  │                  │                     │
  │                  ▼                  Hold/Resume
  └──── Cancel ◀─────┘                     │
                                        Reopen
```

### Cách sử dụng

- **Nhân viên** vào `Operations/Create` → điền tiêu đề, độ ưu tiên (Low/Med/High/Urgent), phòng ban xử lý, khách hàng (nếu có), hạn xử lý, **thêm các dòng hàng hóa/dịch vụ** (AddLine).
- Bấm **Submit** → hệ thống tự gửi thông báo cho quản lý (`NotificationService.SendToManagersAsync`).
- **Trưởng phòng** mở `Details` → bấm **StartWork** để bắt đầu xử lý, **Hold** nếu tạm dừng, **Complete** khi xong, **Reopen** nếu phải làm lại.
- Mọi thay đổi đều **broadcast notification** cho cả tổ chức (gắn 📋📝🔧✅⏸️▶️🔄).
- Trang `Statistics` hiển thị thống kê: tổng số yêu cầu, theo trạng thái, ưu tiên, phòng ban.

### Phân quyền

- **STAFF**: tạo / sửa yêu cầu.
- **DEPARTMENT_MANAGER**: khởi động, hoàn thành, tạm dừng, mở lại.
- **TENANT_ADMIN / SYSTEM_ADMIN**: xóa yêu cầu.

---

## 2) Operation Plans — Kế hoạch vận hành

**File:** `Controllers/OperationPlansController.cs`, `Services/OperationPlanService.cs`

Đây là **kế hoạch công việc dài hơi** (Daily/Weekly/Monthly), khác với "Request" mang tính phát sinh.

### Cách sử dụng

1. Trưởng phòng vào `OperationPlans/Create` → đặt **mã kế hoạch, tiêu đề, loại (Daily/Weekly/Monthly), ghi chú**.
2. Mở chi tiết, bấm **AddTask** → tạo nhiều task con: tên việc, mô tả, **người được giao**, **thiết bị sử dụng**, trạng thái.
3. Bấm **Analyze** → gọi AI (`AnalyzePlanWithAiAsync` qua `GeminiService`) phân tích kế hoạch: đánh giá tính khả thi, gợi ý rủi ro, tối ưu thứ tự thực hiện.

### Điểm hay

Mỗi task có thể gắn 1 thiết bị (Equipment) cụ thể → liên kết trực tiếp với module Resource & Maintenance.

---

## 3) Resource Management — Quản lý tài nguyên vận hành

**File:** `Controllers/ResourceManagementController.cs`, `Views/ResourceManagement/`

Quản lý **4 nhóm tài nguyên** dùng để vận hành:

| Tài nguyên | Mô tả | Thao tác chính |
|---|---|---|
| **Equipment** (thiết bị) | Máy móc có mã, loại, trạng thái | Thêm thiết bị → `ScheduleMaintenance` lên lịch bảo trì → `CompleteMaintenance` xác nhận |
| **WorkShift** (ca làm việc) | Định nghĩa ca + lịch phân công | `CreateShift` → `ShiftSchedule` xem lịch theo ngày → `AssignShift` phân công nhân viên vào ca |
| **Workspace** (khu vực làm) | Phân khu, hỗ trợ phân cấp cha-con | `CreateWorkspace` → chọn workspace cha → tạo cây khu vực |
| **Certificate** (chứng chỉ) | Bằng cấp/chứng chỉ nhân viên | `AddCertificate`, lọc theo loại, **cảnh báo hết hạn** (`expiredOnly`) |

### Cách vận hành

- Trang `Index` là **dashboard** tổng quan: số thiết bị/lỗi/đang bảo trì, ca trong ngày, chứng chỉ sắp hết hạn.
- Khi tạo kế hoạch (Module 2), người dùng chọn thiết bị từ danh sách này.
- Khi báo sự cố (Module 4), cũng dùng chung danh sách Equipment.

---

## 4) Maintenance — Bảo trì thiết bị

**File:** `Controllers/MaintenanceController.cs`, `Views/Maintenance/`

Module quan trọng nhất về **độ ổn định vận hành**, chia 4 mảng:

### (a) Incidents — Sự cố (Corrective Maintenance)

- Bất kỳ ai vào `ReportIncident` → chọn thiết bị, mức độ nghiêm trọng (Low/Med/High/Critical), kỹ thuật viên xử lý.
- Trưởng phòng `ResolveIncident` → ghi nhận giải pháp + chi phí.
- Có nút **AnalyzeIncident** gọi AI gợi ý nguyên nhân & cách khắc phục.

### (b) PM Schedules — Bảo trì định kỳ (Preventive Maintenance)

- `CreatePmSchedule` → chọn thiết bị, chu kỳ (ngày/tuần/tháng), kỹ thuật viên.
- Đến hạn, hệ thống đánh dấu **overdue**; lọc `overdueOnly` để xem.
- Bấm **ExecutePm** → ghi nhận đã làm xong, hệ thống tự đặt **lần bảo trì kế tiếp**.

### (c) Spare Parts — Phụ tùng thay thế

- `CreateSparePart` đăng ký phụ tùng (mã, loại, đơn giá, tồn kho).
- `AdjustStock` nhập (+) / xuất (–) kho khi bảo trì lấy phụ tùng.

### (d) Sensor Monitor — Giám sát IoT

- `SensorMonitor?equipmentId=...` xem **dữ liệu cảm biến gần nhất** (nhiệt độ, rung, áp suất…) của thiết bị.
- `SimulateSensor` / `QuickSimulate` (anonymous) → giả lập số liệu cảm biến ra cảnh báo, dùng cho demo & test luồng anomaly.

---

## Cách 4 module phối hợp khi vận hành thực tế

```
   [Request phát sinh]              [Kế hoạch định kỳ]
   Operations/Create                 OperationPlans/Create
          │                                  │
          ▼                                  ▼
   chọn Equipment ─────────►  Resource Management (Equipment, Shift, Workspace)
          │                                  │
          ▼                                  ▼
   xử lý / hoàn thành          Maintenance:
   (Submit→Start→Complete)    - PM định kỳ → ExecutePm
                              - Sự cố → ReportIncident → Resolve
                              - Lấy Spare Parts (AdjustStock)
                              - IoT Sensor cảnh báo sớm
          │                                  │
          └──────────► Notification + AI Analyze (Gemini) ◄──────┘
```

---

## Tóm tắt

- **Operations**: tiếp nhận yêu cầu vận hành "ad-hoc".
- **OperationPlans**: lập kế hoạch định kỳ, giao việc.
- **ResourceManagement**: kho tài nguyên (thiết bị, ca, khu vực, chứng chỉ) phục vụ 2 module trên.
- **Maintenance**: đảm bảo thiết bị luôn sẵn sàng (PM, CM, kho phụ tùng, IoT).
- Toàn bộ có **multi-tenant** (`ITenantContext`), **phân quyền theo role**, **thông báo realtime** và **tích hợp AI** (Gemini) để phân tích kế hoạch & sự cố.
