# OmniBizAI - Hướng Dẫn Sử Dụng Hệ Thống

> Tách từ tài liệu tổng OmniBizAI.
> Mục đích: dành cho người dùng cuối, người demo và người không cần biết kỹ thuật.
> Nguyên tắc bảo mật: Không ghi password thật, API key thật hoặc secret thật trong tài liệu này.

## 13. Hướng Dẫn Sử Dụng Hệ Thống


Phần này dành cho người không cần biết lập trình, database hay AI.


### 13.1 Bắt đầu sử dụng


#### Mở hệ thống


Mục đích: Truy cập vào OmniBizAI để bắt đầu làm việc.


Ai được dùng: Tất cả người dùng có tài khoản.


1. Mở trình duyệt Chrome, Edge hoặc Firefox.

2. Nhập địa chỉ hệ thống do nhóm triển khai cung cấp.

3. Chờ trang đăng nhập hiển thị.

4. Nếu trang không mở được, kiểm tra internet hoặc liên hệ Admin.


Kết quả mong đợi: Màn hình đăng nhập hiển thị.


[Ảnh minh họa: Màn hình đăng nhập hệ thống]


#### Đăng nhập


1. Nhập email.

2. Nhập mật khẩu.

3. Bấm "Đăng nhập".

4. Nếu thông tin đúng, hệ thống đưa bạn vào dashboard theo vai trò.

5. Nếu thông tin sai, đọc thông báo và nhập lại.


Ghi chú bảo mật:


- Không chia sẻ mật khẩu.

- Đăng xuất sau khi dùng trên máy lạ.

- Nhập sai nhiều lần có thể bị khóa tạm thời.


#### Đăng xuất


1. Bấm ảnh đại diện hoặc tên người dùng ở góc trên.

2. Chọn "Đăng xuất".

3. Chờ hệ thống quay về màn hình đăng nhập.


#### Đổi mật khẩu


1. Bấm ảnh đại diện hoặc tên người dùng.

2. Chọn "Hồ sơ cá nhân".

3. Chọn "Đổi mật khẩu".

4. Nhập mật khẩu hiện tại.

5. Nhập mật khẩu mới.

6. Nhập lại mật khẩu mới.

7. Bấm "Lưu thay đổi".


### 13.2 Vai trò nào làm được gì


| Chức năng | Director | Manager | Accountant | HR | Staff | Admin |

|---|---:|---:|---:|---:|---:|---:|

| Xem dashboard công ty | Có | Không | Một phần | Không | Không | Có |

| Xem dashboard phòng ban | Có | Có | Một phần | Một phần | Không | Có |

| Tạo đề nghị thanh toán | Có | Có | Có | Có | Có | Có |

| Duyệt đề nghị thanh toán | Có | Có | Không | Không | Không | Có |

| Tạo/sửa ngân sách | Có | Không | Có | Không | Không | Có |

| Ghi nhận giao dịch | Không | Không | Có | Không | Không | Có |

| Quản lý nhân viên | Không | Một phần | Không | Có | Không | Có |

| Tạo/sửa KPI/OKR | Có | Có | Không | Không | Không | Có |

| Check-in KPI | Có | Có | Có | Có | Có | Có |

| Duyệt check-in KPI | Có | Có | Không | Không | Không | Có |

| Dùng AI Assistant | Có | Có | Có | Có | Có | Có |

| Xem phân tích thị trường | Có | Có | Có | Một phần | Giới hạn | Có |

| Xuất báo cáo | Có | Có | Có | Có | Không | Có |

| Quản lý tài khoản/phân quyền | Không | Không | Không | Một phần | Không | Có |

| Xem audit log | Không | Không | Không | Không | Không | Có |


### 13.3 Hướng dẫn theo vai trò


#### Director


1. Đăng nhập bằng tài khoản Director.

2. Vào "Dashboard".

3. Chọn kỳ cần xem.

4. Xem tổng thu, tổng chi, ngân sách còn lại, KPI trung bình và pending approvals.

5. Mở "Cảnh báo rủi ro" để xem vấn đề ưu tiên.

6. Bấm "AI Assistant" để hỏi thêm về chiến lược, thị trường hoặc phương án xử lý.

7. Vào "Hàng chờ duyệt" để duyệt các đề nghị cấp cao.


[Ảnh minh họa: Dashboard Director]


#### Manager


1. Vào "Dashboard phòng ban".

2. Xem ngân sách, KPI và việc chờ duyệt của phòng ban.

3. Vào "Hàng chờ duyệt" để xử lý PR.

4. Vào "KPI/OKR" để xem nhân viên chậm tiến độ.

5. Dùng AI để hỏi cách hỗ trợ nhân viên hoặc tối ưu nguồn lực.


[Ảnh minh họa: Dashboard Manager]


#### Accountant


1. Vào "Ngân sách" để xem mức sử dụng ngân sách.

2. Vào "Giao dịch" để ghi nhận thu/chi.

3. Vào "Nhà cung cấp" để quản lý vendor.

4. Vào "Báo cáo" để tạo báo cáo tài chính.

5. Dùng AI để tìm chi phí bất thường hoặc đề xuất tối ưu chi.


[Ảnh minh họa: Màn hình ngân sách của Accountant]


#### HR


1. Vào "Nhân viên".

2. Tìm nhân viên cần cập nhật.

3. Mở hồ sơ, bấm "Chỉnh sửa".

4. Cập nhật phòng ban, chức vụ, quản lý trực tiếp hoặc trạng thái.

5. Bấm "Lưu".

6. Kiểm tra lịch sử thay đổi nếu cần.


[Ảnh minh họa: Màn hình hồ sơ nhân viên]


#### Staff


1. Vào "Dashboard cá nhân".

2. Xem việc cần làm và thông báo.

3. Vào "Đề nghị thanh toán" để tạo đề nghị mới.

4. Vào "KPI của tôi" để check-in tiến độ.

5. Dùng AI để hỏi cách cải thiện KPI hoặc tóm tắt việc cần làm.


[Ảnh minh họa: Dashboard cá nhân của Staff]


#### Admin


1. Vào "Admin".

2. Quản lý user, role, permission.

3. Xem audit log.

4. Cấu hình system settings.

5. Nhập hoặc upload dữ liệu thị trường cho AI.

6. Theo dõi health check và chất lượng dữ liệu.


[Ảnh minh họa: Màn hình quản lý người dùng]


### 13.4 Quy trình nghiệp vụ chính


#### Tạo đề nghị thanh toán


1. Vào "Tài chính".

2. Chọn "Đề nghị thanh toán".

3. Bấm "Tạo mới".

4. Nhập tiêu đề, phòng ban, danh mục, vendor và mô tả.

5. Thêm từng dòng chi tiết: mô tả, số lượng, đơn giá.

6. Đính kèm hóa đơn/báo giá nếu có.

7. Bấm "Lưu nháp" nếu chưa gửi.

8. Xem AI Risk Analysis.

9. Bấm "Gửi duyệt" khi đã kiểm tra xong.


Kết quả mong đợi: De nghị vào workflow duyệt và người duyệt nhận thông báo.


[Ảnh minh họa: Màn hình tạo đề nghị thanh toán]


#### Xem AI Risk Analysis


1. Mở đề nghị thanh toán.

2. Tìm khu vực "AI Risk Analysis".

3. Đọc risk score, risk level, risk factors.

4. Đọc recommendation.

5. Nếu rủi ro cao, bổ sung giải trình hoặc chỉnh sửa trước khi gửi.


Ghi nhớ: AI chỉ cảnh báo và đề xuất, không tự chặn hoặc tự duyệt.


[Ảnh minh họa: Khu vực AI Risk Analysis trong form đề nghị thanh toán]


#### Duyệt/từ chối/yêu cầu chỉnh sửa


1. Vào "Hàng chờ duyệt".

2. Mở đề nghị cần xử lý.

3. Đọc thông tin, file đính kèm, AI risk và lịch sử.

4. Bấm "Duyệt" nếu đồng ý.

5. Bấm "Từ chối" nếu không đồng ý và nhập lý do.

6. Bấm "Yêu cầu chỉnh sửa" nếu cần người tạo bổ sung.


[Ảnh minh họa: Màn hình duyệt đề nghị thanh toán]


#### Tạo và điều chỉnh ngân sách


1. Vào "Ngân sách".

2. Bấm "Tạo ngân sách".

3. Chọn kỳ, phòng ban, danh mục.

4. Nhập số tiền phân bổ và ngưỡng cảnh báo.

5. Bấm "Lưu".

6. Khi cần điều chỉnh, mở ngân sách và bấm "Điều chỉnh".

7. Chọn tăng, giảm hoặc chuyển ngân sách.

8. Nhập lý do và gửi/lưu theo quyền.


[Ảnh minh họa: Màn hình tạo ngân sách]


#### Tạo OKR/KPI và check-in


1. Vào "KPI/OKR".

2. Tạo Objective.

3. Thêm Key Result.

4. Tạo KPI nếu cần đo riêng.

5. Người được giao KPI vào "KPI của tôi".

6. Bấm "Check-in".

7. Nhập giá trị mới, ghi chú và bằng chứng.

8. Gửi cho Manager duyệt.


[Ảnh minh họa: Màn hình tạo OKR và KPI]


#### Dùng AI Assistant


1. Bấm "AI Assistant".

2. Chọn câu hỏi gợi ý hoặc nhập câu hỏi.

3. Ghi rõ thời gian, phòng ban, mục tiêu nếu có.

4. Bấm "Gửi".

5. Đọc summary, analysis, recommendation, confidence và citation.

6. Kiểm chứng thông tin quan trọng trước khi hành động.


Ví dụ:


- "Tháng này phòng ban nào có nguy cơ vượt ngân sách cao nhất?"

- "Quý sau nên ưu tiên giảm chi phí nào?"

- "Dựa trên xu hướng thị trường, phòng Marketing nên điều chỉnh ngân sách ra sao?"


[Ảnh minh họa: Màn hình AI Assistant]


#### Xem Market Signals và AI Recommended Actions


1. Vào dashboard hoặc AI Assistant.

2. Mở "Market Signals".

3. Chọn chủ đề: tài chính, nhân sự, marketing, vendor, KPI.

4. Đọc tín hiệu thị trường.

5. Đọc tác động đến doanh nghiệp.

6. Đọc đề xuất hành động.

7. Chọn Accept/Dismiss nếu UI có hỗ trợ.


[Ảnh minh họa: Market Signals và AI Recommended Actions]


#### Tạo báo cáo và xuất PDF/Excel


1. Vào "Báo cáo".

2. Chọn loại báo cáo.

3. Chọn kỳ và phạm vi.

4. Bấm "Xem trước".

5. Kiểm tra số liệu.

6. Bấm "Tạo tóm tắt AI" nếu cần.

7. Bấm "Xuất PDF" hoặc "Xuất Excel".


[Ảnh minh họa: Màn hình tạo và xuất báo cáo]


#### Xem thông báo


1. Bấm biểu tượng chuông.

2. Xem thông báo mới.

3. Bấm vào thông báo để mở chi tiết.

4. Bấm "Đánh dấu đã đọc" hoặc "Đánh dấu tất cả đã đọc".


[Ảnh minh họa: Bảng thông báo]


#### Tìm kiếm, lọc, sắp xếp, phân trang


1. Vào một màn hình danh sách.

2. Nhập từ khóa vào ô tìm kiếm.

3. Chọn bộ lọc trạng thái, phòng ban, kỳ hoặc ngày.

4. Bấm "Áp dụng".

5. Bấm tên cột để sắp xếp.

6. Dùng phân trang ở cuối bảng.


[Ảnh minh họa: Tìm kiếm và bộ lọc danh sách]


### 13.5 AI cho người không chuyên


AI có thể:


- Giải thích số liệu.

- Tóm tắt tình hình.

- Phát hiện rủi ro.

- Gợi ý hành động.

- Viết bản nháp báo cáo.

- Kết hợp dữ liệu nội bộ và dữ liệu thị trường đã nhập.


AI không thể:


- Tự duyệt đề nghị.

- Tự sửa ngân sách, KPI, giao dịch.

- Xem dữ liệu ngoài quyền của bạn.

- Đảm bảo mọi đề xuất đúng tuyệt đối.

- Thay thế quyết định của con người.


Cách hỏi tốt:


1. Nêu thời gian.

2. Nêu phạm vi.

3. Nêu mục tiêu.

4. Yêu cầu AI đưa lý do và nguồn.


Ví dụ tốt:


- "Trong tháng 5/2026, phòng ban nào có nguy cơ vượt ngân sách cao nhất và vì sao?"

- "Dựa trên KPI quý này, nhân viên nào trong phòng Marketing cần được hỗ trợ trước?"

- "Đề xuất 3 cách giảm chi phí vendor trong tháng tới, dựa trên dữ liệu giao dịch hiện có."


### 13.6 FAQ và lỗi thường gặp


| Tình huống | Nguyên nhân có thể | Cách xử lý |

|---|---|---|

| Không đăng nhập được | Sai email/mật khẩu | Kiểm tra và nhập lại |

| Tài khoản bị khóa | Sai mật khẩu nhiều lần | Chờ mở khóa hoặc liên hệ Admin |

| Không thấy nút thao tác | Không có quyền | Hỏi quản lý/Admin nếu cần cấp quyền |

| Không xem được dữ liệu phòng khác | Data scope giới hạn | Đây là hành vi bảo mật đúng |

| Upload file lỗi | File quá lớn hoặc sai định dạng | Dùng PDF/JPG/PNG/XLSX/DOCX dưới giới hạn |

| Báo cáo không xuất được | Không có quyền hoặc lỗi tạm thời | Kiểm tra quyền, thử lại, báo Admin |

| AI không trả lời | AI lỗi, hết rate limit hoặc câu hỏi ngoài phạm vi | Thử lại sau hoặc đặt câu hỏi rõ hơn |

| AI nói thiếu dữ liệu | Chưa có đủ dữ liệu trong hệ thống | Chọn lại bộ lọc hoặc bổ sung dữ liệu |

| Dashboard chưa cập nhật | Cache/realtime chậm | Tải lại trang hoặc chờ vài phút |


