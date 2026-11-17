using KIGHolding.Options;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services.AutoNews;

/// <summary>Runs <see cref="AutoNewsPublisher"/> a few minutes after startup and then every few hours.</summary>
public sealed class AutoNewsHostedService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AutoNewsSettings _settings;
    private readonly ILogger<AutoNewsHostedService> _logger;

    public AutoNewsHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<AutoNewsSettings> settings,
        ILogger<AutoNewsHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Auto news is disabled (AutoNews:Enabled=false).");
            return;
        }

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var interval = TimeSpan.FromHours(Math.Clamp(_settings.RunIntervalHours, 1, 24));
        using var timer = new PeriodicTimer(interval);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<AutoNewsPublisher>();
            await publisher.RunAsync(manual: false, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Auto news run failed.");
        }
    }
}
