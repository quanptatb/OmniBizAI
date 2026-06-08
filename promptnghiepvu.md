# Prompt phân tích nghiệp vụ Khâu Vận Hành – OmniBizAI

> Dán toàn bộ nội dung dưới đây cho Codex / AI để được phân tích chuyên sâu về khâu vận hành, kèm dữ liệu mẫu tiếng Việt sát thực tế để bạn paste vào web test trực tiếp.

---

## PHẦN 1 — PROMPT GỬI CHO CODEX

```
Bạn là một Business Analyst kiêm Solution Architect cấp cao, chuyên về ERP/Operation
Management cho doanh nghiệp Việt Nam (sản xuất, dịch vụ, vận hành nhà máy).

Tôi cần bạn PHÂN TÍCH KỸ LƯỠNG khâu "Vận Hành" (Operations) của dự án ASP.NET Core MVC
mang tên OmniBizAI (multi-tenant SaaS, dùng EF Core + SQL Server, .NET 10, tích hợp
Gemini AI và NotificationService realtime).

# BỐI CẢNH DỰ ÁN
- Đường dẫn: E:\datn\OmniBizAI
- Stack: ASP.NET Core MVC (Razor Views), EF Core, SQL Server, Identity + Role-based.
- Multi-tenant qua ITenantContext (mỗi công ty là 1 tenant).
- AI: GeminiService (Google Gemini) để phân tích kế hoạch & sự cố.
- Notification: NotificationService.SendToManagersAsync / BroadcastAsync.

# PHẠM VI PHÂN TÍCH (CHỈ KHÂU VẬN HÀNH)
Khâu vận hành gồm 4 module:
1. Operation Requests  – Controllers/OperationsController.cs, Views/Operations/
2. Operation Plans     – Controllers/OperationPlansController.cs, Services/OperationPlanService.cs
3. Resource Management – Controllers/ResourceManagementController.cs, Views/ResourceManagement/
4. Maintenance         – Controllers/MaintenanceController.cs, Views/Maintenance/

Các entity / ViewModel liên quan: OperationPlanViewModels.cs, ResourceManagementViewModels.cs,
MaintenanceViewModels.cs, Models/Entities/*.

# YÊU CẦU CỤ THỂ — TRẢ LỜI ĐẦY ĐỦ TỪNG MỤC

## A. Phân tích nghiệp vụ tổng thể
1. Vẽ sơ đồ luồng nghiệp vụ end-to-end của khâu vận hành (ASCII diagram) thể hiện
   sự phối hợp giữa 4 module, vai trò actor, và điểm chạm AI/Notification.
2. Liệt kê các "domain object" cốt lõi (OperationRequest, OperationPlan, PlanTask,
   Equipment, WorkShift, ShiftAssignment, Workspace, Certificate, Incident,
   PmSchedule, SparePart, SensorReading) — mỗi cái 2-3 dòng giải thích vai trò.
3. Mô tả các state machine (vòng đời) chính: OperationRequest, PmSchedule, Incident,
   PlanTask — vẽ bằng ASCII và liệt kê các transition cùng điều kiện.

## B. Phân tích chi tiết từng module
Với MỖI module trong 4 module, trả lời:
- Mục đích nghiệp vụ (problem nó giải quyết).
- Các use-case chính + actor + role được phép (STAFF, DEPARTMENT_MANAGER,
  EXECUTIVE, TENANT_ADMIN, SYSTEM_ADMIN, HR_MANAGER).
- Các action HTTP endpoint quan trọng và side effect (notification, AI call, DB write).
- Các edge case / business rule ẩn (ví dụ: DueDate không nhỏ hơn hôm nay, PM tự đặt
  next-due sau khi Execute, stock không được âm…).
- Điểm tích hợp với module khác (vd: PlanTask gắn EquipmentId → khi thiết bị down
  thì task ảnh hưởng gì?).

## C. Phân tích AI & Notification
1. AnalyzePlanWithAiAsync và AnalyzeIncidentWithAiAsync hoạt động ra sao? Prompt gửi
   Gemini gồm gì? Kết quả hiển thị ở đâu (TempData)?
2. Mỗi action vận hành nào kích hoạt notification? SendToManagersAsync vs
   BroadcastAsync khác nhau khi nào?

## D. Đánh giá chất lượng & rủi ro
1. Liệt kê các điểm CÒN THIẾU/CHƯA TỐT về nghiệp vụ (vd: thiếu approval chain,
   thiếu SLA, thiếu cost tracking, thiếu liên kết Inventory thật khi xuất spare part…).
2. Liệt kê rủi ro kỹ thuật (race condition khi 2 manager cùng Complete, thiếu
   optimistic concurrency, multi-tenant leak…).
3. Đề xuất 5-10 cải tiến ưu tiên theo thứ tự (Quick win → Big bet).

## E. Hướng dẫn vận hành thực tế (Runbook)
Với mỗi module, đưa ra "kịch bản test thủ công" gồm tài khoản nên dùng + dữ liệu mẫu
+ thứ tự bấm nút để chạy hết happy path và 1 unhappy path.

# OUTPUT FORMAT
- Tiếng Việt, súc tích, có heading rõ ràng.
- Dùng bảng khi liệt kê đối chiếu (use-case × role, transition × condition).
- Dùng ASCII diagram cho luồng nghiệp vụ.
- Cuối cùng có 1 "TL;DR — 10 dòng" tóm tắt cho lãnh đạo không kỹ thuật.

# DỮ LIỆU MẪU TIẾNG VIỆT (BẮT BUỘC DÙNG ĐÚNG NHƯ DƯỚI ĐÂY ĐỂ MINH HỌA)
Phần dữ liệu mẫu được đặt ở PHẦN 2 của tài liệu này. Khi bạn viết ví dụ, hãy lấy
nguyên văn các giá trị đó để tôi có thể paste thẳng vào web mà test theo lời bạn
hướng dẫn.

Hãy bắt đầu phân tích.
```

---

## PHẦN 2 — DỮ LIỆU MẪU TIẾNG VIỆT ĐỂ TEST TRÊN WEB

Tất cả dữ liệu bên dưới đã được thiết kế **sát thực tế công ty sản xuất – cơ khí Việt Nam**, có thể paste thẳng vào form trong app OmniBizAI để chạy thử end-to-end.

### 🏢 Bối cảnh giả định

- **Tenant:** Công ty TNHH Cơ Khí Chính Xác Đại Phát
- **Địa chỉ:** Lô C12, KCN Quang Minh, Mê Linh, Hà Nội
- **Phòng ban:** Sản Xuất, Bảo Trì Kỹ Thuật, Kho Vận, Kinh Doanh, Nhân Sự
- **Người dùng test:**
  | Họ tên | Vai trò | Role |
  |---|---|---|
  | Nguyễn Văn Bảo | Trưởng phòng Bảo Trì | DEPARTMENT_MANAGER |
  | Trần Thị Hương | Quản đốc Sản Xuất | DEPARTMENT_MANAGER |
  | Lê Minh Tuấn | Kỹ thuật viên bảo trì | STAFF |
  | Phạm Quốc Hùng | Công nhân vận hành CNC | STAFF |
  | Đỗ Thị Mai | Giám đốc Điều Hành | EXECUTIVE |

---

### 1️⃣ MODULE: Operation Requests — Yêu cầu vận hành

**Mục `Operations/Create` — Yêu cầu #1 (sửa chữa khẩn cấp):**

| Trường | Giá trị mẫu |
|---|---|
| Tiêu đề | Sửa chữa khẩn cấp máy tiện CNC HAAS ST-20 – trục chính kêu bất thường |
| Mô tả | Trục chính (spindle) phát ra tiếng kêu lạ khi chạy ở tốc độ trên 2000 rpm, nghi vòng bi mòn. Cần kiểm tra và thay thế trước ca tối nay để kịp tiến độ lô hàng Samsung. |
| Phòng ban xử lý | Bảo Trì Kỹ Thuật |
| Khách hàng liên quan | Samsung Electronics Việt Nam (đơn hàng PO-2026-0118) |
| Độ ưu tiên | Urgent |
| Hạn xử lý | 2026-05-24 |
| AddLine #1 | Vòng bi SKF 6206-2RS — SL: 2 — ĐVT: cái |
| AddLine #2 | Dầu bôi trơn Shell Omala S2 G220 — SL: 5 — ĐVT: lít |
| AddLine #3 | Công lao động kỹ thuật viên — SL: 4 — ĐVT: giờ |
| Bình luận khi xử lý | Đã tháo trục chính, xác nhận 2 vòng bi mòn rỗ nặng. Tiến hành thay mới. |

**Yêu cầu #2 (dịch vụ nội bộ – Medium):**

| Trường | Giá trị mẫu |
|---|---|
| Tiêu đề | Yêu cầu kiểm định an toàn cầu trục 5 tấn xưởng A |
| Mô tả | Cầu trục 5 tấn lắp 03/2024, đã đến chu kỳ kiểm định an toàn 2 năm theo quy định TT 36/2019/BLĐTBXH. |
| Phòng ban | Bảo Trì Kỹ Thuật |
| Độ ưu tiên | Medium |
| Hạn xử lý | 2026-06-15 |

**Yêu cầu #3 (mua sắm gấp – High):**

| Trường | Giá trị mẫu |
|---|---|
| Tiêu đề | Bổ sung gấp dao phay hợp kim ∅12 cho dây chuyền B |
| Mô tả | Hết tồn kho dao phay hợp kim đường kính 12mm – không thể tiếp tục gia công lô khuôn nhựa. Cần đặt mua ngay 20 con. |
| Phòng ban | Kho Vận |
| Độ ưu tiên | High |
| Hạn xử lý | 2026-05-26 |

---

### 2️⃣ MODULE: Operation Plans — Kế hoạch vận hành

**`OperationPlans/Create` — Kế hoạch tuần:**

| Trường | Giá trị mẫu |
|---|---|
| Mã KH (tự sinh) | OP-2026-W21 |
| Tiêu đề | Kế hoạch sản xuất tuần 21/2026 – Lô hàng linh kiện ô tô VinFast |
| Loại | Weekly |
| Bắt đầu | 2026-05-25 |
| Kết thúc | 2026-05-31 |
| Ghi chú | Tổng sản lượng mục tiêu: 12.000 chi tiết. Ưu tiên đơn VF-2026-0532. Đảm bảo OEE ≥ 78%. |

**`AddTask` — 4 task con thực tế:**

| # | Tên task | Mô tả | Bắt đầu | Kết thúc | Người được giao | Thiết bị |
|---|---|---|---|---|---|---|
| 1 | Gia công thô 3.000 chi tiết VF-A12 | Phay mặt phẳng + khoan lỗ định vị theo bản vẽ DWG-VF-A12 rev.3 | 25/05 08:00 | 26/05 17:00 | Phạm Quốc Hùng | Máy phay CNC HAAS VF-2 |
| 2 | Tiện hoàn thiện 3.000 chi tiết | Tiện đường kính ngoài đạt dung sai ±0.02mm | 26/05 08:00 | 27/05 17:00 | Phạm Quốc Hùng | Máy tiện CNC HAAS ST-20 |
| 3 | Kiểm tra kích thước (QC) | Kiểm bằng máy CMM, lấy mẫu 10% theo AQL 2.5 | 27/05 13:00 | 28/05 12:00 | Lê Minh Tuấn | Máy đo CMM Mitutoyo Crysta-Apex |
| 4 | Đóng gói + dán nhãn truy xuất | Đóng theo tiêu chuẩn VinFast PKG-STD-04, in nhãn QR theo lô | 28/05 13:00 | 29/05 17:00 | Phạm Quốc Hùng | (không thiết bị) |

> Sau khi tạo xong, bấm **Analyze** để Gemini phân tích kế hoạch.

---

### 3️⃣ MODULE: Resource Management — Quản lý tài nguyên

#### 🛠️ `CreateEquipment` — Thiết bị mẫu

| Trường | TB #1 | TB #2 | TB #3 |
|---|---|---|---|
| Mã | EQ-CNC-001 | EQ-CNC-002 | EQ-CRANE-01 |
| Tên | Máy tiện CNC HAAS ST-20 | Máy phay CNC HAAS VF-2 | Cầu trục 5 tấn xưởng A |
| Loại | Máy gia công | Máy gia công | Thiết bị nâng hạ |
| Vị trí | Xưởng A – Khu vực 1 | Xưởng A – Khu vực 2 | Xưởng A – Trần |
| Năm sản xuất | 2022 | 2023 | 2024 |
| Trạng thái | Active | Active | Active |
| Ghi chú | Bảo hành đến 03/2027, NSX HAAS Automation USA | Tích hợp đầu đo dao Renishaw OTS | Tải nâng tối đa 5T, kiểm định 03/2024 |

#### 🕐 `CreateShift` — Ca làm việc

| Mã ca | Tên | Giờ bắt đầu | Giờ kết thúc | Mô tả |
|---|---|---|---|---|
| CA-S | Ca sáng | 06:00 | 14:00 | Ca chính sản xuất |
| CA-C | Ca chiều | 14:00 | 22:00 | Ca tăng cường |
| CA-D | Ca đêm | 22:00 | 06:00 | Ca duy trì 24/7 |

**`AssignShift` mẫu:** Ngày 2026-05-25, gán Phạm Quốc Hùng vào ca **CA-S**, Lê Minh Tuấn vào ca **CA-C**.

#### 🏭 `CreateWorkspace` — Khu vực làm việc

| Cha | Tên | Loại |
|---|---|---|
| (gốc) | Xưởng A – Cơ khí chính xác | Production |
| Xưởng A | Khu vực CNC 1 | Machining |
| Xưởng A | Khu vực CNC 2 | Machining |
| Xưởng A | Khu vực QC | Quality |
| (gốc) | Kho phụ tùng tầng 1 | Warehouse |

#### 📜 `AddCertificate` — Chứng chỉ nhân viên

| Người | Loại | Mã chứng chỉ | Ngày cấp | Ngày hết hạn |
|---|---|---|---|---|
| Lê Minh Tuấn | Vận hành cầu trục hạng II | ATLĐ-CT-2024-0892 | 2024-03-10 | 2027-03-09 |
| Nguyễn Văn Bảo | An toàn điện hạ áp | ATĐ-2025-1207 | 2025-01-15 | **2026-06-30** (sắp hết hạn → test cảnh báo) |
| Phạm Quốc Hùng | Vận hành máy CNC bậc 3/7 | CNC-2023-V450 | 2023-08-20 | 2028-08-19 |

---

### 4️⃣ MODULE: Maintenance — Bảo trì thiết bị

#### 🚨 (a) `ReportIncident` — Báo sự cố

| Trường | Sự cố #1 | Sự cố #2 |
|---|---|---|
| Thiết bị | Máy tiện CNC HAAS ST-20 | Cầu trục 5 tấn xưởng A |
| Tiêu đề | Trục chính rung mạnh + tiếng kêu lạ ở 2000+ rpm | Phanh giữ tải bị trượt khoảng 5cm khi dừng |
| Mức độ | High | Critical |
| Mô tả chi tiết | Trong ca sáng 23/05, khi gia công chi tiết VF-A12 ở tốc độ 2400 rpm, công nhân nghe tiếng rít kéo dài và rung động vượt 4.5 mm/s. Đã dừng máy ngay. Nghi mòn vòng bi NN3010K-spindle. | Trong lúc hạ kiện hàng 3.2T xuống xe tải, phanh thủy lực không giữ chắc, kiện trượt xuống thêm ~5cm trước khi dừng hẳn. Có nguy cơ rơi tải. Đã treo biển "Cấm sử dụng". |
| Kỹ thuật viên | Lê Minh Tuấn | Nguyễn Văn Bảo |
| Báo bởi | Phạm Quốc Hùng | Trần Thị Hương |

**Khi `ResolveIncident` (Sự cố #1):**

| Trường | Giá trị |
|---|---|
| Giải pháp | Tháo cụm spindle, thay 02 vòng bi SKF 6206-2RS, căn chỉnh đồng tâm bằng đồng hồ so. Chạy thử không tải 30 phút – rung động đo lại 1.2 mm/s (đạt). |
| Chi phí phụ tùng | 2.450.000 đ |
| Chi phí nhân công | 800.000 đ (4h × 200.000đ) |
| Tổng chi phí | 3.250.000 đ |
| Thời gian downtime | 6 giờ |

> Bấm **AnalyzeIncident** để Gemini đề xuất root cause + biện pháp phòng ngừa.

#### 📅 (b) `CreatePmSchedule` — Bảo trì định kỳ

| Mã | Thiết bị | Chu kỳ | Tác vụ | KTV phụ trách |
|---|---|---|---|---|
| PM-CNC-ST20-M | Máy tiện CNC HAAS ST-20 | 30 ngày | Tra dầu trục chính, kiểm rung động, thay lọc dầu thủy lực, làm sạch tủ điện | Lê Minh Tuấn |
| PM-CNC-VF2-W | Máy phay CNC HAAS VF-2 | 7 ngày | Vệ sinh bàn máy, kiểm hệ thống tưới nguội, kiểm áp suất khí nén | Lê Minh Tuấn |
| PM-CRANE-Q | Cầu trục 5 tấn xưởng A | 90 ngày | Kiểm cáp thép, thử tải 110%, bôi trơn ray trượt, kiểm phanh thủy lực | Nguyễn Văn Bảo |

**`ExecutePm` mẫu (PM-CNC-VF2-W):**

| Trường | Giá trị |
|---|---|
| Kết quả | Đã vệ sinh bàn máy, thay dung dịch tưới nguội Castrol Hysol XBB 30L. Áp suất khí nén ổn định 6.2 bar. Phát hiện vòi tưới số 3 bị tắc → đã thay. |
| Phụ tùng đã dùng | Dung dịch tưới nguội Castrol Hysol XBB (30L), Vòi tưới ∅3mm (01 cái) |
| Giờ công | 2 giờ |
| KTV thực hiện | Lê Minh Tuấn |
| Ngày thực hiện | 2026-05-23 |

#### 📦 (c) `CreateSparePart` — Phụ tùng

| Mã | Tên | Loại | Đơn vị | Đơn giá (VNĐ) | Tồn min |
|---|---|---|---|---|---|
| SP-BRG-6206 | Vòng bi SKF 6206-2RS | Vòng bi | cái | 320.000 | 10 |
| SP-OIL-OMALA220 | Dầu bôi trơn Shell Omala S2 G220 | Dầu mỡ | lít | 180.000 | 20 |
| SP-COOL-XBB | Dung dịch tưới nguội Castrol Hysol XBB | Hóa chất | lít | 95.000 | 50 |
| SP-DAO-PHAY12 | Dao phay hợp kim ∅12mm – 4 me | Dụng cụ cắt | cái | 750.000 | 5 |
| SP-CABLE-SHL8 | Cáp thép cẩu trục ∅8mm | Cáp/dây | mét | 145.000 | 30 |

**`AdjustStock` mẫu:**
- Nhập kho: SP-BRG-6206 — `+20` — Lý do: "Nhập theo đơn PO-NCC-2026-0312 từ NCC Bạch Đằng"
- Xuất kho: SP-BRG-6206 — `-2` — Lý do: "Xuất dùng cho sự cố INC-2026-0089 máy ST-20"

#### 📡 (d) `SensorMonitor` — Dữ liệu cảm biến IoT (sau khi bấm `SimulateSensor`)

Ví dụ dữ liệu sẽ thấy hiển thị cho máy **EQ-CNC-001 (HAAS ST-20)**:

| Loại cảm biến | Giá trị | Ngưỡng cảnh báo | Trạng thái |
|---|---|---|---|
| Nhiệt độ trục chính | 72.5 °C | > 65 °C | ⚠️ Vượt ngưỡng |
| Rung động (RMS) | 4.8 mm/s | > 4.5 mm/s | ⚠️ Vượt ngưỡng |
| Áp suất thủy lực | 38 bar | 35–42 bar | ✅ Bình thường |
| Dòng tiêu thụ động cơ | 18.2 A | < 20 A | ✅ Bình thường |
| Giờ chạy tích lũy | 4.872 h | — | ℹ️ Thông tin |

> Mục đích: chạy `QuickSimulate` để hệ thống sinh cảnh báo → kiểm tra notification realtime tới Trưởng phòng Bảo Trì.

---

## PHẦN 3 — KỊCH BẢN TEST END-TO-END (chạy theo thứ tự)

1. **Login** bằng `nguyenvanbao@daiphat.vn` (DEPARTMENT_MANAGER – Bảo Trì).
2. Vào **Resource Management → CreateEquipment** → tạo cả 3 thiết bị ở mục 3️⃣.
3. Vào **Resource Management → CreateShift** → tạo 3 ca CA-S, CA-C, CA-D.
4. Vào **Resource Management → AddCertificate** → thêm 3 chứng chỉ (có 1 cái sắp hết hạn 30/06/2026 để test cảnh báo).
5. Logout, đăng nhập `phamquochung@daiphat.vn` (STAFF).
6. Vào **Operations → Create** → tạo Yêu cầu #1 (sửa CNC ST-20) → Submit.
7. Đăng nhập lại Trưởng phòng Bảo Trì → mở yêu cầu → **StartWork** → vào **Maintenance → ReportIncident** tạo Sự cố #1 (cùng máy ST-20) → **AnalyzeIncident** xem AI phân tích.
8. Vào **Maintenance → SpareParts → CreateSparePart** thêm SP-BRG-6206 → **AdjustStock** nhập 20 cái → xuất 2 cái cho sự cố.
9. Vào **Maintenance → ResolveIncident** điền giải pháp + chi phí 3.250.000đ.
10. Quay lại Operations → **Complete** yêu cầu #1.
11. Vào **OperationPlans → Create** tạo kế hoạch tuần 21 → **AddTask** 4 task → **Analyze** xem AI gợi ý.
12. Vào **Maintenance → CreatePmSchedule** tạo 3 PM → **ExecutePm** lên 1 cái.
13. Vào **Maintenance → QuickSimulate** (hoặc `SimulateSensor` cho EQ-CNC-001) → kiểm tra cảnh báo IoT + notification.
14. Vào **Operations → Statistics** + **ResourceManagement/Index** + **Maintenance/Index** xem dashboard tổng kết.

---

## PHẦN 4 — CÂU HỎI BỔ SUNG GỬI KÈM CHO CODEX (tùy chọn)

- "Hãy đối chiếu luồng vận hành hiện tại với chuẩn ISO 55000 (Asset Management) và ISO 9001 mục 8.5 — chỉ rõ điểm còn thiếu."
- "Đề xuất schema bổ sung để theo dõi OEE (Availability × Performance × Quality) cho từng Equipment."
- "Phân tích khả năng tích hợp với hệ MES/SCADA thực tế (OPC UA / MQTT) thay cho luồng SimulateSensor."
- "Viết 5 user story theo định dạng INVEST cho phần Maintenance còn thiếu (vd: condition-based maintenance, predictive maintenance)."

> Hết tài liệu prompt.
