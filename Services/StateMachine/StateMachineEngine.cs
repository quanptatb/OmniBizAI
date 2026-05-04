// PR #23 — transaction boundary
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Exceptions;

namespace OmniBizAI.Services.StateMachine;

/// <summary>
/// Reads StateTransitionConfig from DB. Validates that a transition exists
/// for the given (StateMachine, FromState, ActionCode, TenantId).
/// Returns ToStateCode if found; throws InvalidStateException otherwise.
/// </summary>
public sealed class StateMachineEngine : IStateMachineEngine
{
    private readonly ApplicationDbContext _db;

    public StateMachineEngine(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> FireAsync(
        string stateMachine,
        string currentState,
        string actionCode,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var transition = await _db.StateTransitionConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.TenantId == tenantId &&
                t.StateMachine == stateMachine &&
                t.FromStateCode == currentState &&
                t.ActionCode == actionCode &&
                t.IsActive,
                ct);

        if (transition is null)
        {
            throw new InvalidStateException(
                $"No active transition found for state machine '{stateMachine}': " +
                $"'{currentState}' --[{actionCode}]--> ??? (tenant: {tenantId}).");
        }

        return transition.ToStateCode;
    }
}
