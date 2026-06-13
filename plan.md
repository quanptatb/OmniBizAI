# Kế hoạch nâng cấp khâu Vận hành — OmniBizAI (Rút gọn)

> **Phạm vi:** toàn bộ module phục vụ khâu **Vận hành** trong hệ thống ERP nội bộ, gồm:
> 1. `Operations` — Yêu cầu vận hành (OperationRequest)
> 2. `Workflow` — Bảng Kanban công việc nội bộ (WorkItem)
> 3. `OperationPlans` — Kế hoạch vận hành dài hạn (OperationPlan + PlanTask)
> 4. `ResourceManagement` — Thiết bị, ca làm, phân ca, chứng chỉ, mặt bằng (Equipment, WorkShift, ShiftAssignment, Certificate, Workspace)
> 5. `Maintenance` — Sự cố, bảo trì định kỳ (PM), phụ tùng, IoT sensor
> 6. `Approvals` — Phê duyệt vận hành (ApprovalTask)
>
> **Mục tiêu nâng cấp:**
> - Chuẩn business hơn: gắn đúng vai trò nghiệp vụ (operator/supervisor/manager/maintenance), có SLA, có audit đầy đủ, có chỉ số đo được (KPI vận hành).
> - Tối ưu hóa vận hành: tăng năng suất, giảm thời gian chờ, giảm xung đột tài nguyên, giảm downtime thiết bị, tăng khả năng dự báo.
> - Code clean: tách Service – Repository, dùng enum thay string, đảm bảo concurrency, tránh trùng mã, validate ở Domain layer thay vì rải rác ở Controller.
>
> **Cập nhật:** 2026-06-01 — Rút gọn từ plan gốc 57 feature xuống còn **39 feature** (29 đã xong + 2 cần polish + 8 cần làm).
> Loại bỏ 18 feature quá phức tạp hoặc ít giá trị cho ĐATN.

---

## Tổng quan tiến độ

| Nhóm | Tổng | ✅ Xong | ⚠️ Một phần | 🔲 Cần làm |
|------|------|---------|-------------|-----------|
| Nền tảng (0.x) | 8 | **8** | 0 | 0 |
| Module 1 — Operations Request | 7 + cleanup | 7 + cleanup | 0 | 0 |
| Module 2 — Workflow Kanban | 7 | **7** | 0 | 0 |
| Module 3 — Operation Plans | 5 + cleanup | **5 + cleanup** | 0 | 0 |
| Module 4 — Resource Management | 1 | 1 | 0 | 0 |
| Module 5 — Maintenance | 6 | 6 | 0 | 0 |
| Module 6 — Approvals | 2 | **2** | 0 | 0 |
| Module 7 — Command Center | 3 | **3** | 0 | 0 |
| **TỔNG** | **39** | **39** | **0** | **0** |

> **Hoàn thành: 100%** (39/39). Tất cả feature đã triển khai xong. Build passed (0 errors).

---

## 0. Nguyên tắc chung khi nâng cấp (Cross-cutting)

### 0.1. Chuẩn hoá kiểu dữ liệu — Enum thay string status/type ✅ Xong

- Tạo enum trong `Models/Entities/Enums/DomainEnums.cs` (547 dòng):
  - `OperationStatus`, `PlanTaskStatus`, `EquipmentStatus`, `IncidentStatus`, `IncidentSeverity`, `MaintenanceType`, `PmFrequency`, `WorkOrderStatus`, `SparePartRequisitionStatus`, `PmTriggerType`, `FailureModeCategory`, ...
- Lưu DB dạng `string` qua `HasConversion<string>()` (giữ tương thích, dễ đọc trong SQL).
- Helper `OperationStatusTransitions.IsAllowed(from, to)` trả về bool — tập trung rule chuyển trạng thái ở 1 chỗ.

### 0.2. Tạo mã chứng từ an toàn (NumberSequence) ✅ Xong

- Service tập trung `Services/NumberingService.cs`:
  ```
  Task<string> NextAsync(string sequenceKey, string prefix, int padLength = 4)
  ```
- Dùng transaction Serializable + row lock (`UPDLOCK, HOLDLOCK`) để increment an toàn.
- Sequence key: `OP_REQ`, `OP_PLAN`, `EQUIP`, `SP_PART`, `WSPACE`, `INCIDENT`, `PM_SCHED`, `WORK_ORDER`, `SP_REQ`, `FAIL_MODE`.

### 0.3. Optimistic concurrency ✅ Xong

- Thêm `byte[] RowVersion` cho 11 entity nhạy cảm: `WorkOrder`, `WorkItem`, `Sprint`, `SparePartRequisition`, `ResourceManagement`, `PlanTask`, `OperationRequest`, `OperationPlan`, `MaintenanceManagement`, `Equipment`, `ApprovalTask`.
- EF Core: `.IsRowVersion()`.
- Service bắt `DbUpdateConcurrencyException` → trả về lỗi concurrency.

### 0.4. Pattern `Result<T>` thay cho `bool`/`(bool, string)` ✅ Xong

**Hiện trạng:** rải rác `Task<bool>`, `Task<(bool Success, string Message)>` — gây bất nhất giữa các service.

**Nâng cấp:**
- Bổ sung `Models/Common/Result.cs`:
  ```csharp
  public record Result(bool Success, string? Message = null, string? ErrorCode = null);
  public record Result<T>(bool Success, T? Data = default, string? Message = null, string? ErrorCode = null);
  ```
- Refactor tuần tự các service public method để dùng `Result`. Controller chỉ render message; không tự dựng UX text trong service.

### 0.5. Tách rule state-machine khỏi service ✅ Xong

- Tạo `Domain/StateMachines/` — 8 file:
  - `OperationRequestStateMachine.cs`, `PlanTaskStateMachine.cs`, `MaintenanceIncidentStateMachine.cs`, `ApprovalTaskStateMachine.cs`, `WorkItemStateMachine.cs`, `WorkOrderStateMachine.cs`, `OperationPlanStateMachine.cs`, `SparePartRequisitionStateMachine.cs`.
  ```csharp
  static readonly Dictionary<Status, HashSet<Status>> Allowed = ...;
  public static bool CanTransition(Status from, Status to);
  public static IReadOnlyList<Status> NextStates(Status from);
  ```
- View dùng `NextStates(...)` để render đúng các nút action (ẩn nút Approve nếu không thể).

### 0.6. Audit log có cấu trúc ✅ Xong

- Service `Services/AuditService.cs`: `LogAsync(entityType, entityId, action, oldValueObj, newValueObj, extra)`.
- Dùng `System.Text.Json.JsonSerializer` (không nối chuỗi tay).
- Lưu thêm `IpAddress`, `UserAgent`, `CorrelationId`.

### 0.7. Realtime notification (SignalR) ✅ Xong

**Hiện trạng:** Đã có `Hubs/KanbanHub.cs` cho Kanban. Chưa có hub chung cho Operations.

**Nâng cấp:**
- Thêm `Hubs/OperationsHub.cs` (SignalR) — clone pattern từ `KanbanHub`.
- Khi `NotificationService` gửi notification, đẩy thêm sự kiện qua hub vào group `tenant:{tid}` và `user:{uid}`.
- Áp dụng cho: ApprovalTask mới giao, Incident Critical mới mở, WorkItem được di chuyển sang cột mình phụ trách, PM schedule đến hạn hôm nay.
- Frontend: subscribe ở `_Layout.cshtml` để hiện badge realtime.

### 0.8. Validation tập trung (FluentValidation) ✅ Xong

**Hiện trạng:** validation đang trộn lẫn (`DueDate < Today` check ở controller; số khác check trong service). Không có folder `Validators/`.

**Nâng cấp:**
- Tạo `Validators/` folder với FluentValidation validator cho mỗi `*CreateViewModel`/`*EditViewModel`.
- Register vào DI: `services.AddValidatorsFromAssemblyContaining<...>()`.
- Controller chỉ check `ModelState.IsValid`.
- Ưu tiên: `OperationRequestCreateViewModel`, `OperationRequestEditViewModel`, `WorkOrderCreateViewModel`, `SparePartRequisitionCreateViewModel`.

---

## 1. Module Operations Request — ✅ Hoàn thành

### 1.1. Hiện trạng tóm tắt
- State machine: Draft → Submitted → Approved → InProgress → OnHold → Completed; có Cancel, Reject, Reopen.
- Tạo line hàng/dịch vụ, comment, gắn customer/department/due date/priority.
- Gắn `GoodsIssue` chỉ khi Approved/InProgress.
- Approval 1–2 cấp (DEPARTMENT_REVIEW, EXECUTIVE_REVIEW).

### 1.2. Các feature đã hoàn thành

#### F1.1 — SLA & Time-to-resolution ✅ Xong
- Entity `OperationSlaPolicy`: `Priority` (Low/Medium/High/Critical) → `MaxApprovalHours`, `MaxResolutionHours`.
- Khi `Submit`: tính `ApprovalDueAt = Submitted + MaxApprovalHours`.
- Khi `Approved`: tính `ResolutionDueAt = Approved + MaxResolutionHours`.
- Background `OperationSlaWatcherService` (HostedService chạy mỗi 15 phút) quét approval sắp quá hạn, quá hạn, resolution quá hạn.
- Bảng `OperationSlaBreach` lưu lịch sử vi phạm.
- **File:** `Services/OperationSlaService.cs`, `Services/OperationSlaWatcherService.cs`, `Models/Entities/OperationSla.cs`

#### F1.2 — Priority-driven queue & color ✅ Xong
- Priority có weight: Critical=4, High=3, Medium=2, Low=1 (enum `PriorityLevel`).
- Trên list mặc định sort theo `(weight DESC, DueDate ASC)`.
- Hiển thị flag/màu rõ ràng.

#### F1.3 — Templates ✅ Xong
- Entity `OperationRequestTemplate`: `Title`, `Type`, `Priority`, `DefaultDepartmentId`, `DefaultLines` (JSON), `Description`, `IsActive`.
- Khi tạo yêu cầu mới: dropdown "Tạo từ template" → autofill.
- **File:** `Models/Entities/OperationRequestTemplate.cs`

#### F1.4 — Progress check-in ✅ Xong
- Entity `OperationProgressLog`: `OperationRequestId`, `ProgressPercent`, `Note`, `CreatedAt`, `CreatedByUserId`.
- Assignee có thể update progress (0–100%) trong giai đoạn InProgress.
- **File:** `Models/Entities/OperationProgressLog.cs`

#### F1.5 — Multi-assignee & watchers ✅ Xong
- Bảng nối `OperationRequestAssignment`: nhiều user cùng phụ trách (`Role`: Primary/Support/Watcher — enum `OperationAssignmentRole`).
- Phân quyền action: Chỉ Primary mới được `StartWork`, `Complete`. Support có thể `AddLine`, `AddComment`. Watcher chỉ xem.
- **File:** `Models/Entities/OperationRequestAssignment.cs`

#### F1.6 — Attachment chính thức ✅ Xong
- Bind entity `Attachment` vào OperationRequest qua `EntityType="OperationRequest", EntityId`.
- UI upload file trên Detail. Validate mime type, max size.
- **File:** `Services/OperationAttachmentService.cs`

#### F1.8 — Comment thông minh ✅ Xong
- Mention `@username` → notify trực tiếp user đó.
- Comment có loại: `Note`, `Question`, `Decision` (enum `OperationCommentType`). UI tô màu.
- Cho phép reply (`ParentCommentId`) để threading.
- **File:** `Models/Entities/OperationComment.cs`

#### Code cleanup ✅ Xong
- Tách `OperationRequestService` thành `OperationRequestService` + `OperationRequestQueryService` (read) + `OperationApprovalService`.
- Chuỗi role lặp lại → constant `OperationRoles.CanCreate`.
- **File:** `Services/OperationRequestQueryService.cs`, `Services/OperationApprovalService.cs`, `Services/OperationRoles.cs`

> ~~F1.7 — Cost variance~~ → **Loại bỏ** (phức tạp, cần liên kết GoodsIssue + Payment, ít giá trị demo).

---

## 2. Module Workflow Kanban — ✅ Gần hoàn thành

### 2.1. Hiện trạng tóm tắt
- WorkItem có column, status, assignment, comment, checklist.
- Có thể link với OperationRequest.
- Kanban column custom (Create/Rename/Delete/Reorder).

### 2.2. Các feature đã hoàn thành

#### F2.1 — WIP limit ✅ Xong
- Column có thêm `WipLimit int?` (null = không giới hạn).
- Khi Move/Create đẩy số thẻ vượt limit → cảnh báo hoặc chặn (cấu hình `WipEnforced bool`).
- Hiển thị `5 / 8` ở header column; vượt thì badge đỏ.
- **File:** `Models/Entities/KanbanColumn.cs`

#### F2.2 — Drag & drop realtime + swimlanes ✅ Xong
- Frontend dùng `SortableJS` cho HTML5 DnD — đã có trong `Views/Workflow/Kanban.cshtml`.
- SignalR hub `KanbanHub` push event `WorkItemMoved` cho tất cả user đang xem board.
- **Cần polish:** Kiểm tra DnD hoạt động đúng, swimlane toggle (by Assignee / Department / Priority).
- **File:** `Hubs/KanbanHub.cs`, `Views/Workflow/Kanban.cshtml`

#### F2.3 — Lead/Cycle time analytics ✅ Xong
- Entity `WorkItemActivity`: `WorkItemId`, `FromColumnId`, `ToColumnId`, `MovedAt`, `MovedByUserId`.
- Tự log mỗi lần Move.
- Tính `LeadTime`, `CycleTime`, `TimeInColumn[col]`.
- **File:** `Models/Entities/WorkItemActivity.cs`

#### F2.4 — Sprint / Iteration ✅ Xong
- Entity `Sprint`: `Name`, `StartDate`, `EndDate`, `Goal`, `Status` (Planned/Active/Closed — enum `SprintStatus`).
- WorkItem có `SprintId?`. Filter Kanban theo sprint.
- **File:** `Models/Entities/Sprint.cs`

#### F2.5 — Dependency ✅ Xong
- Bảng `WorkItemDependency`: `BlockerId`, `BlockedId`, `Type` (BlockedBy/RelatesTo/Duplicates — enum `WorkItemDependencyType`).
- Khi Blocker chưa Done mà ai đó kéo Blocked sang InProgress → cảnh báo.
- Tự động phát hiện circular dependency.
- **File:** `Models/Entities/WorkItemDependency.cs`

#### F2.6 — Checklist nâng cao ✅ Xong
- ChecklistItem có thêm: `AssignedUserId?`, `DueDate?`, `Order`.
- Hiển thị progress `3/7 done` trên card.
- **File:** `Models/Entities/WorkItemChecklist.cs`

#### F2.7 — Search & filter cải tiến ✅ Xong
- Filter: by Assignee, by Priority, by DueDate range.
- Quick filter chips: "Của tôi", "Quá hạn", "Không có người làm".
- Saved view (user lưu cấu hình filter riêng).
- **File:** `Models/Entities/KanbanSavedView.cs`

---

## 3. Module Operation Plans — ✅ Gần hoàn thành

### 3.1. Hiện trạng tóm tắt
- `OperationPlan` (kế hoạch) + nhiều `PlanTask` (task con).
- Task có: assignee, equipment, start/end, status, progress %.
- Conflict check: 1 worker hoặc 1 equipment không được trùng giờ.
- AI Gemini phân tích rủi ro của plan.

### 3.2. Các feature đã hoàn thành

#### F3.1 — Đúng state machine cho Plan ✅ Xong
- States: `Draft → Submitted → Approved → InProgress → Completed | Cancelled` (enum `OperationPlanStatus`).
- Bỏ logic auto-approve trong `CreateTaskAsync`. Thay bằng action `Submit` rõ ràng → `ApprovalTask`.
- Plan ở `Draft` cho phép thêm/sửa task. `Approved` thì task chỉ thay đổi status/progress.
- **File:** `Domain/StateMachines/OperationPlanStateMachine.cs`, `Domain/StateMachines/PlanTaskStateMachine.cs`

#### F3.2 — Baseline & Change Order ✅ Xong
- Khi Plan đầu tiên approved: snapshot `PlanTaskBaseline` (BaselineStart, BaselineEnd, BaselineAssignee).
- Mỗi lần task được edit sau khi Approved → tạo `PlanChangeOrder` với reason, người duyệt (enum `PlanChangeOrderStatus`).
- **File:** `Models/Entities/PlanTaskBaseline.cs`, `Models/Entities/PlanChangeOrder.cs`

#### F3.3 — Dependency & Critical Path Method (CPM) ✅ Xong
- Bảng `PlanTaskDependency(PredecessorId, SuccessorId, Type)` với type FS/SS/FF/SF (enum `PlanTaskDependencyType`).
- Service `CriticalPathCalculator`: tính `EarlyStart`, `EarlyFinish`, `LateStart`, `LateFinish`, `Slack`. Task Slack = 0 → critical → tô đỏ trên Gantt.
- **File:** `Models/Entities/PlanTaskDependency.cs`, `Services/CriticalPathCalculator.cs`

#### F3.4 — Gantt view ✅ Xong
- Trang `/OperationPlans/Gantt/{id}` dùng `frappe-gantt` — đã có view.
- **Cần polish:** Kiểm tra hiển thị đúng, color code (xanh InProgress, vàng Delayed, đỏ Critical Path), drag để update.
- **File:** `Views/OperationPlans/Gantt.cshtml`

#### F3.5 — Resource availability matrix ✅ Xong
- Conflict check mở rộng: check assignee có shift, check leave, check equipment maintenance.
- **File:** `Services/ResourceAvailabilityService.cs`

#### Code cleanup — PlanStatusReconciler ✅ Xong
- Tách logic set `Delayed` khỏi read query, chạy background.
- **File:** `Services/PlanStatusReconcilerService.cs`

> ~~F3.6 — Sinh task từ OperationRequest~~ → **Loại bỏ** (nice-to-have, không cốt lõi).
> ~~F3.7 — OEE cho equipment~~ → **Loại bỏ** (công thức phức tạp, cần data sản xuất thực, vượt scope ĐATN).

---

## 4. Module Resource Management — ✅ Cốt lõi đã xong

### 4.1. Hiện trạng tóm tắt
- Equipment (CRUD + maintenance record).
- WorkShift (định nghĩa ca: tên, giờ bắt đầu/kết thúc, type, hours).
- ShiftAssignment (gán nhân viên vào ca theo từng ngày).
- EmployeeCertificate (chứng chỉ với expiry).
- Workspace (mặt bằng có parent/child).

### 4.2. Feature đã hoàn thành

#### F4.1 — Equipment lifecycle & cost ledger ✅ Xong
- Entity `EquipmentLifecycle`: log mỗi lần status đổi + ghi mỗi khoản chi (enum `EquipmentCostType`: Purchase/Maintenance/Repair/SparePart/Other).
- **File:** `Models/Entities/EquipmentLifecycle.cs`

> Các feature sau đã **loại bỏ** khỏi scope ĐATN:
>
> | Feature | Lý do bỏ |
> |---------|----------|
> | ~~F4.2 — Ca xuyên đêm + overtime~~ | Edge case phức tạp, logic thời gian khó test |
> | ~~F4.3 — Shift swap request~~ | Nice-to-have, không cốt lõi vận hành |
> | ~~F4.4 — Check-in QR + geofence~~ | Cần mobile thực tế, quá nặng |
> | ~~F4.5 — Certificate nâng cao~~ | Logic ràng buộc phức tạp giữa nhiều entity |
> | ~~F4.6 — Workspace booking~~ | Hệ thống booking riêng, vượt scope |
> | ~~F4.7 — Capacity planning dashboard~~ | Cần nhiều data thực, forecast phức tạp |

---

## 5. Module Maintenance — ✅ Hoàn thành

### 5.1. Hiện trạng tóm tắt
- Incident (CM): Open → InProgress → Resolved → Closed.
- PM Schedule: theo Daily/Weekly/Monthly hoặc FrequencyValue ngày.
- SparePart: stock + min stock + adjust.
- Sensor: simulate + show latest.
- AI Gemini cho root cause của incident.

### 5.2. Các feature đã hoàn thành

#### F5.1 — Work Order chuẩn ✅ Xong
- Entity `WorkOrder`: `Code` (WO-YYYY-####), `EquipmentId`, `Type` (enum `WorkOrderType`: Preventive/Corrective/Inspection/Predictive/Emergency), `Priority`, `Status` (enum `WorkOrderStatus`: Open/Assigned/InProgress/OnHold/Completed/Cancelled), `RequestedByUserId`, `AssignedTechnicianId`, `EstimatedHours`, `ActualHours`, `EstimatedCost`, `ActualCost`, `ScheduledStart`, `ScheduledEnd`, `ActualStart`, `ActualEnd`, `WorkDone`, `IncidentId?`, `PmScheduleId?`.
- Khi Resolve Incident → tự sinh WorkOrder. Khi Execute PM → tự sinh WorkOrder.
- **File:** `Models/Entities/WorkOrder.cs`, `Services/WorkOrderService.cs`, `Controllers/WorkOrdersController.cs`, `Domain/StateMachines/WorkOrderStateMachine.cs`

#### F5.2 — Spare Part Usage gắn Work Order ✅ Xong
- WorkOrder entity có spare part lines.
- Khi WorkOrder `Completed` → auto giảm stock + ghi audit + cộng vào ActualCost.

#### F5.3 — Spare Part Requisition ✅ Xong
- Entity `SparePartRequisition`: `Code` (SPR-####), `Lines [{SparePartId, Quantity}]`, `Reason`, `RequestedByUserId`, `Status` (enum `SparePartRequisitionStatus`: Draft/Submitted/Approved/Issued/Rejected/Cancelled), `LinkedWorkOrderId?`.
- Workflow: kỹ thuật viên submit → kho/quản lý duyệt → sinh `GoodsIssue` → confirmed → giảm stock.
- **File:** `Models/Entities/SparePartRequisition.cs`, `Services/SparePartRequisitionService.cs`, `Domain/StateMachines/SparePartRequisitionStateMachine.cs`

#### F5.4 — PM theo điều kiện (Condition-based) ✅ Xong
- `PmSchedule` có field `TriggerType` (enum `PmTriggerType`: TimeBased/RunHoursBased/CyclesBased/ConditionBased).
- Background `PmTriggerService` quét mỗi giờ và đẩy WorkOrder draft.
- **File:** `Services/PmTriggerService.cs`

#### F5.5 — Predictive Maintenance từ Sensor ✅ Xong
- Service `SensorAnomalyDetector`:
  - Tính trung bình động 24h cho mỗi sensor type.
  - Phát hiện trend (linear regression slope) tăng/giảm bất thường.
  - Phát hiện spike (> 3 standard deviations).
- Khi anomaly → tự tạo `MaintenanceIncident` + notify maintenance team.
- API endpoint `/api/sensor/ingest` để thiết bị IoT post lên.
- **File:** `Services/SensorAnomalyDetector.cs`, `Controllers/SensorIngestController.cs`

#### F5.6 — RCA Template & Failure Mode ✅ Xong
- Entity `FailureMode`: `Code`, `Name`, `Category` (enum `FailureModeCategory`: Mechanical/Electrical/Hydraulic/Pneumatic/Software/Human/Environmental/Other), `Description`, `TypicalPreventionMeasure`.
- Incident có `FailureModeId?`. Khi resolve → bắt buộc chọn.
- Trang `/Maintenance/FailureModes/Statistics`: top 10 failure mode.
- **File:** `Models/Entities/FailureMode.cs`, `Services/FailureModeService.cs`, `Controllers/FailureModesController.cs`

> ~~F5.7 — Maintenance KPI dashboard~~ → **Loại bỏ** (cần data lịch sử đủ lâu, khó demo).
> ~~F5.8 — Mobile-friendly checklist~~ → **Loại bỏ** (responsive view riêng, tốn thời gian UI).

---

## 6. Module Approvals — ✅ Hoàn thành

### 6.1. Hiện trạng tóm tắt
- Chuỗi: DEPARTMENT_REVIEW → (optional) EXECUTIVE_REVIEW.
- Approve/Reject/Reassign/ReturnForRevision.
- Áp dụng cho OperationRequest, SalesOrder.

### 6.2. Feature cần triển khai

#### F6.4 — Bulk approve ✅ Xong

**Khối lượng: Nhỏ** — UI checkbox + 1 service method.

- UI list `MyTasks` có checkbox per row + nút "Approve selected" / "Reject selected".
- Service `BulkApproveAsync(List<Guid> taskIds, string? note)` → loop từng task (vẫn check policy/transition cho mỗi cái).
- Trả về `Result` với summary: "Đã duyệt 5/6, 1 lỗi: [task X đã bị huỷ]".
- Audit log cho từng task riêng.

#### F6.6 — Approval timeline visualization ✅ Xong

**Khối lượng: Nhỏ** — Stepper visualization, data đã có sẵn.

- Trên detail của OperationRequest/SalesOrder: hiển thị stepper:
  - ✅ DEPARTMENT_REVIEW — Approved by Nguyễn Văn A (2026-05-23 14:30)
  - ⏳ EXECUTIVE_REVIEW — Pending Trần Thị B
  - ⚪ FINAL_CHECK — Chờ
- Query `ApprovalTask` theo `TargetEntityId` + `TargetEntityType`, sort theo `StepOrder`.
- Cho từng step: hover xem note, decision time, ai delegate (nếu có).

> Các feature sau đã **loại bỏ** khỏi scope ĐATN:
>
> | Feature | Lý do bỏ |
> |---------|----------|
> | ~~F6.1 — ApprovalPolicy động (DB)~~ | Rất phức tạp — phải xây engine đọc condition JSON |
> | ~~F6.2 — Auto-approve rules~~ | Cần rule engine, phức tạp |
> | ~~F6.3 — Delegate (uỷ quyền)~~ | Nice-to-have |
> | ~~F6.5 — Expiration / Auto-escalation~~ | Background job, khó test/demo |
> | ~~F6.7 — Comment trong approval~~ | Trùng với comment đã có ở OperationRequest |

---

## 7. Module Operations Command Center — ✅ Hoàn thành

Thêm 1 trang tổng hợp `/Operations/CommandCenter` (cho EXECUTIVE/TENANT_ADMIN) — như "phòng điều khiển" của vận hành.

#### F7.1 — Tổng quan (KPI cards) ✅ Xong

**Khối lượng: Trung bình** — 1 trang dashboard aggregate, không cần realtime.

- Trang mới: `/Operations/CommandCenter`.
- KPI cards:
  - **Open Requests** — count `OperationRequest` status IN (Submitted, Approved, InProgress)
  - **Critical Incidents** — count `MaintenanceIncident` severity = Critical AND status != Closed
  - **Overdue PM** — count `PmSchedule` where `NextDueDate < Today` AND chưa execute
  - **Equipment in Maintenance** — count `Equipment` status = Maintenance
  - **SLA Breach Today** — count `OperationSlaBreach` createdAt = today
  - **Pending Approvals** — count `ApprovalTask` status = Pending
- Mỗi card hiển thị: icon, số lớn, trend so với hôm qua (↑↓).
- Layout: 2 hàng × 3 cột card, responsive.

#### F7.2 — Heatmap thiết bị ✅ Xong

**Khối lượng: Nhỏ** — Grid đơn giản, color-coded.

- Equipment status grid (mỗi equipment 1 ô, màu theo status, click để xem detail).
  - `Available` → xanh lá
  - `InUse` → xanh dương
  - `Maintenance` → vàng
  - `OutOfOrder` → đỏ
  - `Retired` → xám
- Đặt ngay dưới KPI cards trên trang Command Center.

#### F7.4 — One-click drilldown ✅ Xong

**Khối lượng: Nhỏ** — Click KPI → mở list filter sẵn.

- Mỗi KPI card là link:
  - "Open Requests" → `/Operations?status=InProgress`
  - "Critical Incidents" → `/Maintenance/Incidents?severity=Critical`
  - "Overdue PM" → `/Maintenance/PmSchedules?overdue=true`
  - "Equipment in Maintenance" → `/ResourceManagement/Equipment?status=Maintenance`
  - "SLA Breach Today" → `/Operations?slaBreach=true`
  - "Pending Approvals" → `/Approvals/MyTasks?status=Pending`
- Các list page cần hỗ trợ nhận query param filter.

> Các feature sau đã **loại bỏ** khỏi scope ĐATN:
>
> | Feature | Lý do bỏ |
> |---------|----------|
> | ~~F7.3 — Live activity feed~~ | Phức tạp SignalR, khó demo |
> | ~~F7.5 — AI Daily Brief~~ | Tốn API cost, khó kiểm soát output |

---

## 8. Lộ trình triển khai

> Tổng: **8 feature mới + 2 polish**. Ước lượng **1 sprint (1–2 tuần)**.

### Phase A — Clean-up code ✅ Hoàn thành

- [x] **0.4** — Tạo class `Result<T>` chuẩn (`Models/Common/Result.cs`)
- [x] **0.8** — FluentValidation cho ViewModel chính (`Validators/OperationValidators.cs`)
- [x] **0.7** — Thêm `OperationsHub.cs` + register trong `Program.cs`

### Phase B — Approval + Dashboard ✅ Hoàn thành

- [x] **F6.4** — Bulk approve/reject: `BulkApproveAsync`/`BulkRejectAsync` + controller endpoints
- [x] **F6.6** — Approval timeline stepper (`Views/Shared/_ApprovalTimeline.cshtml`)
- [x] **F7.1** — Trang `/CommandCenter` với 6 KPI cards (`Services/CommandCenterService.cs`)
- [x] **F7.2** — Equipment heatmap grid (color by status, click drilldown)
- [x] **F7.4** — Click KPI → redirect tới list page với filter

### Phase C — Polish ✅ Hoàn thành

- [x] Drag & Drop Kanban + swimlane toggle (**F2.2**) — verified with 4 modes
- [x] Gantt view color code + drag update (**F3.4**) — verified with 5 status colors
- [x] Build verification: `dotnet build` — 0 errors, 3 warnings (pre-existing)

---

## 9. Tiêu chí "Done" và đo lường

Mỗi feature chỉ được coi là Done khi:

1. **Code clean**: theo các nguyên tắc mục 0 (enum, NumberingService, Result, RowVersion, validator).
2. **Service-level test** cho các transition state-machine (chỉ Draft mới Submit được, không có concurrency race).
3. **Audit log** ghi đầy đủ cho mỗi action ghi DB.
4. **Notification** đúng đối tượng (creator, assignee, watcher, manager) — không broadcast bừa.
5. **UI hiển thị action đúng** dựa trên `NextStates(...)` của state machine.
6. **Hiệu suất**: query có pagination, đánh index cho các filter chính.
7. **Tiếng Việt** trong UI, nhưng code identifier dùng tiếng Anh.

---

## 10. Feature đã loại bỏ (tham khảo)

Tổng: **18 feature** bị loại khỏi scope ĐATN.

| Feature | Lý do |
|---------|-------|
| F1.7 — Cost variance | Phức tạp, cần liên kết nhiều module |
| F3.6 — Sinh task từ OperationRequest | Nice-to-have |
| F3.7 — OEE cho equipment | Cần data sản xuất thực |
| F4.2 — Ca xuyên đêm + overtime | Edge case phức tạp |
| F4.3 — Shift swap request | Nice-to-have |
| F4.4 — Check-in QR + geofence | Cần mobile thực tế |
| F4.5 — Certificate nâng cao | Logic ràng buộc phức tạp |
| F4.6 — Workspace booking | Vượt scope |
| F4.7 — Capacity planning dashboard | Cần data thực + forecast |
| F5.7 — Maintenance KPI dashboard | Cần data lịch sử đủ lâu |
| F5.8 — Mobile-friendly checklist | Tốn thời gian UI |
| F6.1 — ApprovalPolicy động | Cần xây rule engine |
| F6.2 — Auto-approve rules | Phức tạp |
| F6.3 — Delegate (uỷ quyền) | Nice-to-have |
| F6.5 — Expiration / Auto-escalation | Khó test/demo |
| F6.7 — Comment trong approval | Trùng với comment đã có |
| F7.3 — Live activity feed | Phức tạp SignalR |
| F7.5 — AI Daily Brief | Tốn API cost |
