# OmniBizAI - Tài Liệu Báo Cáo Học Thuật

> Tách từ tài liệu tổng OmniBizAI.
> Mục đích: dành cho báo cáo thực tập/đồ án tốt nghiệp và phần trình bày trước hội đồng.
> Nguyên tắc bảo mật: Không ghi password thật, API key thật hoặc secret thật trong tài liệu này.

## 16. Phụ Lục Xuất DOCX


### 16.1 Ảnh nên chèn sau khi có UI thật


1. Màn hình đăng nhập.

2. Dashboard Director.

3. Dashboard Manager.

4. Màn hình tạo đề nghị thanh toán.

5. Khu vực AI Risk Analysis.

6. Màn hình duyệt đề nghị thanh toán.

7. Màn hình ngân sách và cảnh báo vượt chi.

8. Màn hình tạo OKR/KPI.

9. Màn hình check-in KPI.

10. Màn hình AI Assistant.

11. Market Signals và Recommended Actions.

12. Màn hình tạo/xuất báo cáo.

13. Bảng thông báo.

14. Màn hình tìm kiếm và bộ lọc.


### 16.2 Nguyên tắc cập nhật tài liệu


- Mọi thay đổi nghiệp vụ phải cập nhật tài liệu trước hoặc cùng PR code.

- Mọi enum/status mới phải cập nhật state machine.

- Mọi route/action mới phải cập nhật route map.

- Mọi bảng mới phải cập nhật Database Blueprint.

- Mọi tính năng AI mới phải cập nhật prompt rule, data scope và test case.

- Mọi màn hình mới phải có hướng dẫn sử dụng nếu người dùng cuối thao tác trực tiếp.


## 17. Khung Báo Cáo Học Thuật Bổ Sung


Mục này dùng để chuyển tài liệu kỹ thuật thành nội dung báo cáo học thuật/đồ án. Khi xuất DOCX, có thể đặt chương này sau phần mở đầu và trước phần triển khai chi tiết; các mục kỹ thuật phía trên vẫn giữ vai trò phụ lục triển khai.


### 17.1 Lý do chọn đề tài và vấn đề nghiên cứu


#### 17.1.1 Lý do chọn đề tài


Trong bối cảnh chuyển đổi số, nhiều doanh nghiệp vừa và nhỏ có nhu cầu quản trị tài chính, nhân sự, KPI và quy trình phê duyệt trên cùng một nền tảng. Tuy nhiên, thực tế vận hành thường phụ thuộc vào bảng tính, email, tin nhắn nội bộ hoặc các phần mềm rời rạc. Điều này làm dữ liệu bị phân tán, giảm khả năng kiểm soát ngân sách, kéo dài thời gian phê duyệt và gây khó khăn khi lãnh đạo cần ra quyết định nhanh dựa trên số liệu.


Đề tài OmniBizAI được lựa chọn vì kết hợp được ba hướng có tính ứng dụng cao:


- Quản trị nguồn lực doanh nghiệp ở quy mô SME thông qua các module tài chính, nhân sự, KPI và báo cáo.

- Tự động hóa quy trình nghiệp vụ bằng workflow động, phân quyền theo vai trò và cơ chế audit.

- Ứng dụng AI Assistant để hỗ trợ phân tích, cảnh báo rủi ro và đề xuất hành động dựa trên dữ liệu nội bộ có kiểm soát.


Về mặt học thuật, đề tài có đủ không gian để áp dụng các kiến thức về phân tích yêu cầu, thiết kế hệ thống, cơ sở dữ liệu, kiến trúc phần mềm, kiểm thử, bảo mật và đánh giá phi chức năng. Về mặt thực tiễn, hệ thống hướng đến bài toán gần với hoạt động hằng ngày của doanh nghiệp: duyệt chi, kiểm soát ngân sách, theo dõi KPI, nhận cảnh báo và xuất báo cáo.


#### 17.1.2 Vấn đề nghiên cứu


Vấn đề nghiên cứu chính của đề tài là: **Làm thế nào để xây dựng một hệ thống quản trị vận hành cho doanh nghiệp vừa và nhỏ, có khả năng tích hợp dữ liệu tài chính, KPI, workflow và AI Assistant trong một kiến trúc dễ bảo trì, an toàn và có thể kiểm thử được?**


Từ vấn đề chính, đề tài tập trung trả lời các câu hỏi nghiên cứu sau:


| Mã | Câu hỏi nghiên cứu | Nội dung cần làm rõ |

|---|---|---|

| RQ1 | Làm thế nào để quản lý dữ liệu vận hành SME một cách tập trung? | Thiết kế database, module tài chính, KPI, nhân sự, dashboard |

| RQ2 | Làm thế nào để quy trình phê duyệt minh bạch và dễ truy vết? | Workflow template, workflow instance, approval action, audit log |

| RQ3 | Làm thế nào để phân quyền đúng vai trò và đúng phạm vi dữ liệu? | RBAC, data scope, policy-based authorization |

| RQ4 | Làm thế nào để AI hỗ trợ ra quyết định nhưng không vượt quyền hoặc bịa số liệu? | AI context provider, citation, fallback, audit AI query |

| RQ5 | Làm thế nào để hệ thống có thể bảo trì và mở rộng theo module? | Modular MVC Monolith, rule tách trách nhiệm, test strategy |

| RQ6 | Làm thế nào để đánh giá hệ thống bằng bằng chứng thực nghiệm? | Unit/integration/E2E test, k6 performance smoke test, screenshot UI thật |


#### 17.1.3 Problem statement theo format học thuật


| Thành phần | Nội dung |

|---|---|

| Background | SME cần quản lý tài chính, phê duyệt, KPI và báo cáo nhanh hơn nhưng thường dùng nhiều công cụ rời rạc như spreadsheet, email và chat nội bộ. |

| Problem | Dữ liệu phân tán làm quy trình duyệt chi chậm, khó kiểm soát ngân sách, khó theo dõi KPI và thiếu bằng chứng truy vết khi có tranh chấp hoặc kiểm tra. |

| Research gap | Nhiều hệ thống ERP/SaaS có chức năng rộng nhưng phức tạp, chi phí cao hoặc chưa kết hợp AI theo data scope; chatbot AI thông thường lại thiếu citation và không gắn với workflow nghiệp vụ. |

| Objective | Xây dựng một MVC web app cho SME, tích hợp Finance, Workflow, KPI, Dashboard, Report và AI Assistant có kiểm soát, đủ khả năng demo end-to-end và đánh giá bằng test/evidence. |

| Expected contribution | Đề tài cung cấp một mẫu hệ thống quản trị SME có kiến trúc đơn giản, phân quyền theo vai trò, workflow minh bạch, AI advisory có citation/fallback và tiêu chí đánh giá rõ ràng. |


### 17.2 Mục tiêu nghiên cứu


#### 17.2.1 Mục tiêu tổng quát


Xây dựng hệ thống OmniBizAI hỗ trợ doanh nghiệp vừa và nhỏ quản trị tài chính, workflow phê duyệt, KPI/OKR, dashboard điều hành, báo cáo và AI Assistant theo vai trò; đồng thời đảm bảo hệ thống có kiến trúc rõ ràng, phân quyền an toàn, dữ liệu có thể truy vết và có bằng chứng kiểm thử/thực nghiệm.


#### 17.2.2 Mục tiêu cụ thể


| Mã | Mục tiêu cụ thể | Kết quả cần đạt |

|---|---|---|

| O1 | Phân tích nghiệp vụ SME | Xác định actor, use case, chức năng, dữ liệu và quy trình chính |

| O2 | Thiết kế kiến trúc hệ thống | Áp dụng Modular MVC Monolith 1 project, định nghĩa rule tách trách nhiệm |

| O3 | Thiết kế cơ sở dữ liệu | Xây dựng ERD, schema theo module, migration order và seed data |

| O4 | Xây dựng phân quyền | Thiết kế role, permission, data scope và policy cho từng nhóm chức năng |

| O5 | Xây dựng workflow duyệt chi | Hỗ trợ tạo, submit, approve, reject, request change và audit action |

| O6 | Xây dựng KPI/OKR | Cho phép tạo mục tiêu, key result, check-in, duyệt check-in và cảnh báo tiến độ |

| O7 | Xây dựng AI Assistant | AI trả lời theo vai trò, dùng dữ liệu tổng hợp, có citation/fallback và audit |

| O8 | Xây dựng dashboard/report | Hiển thị số liệu theo vai trò, biểu đồ, cảnh báo và xuất PDF/XLSX |

| O9 | Kiểm thử và đo hiệu năng | Có unit test, integration test, E2E test, k6 smoke test và evidence |

| O10 | Đánh giá kết quả | Nêu ưu điểm, hạn chế, hướng phát triển và kết luận đề tài |


#### 17.2.3 Phương pháp nghiên cứu và triển khai


Đề tài sử dụng phương pháp thiết kế và xây dựng thực nghiệm theo quy trình Agile/Scrum rút gọn. Nhóm chia hệ thống thành các module độc lập, phát triển theo sprint, sau đó đánh giá bằng kiểm thử chức năng, kiểm thử phân quyền, kiểm thử hiệu năng và minh chứng giao diện thật.


| Giai đoạn | Phương pháp | Kết quả |

|---|---|---|

| Khảo sát và phân tích | Phân tích bài toán SME, actor, use case, quy trình duyệt chi/KPI | Problem statement, requirement, use case |

| Thiết kế | Thiết kế kiến trúc MVC modular, ERD, workflow, RBAC, data scope | Sơ đồ hệ thống, database blueprint, route map |

| Triển khai | Agile/Scrum rút gọn theo module và sprint | MVC app chạy được, seed data, module nghiệp vụ |

| Kiểm thử | Unit/integration/E2E/manual regression/k6 smoke | Test results, bug log, evidence |

| Đánh giá | So sánh mục tiêu với kết quả thực nghiệm | Nhận xét đạt/chưa đạt, hạn chế, hướng phát triển |


Sprint plan học thuật:


| Sprint | Trọng tâm | Deliverable |

|---|---|---|

| Sprint 1 | Scaffold MVC, Identity, DB foundation, layout | App chạy được, login, layout, migration base |

| Sprint 2 | Organization, RBAC, data scope, audit | Role/scope rõ, nhân sự/phòng ban |

| Sprint 3 | Finance, budget, payment request | PR draft/submit, budget impact |

| Sprint 4 | Workflow approval, notification | Approval queue, approve/reject/request change |

| Sprint 5 | KPI/OKR, dashboard | KPI check-in, dashboard theo role |

| Sprint 6 | AI, report/export, hardening | AI advisory, PDF/XLSX, test evidence |


#### 17.2.4 So sánh giải pháp


| Giải pháp | Ưu điểm | Hạn chế | Lý do không chọn làm hướng chính |

|---|---|---|---|

| ERP truyền thống | Chức năng rất đầy đủ, quy trình chuẩn hóa | Nặng, chi phí cao, khó tùy biến cho đồ án MVP | Quá rộng so với SME demo và thời gian nhóm sinh viên |

| SaaS quản trị rời rạc | Nhanh dùng, giao diện tốt | Dữ liệu phân mảnh, khó chứng minh thiết kế hệ thống và kiểm soát dữ liệu | Không thể hiện đủ năng lực phân tích, thiết kế, triển khai |

| Spreadsheet/email/chat | Linh hoạt, quen thuộc | Không có workflow/audit/data scope, khó báo cáo realtime | Là nguồn vấn đề cần giải quyết |

| OmniBizAI | Tập trung dữ liệu, workflow rõ, RBAC/data scope, AI advisory có citation, phù hợp MVP | Chưa có multi-tenant đầy đủ, AI phụ thuộc provider, scope hẹp hơn ERP thật | Phù hợp mục tiêu đồ án: xây dựng được, demo được, đánh giá được |


So sánh quyết định thiết kế quan trọng:


| Quyết định | Phương án A | Phương án B | Lựa chọn cho đề tài | Lý do |

|---|---|---|---|---|

| Kiến trúc triển khai | Monolith modular | Microservices | Monolith modular | Phù hợp team nhỏ, một DB, demo nhanh, giảm rủi ro distributed transaction |

| Workflow routing | Rule-based deterministic | AI-based decision | Rule-based deterministic | Phê duyệt là nghiệp vụ cần ổn định, audit được, không phụ thuộc output AI |

| Vai trò AI | Advisory assistant | Autonomous agent | Advisory assistant | AI hỗ trợ phân tích, không tự duyệt/sửa dữ liệu, giảm rủi ro hallucination |

| UI architecture | MVC Razor | SPA + API | MVC Razor | Giảm overhead FE/BE, phù hợp `dotnet new mvc`, đủ cho form/dashboard/report |

| Market data | Curated/manual | Live web crawling | Curated/manual | Dữ liệu kiểm soát được, dễ demo, tránh rủi ro nguồn tin và realtime dependency |


#### 17.2.5 Khung giả thuyết và đánh giá


| Mã | Giả thuyết/nhận định cần kiểm chứng | Metric | Phương pháp |

|---|---|---|---|

| H1 | Hệ thống tập trung dữ liệu giúp giảm thao tác thủ công trong luồng duyệt chi | Số bước thao tác, thời gian hoàn thành scenario | Manual scenario timing, screenshot evidence |

| H2 | Workflow rule-based giúp phê duyệt minh bạch và truy vết được | Tỷ lệ PR có timeline/audit đầy đủ | Integration test + audit review |

| H3 | RBAC + data scope ngăn người dùng xem dữ liệu ngoài quyền | Số test unauthorized bị chặn | Unit/integration authorization test |

| H4 | AI advisory giúp phát hiện rủi ro nhưng không làm hỏng nghiệp vụ khi provider lỗi | Citation/fallback rate, core flow pass khi AI mock fail | AI fallback test, E2E with AI disabled |

| H5 | MVC modular đủ hiệu năng cho demo SME | Page load, k6 fail rate, p95 JSON phụ trợ | Playwright/k6 |


Các giả thuyết này không cần chứng minh bằng số liệu doanh nghiệp thật, nhưng cần evidence từ hệ thống chạy thật với seed data đã định nghĩa.


### 17.3 Cơ sở lý thuyết


#### 17.3.1 ERP


ERP (Enterprise Resource Planning) là hệ thống tích hợp các hoạt động quản trị cốt lõi của doanh nghiệp như tài chính, nhân sự, vận hành, mua hàng và báo cáo. Đối với OmniBizAI, khái niệm ERP được vận dụng ở mức phù hợp với SME: không xây dựng toàn bộ ERP enterprise, mà tập trung vào các phân hệ có giá trị demo và giá trị quản trị cao gồm Finance, Organization/HR Basic, KPI/OKR, Workflow, Dashboard và Report.


Đặc điểm ERP được áp dụng trong đề tài:


- Dữ liệu tập trung thay vì phân tán ở nhiều file hoặc công cụ.

- Các module dùng chung danh mục phòng ban, nhân viên, vai trò và kỳ báo cáo.

- Giao dịch nghiệp vụ có trạng thái, lịch sử và audit.

- Báo cáo tổng hợp lấy dữ liệu từ nhiều phân hệ.


#### 17.3.2 BI


BI (Business Intelligence) là tập hợp phương pháp và công cụ giúp chuyển dữ liệu thô thành thông tin hỗ trợ ra quyết định. Trong OmniBizAI, BI được thể hiện qua dashboard theo vai trò, biểu đồ tài chính, chỉ số KPI, cảnh báo ngân sách, báo cáo PDF/XLSX và dữ liệu tổng hợp cung cấp cho AI Assistant.


Các nguyên tắc BI áp dụng:


- Dữ liệu báo cáo phải có nguồn gốc từ bảng nghiệp vụ rõ ràng.

- Dashboard cần ưu tiên chỉ số hành động được, không chỉ hiển thị số liệu.

- Bộ lọc thời gian, phòng ban và phạm vi quyền phải nhất quán.

- Số liệu nhạy cảm phải tuân theo data scope của người dùng hiện tại.


#### 17.3.3 Workflow


Workflow là mô hình biểu diễn chuỗi bước xử lý nghiệp vụ, trong đó mỗi bước có người thực hiện, điều kiện chuyển trạng thái và kết quả hành động. Trong đề tài, workflow được dùng cho quy trình duyệt đề nghị thanh toán và có thể mở rộng cho KPI check-in hoặc các quy trình nội bộ khác.


Các thành phần lý thuyết được ánh xạ vào hệ thống:


| Khái niệm | Ánh xạ trong OmniBizAI |

|---|---|

| Workflow template | Mẫu quy trình duyệt theo loại nghiệp vụ |

| Workflow step | Bước duyệt như Manager, Accountant, Director |

| Workflow condition | Điều kiện theo số tiền, phòng ban, loại chi phí |

| Workflow instance | Một lần chạy workflow cho một đề nghị cụ thể |

| Approval action | Approve, Reject, RequestChange |

| Escalation | Cảnh báo/quá hạn xử lý |


#### 17.3.4 RBAC


RBAC (Role-Based Access Control) là mô hình phân quyền dựa trên vai trò. Người dùng được gán một hoặc nhiều vai trò, mỗi vai trò có tập quyền tương ứng. OmniBizAI dùng RBAC để kiểm soát chức năng được phép truy cập và kết hợp data scope để kiểm soát dữ liệu được phép xem/sửa.


Các vai trò chính gồm Admin, Director, Manager, Accountant, HR và Staff. Ngoài quyền chức năng, hệ thống cần kiểm soát phạm vi dữ liệu:


- Staff chỉ thao tác dữ liệu của bản thân hoặc đề nghị do mình tạo.

- Manager xem dữ liệu thuộc phòng ban phụ trách.

- Director xem dữ liệu cấp toàn công ty.

- Accountant thao tác dữ liệu tài chính theo quyền nghiệp vụ.

- Admin quản trị hệ thống nhưng vẫn phải ghi audit với hành động nhạy cảm.


#### 17.3.5 AI Assistant


AI Assistant trong đề tài không được xem là chatbot tự do, mà là thành phần hỗ trợ phân tích nghiệp vụ theo vai trò. AI chỉ nhận dữ liệu đã được tổng hợp, lọc theo quyền người dùng và có citation đến nguồn dữ liệu liên quan.


Nguyên tắc thiết kế AI Assistant:


- Không gửi secret, password, token hoặc dữ liệu ngoài quyền vào prompt.

- Không để AI tự quyết định duyệt/từ chối nghiệp vụ.

- Câu trả lời về số liệu phải có citation hoặc fallback rõ ràng.

- Mọi yêu cầu AI cần được ghi audit để phục vụ truy vết.

- Khi provider lỗi hoặc timeout, hệ thống trả fallback có kiểm soát.


#### 17.3.6 Modular MVC Monolith


Modular MVC Monolith là cách tổ chức một ứng dụng ASP.NET Core MVC trong một project duy nhất nhưng chia rõ trách nhiệm theo module nghiệp vụ. Cách này phù hợp với đồ án SME vì giảm chi phí thiết lập, dễ debug, dễ demo, nhưng vẫn giữ nguyên tắc controller mỏng và service chứa business rule.


Áp dụng trong OmniBizAI:


| Khu vực | Vai trò |

|---|---|

| Controllers | Nhận request MVC, gọi service, trả View/Redirect/JSON phụ trợ |

| Services | Use case nghiệp vụ, validation, authorization/data scope, transaction boundary |

| Models/Entities | Entity, enum, state/invariant nghiệp vụ |

| Data | EF Core DbContext, configuration, migration, seed |

| Views/ViewModels | Razor View và model hiển thị/input cho UI |


Quy tắc chính: Controller không chứa business rule phức tạp; Razor không truy vấn EF trực tiếp; Gemini, file storage, notification và export đều đi qua service; route MVC là giao diện chính, JSON chỉ phụ trợ cho UI.


#### 17.3.7 Lý do lựa chọn công nghệ


Phần này dùng cho báo cáo/hội đồng để giải thích vì sao chọn công nghệ, thay vì chỉ liệt kê stack.


| Quyết định | So sánh | Lý do chọn |

|---|---|---|

| ASP.NET Core MVC thay vì Node.js/Express | Node.js linh hoạt và phổ biến cho API realtime, nhưng cần tự ghép nhiều thư viện cho MVC, Identity, validation, EF tương đương | ASP.NET Core MVC có template chuẩn, Identity, Razor, EF Core và tooling tốt, phù hợp nhóm cần làm web app server-rendered ổn định |

| Modular MVC Monolith thay vì microservices | Microservices hỗ trợ scale độc lập nhưng tăng độ phức tạp deploy, network, observability và transaction | MVP SME chỉ cần một app, một database; monolith modular giúp dễ debug, dễ demo, vẫn tách module rõ |

| SQL Server thay vì PostgreSQL | PostgreSQL mạnh, open-source, JSON tốt; SQL Server tích hợp tốt với .NET/EF Core và LocalDB | Nhóm dùng .NET MVC, EF Core, Identity scaffold và môi trường Windows nên SQL Server giảm rủi ro setup/demo |

| Gemini thay vì OpenAI/provider khác | OpenAI và các provider khác đều có thể dùng qua abstraction; chất lượng và chi phí thay đổi theo thời điểm | MVP chọn Gemini vì đã được chốt trong phạm vi đề tài/khả năng truy cập demo; hệ thống dùng `IAIProvider` để có thể thay provider nếu cần |

| Razor Views thay vì SPA React/Vue | SPA mạnh cho UX phức tạp nhưng cần API layer và frontend build pipeline riêng | Razor Views đủ cho dashboard/form/report của MVP, giảm overhead FE-BE và khớp `dotnet new mvc` |

| ECharts thay vì chart tự viết | Tự viết chart mất thời gian và dễ lỗi responsive | ECharts đủ mạnh cho dashboard tài chính/KPI, dễ nhúng vào Razor bằng JavaScript |


Kết luận học thuật: các lựa chọn công nghệ ưu tiên tính phù hợp với phạm vi, khả năng triển khai đúng hạn, dễ kiểm thử và dễ bảo vệ hơn là tối ưu enterprise-scale ngay từ đầu.


#### 17.3.8 Nguồn tham khảo học thuật/kỹ thuật đề xuất


Khi viết báo cáo chính thức, nhóm cần chuyển các nguồn dưới đây sang chuẩn trích dẫn của trường. Không để mục này ở dạng liệt kê thô trong bản nộp cuối.


| Chủ đề | Nguồn tham khảo đề xuất |

|---|---|

| Software Engineering, requirement, testing | Sommerville - Software Engineering; Pressman - Software Engineering: A Practitioner's Approach |

| UML/use case/class/activity/sequence diagram | OMG UML specification hoặc giáo trình phân tích thiết kế hệ thống của trường |

| RBAC | NIST RBAC model, tài liệu role-based access control |

| MVC và ASP.NET Core | Microsoft ASP.NET Core MVC, Identity, EF Core documentation |

| Monolith vs Microservices | Martin Fowler articles về microservices và monolith-first; tài liệu kiến trúc phần mềm hiện đại |

| Workflow/BPM | Tài liệu Business Process Management/workflow systems |

| BI/dashboard | Tài liệu Business Intelligence và dashboard design |

| AI decision support | Tài liệu decision support systems, responsible AI, prompt/citation/fallback practices |


Trong phần báo cáo, mỗi nguồn nên được dùng để giải thích một lựa chọn cụ thể, ví dụ: RBAC cho phân quyền, MVC cho cấu trúc web app, BPM cho workflow, BI cho dashboard, responsible AI cho AI advisory.


### 17.4 Phân tích yêu cầu theo format học thuật


#### 17.4.1 Tác nhân hệ thống


| Actor | Mô tả | Nhu cầu chính |

|---|---|---|

| Admin | Quản trị hệ thống | Quản lý user, role, permission, audit, cấu hình |

| Director | Ban giám đốc | Xem dashboard toàn công ty, duyệt khoản lớn, dùng AI phân tích |

| Manager | Trưởng phòng | Theo dõi ngân sách/KPI phòng ban, duyệt đề nghị của nhân viên |

| Accountant | Kế toán | Quản lý ngân sách, giao dịch, vendor, đối soát và báo cáo tài chính |

| HR | Nhân sự | Quản lý nhân viên, phòng ban, chức vụ và hỗ trợ KPI |

| Staff | Nhân viên | Tạo đề nghị thanh toán, check-in KPI, xem thông báo |


#### 17.4.2 Yêu cầu chức năng


| Mã | Tên yêu cầu | Mô tả học thuật | Actor chính | Mức ưu tiên | Tiêu chí chấp nhận |

|---|---|---|---|---|---|

| FR-01 | Xác thực người dùng | Hệ thống cho phép người dùng đăng nhập, đăng xuất, khóa tài khoản khi đăng nhập sai nhiều lần | Tất cả | Cao | Người dùng hợp lệ đăng nhập được; tài khoản sai bị từ chối; sự kiện đăng nhập được audit |

| FR-02 | Phân quyền theo vai trò | Hệ thống kiểm soát quyền truy cập theo role và permission | Admin | Cao | Người dùng không có quyền truy cập route/action nhạy cảm nhận 403 |

| FR-03 | Quản lý tổ chức | Hệ thống quản lý phòng ban, nhân viên, chức vụ và liên kết user-employee | HR/Admin | Cao | HR tạo/sửa nhân viên; lịch sử thay đổi được lưu |

| FR-04 | Quản lý ngân sách | Hệ thống cho phép tạo ngân sách theo kỳ, phòng ban, danh mục và theo dõi tỷ lệ sử dụng | Accountant/Director | Cao | Khi chi vượt ngưỡng, dashboard hiển thị cảnh báo |

| FR-05 | Tạo đề nghị thanh toán | Nhân viên tạo đề nghị thanh toán kèm danh mục, số tiền, mô tả và file đính kèm | Staff | Cao | PR ở trạng thái Draft/Submitted đúng state machine |

| FR-06 | Duyệt đề nghị thanh toán | Hệ thống định tuyến PR qua các bước duyệt theo workflow động | Manager/Director/Accountant | Cao | Người duyệt hợp lệ approve/reject/request change; action được audit |

| FR-07 | Ghi nhận giao dịch | Kế toán tạo giao dịch sau khi PR được duyệt đầy đủ | Accountant | Cao | Giao dịch làm tăng actual spend và cập nhật ngân sách |

| FR-08 | Quản lý KPI/OKR | Hệ thống cho phép tạo objective, key result, KPI và kỳ đánh giá | Manager/HR/Director | Cao | KPI có owner, target, actual, progress và status |

| FR-09 | Check-in KPI | Nhân viên cập nhật tiến độ KPI và gửi duyệt | Staff/Manager | Trung bình | Check-in được lưu, có trạng thái chờ duyệt và lịch sử |

| FR-10 | Dashboard theo vai trò | Hệ thống hiển thị dashboard phù hợp với role và data scope | Tất cả | Cao | Director thấy toàn công ty; Manager chỉ thấy phòng ban; Staff thấy dữ liệu cá nhân |

| FR-11 | AI Assistant | Hệ thống cho phép hỏi AI về rủi ro, KPI, ngân sách và gợi ý hành động | Director/Manager/Staff | Cao | Câu trả lời có citation/fallback; không lộ dữ liệu ngoài quyền |

| FR-12 | Market Intelligence | Hệ thống quản lý tín hiệu thị trường curated phục vụ phân tích | Admin/Director | Trung bình | Market note có nguồn, thời gian, tag và liên kết phân tích |

| FR-13 | Notification | Hệ thống gửi thông báo khi có approval, KPI, cảnh báo hoặc report | Tất cả | Trung bình | Người nhận thấy unread count và notification realtime nếu SignalR hoạt động |

| FR-14 | Báo cáo và xuất file | Hệ thống xuất báo cáo tài chính/KPI/AI summary ra PDF/XLSX | Director/Accountant/Manager | Trung bình | File có header, filter, người tạo, thời gian tạo và audit export |

| FR-15 | Audit log | Hệ thống ghi nhận hành động quan trọng để phục vụ truy vết | Admin | Cao | Audit có user, event, entity, thời gian, IP, dữ liệu đã mask |


#### 17.4.3 Yêu cầu phi chức năng


| Mã | Nhóm yêu cầu | Mô tả học thuật | Chỉ số đo | Phương pháp kiểm chứng |

|---|---|---|---|---|

| NFR-01 | Hiệu năng | Dashboard và JSON phụ trợ phổ biến phải phản hồi đủ nhanh với dữ liệu seed SME | JSON phụ trợ p95 < 500ms, page load < 3s | k6, Playwright performance test |

| NFR-02 | Khả năng chịu tải | Hệ thống đáp ứng phiên demo với nhiều người dùng đồng thời | 50 concurrent users, fail rate < 1% | k6 constant-vus |

| NFR-03 | Bảo mật | MVC action mutation phải có authorization, anti-forgery và kiểm tra data scope | 100% action nhạy cảm có policy | Unit/integration authorization test |

| NFR-04 | Bảo mật AI | AI không được nhận secret hoặc dữ liệu ngoài quyền | Prompt không chứa field nhạy cảm | Prompt inspection test, audit review |

| NFR-05 | Tính tin cậy | Lỗi AI/SignalR/export không làm hỏng giao dịch nghiệp vụ chính | Có fallback/transaction boundary | Integration test và chaos case thủ công |

| NFR-06 | Khả dụng | Người dùng vẫn thao tác nghiệp vụ cốt lõi khi AI provider không sẵn sàng | Core flow không phụ thuộc AI | E2E test với AI mock/fallback |

| NFR-07 | Dễ bảo trì | Mã nguồn phải tách layer và module rõ ràng | Không vi phạm dependency rule | Architecture test/code review |

| NFR-08 | Khả năng mở rộng SME | Có thể bổ sung module/report/workflow mới mà không phá module cũ | Module mới thêm theo interface | Review thiết kế và integration test |

| NFR-09 | Truy vết | Hành động quan trọng phải có audit log | 100% event trong audit catalog được ghi | Integration test audit |

| NFR-10 | Dễ sử dụng | Giao diện phải phù hợp người dùng không chuyên | Flow chính không quá nhiều bước, form có validation rõ | Manual UX checklist, screenshot evidence |


#### 17.4.4 Ràng buộc và giả định


| Loại | Nội dung |

|---|---|

| Ràng buộc công nghệ | ASP.NET Core MVC, SQL Server, EF Core, Bootstrap, ECharts, Gemini |

| Ràng buộc dữ liệu | MVP dùng một công ty, 7 phòng ban, 45 nhân sự active và seed data có tình huống nghiệp vụ |

| Ràng buộc bảo mật | Không đưa secret thật vào tài liệu, repository hoặc prompt AI |

| Ràng buộc phạm vi | Không triển khai mobile native, payroll đầy đủ, banking integration hoặc multi-tenant phức tạp |

| Giả định nghiệp vụ | SME có quy trình duyệt chi theo cấp quản lý và cần báo cáo tháng/quý |


### 17.5 Thiết kế hệ thống


Các sơ đồ thiết kế chi tiết đã được chuẩn hóa ở mục 3.2. Khi đưa vào báo cáo học thuật, nên trình bày theo thứ tự từ nghiệp vụ đến kỹ thuật để người đọc không bị nhảy tầng trừu tượng.


| Loại thiết kế | Vị trí sơ đồ trong tài liệu | Nội dung trình bày trong báo cáo |

|---|---|---|

| Use case diagram | Mục 3.2.2 | Actor và chức năng chính của từng vai trò |

| Activity diagram | Mục 3.2.3 | Luồng tạo, phân tích rủi ro và duyệt đề nghị thanh toán |

| Sequence diagram - Payment Request | Mục 3.2.10 | Tương tác giữa user, MVC, application service, workflow, AI risk và database |

| Flowchart - AI Assistant | Mục 3.2.12 | Luồng lấy context, gọi provider, kiểm tra citation và trả fallback |

| Sequence diagram - KPI check-in | Mục 3.2.13 | Tương tác giữa Staff, Web, KPI service, Workflow, Manager, AI và database |

| Class diagram | Mục 3.2.7 | Entity/service/interface cốt lõi của Finance, Workflow, KPI, AI và Audit |

| ERD | Mục 3.2.9 | Quan hệ bảng theo module: Identity, Organization, Finance, Workflow, KPI, AI, Report, Audit |

| Deployment diagram | Mục 3.2.14 | Thành phần chạy khi demo/triển khai MVP |


#### 17.5.1 Use case


Use case của hệ thống tập trung vào sáu actor: Admin, Director, Manager, Accountant, HR và Staff. Các chức năng có tính liên module như dashboard, notification, audit và AI Assistant được thiết kế theo vai trò để đảm bảo mỗi actor chỉ nhìn thấy dữ liệu cần thiết.


Khi đưa vào báo cáo, phần mô tả use case nên gồm:


- Tên use case.

- Actor chính/phụ.

- Tiền điều kiện.

- Luồng chính.

- Luồng thay thế/lỗi.

- Hậu điều kiện.

- Liên kết với yêu cầu chức năng.


#### 17.5.2 Activity


Activity diagram quan trọng nhất là luồng đề nghị thanh toán. Luồng này thể hiện rõ giá trị của đề tài vì kết hợp nhiều thành phần: nhập form, validate, AI risk advisory, chọn workflow bằng rule deterministic, duyệt nhiều cấp, ghi transaction, cập nhật ngân sách, gửi notification và audit.


#### 17.5.3 Sequence


Sequence diagram dùng để chứng minh thiết kế runtime không đặt business rule ở View/Controller. Controller chỉ nhận request và gọi Application Service; Application Service điều phối Domain rule, Repository, Workflow Service, Notification Service, AI Service và Audit Service.


#### 17.5.4 Class


Class diagram thể hiện các entity và service cốt lõi. Khi bảo vệ đồ án, nên nhấn mạnh rằng entity nghiệp vụ không phụ thuộc trực tiếp vào UI hoặc provider bên ngoài; các tích hợp như Gemini, Redis, file storage, PDF export nằm ở Infrastructure thông qua interface.


#### 17.5.5 ERD


ERD được chia theo nhóm bảng để dễ đọc:


- Identity/RBAC: user, role, permission, user_role.

- Organization: department, position, employee.

- Finance: budget, payment_request, transaction, vendor, wallet/category.

- Workflow: workflow_template, workflow_step, workflow_instance, approval_action.

- KPI/OKR: evaluation_period, objective, key_result, kpi, kpi_check_in.

- AI/Market: ai_conversation, ai_message, ai_recommendation, market_insight.

- Audit/Report/System: audit_log, notification, report_export, system_setting.


### 17.6 Kết quả thực nghiệm và bằng chứng đánh giá


Phần này chỉ được hoàn tất sau khi có ứng dụng chạy thật. Không sử dụng ảnh mockup hoặc số liệu ước lượng làm kết quả thực nghiệm. Nếu chưa có evidence, ghi rõ trạng thái "Chưa thực hiện" và kế hoạch thu thập.


#### 17.6.1 Ảnh giao diện thật cần chèn


| Mã ảnh | Màn hình | Mục đích minh chứng | Đường dẫn evidence đề xuất |

|---|---|---|---|

| UI-01 | Đăng nhập | Hệ thống có xác thực người dùng | `docs/test-evidence/screenshots/ui-01-login.png` |

| UI-02 | Director dashboard | Dashboard điều hành, biểu đồ, cảnh báo tổng quan | `docs/test-evidence/screenshots/ui-02-director-dashboard.png` |

| UI-03 | Manager dashboard | Data scope theo phòng ban | `docs/test-evidence/screenshots/ui-03-manager-dashboard.png` |

| UI-04 | Tạo đề nghị thanh toán | Form nghiệp vụ, validation, file đính kèm | `docs/test-evidence/screenshots/ui-04-create-payment-request.png` |

| UI-05 | AI Risk Analysis | AI hỗ trợ đánh giá rủi ro trước khi submit | `docs/test-evidence/screenshots/ui-05-ai-risk.png` |

| UI-06 | Duyệt đề nghị thanh toán | Workflow approve/reject/request change | `docs/test-evidence/screenshots/ui-06-approval.png` |

| UI-07 | Ngân sách | Budget utilization và cảnh báo vượt ngưỡng | `docs/test-evidence/screenshots/ui-07-budget.png` |

| UI-08 | KPI/OKR | Objective, key result, KPI progress | `docs/test-evidence/screenshots/ui-08-kpi.png` |

| UI-09 | AI Assistant | Câu trả lời có citation/fallback | `docs/test-evidence/screenshots/ui-09-ai-assistant.png` |

| UI-10 | Báo cáo/export | Xuất PDF/XLSX và audit export | `docs/test-evidence/screenshots/ui-10-report-export.png` |


Yêu cầu khi chụp ảnh:


- Chụp từ ứng dụng chạy thật, có URL môi trường demo hoặc localhost.

- Không hiển thị password, API key, connection string, token hoặc dữ liệu cá nhân nhạy cảm.

- Ảnh phải thể hiện được dữ liệu seed có ý nghĩa: vượt ngân sách, KPI trễ, PR đang chờ duyệt, AI có citation.

- Mỗi ảnh trong báo cáo cần có chú thích: tên màn hình, vai trò đăng nhập, tình huống minh chứng.


#### 17.6.2 Kết quả kiểm thử chức năng


| Nhóm test | Công cụ | Số lượng test | Passed | Failed | Evidence |

|---|---|---:|---:|---:|---|

| Unit test | xUnit + Moq | TBD | TBD | TBD | `docs/test-evidence/unit-test-results.trx` |

| Integration test | xUnit + Testcontainers SQL Server | TBD | TBD | TBD | `docs/test-evidence/integration-test-results.trx` |

| E2E test | Playwright | TBD | TBD | TBD | `docs/test-evidence/playwright-report/` |

| Manual regression | Checklist QA | TBD | TBD | TBD | `docs/test-evidence/manual-regression.xlsx` |


Các luồng bắt buộc có test evidence:


- Đăng nhập và kiểm tra quyền theo role.

- Staff tạo PR, submit, Manager/Director duyệt, Accountant ghi transaction.

- Staff không được xem/sửa PR của người khác ngoài quyền.

- Manager chỉ xem dữ liệu phòng ban của mình.

- KPI check-in được submit và duyệt đúng trạng thái.

- AI Assistant trả citation/fallback và không lộ dữ liệu ngoài quyền.

- Export report tạo file và ghi audit.


#### 17.6.3 Kết quả đo hiệu năng


| Chỉ số | Công cụ | Mục tiêu | Kết quả đo | Trạng thái | Evidence |

|---|---|---:|---:|---|---|

| JSON phụ trợ p95 latency | k6 | < 500ms | TBD | TBD | `docs/test-evidence/k6-mvc-summary.json` |

| MVC/JSON fail rate | k6 | < 1% | TBD | TBD | `docs/test-evidence/k6-mvc-summary.json` |

| Concurrent users | k6 | 50 VUs/3 phút | TBD | TBD | `docs/test-evidence/k6-console.png` |

| AI p95 latency | k6 | < 10s | TBD | TBD | `docs/test-evidence/k6-ai-summary.json` |

| Dashboard page load | Playwright | < 3s | TBD | TBD | `docs/test-evidence/playwright-performance.json` |


Command đo hiệu năng tham chiếu:


```powershell

k6 run --summary-export docs/test-evidence/k6-mvc-summary.json docs/perf/k6-nfr-smoke.js

k6 run -e VUS=50 -e DURATION=3m --summary-export docs/test-evidence/k6-50vus-summary.json docs/perf/k6-nfr-smoke.js

k6 run -e AUTH_COOKIE="..." -e ROUTES="/Dashboard,/PaymentRequests,/Kpis" --summary-export docs/test-evidence/k6-authenticated-summary.json docs/perf/k6-nfr-smoke.js

```


Khi ghi vào báo cáo cuối cùng, không để `TBD`. Nếu test chưa đạt, ghi rõ nguyên nhân, ảnh hưởng và hướng khắc phục.


#### 17.6.4 Mẫu nhận xét kết quả thực nghiệm


Sau khi có số liệu thật, viết nhận xét theo cấu trúc:


1. Môi trường thử nghiệm: máy chạy, database, seed data, số user, thời gian chạy.

2. Kết quả chức năng: số test pass/fail và các luồng đã được xác nhận.

3. Kết quả hiệu năng: p95 latency, fail rate, page load, AI latency.

4. Phân tích: chỉ số nào đạt, chỉ số nào chưa đạt, nguyên nhân dự kiến.

5. Kết luận thực nghiệm: hệ thống có đáp ứng mục tiêu MVP hay không.


#### 17.6.5 Chỉ số đánh giá hiệu quả hệ thống


Ngoài test kỹ thuật, báo cáo cần trả lời hệ thống cải thiện điều gì so với cách làm thủ công. Các chỉ số dưới đây có thể đo bằng demo scenario, seed data và khảo sát nhóm người dùng thử.


| Nhóm đánh giá | Chỉ số | Cách đo | Kỳ vọng MVP |

|---|---|---|---|

| Quy trình duyệt chi | Số bước thao tác để tạo và submit PR | So sánh checklist thao tác trước/sau | Luồng chính hoàn tất trong một form + một action submit |

| Minh bạch phê duyệt | Tỷ lệ PR có timeline và audit đầy đủ | Kiểm tra payment request sample | 100% PR submitted có workflow/audit |

| Kiểm soát ngân sách | Tỷ lệ PR rủi ro được cảnh báo trước khi duyệt | Seed PR vượt ngưỡng, kiểm dashboard/AI risk | PR lớn/vượt 80% được flag và route 3 cấp |

| Data scope | Số case truy cập ngoài quyền bị chặn | Test Staff/Manager/Director | 100% case nhạy cảm bị 403 hoặc không xuất hiện |

| Báo cáo | Thời gian tạo báo cáo tài chính/KPI | Thao tác demo và export file | Xuất được PDF/XLSX trong phạm vi dữ liệu seed |

| AI hỗ trợ quyết định | Tỷ lệ câu trả lời có citation/fallback hợp lệ | Test câu hỏi finance/KPI/market | Không trả số liệu không citation; lỗi provider có fallback |

| Hiệu năng | Page load dashboard và k6 smoke | Playwright/k6 | Page load < 3s, fail rate < 1% |


Nếu chưa có số đo thật khi viết nháp báo cáo, ghi `Chưa thực hiện` và mô tả kế hoạch đo; không tự bịa số liệu.


### 17.7 Đánh giá hệ thống


#### 17.7.1 Ưu điểm


- Hệ thống tích hợp nhiều phân hệ vận hành quan trọng của SME trong một kiến trúc thống nhất.

- Modular MVC Monolith giúp tách business rule khỏi UI trong khi vẫn đơn giản, thuận lợi cho nhóm triển khai và demo.

- Workflow động giúp mô phỏng quy trình duyệt thực tế, có thể mở rộng theo điều kiện nghiệp vụ.

- RBAC kết hợp data scope giúp kiểm soát cả quyền chức năng và quyền dữ liệu.

- AI Assistant được thiết kế có kiểm soát: theo vai trò, có citation, fallback và audit.

- Dashboard/report hỗ trợ lãnh đạo theo dõi ngân sách, KPI và rủi ro nhanh hơn thao tác thủ công.

- Có kế hoạch kiểm thử chức năng, bảo mật, hiệu năng và bằng chứng thực nghiệm rõ ràng.


#### 17.7.2 Hạn chế


- MVP chỉ hỗ trợ một công ty, chưa có multi-tenant SaaS đầy đủ.

- Dữ liệu thị trường là curated/manual, chưa tích hợp nguồn dữ liệu thị trường realtime.

- AI phụ thuộc provider bên ngoài nên cần fallback khi timeout, quota hoặc lỗi mạng.

- Chưa có mobile app native; trải nghiệm di động dựa trên responsive web.

- Payroll, inventory, CRM nâng cao và tích hợp ngân hàng thật nằm ngoài phạm vi.

- Một số báo cáo nâng cao cần dữ liệu lịch sử dài hơn để đánh giá xu hướng chính xác.


#### 17.7.3 Hướng phát triển


| Hướng phát triển | Mô tả |

|---|---|

| Multi-tenant | Hỗ trợ nhiều công ty, phân vùng dữ liệu và cấu hình riêng theo tenant |

| Workflow designer UI | Cho phép Admin thiết kế workflow bằng giao diện kéo thả hoặc form cấu hình |

| AI recommendation lifecycle | Theo dõi đề xuất AI từ lúc tạo đến khi được xử lý, bỏ qua hoặc chuyển thành task |

| RAG nâng cao | Dùng vector search cho tài liệu nội bộ, chính sách, quy định và tri thức doanh nghiệp |

| Tích hợp ngân hàng/kế toán | Đồng bộ giao dịch, hóa đơn và đối soát tự động |

| Mobile/PWA | Tối ưu duyệt đề nghị, check-in KPI và nhận thông báo trên thiết bị di động |

| Advanced BI | Thêm drill-down, forecast, anomaly detection và dashboard tùy biến |

| Security hardening | Bổ sung 2FA, secret rotation, rate limit, security headers và audit dashboard nâng cao |


### 17.8 Kết luận


Đề tài OmniBizAI đã xác định được bài toán quản trị vận hành phổ biến trong doanh nghiệp vừa và nhỏ: dữ liệu phân tán, quy trình phê duyệt thiếu minh bạch, khó kiểm soát ngân sách, KPI chưa được theo dõi liên tục và báo cáo còn thủ công. Trên cơ sở đó, đề tài đề xuất một hệ thống tích hợp các phân hệ Finance, Workflow, KPI/OKR, Dashboard, Report, RBAC và AI Assistant trong mô hình Modular MVC Monolith bằng ASP.NET Core MVC.


Về mặt thiết kế, hệ thống có đầy đủ actor, use case, activity, sequence, class diagram và ERD để mô tả từ góc nhìn nghiệp vụ đến kỹ thuật. Về mặt triển khai, tài liệu đã chốt công nghệ, kiến trúc, database blueprint, route map MVC, seed data, phân quyền, test strategy và tiêu chí nghiệm thu phi chức năng. Về mặt đánh giá, đề tài yêu cầu bằng chứng thực nghiệm gồm ảnh giao diện thật, kết quả test và số liệu hiệu năng thay vì chỉ mô tả lý thuyết.


Kết quả kỳ vọng của đề tài là một MVP có thể demo end-to-end: người dùng đăng nhập theo vai trò, tạo và duyệt đề nghị thanh toán, theo dõi ngân sách/KPI, nhận cảnh báo, hỏi AI có citation và xuất báo cáo. Dù còn các hạn chế về phạm vi như chưa có multi-tenant, chưa tích hợp dữ liệu thị trường realtime và chưa có mobile native, hệ thống vẫn đáp ứng mục tiêu nghiên cứu ban đầu và tạo nền tảng rõ ràng để mở rộng trong các giai đoạn tiếp theo.
