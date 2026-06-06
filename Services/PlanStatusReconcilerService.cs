namespace OmniBizAI.Services;

public sealed class PlanStatusReconcilerService(
    IServiceScopeFactory scopeFactory,
    ILogger<PlanStatusReconcilerService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim runGate = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAsync("startup", stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunAsync("schedule", stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown path.
        }
    }

    private async Task RunAsync(string trigger, CancellationToken cancellationToken)
    {
        if (!await runGate.WaitAsync(0, cancellationToken))
        {
            logger.LogDebug("Plan status reconciler skipped {Trigger} because another run is active.", trigger);
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var operationPlans = scope.ServiceProvider.GetRequiredService<OperationPlanService>();
            var delayedCount = await operationPlans.ReconcileDelayedTasksAsync(cancellationToken);

            if (delayedCount > 0)
            {
                logger.LogInformation(
                    "Plan status reconciler marked {DelayedCount} plan tasks as delayed from {Trigger}.",
                    delayedCount,
                    trigger);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown path.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Plan status reconciler failed from {Trigger}.", trigger);
        }
        finally
        {
            runGate.Release();
        }
    }
}
