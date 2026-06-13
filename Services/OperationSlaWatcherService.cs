using System.Threading.Channels;

namespace OmniBizAI.Services;

public sealed record OperationSlaWatcherSignal(string Reason, DateTimeOffset QueuedAt);

public interface IOperationSlaWatcherQueue
{
    bool TryQueue(string reason);
    IAsyncEnumerable<OperationSlaWatcherSignal> ReadAllAsync(CancellationToken cancellationToken);
}

public sealed class OperationSlaWatcherQueue : IOperationSlaWatcherQueue
{
    private readonly Channel<OperationSlaWatcherSignal> channel = Channel.CreateBounded<OperationSlaWatcherSignal>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public bool TryQueue(string reason) =>
        channel.Writer.TryWrite(new OperationSlaWatcherSignal(reason, DateTimeOffset.UtcNow));

    public IAsyncEnumerable<OperationSlaWatcherSignal> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class OperationSlaWatcherService(
    IServiceScopeFactory scopeFactory,
    IOperationSlaWatcherQueue queue,
    ILogger<OperationSlaWatcherService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim runGate = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCheckAsync("startup", stoppingToken);

        var periodicTask = RunPeriodicAsync(stoppingToken);
        var queuedTask = RunQueuedAsync(stoppingToken);

        try
        {
            await Task.WhenAll(periodicTask, queuedTask);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown path.
        }
    }

    private async Task RunPeriodicAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCheckAsync("schedule", stoppingToken);
        }
    }

    private async Task RunQueuedAsync(CancellationToken stoppingToken)
    {
        await foreach (var signal in queue.ReadAllAsync(stoppingToken))
        {
            await RunCheckAsync(signal.Reason, stoppingToken);
        }
    }

    private async Task RunCheckAsync(string trigger, CancellationToken cancellationToken)
    {
        if (!await runGate.WaitAsync(0, cancellationToken))
        {
            logger.LogDebug("Operation SLA watcher skipped {Trigger} because another run is active.", trigger);
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var slaService = scope.ServiceProvider.GetRequiredService<IOperationSlaService>();
            var trackedCount = await slaService.CheckBreachesAsync(cancellationToken);

            if (trackedCount > 0)
            {
                logger.LogInformation(
                    "Operation SLA watcher tracked {TrackedCount} new SLA events from {Trigger}.",
                    trackedCount,
                    trigger);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown path.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Operation SLA watcher failed from {Trigger}.", trigger);
        }
        finally
        {
            runGate.Release();
        }
    }
}
