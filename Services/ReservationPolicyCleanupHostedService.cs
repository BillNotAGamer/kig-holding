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
            var branchPolicyService = scope.ServiceProvider.GetRequiredService<IBranchReservationDatePolicyService>();
            var vietnamToday = VietnamClock.GetVietnamToday(_timeProvider);
            var removedGlobal = await blockedDateService.CleanupPastDatesAsync(vietnamToday, cancellationToken);
            var removedBranch = await branchPolicyService.CleanupPastPoliciesAsync(vietnamToday, cancellationToken);

            if (removedGlobal > 0 || removedBranch > 0)
            {
                _logger.LogInformation(
                    "Removed {GlobalRemovedCount} expired global and {BranchRemovedCount} expired branch reservation policy records before {VietnamToday}.",
                    removedGlobal,
                    removedBranch,
                    vietnamToday);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to clean expired reservation policy records.");
        }
    }
}
