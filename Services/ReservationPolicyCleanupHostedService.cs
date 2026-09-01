namespace KIGHolding.Services;

public sealed class ReservationPolicyCleanupHostedService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReservationPolicyCleanupHostedService> _logger;

    public ReservationPolicyCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<ReservationPolicyCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(CleanupInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var blockedDateService = scope.ServiceProvider.GetRequiredService<IReservationBlockedDateService>();
            var vietnamToday = VietnamClock.GetVietnamToday(_timeProvider);
            var removed = await blockedDateService.CleanupPastDatesAsync(vietnamToday, cancellationToken);

            if (removed > 0)
            {
                _logger.LogInformation(
                    "Removed {RemovedCount} expired blocked reservation date records before {VietnamToday}.",
                    removed,
                    vietnamToday);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to clean expired blocked reservation date records.");
        }
    }
}
