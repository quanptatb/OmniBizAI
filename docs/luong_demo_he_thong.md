# Kịch bản demo end-to-end OmniBizAI

Tài liệu này dùng để demo toàn bộ hệ thống OmniBizAI theo một câu chuyện xuyên suốt, có vai diễn, lời thoại, tài khoản, màn hình thao tác và kết quả kỳ vọng sau mỗi cảnh.

Chủ đề chính của buổi demo:

**Ngày 02/07/2026, Ban điều hành OmniBiz họp liên phòng ban để chốt mục tiêu Quý 3/2026 cho chiến dịch upsell khách hàng hiện hữu. Sau cuộc họp, hệ thống phải tự biến bản summarize thành OKR/KPI, rồi kéo toàn bộ dữ liệu đi xuyên suốt CRM, vận hành, phê duyệt, mua sắm, kho, tài chính, đánh giá hiệu suất và dashboard.**

---

## 1. Tài khoản dùng trong demo

Mật khẩu mặc định cho toàn bộ tài khoản: `123`

| Vai trò | Họ tên | Email | Bộ phận |
|---|---|---|---|
| Giám đốc | Nguyễn Minh Tuấn | `giamdoc@omnibiz.vn` | Ban Giám Đốc |
| TP Kinh doanh | Hoàng Thị Mai | `tp.sales@omnibiz.vn` | Phòng Kinh Doanh |
| TP Marketing | Bùi Quang Hải | `tp.marketing@omnibiz.vn` | Phòng Marketing |
| TP Vận hành | Ngô Thị Thanh Hằng | `tp.ops@omnibiz.vn` | Phòng Vận Hành |
| TP CNTT | Phạm Đức Anh | `tp.it@omnibiz.vn` | Phòng Công Nghệ Thông Tin |
| TP Tài chính | Võ Thị Lan Anh | `tp.finance@omnibiz.vn` | Phòng Tài Chính |
| TP Nhân sự | Đặng Văn Khôi | `tp.hr@omnibiz.vn` | Phòng Nhân Sự |
| Kế toán trưởng | Nguyễn Thị Hạnh | `ketoan.truong@omnibiz.vn` | Phòng Tài Chính |
| NV Kinh doanh | Vũ Ngọc Hải | `vu.ngoc.hai@omnibiz.vn` | Phòng Kinh Doanh |
| Quản trị hệ thống | System Administrator | `sysadmin@omnibiz.vn` | Phòng Công Nghệ Thông Tin |

---

## 2. Đạo cụ dữ liệu đã chuẩn bị

1. Summary cuộc họp mẫu để import OKR/KPI: [demo_meeting_summary_q3_2026.md](/d:/DATN/OmniBizAI/docs/demo_meeting_summary_q3_2026.md)
2. Route import mới: `/KpiSetup/ImportMeetingSummary`
3. Khách hàng chiến lược đã seed:
   - `CUST-002` Tập đoàn Vingroup
   - Site: `SITE-VGR-HCM`, `SITE-VGR-HN`
   - Contact: Nguyễn Thùy Dương, Phạm Quốc Cường
4. Mission/Vision đã seed sẵn cho demo import:
   - Tăng doanh thu recurring từ khách hàng hiện hữu 25% trong năm 2026
   - Rút ngắn thời gian go-live trung bình còn 30 ngày
   - Nâng điểm NPS khách hàng doanh nghiệp lên tối thiểu 55 điểm
5. Evaluation period đã seed sẵn:
   - `Đánh giá hiệu suất Q3/2026`
   - `Đánh giá tháng 07/2026`
   - `Đánh giá tháng 08/2026`
   - `Đánh giá tháng 09/2026`

---

## 3. Bản đồ coverage module

| Cảnh | Module được cover |
|---|---|
| Cảnh 1 | Mission/Vision, OKR, KPI Setup, Import từ summary cuộc họp |
| Cảnh 2 | CRM, Customer, Contact, Site, Sales Opportunity |
| Cảnh 3 | Order Management, Order Process |
| Cảnh 4 | Operations, AI Insights |
| Cảnh 5 | Approvals |
| Cảnh 6 | Workflow Kanban, Operation Plans |
| Cảnh 7 | Resource Management, Maintenance |
| Cảnh 8 | Procurement, Purchase Order, Inventory, Goods Receipt, Goods Issue |
| Cảnh 9 | Finance, Budget, Payment Request, Expenses, Cash Book |
| Cảnh 10 | KPI Check-in, Evaluation, Leave |
| Cảnh 11 | Dashboard, Reports, Anomaly Alerts, Settings |

---

## 4. Vở diễn chính

## Cảnh 1. Phòng họp điều hành: import OKR/KPI từ summary cuộc họp

**Thời điểm:** sáng ngày **02/07/2026**

**Lời thoại mở màn**

- Giám đốc: “Quý 3 này chúng ta phải tăng doanh thu từ khách hàng hiện hữu, không chỉ bán thêm mà còn phải go-live nhanh hơn.”
- TP Kinh doanh: “Sales cần KPI theo doanh thu upsell từng tháng.”
- TP Marketing: “Marketing sẽ chịu KPI về số demo upsell đủ điều kiện.”
- TP Vận hành: “Operations cam kết giảm thời gian go-live và kéo NPS lên.”

**Người thao tác:** `giamdoc@omnibiz.vn`

**Màn hình**

1. Vào `/MissionVision`
2. Xác nhận các chiến lược seed sẵn đã có trong hệ thống
3. Vào `/KpiSetup/ImportMeetingSummary`
4. Nhấn `Nạp mẫu demo Q3/2026`
5. Nhấn `Phân tích summary và dựng preview`
6. Kiểm tra preview:
   - 1 objective
   - 4 key result
   - 4 KPI
   - match được Sales, Marketing, Operations
   - match được kỳ `Đánh giá hiệu suất Q3/2026`
7. Nhấn `Tạo 1 OKR + 4 KPI`

**Kết quả mong đợi**

1. Hệ thống tạo 1 objective:
   - `Tăng doanh thu khách hàng hiện hữu thêm 18% trong Q3/2026 thông qua upsell gói dịch vụ ERP và rút ngắn thời gian triển khai`
2. Hệ thống tạo 4 KR:
   - Doanh thu upsell đạt 18 tỷ
   - Tỷ lệ chốt cơ hội upsell đạt 32%
   - Thời gian go-live giảm còn 21 ngày
   - NPS đạt 55 điểm
3. Hệ thống tạo 4 KPI linked với từng KR
4. Có thể mở `/Okr/Details/{id}` để chứng minh objective, KR và mapping phòng ban đã được tạo thật

**Thông điệp demo**

“Điểm nhấn ở đây là ban điều hành không cần nhập tay từng OKR/KPI sau họp nữa. Summary cuộc họp được chuyển thành cấu hình thực thi ngay trong hệ thống.”

---

## Cảnh 2. Đội kinh doanh tiếp nhận mục tiêu: CRM và cơ hội bán hàng

**Thời điểm:** chiều ngày **02/07/2026**

**Lời thoại**

- TP Kinh doanh: “Muốn đạt 18 tỷ upsell thì phải ưu tiên nhóm enterprise hiện hữu trước.”
- NV Kinh doanh: “Em sẽ bám Vingroup vì bên đó đang có nhu cầu mở rộng rollout.”

**Người thao tác:** `vu.ngoc.hai@omnibiz.vn`

**Màn hình**

1. Vào `/Customers`
2. Mở khách hàng `CUST-002 - Tập đoàn Vingroup`
3. Trình diễn luôn:
   - contact chính
   - 2 địa điểm triển khai
4. Vào `/SalesOpportunity`
5. Tạo mới cơ hội:
   - Tiêu đề: `Upsell rollout ERP giai đoạn 2 cho Vingroup`
   - Giá trị ước tính: `18.000.000.000`
   - Giai đoạn: `Proposal`
   - Probability: `65%`
   - Temperature: `Warm`
6. Ghi nhận interaction:
   - Subject: `Họp scope rollout Q3/2026`
   - Type: `Meeting`
   - Priority: `High`

**Kết quả mong đợi**

1. CRM thể hiện khách hàng, site, contact, lịch sử tương tác
2. Sales opportunity xuất hiện trên pipeline
3. Có thể nói rõ đây chính là đầu vào thực tế để nuôi `KR1` và `KR2`

---

## Cảnh 3. Từ cơ hội sang thực thi: Order Management và Order Process

**Thời điểm:** ngày **03/07/2026**

**Lời thoại**

- TP Kinh doanh: “Nếu Vingroup duyệt proposal, đơn hàng phải vào hệ thống ngay để vận hành và tài chính bám được.”

**Người thao tác:** `tp.sales@omnibiz.vn`

**Màn hình**

1. Vào `/OrderManagement`
2. Tạo một đơn hàng/dự án dịch vụ mới cho rollout ERP giai đoạn 2
3. Gắn mô tả liên quan Vingroup, giá trị hợp đồng dự kiến, deadline triển khai
4. Vào `/OrderProcess`
5. Trình diễn luồng trạng thái xử lý đơn hàng

**Thông điệp demo**

“Cơ hội bán hàng không đứng riêng. Khi chín muồi, nó đi tiếp vào order/process để các phòng ban downstream bám tiến độ thật.”

---

## Cảnh 4. Đội IT và vận hành mở yêu cầu thực thi: Operations + AI Insights

**Thời điểm:** ngày **04/07/2026**

**Lời thoại**

- TP Vận hành: “Muốn go-live trong 21 ngày thì hạ tầng và kế hoạch triển khai phải nâng cấp.”
- TP CNTT: “Tôi sẽ tạo yêu cầu vận hành và cho AI phân tích rủi ro ngay.”

**Người thao tác:** `tp.it@omnibiz.vn`

**Màn hình**

1. Vào `/Operations/Create`
2. Tạo yêu cầu:
   - Tiêu đề: `Nâng cấp hạ tầng triển khai ERP cho Vingroup phase 2`
   - Type: `IT-SUPPORT`
   - Priority: `High`
   - Tổng kinh phí dự kiến: `50.000.000`
   - Mô tả: thêm RAM, SSD, backup plan, lịch cutover
3. Mở AI Insights từ module Operations
4. Hỏi AI theo ngữ cảnh:
   - “Phân tích rủi ro downtime và đề xuất lịch nâng cấp phù hợp”

**Kết quả mong đợi**

1. Yêu cầu vận hành mới xuất hiện trong danh sách
2. Phần AI trả về phân tích rủi ro và gợi ý giờ nâng cấp
3. Đây là bước nối giữa mục tiêu chiến lược và công việc triển khai thật

---

## Cảnh 5. Phê duyệt đa cấp

**Thời điểm:** ngày **04/07/2026**

**Lời thoại**

- TP Tài chính: “Tôi chỉ duyệt khi nhìn thấy ngân sách còn đủ.”
- Giám đốc: “Nếu rủi ro AI đã rõ và ngân sách ổn, tôi duyệt để chạy kịp quý.”

**Người thao tác**

1. `tp.finance@omnibiz.vn`
2. `giamdoc@omnibiz.vn`

**Màn hình**

1. Vào `/Approvals/MyTasks`
2. Tài chính duyệt bước 1, ghi chú:
   - `Ngân sách IT hiện còn đủ cho hạng mục nâng cấp`
3. Giám đốc duyệt bước 2, ghi chú:
   - `Phê duyệt, yêu cầu backup đầy đủ trước cutover`

**Kết quả mong đợi**

1. Approval task đổi trạng thái
2. Operation request đi từ submitted sang approved/in progress
3. Có thể mở audit trail hoặc activity log để chứng minh phê duyệt đa cấp

---

## Cảnh 6. Chuyển yêu cầu thành kế hoạch và công việc: Workflow Kanban + Operation Plans

**Thời điểm:** ngày **05/07/2026**

**Lời thoại**

- TP CNTT: “Sau khi duyệt, tôi không muốn chỉ có một request. Tôi cần checklist, người phụ trách, deadline và tiến độ nhìn được ngay.”

**Người thao tác:** `tp.it@omnibiz.vn`

**Màn hình**

1. Vào `/Workflow/Kanban`
2. Trình diễn các thẻ công việc được sinh ra hoặc tạo thêm:
   - Khảo sát hạ tầng
   - Chốt cấu hình mua sắm
   - Chuẩn bị backup
3. Kéo thả trạng thái `Todo -> In Progress -> Done`
4. Vào `/OperationPlans/Create`
5. Tạo kế hoạch triển khai cutover từ **06/07/2026** đến **20/07/2026**
6. Nếu có AI plan analysis, trình diễn thêm phần phân tích rủi ro lịch

**Kết quả mong đợi**

1. Lãnh đạo thấy được request không còn là “một tờ phiếu”
2. Nó đã biến thành work items có deadline, có assignee và có plan thực thi

---

## Cảnh 7. Bảo đảm nguồn lực và độ sẵn sàng: Resource Management + Maintenance

**Thời điểm:** ngày **06/07/2026**

**Lời thoại**

- TP Vận hành: “Nếu thiết bị, ca trực và lịch bảo trì không sẵn sàng thì KR3 sẽ vỡ.”

**Người thao tác:** `tp.ops@omnibiz.vn`

**Màn hình**

1. Vào `/ResourceManagement`
2. Xem phân công không gian, thiết bị, ca làm việc hoặc chứng chỉ nhân sự
3. Vào `/Maintenance`
4. Tạo hoặc mở sự cố/bảo trì liên quan server triển khai
5. Nếu phù hợp, bấm phân tích AI cho incident

**Thông điệp demo**

“OmniBizAI không chỉ quản KPI trên giấy. Nó còn chạm tới tài nguyên, thiết bị và readiness thực tế để mục tiêu khả thi.”

---

## Cảnh 8. Mua sắm và kho: Procurement + Inventory

**Thời điểm:** ngày **07/07/2026**

**Lời thoại**

- TP CNTT: “Đã duyệt thì phải mua hàng và nhập kho theo đúng trace.”
- TP Vận hành: “Tôi cần nhìn được hàng về kho trước khi triển khai.”

**Người thao tác**

1. `tp.it@omnibiz.vn`
2. `tp.ops@omnibiz.vn`

**Màn hình**

1. Vào `/Procurement`
2. Tạo Procurement Request cho server/phụ kiện
3. Chuyển sang Purchase Order
4. Vào `/GoodsReceipt/Create`
5. Lập phiếu nhập kho theo PO
6. Vào `/Inventory`
7. Xem tồn kho, cảnh báo stock alert, threshold
8. Nếu cần, trình diễn `/GoodsIssue/Create` để xuất thiết bị cho đội triển khai

**Kết quả mong đợi**

1. Chuỗi PR -> PO -> GR hoạt động đầy đủ
2. Dashboard kho phản ánh được thay đổi tồn
3. Có thể nói rõ chi phí này là phần thực thi cho OKR đã import từ cuộc họp

---

## Cảnh 9. Thanh toán và dòng tiền: Finance + Cash Book

**Thời điểm:** ngày **08/07/2026**

**Lời thoại**

- TP Tài chính: “Tôi muốn biết khoản mua này ăn vào ngân sách nào.”
- Kế toán trưởng: “Sau khi duyệt thanh toán, sổ quỹ phải phản ánh ngay.”

**Người thao tác**

1. `tp.it@omnibiz.vn`
2. `ketoan.truong@omnibiz.vn`

**Màn hình**

1. Vào `/Finance`
2. Tạo Payment Request đợt 1 cho hạng mục nâng cấp
3. Mở Budget IT để chỉ ra hạn mức còn lại
4. Kế toán trưởng duyệt/ghi nhận expense
5. Vào `/CashBook`
6. Trình diễn giao dịch chi tiền đã xuất hiện

**Kết quả mong đợi**

1. Budget usage thay đổi theo thời gian thực
2. Cash transaction phản ánh luồng tiền chi thật
3. Đây là điểm cực mạnh để demo “từ mục tiêu -> công việc -> tiền”

---

## Cảnh 10. Cuối tháng đầu quý: KPI Check-in + Evaluation + Leave

**Thời điểm:** ngày **31/07/2026**

**Lời thoại**

- TP Kinh doanh: “Tháng đầu quý tôi muốn check-in luôn KPI upsell.”
- TP Nhân sự: “Dữ liệu KPI phải chảy xuống đánh giá hiệu suất cuối kỳ.”

**Người thao tác**

1. `tp.sales@omnibiz.vn`
2. `tp.ops@omnibiz.vn`
3. `tp.hr@omnibiz.vn`
4. `giamdoc@omnibiz.vn`

**Màn hình**

1. Vào `/KpiCheckIn`
2. Check-in KPI doanh thu upsell, KPI lead upsell, KPI go-live hoặc NPS
3. Nếu muốn cover thêm HR, vào `/Leave/Create`
4. Tạo một đơn nghỉ phép mẫu cho nhân sự triển khai
5. Vào `/Evaluation/Create`
6. Tạo evaluation cuối kỳ gắn với period `Đánh giá hiệu suất Q3/2026`

**Kết quả mong đợi**

1. KPI imported ở Cảnh 1 có thể check-in thật
2. Evaluation lấy được ngữ cảnh từ kỳ đánh giá
3. HR module không đứng riêng mà sống cùng nhịp quản trị hiệu suất

---

## Cảnh 11. Tổng kết với lãnh đạo: Dashboard + Reports + Anomaly Alerts + Settings

**Thời điểm:** ngày **01/08/2026**

**Lời thoại kết**

- Giám đốc: “Tôi muốn mở một màn hình và thấy từ chiến lược, doanh thu, vận hành, ngân sách đến rủi ro.”

**Người thao tác:** `giamdoc@omnibiz.vn` hoặc `sysadmin@omnibiz.vn`

**Màn hình**

1. Vào `/Dashboard`
2. Vào `/Okr/Dashboard`
3. Vào `/Reports/KpiOkr`, `/Reports/Executive`, `/Reports/Finance`, `/Reports/Crm`
4. Vào `/AnomalyAlerts`
5. Nếu cần kết thúc đẹp, vào `/Settings/Company` hoặc `/Settings/Modules` để cho thấy hệ thống còn hỗ trợ cấu hình doanh nghiệp

**Kết quả mong đợi**

1. Dashboard hiển thị OKR/KPI vừa import cùng dữ liệu vận hành và tài chính
2. Reports cho thấy cùng một câu chuyện nhưng dưới góc nhìn lãnh đạo
3. Anomaly Alerts cho thấy lớp cảnh báo chủ động, không chờ sự cố bùng nổ rồi mới phản ứng

---

## 5. Checklist để buổi demo “mượt như thật”

1. Seed lại database trước buổi demo bằng `Data/Seed/seed_data.sql`
2. Đảm bảo đăng nhập được bằng các tài khoản ở mục 1
3. Ưu tiên bắt đầu bằng `/KpiSetup/ImportMeetingSummary` vì đây là “wow moment”
4. Sau mỗi cảnh chỉ cần chốt một câu: dữ liệu từ bước trước đã chảy sang bước sau như thế nào
5. Nếu thời gian ngắn, chạy tối thiểu 5 cảnh:
   - Cảnh 1
   - Cảnh 2
   - Cảnh 4
   - Cảnh 9
   - Cảnh 11

---

## 6. Câu chốt khi kết thúc demo

“OmniBizAI không chỉ là bộ màn hình rời rạc. Một summary cuộc họp ngày 02/07/2026 có thể được biến thành OKR/KPI thực thi, rồi toàn bộ dữ liệu ấy tiếp tục sống trong CRM, vận hành, phê duyệt, mua sắm, kho, tài chính, nhân sự và dashboard lãnh đạo mà không cần nhập đi nhập lại bằng tay.”
