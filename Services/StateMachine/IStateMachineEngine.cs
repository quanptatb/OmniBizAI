// PR #23 — transaction boundary
namespace OmniBizAI.Services.StateMachine;

/// <summary>
/// Engine reads StateTransitionConfig from DB to validate and execute state transitions.
/// No hard-coded state codes — all transitions are config-driven per tenant.
/// </summary>
public interface IStateMachineEngine
{
    /// <summary>
    /// Validates and executes a state transition.
    /// </summary>
    /// <param name="stateMachine">State machine name, e.g. "menu_plan", "procurement".</param>
    /// <param name="currentState">Current state code of the entity.</param>
    /// <param name="actionCode">Action code triggering the transition.</param>
    /// <param name="tenantId">Tenant ID for config lookup.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new state code after transition.</returns>
    /// <exception cref="Exceptions.InvalidStateException">
    /// Thrown when the transition is not configured or not allowed.
    /// </exception>
    Task<string> FireAsync(
        string stateMachine,
        string currentState,
        string actionCode,
        Guid tenantId,
        CancellationToken ct = default);
}
