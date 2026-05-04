// PR #23 — transaction boundary
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Exceptions;
using OmniBizAI.Models.Entities.Approvals;
using OmniBizAI.Models.Entities.Audit;
using OmniBizAI.Models.Entities.Menus;
using OmniBizAI.Services.Authorization;
using OmniBizAI.Services.Dtos;
using OmniBizAI.Services.StateMachine;

namespace OmniBizAI.Services.Approvals;

/// <summary>
/// Internal approval workflow with full transaction boundary.
/// Each mutating method wraps all steps in a single DB transaction.
/// </summary>
public sealed class InternalApprovalService : IInternalApprovalService
{
    private readonly ApplicationDbContext _db;
    private readonly IStateMachineEngine _stateMachine;
    private readonly IUserRoleService _userRoleService;

    public InternalApprovalService(
        ApplicationDbContext db,
        IStateMachineEngine stateMachine,
        IUserRoleService userRoleService)
    {
        _db = db;
        _stateMachine = stateMachine;
        _userRoleService = userRoleService;
    }

    /// <summary>
    /// Returns pending approval steps assigned to any role the user has.
    /// </summary>
    public async Task<IReadOnlyList<InternalApprovalQueueItemDto>> GetQueueAsync(
        string approverUserId, CancellationToken ct = default)
    {
        // Get all role definition IDs this user has
        var userRoleIds = await _db.UserRoleAssignments
            .AsNoTracking()
            .Where(ura => ura.UserId == approverUserId && ura.IsActive)
            .Select(ura => ura.RoleDefinitionId)
            .ToListAsync(ct);

        if (userRoleIds.Count == 0)
            return [];

        var pendingApprovals = await _db.InternalApprovals
            .AsNoTracking()
            .Where(ia => ia.Status == "pending" && userRoleIds.Contains(ia.RequiredRoleDefinitionId))
            .Join(
                _db.MenuPlans.AsNoTracking(),
                ia => ia.MenuPlanId,
                mp => mp.Id,
                (ia, mp) => new InternalApprovalQueueItemDto
                {
                    ApprovalId = ia.Id,
                    MenuPlanId = mp.Id,
                    MenuPlanCode = mp.Code,
                    CustomerName = "", // Would be populated via join to CustomerCompany in full impl
                    ServiceDate = mp.ServiceDate,
                    MealShiftName = "", // Would be populated via join to MealShift in full impl
                    StepName = ia.StepName,
                    RequiredRoleDisplayName = "", // Would be populated via join to RoleDefinition
                    CreatedAt = ia.CreatedAt
                })
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return pendingApprovals;
    }

    /// <summary>
    /// Approves an internal approval step within a single transaction.
    /// If this is the final step, transitions the MenuPlan to internal_approved.
    /// Otherwise, creates the next pending approval step from workflow config.
    /// </summary>
    public async Task ApproveAsync(
        Guid approvalId, string actorUserId, string? comment, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // 1. Load InternalApproval — must be Pending
            var approval = await _db.InternalApprovals
                .FirstOrDefaultAsync(ia => ia.Id == approvalId, ct)
                ?? throw new NotFoundException(nameof(InternalApproval), approvalId);

            if (approval.Status != "pending")
            {
                throw new InvalidStateException(
                    nameof(InternalApproval), approval.Status, "pending");
            }

            // 2. Load MenuPlan
            var menuPlan = await _db.MenuPlans
                .FirstOrDefaultAsync(mp => mp.Id == approval.MenuPlanId, ct)
                ?? throw new NotFoundException(nameof(MenuPlan), approval.MenuPlanId);

            // 3. Check role — user must have the RequiredRoleDefinitionId
            var hasRole = await _userRoleService.HasRoleAsync(
                actorUserId, approval.RequiredRoleDefinitionId, menuPlan.TenantId, ct);

            if (!hasRole)
            {
                throw new ForbiddenException(actorUserId,
                    $"RoleDefinitionId:{approval.RequiredRoleDefinitionId}");
            }

            // 4. Capture old values for audit
            var oldStatus = approval.Status;
            var oldMenuPlanStatus = menuPlan.Status;

            // 5. Mark this approval as Approved
            approval.Status = "approved";
            approval.DecidedByUserId = actorUserId;
            approval.DecidedAt = DateTimeOffset.UtcNow;
            approval.Comment = comment;

            // 6. Check if this is the final step in the workflow
            var workflowSteps = await _db.ApprovalWorkflowConfigs
                .AsNoTracking()
                .Where(awc =>
                    awc.TenantId == menuPlan.TenantId &&
                    awc.WorkflowType == "menu_internal_approval" &&
                    awc.IsActive &&
                    awc.IsRequired)
                .OrderBy(awc => awc.StepNo)
                .ToListAsync(ct);

            var currentStepConfig = workflowSteps
                .FirstOrDefault(s => s.StepNo == approval.SequenceNo);

            var nextStepConfig = workflowSteps
                .FirstOrDefault(s => s.StepNo > approval.SequenceNo);

            if (nextStepConfig is not null)
            {
                // 6a. Not the final step — create next approval step
                var nextApproval = new InternalApproval
                {
                    Id = Guid.NewGuid(),
                    MenuPlanId = menuPlan.Id,
                    SequenceNo = nextStepConfig.StepNo,
                    StepName = nextStepConfig.StepName,
                    RequiredRoleDefinitionId = nextStepConfig.RequiredRoleDefinitionId,
                    IsRequired = nextStepConfig.IsRequired,
                    Status = "pending",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.InternalApprovals.Add(nextApproval);
            }
            else
            {
                // 6b. Final step — transition MenuPlan to internal_approved
                var newState = await _stateMachine.FireAsync(
                    "menu_plan", menuPlan.Status, "internal_approve", menuPlan.TenantId, ct);
                menuPlan.Status = newState;
                menuPlan.InternalApprovedAt = DateTimeOffset.UtcNow;
                menuPlan.UpdatedAt = DateTimeOffset.UtcNow;
                menuPlan.UpdatedByUserId = actorUserId;
            }

            // 7. Write ApprovalTimeline
            _db.ApprovalTimelines.Add(new ApprovalTimeline
            {
                Id = Guid.NewGuid(),
                EntityType = nameof(MenuPlan),
                EntityId = menuPlan.Id,
                Action = "internal_approve",
                ActorType = "Internal",
                ActorName = actorUserId,
                Comment = comment,
                CreatedAt = DateTimeOffset.UtcNow
            });

            // 8. Write AuditLog
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = menuPlan.TenantId,
                UserId = actorUserId,
                Action = "internal_approve",
                EntityType = nameof(MenuPlan),
                EntityId = menuPlan.Id,
                OldValuesJson = JsonSerializer.Serialize(new
                {
                    ApprovalStatus = oldStatus,
                    MenuPlanStatus = oldMenuPlanStatus
                }),
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    ApprovalStatus = approval.Status,
                    MenuPlanStatus = menuPlan.Status
                }),
                CreatedAt = DateTimeOffset.UtcNow
            });

            // 9. SaveChanges + Commit
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await tx.RollbackAsync(ct);
            throw new ConcurrencyConflictException(nameof(MenuPlan), approvalId.ToString(), ex);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Requests changes on an internal approval step within a single transaction.
    /// Cancels all other pending approval steps for this menu plan.
    /// Creates a MenuPlanRevision snapshot and increments RevisionNo.
    /// </summary>
    public async Task RequestChangeAsync(
        Guid approvalId, string actorUserId, string comment, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // 1. Load InternalApproval — must be Pending
            var approval = await _db.InternalApprovals
                .FirstOrDefaultAsync(ia => ia.Id == approvalId, ct)
                ?? throw new NotFoundException(nameof(InternalApproval), approvalId);

            if (approval.Status != "pending")
            {
                throw new InvalidStateException(
                    nameof(InternalApproval), approval.Status, "pending");
            }

            // 2. Load MenuPlan
            var menuPlan = await _db.MenuPlans
                .Include(mp => mp.Items)
                .FirstOrDefaultAsync(mp => mp.Id == approval.MenuPlanId, ct)
                ?? throw new NotFoundException(nameof(MenuPlan), approval.MenuPlanId);

            // 3. Check role
            var hasRole = await _userRoleService.HasRoleAsync(
                actorUserId, approval.RequiredRoleDefinitionId, menuPlan.TenantId, ct);

            if (!hasRole)
            {
                throw new ForbiddenException(actorUserId,
                    $"RoleDefinitionId:{approval.RequiredRoleDefinitionId}");
            }

            // 4. Capture old values
            var oldApprovalStatus = approval.Status;
            var oldMenuPlanStatus = menuPlan.Status;
            var oldRevisionNo = menuPlan.RevisionNo;

            // 5. Set approval to ChangeRequested
            approval.Status = "change_requested";
            approval.Comment = comment;
            approval.DecidedByUserId = actorUserId;
            approval.DecidedAt = DateTimeOffset.UtcNow;

            // 6. Cancel all other Pending approvals for this menu
            var otherPending = await _db.InternalApprovals
                .Where(ia => ia.MenuPlanId == menuPlan.Id &&
                             ia.Id != approvalId &&
                             ia.Status == "pending")
                .ToListAsync(ct);

            foreach (var other in otherPending)
            {
                other.Status = "cancelled";
            }

            // 7. Transition MenuPlan status via state machine
            var newState = await _stateMachine.FireAsync(
                "menu_plan", menuPlan.Status, "request_change_internal", menuPlan.TenantId, ct);
            menuPlan.Status = newState;
            menuPlan.UpdatedAt = DateTimeOffset.UtcNow;
            menuPlan.UpdatedByUserId = actorUserId;

            // 8. Increment RevisionNo and create snapshot
            menuPlan.RevisionNo++;
            var snapshot = new MenuPlanRevision
            {
                Id = Guid.NewGuid(),
                MenuPlanId = menuPlan.Id,
                RevisionNo = menuPlan.RevisionNo,
                Reason = comment,
                SnapshotJson = JsonSerializer.Serialize(new
                {
                    menuPlan.Id,
                    menuPlan.Code,
                    menuPlan.Status,
                    menuPlan.ServiceDate,
                    Items = menuPlan.Items.Select(i => new
                    {
                        i.MealSlotDefinitionId,
                        i.DishId,
                        i.SortOrder
                    })
                }),
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.MenuPlanRevisions.Add(snapshot);

            // 9. Write ApprovalTimeline
            _db.ApprovalTimelines.Add(new ApprovalTimeline
            {
                Id = Guid.NewGuid(),
                EntityType = nameof(MenuPlan),
                EntityId = menuPlan.Id,
                Action = "request_change_internal",
                ActorType = "Internal",
                ActorName = actorUserId,
                Comment = comment,
                CreatedAt = DateTimeOffset.UtcNow
            });

            // 10. Write AuditLog
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = menuPlan.TenantId,
                UserId = actorUserId,
                Action = "internal_request_change",
                EntityType = nameof(MenuPlan),
                EntityId = menuPlan.Id,
                OldValuesJson = JsonSerializer.Serialize(new
                {
                    ApprovalStatus = oldApprovalStatus,
                    MenuPlanStatus = oldMenuPlanStatus,
                    RevisionNo = oldRevisionNo
                }),
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    ApprovalStatus = approval.Status,
                    MenuPlanStatus = menuPlan.Status,
                    RevisionNo = menuPlan.RevisionNo
                }),
                CreatedAt = DateTimeOffset.UtcNow
            });

            // 11. SaveChanges + Commit
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await tx.RollbackAsync(ct);
            throw new ConcurrencyConflictException(nameof(MenuPlan), approvalId.ToString(), ex);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
