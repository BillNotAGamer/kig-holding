using System.Net.Http.Json;
using KIGHolding.Data;

namespace KIGHolding.Services;

/// <summary>
/// Once a day, pushes the full Meta audience list (see <see cref="MetaAudienceExport"/>)
/// to a Google Apps Script web app that rewrites a Google Sheet on Drive.
/// Disabled unless the LeadSync__WebhookUrl environment variable is set.
/// </summary>
public sealed class MetaAudienceSyncHostedService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan SyncInterval = TimeSpan.FromDays(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetaAudienceSyncHostedService> _logger;

    public MetaAudienceSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MetaAudienceSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var webhookUrl = _configuration["LeadSync:WebhookUrl"];
        if (string.IsNullOrWhiteSpace(webhookUrl) ||
            !Uri.TryCreate(webhookUrl.Trim(), UriKind.Absolute, out var webhookUri) ||
            webhookUri.Scheme != Uri.UriSchemeHttps)
        {
            _logger.LogInformation("Meta audience sync is disabled (LeadSync:WebhookUrl is not set).");
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

        await SyncAsync(webhookUri, stoppingToken);

        using var timer = new PeriodicTimer(SyncInterval);
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

            await SyncAsync(webhookUri, stoppingToken);
        }
    }

    private async Task SyncAsync(Uri webhookUri, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rows = await MetaAudienceExport.BuildRowsAsync(dbContext, cancellationToken);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            var payload = new
            {
                secret = _configuration["LeadSync:Secret"] ?? string.Empty,
                header = MetaAudienceExport.Header,
                rows
            };

            using var response = await client.PostAsJsonAsync(webhookUri, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Meta audience sync pushed {RowCount} rows.", rows.Count);
            }
            else
            {
                _logger.LogWarning("Meta audience sync failed with status {StatusCode}.", (int)response.StatusCode);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Meta audience sync failed.");
        }
    }
}
