// PR #23 — transaction boundary
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Exceptions;
using OmniBizAI.Models.Entities.Approvals;
using OmniBizAI.Models.Entities.Audit;
using OmniBizAI.Models.Entities.Menus;
using OmniBizAI.Models.Entities.Quantities;
using OmniBizAI.Services.Dtos;
using OmniBizAI.Services.Quantities;
using OmniBizAI.Services.StateMachine;

namespace OmniBizAI.Services.Approvals;

/// <summary>
/// Customer approval via email token link with full transaction boundary.
/// SubmitAsync handles both "approve" and "request_change" flows in a single transaction.
/// </summary>
public sealed class CustomerApprovalService : ICustomerApprovalService
{
    private readonly ApplicationDbContext _db;
    private readonly IStateMachineEngine _stateMachine;
    private readonly IQuantityResolutionService _quantityResolver;

    public CustomerApprovalService(
        ApplicationDbContext db,
        IStateMachineEngine stateMachine,
        IQuantityResolutionService quantityResolver)
    {
        _db = db;
        _stateMachine = stateMachine;
        _quantityResolver = quantityResolver;
    }

    /// <summary>
    /// Stub — sending email is out of scope for this PR.
    /// </summary>
    public Task SendAsync(Guid menuPlanId, Guid customerContactId, string actorUserId,
        CancellationToken ct = default)
    {
        throw new NotImplementedException("SendAsync will be implemented in a separate PR.");
    }

    /// <summary>
    /// Stub — review page is out of scope for this PR.
    /// </summary>
    public Task<CustomerApprovalReviewViewModel> GetReviewAsync(string token,
        CancellationToken ct = default)
    {
        throw new NotImplementedException("GetReviewAsync will be implemented in a separate PR.");
    }

    /// <summary>
    /// Handles customer approval submission from email link.
    /// All steps run in a single transaction — if any step fails, everything rolls back.
    /// 
    /// Two branches:
    /// - "request_change": save comment, transition to customer_change_requested, mark token used.
    /// - "approve": transition to customer_approved, resolve quantity, then either:
    ///   - If quantity resolves: upsert DailyMealOrder, create QuantitySubmission, transition to quantity_confirmed.
    ///   - If fallback missing: transition to quantity_open, create Notification for CS/Ops.
    /// </summary>
    public async Task SubmitAsync(CustomerApprovalSubmitRequest request, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow;

            // ── Step 1: Validate token ──────────────────────────────────────
            var token = await _db.CustomerApprovalTokens
                .FirstOrDefaultAsync(t => t.TokenHash == request.Token, ct)
                ?? throw new TokenExpiredException("Token not found.");

            if (token.Status != "pending")
            {
                throw new TokenExpiredException($"Token has already been used (status: {token.Status}).");
            }

            if (token.ExpiresAt <= now)
            {
                throw new TokenExpiredException($"Token expired at {token.ExpiresAt}.");
            }

            // ── Step 2: Load MenuPlan — must be SentToCustomer ──────────────
            var menuPlan = await _db.MenuPlans
                .FirstOrDefaultAsync(mp => mp.Id == token.MenuPlanId, ct)
                ?? throw new NotFoundException(nameof(MenuPlan), token.MenuPlanId);

            if (menuPlan.Status != "sent_to_customer")
            {
                throw new InvalidStateException(
                    nameof(MenuPlan), menuPlan.Status, "sent_to_customer");
            }

            // Capture old values for audit
            var oldMenuPlanStatus = menuPlan.Status;

            // Load customer contact name for timeline
            var contactName = await _db.CustomerContacts
                .AsNoTracking()
                .Where(cc => cc.Id == token.CustomerContactId)
                .Select(cc => cc.FullName)
                .FirstOrDefaultAsync(ct) ?? "Customer";

            // ── Branch: request_change ──────────────────────────────────────
            if (request.DecisionActionCode == "request_change")
            {
                // Save comment on token
                token.Comment = request.Comment;

                // Transition MenuPlan via state machine
                var newState = await _stateMachine.FireAsync(
                    "menu_plan", menuPlan.Status, "request_change_customer",
                    menuPlan.TenantId, ct);
                menuPlan.Status = newState;
                menuPlan.UpdatedAt = now;

                // Write ApprovalTimeline
                _db.ApprovalTimelines.Add(new ApprovalTimeline
                {
                    Id = Guid.NewGuid(),
                    EntityType = nameof(MenuPlan),
                    EntityId = menuPlan.Id,
                    Action = "request_change",
                    ActorType = "Customer",
                    ActorName = contactName,
                    Comment = request.Comment,
                    CreatedAt = now
                });

                // Write AuditLog
                _db.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = menuPlan.TenantId,
                    UserId = null, // Customer — no internal user
                    Action = "customer_request_change",
                    EntityType = nameof(MenuPlan),
                    EntityId = menuPlan.Id,
                    OldValuesJson = JsonSerializer.Serialize(new { Status = oldMenuPlanStatus }),
                    NewValuesJson = JsonSerializer.Serialize(new
                    {
                        Status = menuPlan.Status,
                        Comment = request.Comment
                    }),
                    CreatedAt = now
                });

                // Mark token used
                token.Status = "used";
                token.UsedAt = now;
                token.SubmittedAt = now;
            }
            // ── Branch: approve ─────────────────────────────────────────────
            else if (request.DecisionActionCode == "approve")
            {
                // Transition to customer_approved
                var approvedState = await _stateMachine.FireAsync(
                    "menu_plan", menuPlan.Status, "customer_approve",
                    menuPlan.TenantId, ct);
                menuPlan.Status = approvedState;
                menuPlan.CustomerApprovedAt = now;

                // Resolve quantity
                var resolveRequest = new ResolveQuantityRequest
                {
                    MenuPlanId = menuPlan.Id,
                    ExpectedQtyInput = request.ExpectedQtyInput,
                    FinalQtyInput = request.FinalQtyInput,
                    ExtraInputQty = request.ExtraInputQty,
                    ExtraModeCode = request.ExtraModeCode
                };

                var resolved = await _quantityResolver.ResolveAsync(resolveRequest, ct);

                if (resolved is not null && resolved.TotalCookingQty > 0)
                {
                    // ── Resolve succeeded — upsert DailyMealOrder ───────────
                    var existingOrder = await _db.DailyMealOrders
                        .FirstOrDefaultAsync(dmo => dmo.MenuPlanId == menuPlan.Id, ct);

                    if (existingOrder is not null)
                    {
                        existingOrder.ExpectedQty = resolved.ExpectedQty;
                        existingOrder.ExpectedQtySourceCode = resolved.ExpectedQtySourceCode;
                        existingOrder.FinalQty = resolved.FinalQty;
                        existingOrder.FinalQtySourceCode = resolved.FinalQtySourceCode;
                        existingOrder.ExtraInputQty = resolved.ExtraInputQty;
                        existingOrder.ExtraModeCode = resolved.ExtraModeCode;
                        existingOrder.TotalCookingQty = resolved.TotalCookingQty;
                        existingOrder.IsLocked = true;
                        existingOrder.UpdatedAt = now;
                    }
                    else
                    {
                        var newOrder = new DailyMealOrder
                        {
                            Id = Guid.NewGuid(),
                            TenantId = menuPlan.TenantId,
                            MenuPlanId = menuPlan.Id,
                            CustomerCompanyId = menuPlan.CustomerCompanyId,
                            ServiceContractId = menuPlan.ServiceContractId,
                            DeliveryLocationId = menuPlan.DeliveryLocationId,
                            ServiceDate = menuPlan.ServiceDate,
                            MealShiftId = menuPlan.MealShiftId,
                            MealTypeId = menuPlan.MealTypeId,
                            ExpectedQty = resolved.ExpectedQty,
                            ExpectedQtySourceCode = resolved.ExpectedQtySourceCode,
                            FinalQty = resolved.FinalQty,
                            FinalQtySourceCode = resolved.FinalQtySourceCode,
                            ExtraInputQty = resolved.ExtraInputQty,
                            ExtraModeCode = resolved.ExtraModeCode,
                            TotalCookingQty = resolved.TotalCookingQty,
                            IsLocked = true,
                            CreatedAt = now
                        };
                        _db.DailyMealOrders.Add(newOrder);
                        existingOrder = newOrder;
                    }

                    // Create QuantitySubmission — source: customer_email
                    _db.QuantitySubmissions.Add(new QuantitySubmission
                    {
                        Id = Guid.NewGuid(),
                        DailyMealOrderId = existingOrder.Id,
                        SubmittedByName = contactName,
                        SubmittedByEmail = token.SentToEmail,
                        SourceCode = "customer_email",
                        ExpectedQtyInput = request.ExpectedQtyInput,
                        FinalQtyInput = request.FinalQtyInput,
                        ExtraQtyInput = request.ExtraInputQty,
                        ExtraModeCode = request.ExtraModeCode,
                        CreatedAt = now
                    });

                    // Transition to quantity_confirmed
                    var confirmedState = await _stateMachine.FireAsync(
                        "menu_plan", menuPlan.Status, "quantity_confirm",
                        menuPlan.TenantId, ct);
                    menuPlan.Status = confirmedState;
                }
                else
                {
                    // ── Fallback missing — transition to quantity_open ───────
                    var openState = await _stateMachine.FireAsync(
                        "menu_plan", menuPlan.Status, "quantity_open",
                        menuPlan.TenantId, ct);
                    menuPlan.Status = openState;

                    // Create Notification for CS/Ops
                    _db.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = "", // Will be picked up by CS/Ops role notification system
                        Title = "Cần nhập số lượng thủ công",
                        Message = $"Cần nhập số lượng thủ công cho menu [{menuPlan.Code}] " +
                                  $"ngày {menuPlan.ServiceDate}. Thiếu dữ liệu fallback bắt buộc.",
                        EntityType = nameof(MenuPlan),
                        EntityId = menuPlan.Id,
                        IsRead = false,
                        CreatedAt = now
                    });
                }

                menuPlan.UpdatedAt = now;

                // Write ApprovalTimeline
                _db.ApprovalTimelines.Add(new ApprovalTimeline
                {
                    Id = Guid.NewGuid(),
                    EntityType = nameof(MenuPlan),
                    EntityId = menuPlan.Id,
                    Action = "approve",
                    ActorType = "Customer",
                    ActorName = contactName,
                    Comment = request.Comment,
                    CreatedAt = now
                });

                // Write AuditLog
                _db.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = menuPlan.TenantId,
                    UserId = null,
                    Action = "customer_approve",
                    EntityType = nameof(MenuPlan),
                    EntityId = menuPlan.Id,
                    OldValuesJson = JsonSerializer.Serialize(new { Status = oldMenuPlanStatus }),
                    NewValuesJson = JsonSerializer.Serialize(new { Status = menuPlan.Status }),
                    CreatedAt = now
                });

                // Mark token used
                token.Status = "used";
                token.UsedAt = now;
                token.SubmittedAt = now;
            }
            else
            {
                throw new InvalidStateException(
                    $"Unknown DecisionActionCode: '{request.DecisionActionCode}'. " +
                    "Expected 'approve' or 'request_change'.");
            }

            // ── Final: SaveChanges + Commit ─────────────────────────────────
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await tx.RollbackAsync(ct);
            throw new ConcurrencyConflictException(nameof(MenuPlan), request.Token, ex);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
