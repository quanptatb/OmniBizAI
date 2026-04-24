# 🎨 OmniBiz AI — UI/UX Design Document

> **Version**: 1.0 | **Updated**: 2026-04-24

---

## 1. Design System

### 1.1 Color Palette

| Token | Light Mode | Dark Mode | Usage |
|-------|-----------|-----------|-------|
| `--bg-primary` | #FFFFFF | #07111F | Page background |
| `--bg-secondary` | #F8FAFC | #0D1B2E | Card background |
| `--bg-tertiary` | #F1F5F9 | #10243D | Sidebar, input bg |
| `--text-primary` | #0F172A | #EAF4FF | Main text |
| `--text-secondary` | #475569 | #9DB3C9 | Muted text |
| `--accent-blue` | #2563EB | #38BDF8 | Primary actions, links |
| `--accent-green` | #16A34A | #34D399 | Success, positive values |
| `--accent-orange` | #EA580C | #FB923C | Warning, medium risk |
| `--accent-red` | #DC2626 | #F87171 | Error, high risk |
| `--accent-violet` | #7C3AED | #A78BFA | AI features, special |
| `--accent-yellow` | #CA8A04 | #FACC15 | Caution, pending |
| `--border` | #E2E8F0 | #21415F | Borders, dividers |

### 1.2 Typography

| Element | Font | Weight | Size | Line Height |
|---------|------|--------|------|-------------|
| H1 (Page title) | Inter | 800 | 28-32px | 1.2 |
| H2 (Section) | Inter | 700 | 22-24px | 1.3 |
| H3 (Card title) | Inter | 600 | 16-18px | 1.4 |
| Body | Inter | 400 | 14px | 1.65 |
| Small/Caption | Inter | 500 | 12px | 1.5 |
| Metric (Dashboard) | Inter | 900 | 32-48px | 1.0 |
| Monospace (Code) | JetBrains Mono | 400 | 13px | 1.5 |

### 1.3 Spacing Scale

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | Icon gaps |
| sm | 8px | Compact padding |
| md | 12px | Card padding, form gaps |
| lg | 16px | Section padding |
| xl | 24px | Page padding |
| 2xl | 32px | Section margins |
| 3xl | 48px | Major section separators |

### 1.4 Border Radius

| Size | Value | Usage |
|------|-------|-------|
| sm | 6px | Buttons, inputs |
| md | 10px | Cards, dropdowns |
| lg | 16px | Modals, panels |
| xl | 24px | Feature cards |
| full | 9999px | Tags, badges, avatars |

### 1.5 Shadows

| Level | Value | Usage |
|-------|-------|-------|
| sm | `0 1px 2px rgba(0,0,0,.05)` | Buttons, inputs |
| md | `0 4px 12px rgba(0,0,0,.1)` | Cards, dropdowns |
| lg | `0 12px 40px rgba(0,0,0,.15)` | Modals, panels |
| xl | `0 24px 80px rgba(0,0,0,.2)` | Hero elements |

---

## 2. Component Library

### 2.1 Core Components

| Component | Variants | States |
|-----------|----------|--------|
| **Button** | Primary, Secondary, Ghost, Danger, Icon | Default, Hover, Active, Disabled, Loading |
| **Input** | Text, Number, Date, Select, Textarea, Search | Default, Focus, Error, Disabled |
| **Badge/Tag** | Info, Success, Warning, Danger, Neutral | Static, Removable |
| **Card** | Default, Interactive, Stat, Feature | Default, Hover, Selected |
| **Table** | Default, Compact, Expandable | With sorting, filtering, pagination |
| **Modal** | Small (400px), Medium (600px), Large (800px), Full | With header, body, footer |
| **Toast/Alert** | Info, Success, Warning, Error | Auto-dismiss (5s), Persistent |
| **Avatar** | Sizes: XS(24), SM(32), MD(40), LG(56), XL(80) | Image, Initials, Group |
| **Skeleton** | Text, Card, Table, Chart | Pulse animation |
| **Empty State** | No data, Error, Search no results | With illustration + action |

### 2.2 Chart Components

| Chart Type | Library | Usage |
|-----------|---------|-------|
| Line Chart | Recharts | Cashflow trend, KPI progress over time |
| Bar Chart | Recharts | Budget comparison by department |
| Stacked Bar | Recharts | Income vs Expense by category |
| Donut Chart | Recharts | Budget utilization, KPI distribution |
| Area Chart | Recharts | Revenue trend |
| Progress Ring | Custom SVG | KPI completion rate |
| Gauge | Custom SVG | Overall performance score |
| Sparkline | Recharts | Mini trend in table cells |

---

## 3. Layout Structure

### 3.1 Main Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│  SIDEBAR (260px, collapsible to 72px)    │  MAIN CONTENT            │
│  ┌─────────────────────────────────────┐ │  ┌─────────────────────┐ │
│  │  Logo + Company Name               │ │  │  HEADER (64px)       │ │
│  │  ─────────────────────────          │ │  │  Breadcrumb │ Search │ │
│  │  📊 Dashboard                       │ │  │  │ Notifications    │ │
│  │  ─────── FINANCE ───────            │ │  │  │ Profile Avatar   │ │
│  │  💰 Ngân sách                       │ │  └─────────────────────┘ │
│  │  📋 Đề nghị chi                     │ │  ┌─────────────────────┐ │
│  │  💳 Giao dịch                       │ │  │                     │ │
│  │  🏢 Nhà cung cấp                   │ │  │  PAGE CONTENT       │ │
│  │  ─────── KPI/OKR ──────            │ │  │  (Scrollable)       │ │
│  │  🎯 Mục tiêu (OKR)                 │ │  │                     │ │
│  │  📈 KPI                            │ │  │                     │ │
│  │  ✅ Check-in                        │ │  │                     │ │
│  │  📊 Đánh giá                        │ │  │                     │ │
│  │  ─────── WORKFLOW ──────            │ │  │                     │ │
│  │  ✔️ Duyệt                           │ │  │                     │ │
│  │  📝 Audit Log                       │ │  │                     │ │
│  │  ─────── TỔ CHỨC ──────            │ │  │                     │ │
│  │  🏛️ Phòng ban                       │ │  │                     │ │
│  │  👥 Nhân viên                       │ │  │                     │ │
│  │  ─────── AI ────────────            │ │  │                     │ │
│  │  🤖 AI Copilot                      │ │  │                     │ │
│  │  📝 Báo cáo AI                      │ │  │                     │ │
│  │  ─────────────────────              │ │  │                     │ │
│  │  ⚙️ Cài đặt                         │ │  │                     │ │
│  └─────────────────────────────────────┘ │  └─────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.2 AI Copilot Panel (Slide-in from right)

```
┌────────────────────────────────────────────────────────┐
│  🤖 AI Copilot                              [─] [×]   │
├────────────────────────────────────────────────────────┤
│                                                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │ 💬 User: Tháng này phòng nào vượt ngân sách?    │  │
│  └──────────────────────────────────────────────────┘  │
│                                                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │ 🤖 AI: Dựa trên dữ liệu tháng 4/2026:          │  │
│  │                                                  │  │
│  │ 1. **Phòng Marketing**: 85tr/70tr (121%) ⚠️      │  │
│  │ 2. **Phòng IT**: 45tr/40tr (112.5%) ⚠️           │  │
│  │                                                  │  │
│  │ ┌────────────────────────────────────┐            │  │
│  │ │  [Bar Chart: Budget vs Actual]     │            │  │
│  │ └────────────────────────────────────┘            │  │
│  │                                                  │  │
│  │ 📎 Sources: Budget Marketing T4, Budget IT T4    │  │
│  └──────────────────────────────────────────────────┘  │
│                                                        │
│  ┌────────────────────────────────────┐  [Send ▶]     │
│  │ Nhập câu hỏi...                   │               │
│  └────────────────────────────────────┘               │
└────────────────────────────────────────────────────────┘
```

---

## 4. Key Page Wireframes

### 4.1 Director Dashboard

```
┌──────────────────────────────────────────────────────────────────────┐
│  Dashboard Điều hành                      [Kỳ: T4/2026 ▼] [🤖 AI] │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│  │ Tổng thu │ │ Tổng chi │ │ Ngân sách│ │ KPI avg  │ │ Pending  │  │
│  │ 1.2 tỷ   │ │ 800 tr   │ │ còn lại  │ │ 72%      │ │ Approvals│  │
│  │ ↑12%     │ │ ↑8%      │ │ 400 tr   │ │ ↑5%      │ │ 8        │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘  │
│                                                                      │
│  ┌────────────────────────────────┐ ┌────────────────────────────┐   │
│  │  Thu/Chi theo thời gian        │ │  KPI theo phòng ban        │   │
│  │  [Line Chart]                  │ │  [Horizontal Bar Chart]    │   │
│  │                                │ │  Marketing ████████ 85%    │   │
│  │                                │ │  Sales     ███████░ 72%    │   │
│  │                                │ │  IT        ██████░░ 65%    │   │
│  └────────────────────────────────┘ └────────────────────────────┘   │
│                                                                      │
│  ┌────────────────────────────────┐ ┌────────────────────────────┐   │
│  │  ⚠️ Cảnh báo rủi ro            │ │  Chờ duyệt                 │   │
│  │  🔴 Marketing vượt budget 21%  │ │  PR-001 Payment 80tr  [→]  │   │
│  │  🟡 KPI Sales tụt 15%         │ │  PR-002 Software 25tr [→]  │   │
│  │  🟡 Cash runway: 45 ngày      │ │  KPI Check-in x3      [→]  │   │
│  └────────────────────────────────┘ └────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
```

### 4.2 Payment Request Create/Edit Form

```
┌──────────────────────────────────────────────────────────────────────┐
│  ← Đề nghị thanh toán / Tạo mới                                     │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Tiêu đề *          [____________________________________]           │
│  Phòng ban *         [Marketing ▼]     Danh mục * [Digital Ads ▼]   │
│  Nhà cung cấp       [Google Vietnam ▼]                               │
│  Mô tả              [____________________________________]           │
│                      [____________________________________]           │
│                                                                      │
│  ── Chi tiết khoản chi ──────────────────────────────────────        │
│  │ # │ Mô tả               │ SL  │ Đơn giá    │ Thành tiền │       │
│  │ 1 │ Google Ads Campaign  │ 1   │ 50,000,000 │ 50,000,000 │ [🗑] │
│  │ 2 │ Facebook Ads         │ 1   │ 30,000,000 │ 30,000,000 │ [🗑] │
│  │   │                      │     │            │            │       │
│  │   [+ Thêm dòng]                │   Tổng:    │ 80,000,000 │       │
│                                                                      │
│  Ưu tiên   [Normal ▼]    Hạn thanh toán  [2026-05-15]              │
│                                                                      │
│  📎 Đính kèm (0/5)                                                  │
│  [Chọn file...] PDF, JPG, PNG, XLSX - Tối đa 10MB/file              │
│                                                                      │
│  ┌──────────────────────────────────────────────────┐                │
│  │ 🤖 AI Risk Analysis                              │                │
│  │ ⚠️ Risk Score: 65/100 (Medium)                    │                │
│  │ • Chiếm 45% ngân sách tháng Marketing             │                │
│  │ • NCC Google Vietnam đã có PR trong 7 ngày qua    │                │
│  │ 💡 Khuyến nghị: Cân nhắc giãn tiến độ             │                │
│  └──────────────────────────────────────────────────┘                │
│                                                                      │
│  [Lưu nháp]  [Gửi duyệt →]                                         │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 5. User Flows

### 5.1 Payment Request Approval Flow

```
Staff                          System                        Manager/Director
  │                              │                              │
  │ 1. Tạo PR (Draft)           │                              │
  │─────────────────────────────►│                              │
  │                              │ 2. AI Risk Analysis          │
  │ 3. Xem risk warnings        │◄──────────────────           │
  │◄─────────────────────────────│                              │
  │                              │                              │
  │ 4. Submit (gửi duyệt)       │                              │
  │─────────────────────────────►│ 5. Create workflow instance  │
  │                              │ 6. Notify approver           │
  │                              │─────────────────────────────►│
  │                              │                              │
  │                              │            7. Review PR      │
  │                              │◄─────────────────────────────│
  │                              │     8. Approve / Reject      │
  │                              │                              │
  │ 9. Receive notification      │                              │
  │◄─────────────────────────────│                              │
  │                              │ 10. If approved:             │
  │                              │     - Create transaction     │
  │                              │     - Update budget          │
  │                              │     - Update dashboard       │
```

### 5.2 KPI Check-in Flow

```
Employee → Mở KPI → Tạo Check-in → Nhập giá trị mới + Note + Evidence 
→ Submit → Manager nhận notification → Review check-in → Approve/Reject
→ If Approved: Update KPI current value + progress + dashboard
→ If Rejected: Employee nhận feedback → Sửa → Re-submit
```

---

## 6. Responsive Behavior

| Breakpoint | Layout | Changes |
|-----------|--------|---------|
| Desktop (≥1280px) | Sidebar + Full content | Default layout |
| Laptop (1024-1279px) | Sidebar collapsed (72px) + Content | Sidebar shows icons only |
| Tablet (768-1023px) | Sidebar overlay + Content | Sidebar opens as overlay |
| Mobile (< 768px) | Bottom nav + Content | Sidebar → bottom nav bar, tables → card list |

### Mobile Adaptations
- Tables → Card-based list view
- Dashboard grid → Single column stack
- Charts → Full-width, swipeable
- AI Panel → Full-screen modal
- Forms → Full-width, stacked labels
- Approval queue → Swipe-to-approve (optional)
