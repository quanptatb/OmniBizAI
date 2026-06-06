# Logic nghiep vu OmniBizAI

Tai lieu nay tong hop logic nghiep vu doc tu code trong project `OmniBizAI`. He thong la ung dung ASP.NET Core MVC, EF Core, SQL Server, Identity, multi-tenant demo qua `ITenantContext`, tich hop Gemini AI, notification, audit log va cac dashboard quan tri.

## 1. Tong quan he thong

OmniBizAI la he thong ERP/Business Management noi bo cho doanh nghiep, gom cac mien nghiep vu chinh:

| Mien nghiep vu | Module/man hinh chinh | Muc dich |
|---|---|---|
| Nhan dang, tenant, phan quyen | Account, Users, Roles, Settings | Dang nhap, quan ly nguoi dung, cau hinh cong ty/module/tham so |
| To chuc va nhan su | Organization, Employees, Positions, Leave | Co cau phong ban, ho so nhan vien, hop dong, nghi phep |
| CRM va danh muc | Customers, Vendors, Products, CustomerCare, SalesOpportunity | Khach hang, nha cung cap, san pham/dich vu, cham soc KH, co hoi ban hang |
| Van hanh | Operations, Workflow Kanban, OperationPlans, ResourceManagement, Maintenance | Yeu cau van hanh, ke hoach, task, thiet bi, ca lam, bao tri |
| Mua sam, kho, tai chinh | Procurement, PurchaseOrders, GoodsReceipt, GoodsIssue, Inventory, Finance, CashBook | De xuat mua, PO, nhap/xuat kho, ton kho, ngan sach, thanh toan, thu chi |
| Don hang va san xuat | OrderManagement, OrderProcess | Don ban hang, phe duyet, san xuat, QC, truy xuat lo |
| KPI/OKR va danh gia | MissionVision, Okr, KpiSetup, KpiCheckIn, Evaluation | Chien luoc, muc tieu, KPI, check-in, review, xep hang |
| AI, bao cao, canh bao | AiInsights, AnomalyAlerts, Reports, Dashboard | Tong hop du lieu, phan tich AI, canh bao rui ro, dashboard dieu hanh |
| He thong phu tro | Notifications, Audit, Backup, Profile | Thong bao, nhat ky, sao luu, thong tin ca nhan |

Luon xu ly tong quat:

```text
User dang nhap
  -> TenantContext xac dinh TenantId/UserId/Roles
  -> Controller nhan request
  -> Service ap dung rule nghiep vu va ghi DB
  -> AuditLog/Notification/AI neu co
  -> View hien thi danh sach, chi tiet, dashboard hoac canh bao
```

## 2. Nen tang du lieu va cross-cutting logic

### 2.1 Tenant va nguoi dung

- `TenantContextService` dang dung tenant demo co ID co dinh `00000000-0000-0000-0000-000000000001`.
- Khi user da dang nhap, service tim `AppUser` theo email va tenant demo de lay `UserId`, `FullName`, roles.
- Hau het service loc du lieu bang `TenantId == tenant.TenantId` va `!IsDeleted`.
- Identity dung `IdentityUser<Guid>` va `IdentityRole<Guid>`. Password policy dang de rat thoang cho demo; user SQL-seeded chua co password se duoc gan mat khau mac dinh `123` khi app start.

### 2.2 Soft delete, audit, notification

- Entity nghiep vu ke thua `TenantEntity` co `TenantId`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`, `IsDeleted`.
- EF configurations dat query filter `!IsDeleted` cho nhieu bang.
- Nhieu action quan trong ghi `AuditLog`: create/update/submit/approve/reject/cancel/confirm/delete.
- `NotificationService` co 4 kieu gui:
  - `SendAsync`: gui toi danh sach user cu the.
  - `BroadcastAsync`: gui toi tat ca user active trong tenant.
  - `SendToDepartmentAsync`: gui toi user active thuoc phong ban.
  - `SendToManagersAsync`: gui toi cac user co role/ten role quan ly nhu manager/admin/executive.

### 2.3 Dashboard va canh bao bat thuong

`DashboardService` tong hop:

- Yeu cau van hanh theo trang thai, qua han.
- Phan viec theo phong ban.
- Approval dang cho.
- User active.
- KPI summary.
- Ngan sach ke hoach va da su dung.
- Audit gan day.

`AnomalyDetectionService` quet nhieu mien:

- Operations: yeu cau qua han, approval ton dong.
- Finance: ngan sach vuot 75%/90%, thanh toan cho lon.
- CashFlow: dong tien am, giao dich thu chi cho duyet.
- Inventory: ton kho critical/low/overstock.
- CRM: win rate thap, co hoi qua han chot.
- HR: nhieu don nghi phep cho duyet.
- Procurement: de xuat mua sam ton dong.
- KPI/OKR: tien do OKR thap.

## 3. CRM, danh muc va ban hang truoc don

### 3.1 Customer, contact, site

Khach hang la doi tuong trung tam cho CRM, co:

- Thong tin khach hang (`Customer`): code, name, industry, status active.
- Nguoi lien he (`CustomerContact`): co the dat primary contact.
- Dia diem/site (`CustomerSite`): phuc vu giao hang, cham soc, van hanh tai dia diem.

Logic chinh:

```text
Create Customer
  -> AddContact/AddSite
  -> Toggle active/inactive hoac soft delete
  -> Dung trong SalesOpportunity, SalesOrder, OperationRequest, GoodsIssue
```

### 3.2 Vendor va Product/Service

- `Vendor` dung trong PO, payment, procurement.
- `ProductService` dung chung cho san pham/dich vu, PO line, nhap kho, xuat kho, don ban hang, ton kho.
- `ProductService.Type == "Product"` moi tham gia tinh ton kho/san xuat.
- Cac nguong `ReorderPoint`, `SafetyStock`, `MaxStock` duoc Inventory dung de tao canh bao.

### 3.3 Customer Care

`CrmInteraction` quan ly tuong tac/cham soc khach hang:

```text
Create interaction
  -> Start
  -> Complete hoac Cancel
  -> Edit/Delete khi phu hop
```

Muc dich la ghi nhan lich su goi dien, gap mat, ho tro, khieu nai, follow-up voi khach hang.

### 3.4 Sales Opportunity

`SalesOpportunity` quan ly pipeline ban hang:

```text
Create opportunity
  -> Edit thong tin gia tri, khach hang, expected close date
  -> ChangeStage
  -> ClosedWon / ClosedLost la trang thai ket thuc pipeline
```

Dashboard/AI dung cac chi so:

- So co hoi.
- Co hoi thang/thua.
- Win rate.
- Pipeline value.
- Co hoi qua han ngay du kien chot.

## 4. Van hanh noi bo

### 4.1 Operation Request

`OperationRequest` la yeu cau van hanh phat sinh, co the gan phong ban, khach hang, due date, priority, tong tien va cac line san pham/dich vu.

State machine chinh:

```text
Draft
  -> Submit -> Submitted
  -> Cancel -> Cancelled

Submitted
  -> Approve qua ApprovalTask -> Approved
  -> Reject -> Rejected
  -> Cancel -> Cancelled

Approved
  -> StartWork -> InProgress

InProgress
  -> Hold -> OnHold
  -> Complete -> Completed

OnHold
  -> Resume -> InProgress

Completed
  -> Reopen -> InProgress
```

Rule dang co trong service:

- Chi `Draft` moi submit.
- Chi `Draft` hoac `Rejected` moi edit.
- Khi resubmit tu rejected, status duoc dua ve `Draft`.
- Submit tao `ApprovalTask` buoc `DEPARTMENT_REVIEW`, role `DEPARTMENT_MANAGER`.
- StartWork chi khi `Approved`.
- Complete chi khi `InProgress`.
- Delete la soft delete.
- Add line cap nhat du lieu chi tiet hang hoa/dich vu cua request.
- Add comment luu trao doi trong `OperationComment`.

Lien ket:

- `GoodsIssue` chi cho chon OperationRequest o `Approved` hoac `InProgress`.
- Work item, AI insight, approval task, audit log duoc hien tren detail.

### 4.2 Approval

`ApprovalTask` la co che phe duyet dung chung cho OperationRequest va SalesOrder, co target type/id, step code, assigned role/user, decision note.

Voi OperationRequest:

```text
ApprovalTask Pending
  -> Approve lan 1 DEPARTMENT_REVIEW
       neu can buoc tiep: tao EXECUTIVE_REVIEW
       neu ket thuc: target Approved
  -> Reject -> target Rejected
  -> Reassign -> doi user phu trach
```

Voi SalesOrder:

```text
Draft SalesOrder
  -> SubmitForApproval
  -> tao WorkflowInstance + ApprovalTask DEPARTMENT_REVIEW
  -> approve/reject qua module Approvals
```

### 4.3 Workflow Kanban

`WorkItem`, `KanbanColumn`, checklist, comment, assignment tao ban cong viec noi bo.

Logic:

- Tao card cong viec, gan phong ban, priority, due date.
- Move card giua column/status.
- Them comment/checklist va toggle checklist.
- Quan ly column: tao, rename, xoa, sap xep.
- Co the lien ket `WorkItem` voi `OperationRequest`.

### 4.4 Operation Plan

`OperationPlan` la ke hoach van hanh dinh ky/dai han; `PlanTask` la task con.

```text
Create plan
  -> AddTask cho nhan vien/thiet bi
  -> UpdateTaskStatus(Todo/InProgress/Completed...)
  -> Analyze -> Gemini phan tich tinh kha thi, rui ro, goi y toi uu
```

Rule:

- Task co the gan `EquipmentId`, `AssignedUserId`.
- Update progress/status cap nhat tien do task.
- Tao task/thay doi task co the phat notification.

### 4.5 Resource Management

Quan ly tai nguyen van hanh:

- `Equipment`: may moc/thiet bi, status, vi tri, thong tin bao tri.
- `MaintenanceRecord`: lich bao tri trong module resource co ban.
- `WorkShift` va `ShiftAssignment`: ca lam va phan ca nhan vien theo ngay.
- `EmployeeCertificate`: chung chi/bang cap, canh bao sap het han.
- `Workspace`: khu vuc lam viec, co quan he cha-con.

Luon nghiep vu:

```text
CreateEquipment
  -> ScheduleMaintenance/CompleteMaintenance
  -> duoc OperationPlan va Maintenance su dung

CreateShift
  -> AssignShift theo ngay
  -> xem ShiftSchedule

AddCertificate
  -> dashboard loc chung chi sap het han/da het han
```

### 4.6 Maintenance

Module bao tri day du gom incident, PM, spare part va sensor.

Incident:

```text
Open
  -> StartIncident -> InProgress
  -> ResolveIncident -> Resolved
  -> CloseIncident -> Closed
```

Logic:

- Tao su co voi equipment, severity, assigned technician.
- Start chuyen su co sang dang xu ly.
- Resolve ghi solution, chi phi, downtime va cap nhat status.
- AnalyzeIncident goi Gemini de goi y root cause/hanh dong phong ngua.

PM schedule:

```text
Create PM
  -> NextDueDate duoc tinh theo Daily/Weekly/Monthly hoac frequency value
  -> ExecutePm
  -> cap nhat LastDoneDate va NextDueDate moi
```

Spare part:

- Tao phu tung, don vi, don gia, ton kho toi thieu.
- `AdjustStock(delta)`: tang/giam ton kho phu tung; khong cho xuat lam ton am.

Sensor:

- `SimulateSensorDataAsync` tao du lieu cam bien cho demo.
- `SensorMonitor` hien thi readings gan nhat.
- Sensor vuot nguong co the tao notification/canh bao.

## 5. Mua sam, kho va tai chinh

### 5.1 Procurement Request

`ProcurementRequest` la de xuat mua sam noi bo.

```text
Draft
  -> Submit -> Submitted
  -> Cancel -> Cancelled

Submitted
  -> Approved -> Approved
  -> Ordered -> Ordered
  -> Received -> Received
```

Trong code hien tai:

- Create tao `PR-{year}-{seq}` va lines.
- Submit chi tu `Draft`.
- Cancel chi tu `Draft`/`Submitted`.
- PO form chi chon procurement da `Approved`.
- Khi GoodsReceipt cua PO xong, PR co the chuyen `Received` neu PO completed.

### 5.2 Purchase Order

`PurchaseOrder` la don dat mua NCC:

```text
Draft -> Sent -> PartiallyReceived -> Completed
Draft/Sent -> Cancelled
```

Trong code:

- Create PO voi vendor, procurement request optional, line san pham/dich vu.
- Tong tien = sum quantity * unit price.
- Khi nhap kho confirmed, PO chuyen `PartiallyReceived` hoac `Completed` dua tren tong so nhan so voi tong so dat.

### 5.3 Goods Receipt

Nhap kho:

```text
Draft
  -> Confirmed
  -> Cancelled
```

Rule:

- Co the tao tu PO, form tu dong lay line con lai chua nhan.
- Confirm GR:
  - Cap nhat status GR.
  - Tinh tong da nhan cua PO.
  - Cap nhat PO `PartiallyReceived`/`Completed`.
  - Neu PO gan PR va PO completed, cap nhat PR `Received`.
- Ton kho khong luu bang so luong co dinh; duoc tinh tu GR confirmed tru GI confirmed.

### 5.4 Goods Issue va Inventory

Xuat kho:

```text
Draft
  -> Confirmed
  -> Cancelled
```

Rule:

- Tao GI co issue type, operation request optional, customer/phong ban/destination.
- GI form chi cho link OperationRequest status `Approved` hoac `InProgress`.
- Confirm GI lam giam ton kho theo cong thuc tinh ton.
- Inventory dashboard tinh stock:

```text
CurrentStock = sum(GR confirmed received - rejected) - sum(GI confirmed issued)
```

Stock alert:

- Critical: ton kho <= safety stock.
- Low: ton kho <= reorder point va > safety stock.
- Overstock: ton kho >= max stock.
- Khong tao duplicate alert active cung product/alert type.
- Tu auto-resolve alert khi dieu kien khong con dung.
- Alert co state `Active -> Acknowledged -> Resolved`.

### 5.5 Budget, Expense, Payment

Budget:

```text
Active
  -> Update khi Active
  -> Close -> Closed
```

- Tao budget mac dinh status `Active`.
- UsedAmount = sum expenses trong budget.
- Dashboard canh bao budget dung tren 70%.

Expense:

```text
Recorded -> Approved
Recorded -> Reversed (enum co, code chua day du UI)
```

- Tao chi phi gan budget optional.
- Approve chi khi status `Recorded`.

PaymentRequest:

```text
Draft
  -> Submit -> Submitted
  -> Approve -> Approved
  -> MarkPaid -> Paid
Submitted -> Reject -> Rejected
```

- Payment co vendor, PO optional, due date, total amount.
- Audit log cho submit/approve/reject/paid.

### 5.6 CashBook

`CashTransaction` quan ly thu chi:

```text
Recorded
  -> Approve -> Approved
  -> Reject -> Rejected
  -> Void -> Voided
```

Dashboard tinh:

- Tong thu, tong chi.
- So du = income - expense.
- Giao dich trong thang.
- Giao dich cho duyet.
- Loc theo type/status/category/date/search.

## 6. Don hang va san xuat

`SalesOrder` la don ban hang sau CRM.

State machine:

```text
Draft
  -> SubmitForApproval -> Submitted
  -> Approval approve -> Approved
  -> StartProduction -> InProduction
  -> tat ca step Completed va QC Passed -> Completed
  -> Cancelled
```

Logic chinh:

- Create order tao `SO-{yyyyMMdd}-{seq}`, line lay gia tu input hoac gia chuan product.
- SubmitForApproval:
  - Tao workflow definition mac dinh neu chua co.
  - Tao `WorkflowInstance`.
  - Doi order sang `Submitted`.
  - Tao `ApprovalTask` buoc `DEPARTMENT_REVIEW`.
- StartProduction chi khi order `Approved`:
  - Doi status `InProduction`.
  - Tao 5 production steps mac dinh: chuan bi NVL, gia cong, lap rap, QA/QC, dong goi & ban giao.
  - Tao `ProductTraceability` theo line/lot.
- UpdateProductionStep:
  - Doi status Todo/InProgress/Completed.
  - Gan nhan vien neu co.
  - Ghi traceability.
- SubmitQc:
  - Ghi QC status/notes/user/time.
  - Neu failed: reset step ve `Todo`.
  - Neu passed va tat ca step Completed + QC Passed: order `Completed`, ghi traceability hoan thanh.

## 7. HR va to chuc

### 7.1 Organization

`OrganizationUnit` la co cau phong ban dang cay:

- Co `ParentId`, `Children`.
- Dung trong AppUser, Employee, OperationRequest, Procurement, Budget, GoodsIssue.
- Co the tao/sua/xem chi tiet phong ban, nhan vien, vi tri con.

### 7.2 Employee va Position

`EmployeeProfile` la ho so nhan su, lien ket `AppUser`, phong ban, position.

Logic:

- Tao nhan vien: chon phong ban, chuc danh, thong tin ca nhan.
- Edit cap nhat ho so.
- Deactivate chuyen status/user inactive thay vi xoa vat ly.
- AddContract tao hop dong nhan vien.
- Dashboard HR dem nhan vien active, hop dong sap het han, phan bo phong ban.

### 7.3 Leave

Don nghi phep:

```text
Draft/Submitted
  -> Approve -> Approved
  -> Reject -> Rejected
  -> Delete/Cancel tuy UI
```

Trong service:

- Tao leave request.
- Approve/Reject theo id, ghi reason neu reject.
- Dashboard/anomaly dem don `Submitted` dang cho duyet.

## 8. KPI, OKR va danh gia

### 8.1 Mission/Vision

`MissionVision` luu vision, mission, yearly goal.

```text
Create
  -> Edit
  -> Toggle active/inactive
```

Duoc dung lam nen tang lien ket voi OKR.

### 8.2 OKR

`OkrObjective` co level Company/Department/Individual, cycle, status; co nhieu `OkrKeyResult`.

```text
Draft
  -> Activate -> Active
  -> Close -> Completed
  -> Cancelled
```

Logic:

- Create objective kem key results.
- Update key result current value.
- `OkrProgressService.RecalculateAsync` tinh lai tien do objective tu key results.
- Dashboard tinh OKR active, progress trung binh, objective sap het han.

### 8.3 KPI

`KpiDefinition` mo ta KPI, owner type, period, measure type, target/pass/fail threshold.

```text
Draft
  -> Activate -> Active
  -> Close -> Completed/Closed
```

KPI co the assign cho phong ban/user, co targets, results, comments, fail reasons, adjustment history.

### 8.4 KPI Check-in

```text
Check-in draft/edit
  -> Submit -> ReviewStatus Pending
  -> Review Approve/Reject
```

Logic:

- User cap nhat progress/comment/detail.
- Submit tao ban ghi cho manager review.
- Review co decision, comment, score.
- History log ghi hanh dong.

### 8.5 Evaluation

Danh gia gom:

- `EvaluationPeriod`: ky danh gia.
- `EvaluationResult`: diem, classification, reviewer/director comment.
- `GradingRank`, `BonusRule`, `RealtimeExpectedBonus`: xep hang va thuong du kien.

## 9. AI va bao cao

### 9.1 Gemini AI

Cac diem tich hop AI:

- `AiInsightService`: AI Copilot phan tich tong quan doanh nghiep dua tren data thuc.
- `OperationPlanService.AnalyzePlanWithAiAsync`: phan tich ke hoach van hanh.
- `MaintenanceService.AnalyzeIncidentWithAiAsync`: phan tich su co/thiet bi/root cause.
- `AnomalyDetectionService`: khong nhat thiet goi AI, nhung tao canh bao rule-based tu data.

AI Copilot lay ngu canh:

- Van hanh: request count, overdue, completed month, pending approval.
- HR: employees, departments, leave.
- Finance/Cashflow: budget, expense, payment, cash in/out.
- Procurement/Inventory: PR, PO, GR, GI, stock alerts.
- CRM/Sales: customers, opportunities, win rate, pipeline.
- KPI/OKR: KPI count, OKR progress.

Neu Gemini loi/khong cau hinh, service co fallback local dua tren rules.

### 9.2 Reports

Reports gom:

- Executive: tong quan lanh dao.
- Finance/CashFlow.
- HR.
- CRM.
- Inventory.
- KPI/OKR.
- Export center.

Bao cao chu yeu doc du lieu tong hop tu cac entity, khong phai luong ghi nghiep vu rieng.

## 10. So do lien ket nghiep vu end-to-end

```text
CRM
  Customer + Product
      |
      v
SalesOpportunity -> SalesOrder -> Approval -> ProductionSteps -> QC -> Completed
      |                 |                         |
      |                 |                         v
      |                 |                  ProductTraceability
      v                 |
OperationRequest -------+
      |
      v
GoodsIssue -> Inventory stock -> StockAlert

ProcurementRequest -> PurchaseOrder -> GoodsReceipt -> Inventory stock
        |                 |
        v                 v
     Budget/Expense    PaymentRequest -> CashBook

Organization/Employee
      |        |
      |        +-> Leave / KPI Check-in / Evaluation
      |
      +-> OperationRequest / Procurement / Budget / ShiftAssignment

Equipment/Resource
      |
      +-> OperationPlan -> PlanTask
      |
      +-> Maintenance -> Incident / PM / SparePart / SensorReading

Dashboard + Reports + AnomalyDetection + AiInsight doc du lieu tu tat ca module
Notification + AuditLog ghi nhan cac action quan trong
```

## 11. Cac rui ro va diem can luu y trong logic hien tai

1. Multi-tenant hien dang hard-code demo tenant trong `TenantContextService`; neu len SaaS that can lay tenant theo domain/user membership.
2. Password policy va mat khau seeded `123` phu hop demo, khong phu hop production.
3. Mot so state co enum nhung chua co day du action UI/service, vi du Procurement approve, PO sent/cancel, Expense reversed.
4. Ton kho san pham duoc tinh tu GR/GI confirmed, nhung confirm GI chua thay check ton am trong `InventoryService`.
5. Sinh ma bang count + 1 co rui ro trung ma khi nhieu user tao dong thoi.
6. Nhieu transition chua co optimistic concurrency/row version, de xay ra race condition khi hai manager cung approve/confirm.
7. Approval chain con don gian, chu yeu dua tren role/step code, chua thay cau hinh workflow dong day du.
8. Notification la DB-based delivery, chua thay SignalR realtime that su trong code doc duoc.
9. Audit log dang luu JSON string tu noi chuoi, can can than escape ky tu dac biet.
10. Mot so module dung string status/type/stage thay vi enum, de sai chinh ta va kho validate.

## 12. TL;DR

- OmniBizAI la ERP mini da module cho doanh nghiep: CRM, operations, procurement, inventory, finance, HR, KPI/OKR, production.
- Moi du lieu nghiep vu gan tenant va soft delete.
- Luong chinh bat dau tu CRM/customer/product, tao order hoac operation request, qua approval, thuc thi, kho/tai chinh va bao cao.
- Inventory khong luu ton truc tiep ma tinh tu phieu nhap confirmed tru phieu xuat confirmed.
- Operations co workflow Draft -> Submitted -> Approved -> InProgress -> Completed.
- SalesOrder co workflow Draft -> Submitted -> Approved -> InProduction -> Completed sau QC.
- KPI/OKR quan ly muc tieu, chi so, check-in va review.
- Maintenance quan ly su co, bao tri dinh ky, phu tung va sensor demo.
- AI Gemini doc du lieu tong hop de phan tich, co fallback rule-based.
- Diem can cai tien lon nhat: tenant production, approval chain, check ton am, sinh ma an toan, concurrency va bao mat password.
