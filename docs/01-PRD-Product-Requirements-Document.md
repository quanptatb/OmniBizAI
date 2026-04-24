# 📋 OmniBiz AI — Product Requirements Document (PRD)

> **Version**: 1.0  
> **Last Updated**: 2026-04-24  
> **Owner**: Product Team  
> **Status**: Approved for Development  

---

## 1. Executive Summary

**OmniBiz AI** là một hệ thống vận hành doanh nghiệp thông minh (AI Business Operating System) tích hợp quản lý tài chính, KPI/OKR, nhân sự cơ bản, workflow phê duyệt đa cấp và AI Copilot. Hệ thống nhắm đến doanh nghiệp vừa và nhỏ (SME), giúp chuyển đổi từ quản lý bằng cảm tính sang ra quyết định dựa trên dữ liệu và AI.

**Tagline**: *Intelligent Operating System for SME Decision-Making*

---

## 2. Problem Statement

### 2.1 Thực trạng doanh nghiệp SME

| Vấn đề | Mô tả chi tiết | Hậu quả |
|---------|----------------|---------|
| **Dữ liệu rời rạc** | DN sử dụng Excel, Zalo, Google Sheet, phần mềm kế toán riêng lẻ | Khó tổng hợp, khó audit, chậm ra quyết định |
| **Quy trình thủ công** | Đề nghị chi, duyệt ngân sách, đánh giá KPI qua tin nhắn/giấy tờ | Thất lạc, thiếu minh bạch, không truy vết được |
| **AI chưa gắn nghiệp vụ** | Nhiều hệ thống chỉ "gắn chatbot cho có", không có dữ liệu thật | Không tạo giá trị thực, không hỗ trợ ra quyết định |

### 2.2 Cơ hội

- SME chiếm >97% doanh nghiệp Việt Nam nhưng thiếu công cụ quản trị hiện đại
- AI (LLM) đã đủ trưởng thành để áp dụng vào nghiệp vụ cụ thể
- Xu hướng all-in-one platform thay vì mua nhiều phần mềm rời rạc

---

## 3. Product Vision

> **"Mọi doanh nghiệp nhỏ đều có thể ra quyết định như tập đoàn lớn — nhờ dữ liệu thống nhất và AI trợ lý."**

### 3.1 Core Value Proposition

```
┌─────────────────────────────────────────────────────────────────┐
│  Data Unification  →  Smart Workflow  →  AI-Powered Insight    │
│  (Thu thập)           (Xử lý)           (Ra quyết định)        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Target Users & Personas

### 4.1 Persona Matrix

| Persona | Vai trò | Nhu cầu chính | Tần suất sử dụng | Giá trị nhận được |
|---------|---------|---------------|-------------------|-------------------|
| **Director / CEO** | Giám đốc, Chủ DN | Nhìn toàn cảnh tài chính, KPI, rủi ro | Hàng ngày | Dashboard tổng quan, AI insight, dự báo |
| **Manager** | Trưởng phòng ban | Quản lý team, budget phòng, duyệt | Hàng ngày | Dashboard phòng ban, quản lý KPI team |
| **Accountant** | Kế toán | Quản lý thu/chi, giao dịch, báo cáo | Hàng ngày | Quản lý tài chính, xuất báo cáo |
| **HR Admin** | Nhân sự | Quản lý nhân viên, phân quyền | Hàng tuần | Quản lý org chart, employee data |
| **Staff / Employee** | Nhân viên | Tạo đề nghị chi, check-in KPI | Hàng ngày | Self-service, theo dõi tiến độ |
| **System Admin** | Quản trị hệ thống | Cấu hình hệ thống, audit | Theo yêu cầu | Quản lý users, roles, audit log |

### 4.2 User Stories ưu tiên cao

#### Director
- Là Director, tôi muốn xem dashboard tổng quan thu/chi/KPI để nắm tình hình công ty trong 30 giây
- Là Director, tôi muốn hỏi AI: "Tháng này phòng nào vượt ngân sách?" để ra quyết định nhanh
- Là Director, tôi muốn nhận cảnh báo rủi ro tự động khi có khoản chi bất thường

#### Manager
- Là Manager, tôi muốn theo dõi tiến độ KPI của team để can thiệp kịp thời
- Là Manager, tôi muốn duyệt đề nghị chi của nhân viên trên hệ thống thay vì qua Zalo
- Là Manager, tôi muốn xem báo cáo hiệu suất phòng ban theo tháng/quý

#### Accountant
- Là Accountant, tôi muốn quản lý ngân sách theo phòng ban/danh mục để kiểm soát chi phí
- Là Accountant, tôi muốn ghi nhận giao dịch và đối soát tự động

#### Staff
- Là Staff, tôi muốn tạo đề nghị thanh toán trực tuyến kèm file đính kèm
- Là Staff, tôi muốn check-in tiến độ KPI hàng tuần và xem đánh giá

---

## 5. Product Scope — MVP

### 5.1 Modules trong phạm vi (In-Scope)

| # | Module | Mô tả | Chức năng MVP |
|---|--------|-------|--------------|
| 1 | **Finance** | Quản lý ngân sách, chi phí, dòng tiền | Budget CRUD, Payment Request, Transaction, Wallet, Cashflow Chart |
| 2 | **KPI/OKR** | Theo dõi mục tiêu và hiệu suất | OKR, KPI, Check-in, Duyệt kết quả, Dashboard tiến độ |
| 3 | **HR Basic** | Quản lý nhân sự nền tảng | Employee, Department, Position, Role, Permission |
| 4 | **Workflow Engine** | Chuẩn hóa phê duyệt nội bộ | Template, Approval Step, Condition, Approve/Reject, Audit Log |
| 5 | **AI Copilot** | Trợ lý phân tích dữ liệu DN | Chat Q&A, Insight, Risk Alert, Report Summary |

### 5.2 Ngoài phạm vi MVP (Out-of-Scope)

| Chức năng | Lý do loại bỏ | Giai đoạn dự kiến |
|-----------|---------------|-------------------|
| Payroll (Bảng lương) | Phức tạp, cần tích hợp thuế/BHXH | Phase 2 |
| Inventory Management | Khác domain, mở rộng quá lớn | Phase 3 |
| CRM / Sales Pipeline | Cần module riêng biệt | Phase 2 |
| Multi-tenant SaaS | Cần infrastructure phức tạp | Phase 3 |
| Mobile App (Native) | Web responsive đủ cho MVP | Phase 2 |
| Advanced Reporting (BI) | Power BI / Metabase integration | Phase 2 |
| E-Invoice Integration | Cần API nhà cung cấp hóa đơn điện tử | Phase 2 |
| Multi-language (i18n) | Ưu tiên tiếng Việt cho MVP | Phase 2 |

### 5.3 Constraints

- **Team size**: 7 người
- **Timeline**: 12 tuần (MVP)
- **Budget**: Miễn phí / tier miễn phí của các dịch vụ cloud
- **Tech stack**: ASP.NET Core (Backend), Next.js + React (Frontend), SQL Server/SQL Server, Redis
- **AI Provider**: LLM API (Groq/OpenAI/Claude) — sử dụng free/low-cost tier

---

## 6. Feature Requirements — Chi tiết

### 6.1 Dashboard Điều hành

| Feature ID | Tên | Mô tả | Priority | Acceptance Criteria |
|-----------|-----|-------|----------|-------------------|
| DASH-001 | Tổng quan tài chính | Thu/chi, ngân sách còn lại | P0 | Hiển thị real-time, cập nhật khi có transaction mới |
| DASH-002 | KPI completion rate | % KPI hoàn thành theo phòng ban | P0 | Tính đúng theo công thức weighted average |
| DASH-003 | Biểu đồ chi phí | Chart theo phòng ban, danh mục, thời gian | P0 | Hỗ trợ filter theo period, department |
| DASH-004 | Top rủi ro | Khoản chi bất thường, KPI tụt, vượt budget | P1 | AI phân tích và highlight top 5 risks |
| DASH-005 | Role-based view | Director = toàn công ty, Manager = phòng ban | P0 | Data scoping chính xác theo role |
| DASH-006 | Quick actions | Approve pending requests từ dashboard | P1 | Click để chuyển đến approval screen |

### 6.2 Quản lý Ngân sách & Đề nghị chi

| Feature ID | Tên | Priority | Acceptance Criteria |
|-----------|-----|----------|-------------------|
| FIN-001 | Budget CRUD | P0 | Tạo/sửa/xóa ngân sách theo kỳ, phòng ban, danh mục |
| FIN-002 | Payment Request | P0 | Tạo đề nghị thanh toán kèm line items, NCC, file đính kèm |
| FIN-003 | Multi-level Approval | P0 | Duyệt chi nhiều cấp theo workflow rule |
| FIN-004 | AI Risk Check | P1 | AI kiểm tra rủi ro trước khi gửi duyệt (vượt budget, trùng NCC) |
| FIN-005 | Transaction Recording | P0 | Ghi nhận thu/chi thực tế, liên kết với budget |
| FIN-006 | Cashflow Chart | P1 | Biểu đồ dòng tiền theo thời gian |
| FIN-007 | Budget Utilization | P0 | % sử dụng ngân sách real-time |
| FIN-008 | Vendor Management | P1 | Quản lý nhà cung cấp, lịch sử giao dịch |

### 6.3 KPI/OKR & Đánh giá Hiệu suất

| Feature ID | Tên | Priority | Acceptance Criteria |
|-----------|-----|----------|-------------------|
| KPI-001 | OKR Management | P0 | Tạo/sửa Objective, Key Result theo kỳ |
| KPI-002 | KPI Assignment | P0 | Phân bổ KPI cho phòng ban hoặc nhân viên |
| KPI-003 | Check-in System | P0 | Check-in tiến độ định kỳ (tuần/tháng) |
| KPI-004 | Check-in Approval | P0 | Manager duyệt check-in của nhân viên |
| KPI-005 | Auto Scoring | P1 | Tự tính điểm dựa trên formula |
| KPI-006 | Performance Rating | P1 | Xếp loại A/B/C/D dựa trên điểm |
| KPI-007 | Risk Warning | P1 | Cảnh báo KPI có nguy cơ fail |
| KPI-008 | Progress Dashboard | P0 | Dashboard tiến độ theo phòng ban/cá nhân |

### 6.4 AI Copilot

| Feature ID | Tên | Priority | Acceptance Criteria |
|-----------|-----|----------|-------------------|
| AI-001 | Natural Language Q&A | P0 | Hỏi đáp tiếng Việt, trả lời từ data nội bộ |
| AI-002 | Report Summary | P1 | Tạo báo cáo tóm tắt tháng/quý tự động |
| AI-003 | Action Suggestions | P1 | Gợi ý: cắt chi, tăng budget, điều chỉnh KPI |
| AI-004 | RAG + Citation | P1 | Trả lời có trích dẫn nguồn dữ liệu |
| AI-005 | Spend Guardrail | P0 | Phân tích rủi ro khoản chi trước khi duyệt |
| AI-006 | KPI Insight | P1 | Phát hiện KPI tụt, phòng ban chậm, NV cần hỗ trợ |
| AI-007 | Forecast | P2 | Dự báo cash runway, trend hiệu suất |

### 6.5 Workflow & Audit

| Feature ID | Tên | Priority | Acceptance Criteria |
|-----------|-----|----------|-------------------|
| WF-001 | Workflow Template | P0 | Thiết lập luồng duyệt theo loại nghiệp vụ |
| WF-002 | Approval Rules | P0 | Điều kiện: số tiền, % budget, vai trò, phòng ban |
| WF-003 | Approval Instance | P0 | Tạo instance khi submit request |
| WF-004 | Approve/Reject | P0 | Duyệt/từ chối kèm comment |
| WF-005 | Audit Log | P0 | Lưu log đăng nhập, CRUD, duyệt, xuất báo cáo |
| WF-006 | Notification | P1 | Thông báo realtime khi có request cần duyệt |

### 6.6 Bảo mật & Phân quyền

| Feature ID | Tên | Priority | Acceptance Criteria |
|-----------|-----|----------|-------------------|
| SEC-001 | Authentication | P0 | Email/password login |
| SEC-002 | OAuth (Optional) | P2 | Google OAuth login |
| SEC-003 | RBAC | P0 | Admin, Director, Manager, Accountant, HR, Staff |
| SEC-004 | Permission-level | P0 | Phân quyền từng action (create, read, update, delete) |
| SEC-005 | Data Scoping | P0 | User chỉ thấy dữ liệu trong phạm vi được phép |

---

## 7. Success Criteria

### 7.1 Technical KPIs

| Metric | Target | Đo bằng |
|--------|--------|---------|
| API Response Time (P95) | < 500ms | Application monitoring |
| Page Load Time | < 3s | Lighthouse |
| Uptime | > 99% (demo period) | Health check |
| Test Coverage | > 70% | Code coverage tool |
| Zero critical bugs | 0 P0 bugs at demo | Bug tracker |

### 7.2 Product KPIs (Demo Evaluation)

| Metric | Target | Đo bằng |
|--------|--------|---------|
| Demo scenario completion | 100% (5/5 flows) | Demo script |
| Role-based data accuracy | 100% correct scoping | Manual test |
| AI response relevance | > 80% useful answers | Review panel |
| End-to-end workflow | Complete in < 2 min | Timer during demo |
| Dashboard data consistency | Real-time update < 5s | Visual verification |

### 7.3 Academic Evaluation Criteria

| Tiêu chí | Trọng số | Mục tiêu |
|----------|----------|----------|
| Giải quyết bài toán thực tế | 20% | Chứng minh qua demo scenario |
| Chiều sâu kỹ thuật | 25% | RBAC, Workflow, AI, Analytics, Audit |
| Chất lượng code & architecture | 20% | Clean Architecture, CI/CD, Testing |
| AI integration | 20% | RAG, Risk Analysis, Report Generation |
| Demo & Presentation | 15% | Kịch bản rõ ràng, dữ liệu thực tế |

---

## 8. Assumptions & Dependencies

### 8.1 Assumptions

1. Team 7 người có kỹ năng .NET Core và React/Next.js cơ bản
2. LLM API (Groq/OpenAI) available với free/low-cost tier
3. Không cần tích hợp hệ thống bên ngoài thực tế (bank, tax, etc.)
4. Demo data đủ realistic để thuyết phục hội đồng
5. Internet access ổn định cho AI API calls

### 8.2 Dependencies

| Dependency | Type | Risk | Mitigation |
|-----------|------|------|-----------|
| LLM API availability | External | Medium | Fallback to multiple providers |
| SQL Server hosting | Infrastructure | Low | Docker local + cloud backup |
| Domain expertise (Finance, HR) | Knowledge | Medium | Research + advisor consultation |
| .NET SDK compatibility | Technical | Low | Lock version in Dockerfile |

---

## 9. Release Strategy

### 9.1 MVP Release (Week 12)

- Full 5 modules functional
- Demo-ready with seed data
- Deployed on cloud (Azure/Render/VPS)
- Documentation complete

### 9.2 Future Roadmap (Post-graduation)

| Phase | Timeline | Features |
|-------|----------|----------|
| Phase 2 | +3 months | CRM, Payroll, Mobile App, E-Invoice |
| Phase 3 | +6 months | Multi-tenant SaaS, BI Integration, Inventory |
| Phase 4 | +12 months | Marketplace, Plugin System, Advanced AI |
