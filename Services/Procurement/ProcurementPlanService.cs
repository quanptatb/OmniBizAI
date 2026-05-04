// PR #23 — transaction boundary
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Exceptions;
using OmniBizAI.Models.Entities.Audit;
using OmniBizAI.Models.Entities.Menus;
using OmniBizAI.Models.Entities.Procurement;
using OmniBizAI.Services.Dtos;
using OmniBizAI.Services.StateMachine;

namespace OmniBizAI.Services.Procurement;

/// <summary>
/// Procurement plan (giấy đi chợ) service with full transaction boundary for IssueAsync.
/// </summary>
public sealed class ProcurementPlanService : IProcurementPlanService
{
    private readonly ApplicationDbContext _db;
    private readonly IStateMachineEngine _stateMachine;

    public ProcurementPlanService(
        ApplicationDbContext db,
        IStateMachineEngine stateMachine)
    {
        _db = db;
        _stateMachine = stateMachine;
    }

    /// <summary>
    /// Stub — preview BOM is out of scope for this PR.
    /// </summary>
    public Task<IReadOnlyList<ProcurementLineDto>> PreviewAsync(
        GenerateProcurementPlanRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("PreviewAsync will be implemented in a separate PR.");
    }

    /// <summary>
    /// Stub — generate procurement plan is out of scope for this PR.
    /// </summary>
    public Task<ProcurementPlan> GenerateAsync(
        GenerateProcurementPlanRequest request, string actorUserId, CancellationToken ct = default)
    {
        throw new NotImplementedException("GenerateAsync will be implemented in a separate PR.");
    }

    /// <summary>
    /// Issues a procurement plan within a single transaction.
    /// 
    /// Validations:
    /// - ProcurementPlan must be in Draft status.
    /// - All linked MenuPlans must be in quantity_confirmed or bom_calculated.
    /// - All dishes in linked menus must have complete BOM (no missing DishBomItems).
    /// 
    /// On success:
    /// - Sets ProcurementPlan to Issued.
    /// - Transitions all linked MenuPlans to procurement_issued.
    /// - Writes AuditLog for ProcurementPlan and each MenuPlan.
    /// </summary>
    public async Task IssueAsync(Guid procurementPlanId, string actorUserId,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow;

            // ── Step 1: Load ProcurementPlan — must be Draft ────────────────
            var plan = await _db.ProcurementPlans
                .FirstOrDefaultAsync(pp => pp.Id == procurementPlanId, ct)
                ?? throw new NotFoundException(nameof(ProcurementPlan), procurementPlanId);

            if (plan.Status != "draft")
            {
                throw new InvalidStateException(
                    nameof(ProcurementPlan), plan.Status, "draft");
            }

            var oldPlanStatus = plan.Status;

            // ── Step 2: Load all linked MenuPlans ───────────────────────────
            var linkedMenuPlanIds = await _db.ProcurementPlanMenus
                .AsNoTracking()
                .Where(ppm => ppm.ProcurementPlanId == procurementPlanId)
                .Select(ppm => ppm.MenuPlanId)
                .ToListAsync(ct);

            if (linkedMenuPlanIds.Count == 0)
            {
                throw new ValidationFailedException(
                    "Procurement plan has no linked menu plans.");
            }

            var menuPlans = await _db.MenuPlans
                .Where(mp => linkedMenuPlanIds.Contains(mp.Id))
                .ToListAsync(ct);

            // ── Step 2a: Validate MenuPlan statuses ─────────────────────────
            var validStatuses = new HashSet<string> { "quantity_confirmed", "bom_calculated" };
            var invalidMenus = menuPlans
                .Where(mp => !validStatuses.Contains(mp.Status))
                .ToList();

            var errors = new List<string>();

            if (invalidMenus.Count > 0)
            {
                foreach (var mp in invalidMenus)
                {
                    errors.Add(
                        $"MenuPlan [{mp.Code}] is in status '{mp.Status}', " +
                        $"expected 'quantity_confirmed' or 'bom_calculated'.");
                }
            }

            // ── Step 2b: Validate BOM completeness ──────────────────────────
            // Load all dishes referenced by linked menu plans
            var allMenuPlanItems = await _db.MenuPlanItems
                .AsNoTracking()
                .Where(mpi => linkedMenuPlanIds.Contains(mpi.MenuPlanId))
                .ToListAsync(ct);

            var allDishIds = allMenuPlanItems
                .Select(mpi => mpi.DishId)
                .Distinct()
                .ToList();

            // Find dishes that have at least one BOM item
            var dishesWithBom = await _db.DishBomItems
                .AsNoTracking()
                .Where(dbi => allDishIds.Contains(dbi.DishId))
                .Select(dbi => dbi.DishId)
                .Distinct()
                .ToListAsync(ct);

            var dishesWithBomSet = new HashSet<Guid>(dishesWithBom);
            var dishesWithoutBom = allDishIds
                .Where(d => !dishesWithBomSet.Contains(d))
                .ToList();

            if (dishesWithoutBom.Count > 0)
            {
                // Find which menu plans reference these dishes
                var missingBomDetails = allMenuPlanItems
                    .Where(mpi => dishesWithoutBom.Contains(mpi.DishId))
                    .GroupBy(mpi => mpi.MenuPlanId)
                    .Select(g =>
                    {
                        var mp = menuPlans.FirstOrDefault(m => m.Id == g.Key);
                        var dishCount = g.Select(i => i.DishId).Distinct().Count();
                        return $"MenuPlan [{mp?.Code ?? g.Key.ToString()}] has {dishCount} dish(es) without BOM.";
                    });

                errors.AddRange(missingBomDetails);
            }

            if (errors.Count > 0)
            {
                throw new ValidationFailedException(errors);
            }

            // ── Step 3: Set ProcurementPlan to Issued ───────────────────────
            plan.Status = "issued";
            plan.UpdatedAt = now;

            // ── Step 4: Transition each linked MenuPlan ─────────────────────
            foreach (var mp in menuPlans)
            {
                var oldMpStatus = mp.Status;
                var newMpStatus = await _stateMachine.FireAsync(
                    "menu_plan", mp.Status, "procurement_issue",
                    mp.TenantId, ct);
                mp.Status = newMpStatus;
                mp.UpdatedAt = now;
                mp.UpdatedByUserId = actorUserId;

                // AuditLog for each MenuPlan
                _db.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = mp.TenantId,
                    UserId = actorUserId,
                    Action = "procurement_issued",
                    EntityType = nameof(MenuPlan),
                    EntityId = mp.Id,
                    OldValuesJson = JsonSerializer.Serialize(new { Status = oldMpStatus }),
                    NewValuesJson = JsonSerializer.Serialize(new { Status = mp.Status }),
                    CreatedAt = now
                });
            }

            // ── Step 5: AuditLog for ProcurementPlan ────────────────────────
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = plan.TenantId,
                UserId = actorUserId,
                Action = "issue",
                EntityType = nameof(ProcurementPlan),
                EntityId = plan.Id,
                OldValuesJson = JsonSerializer.Serialize(new { Status = oldPlanStatus }),
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    Status = plan.Status,
                    MenuPlanCount = menuPlans.Count
                }),
                CreatedAt = now
            });

            // ── Step 6: SaveChanges + Commit ────────────────────────────────
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await tx.RollbackAsync(ct);
            throw new ConcurrencyConflictException(
                nameof(ProcurementPlan), procurementPlanId.ToString(), ex);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
