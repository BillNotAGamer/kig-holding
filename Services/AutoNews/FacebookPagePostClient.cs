using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using KIGHolding.Options;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services.AutoNews;

public sealed record FacebookPagePost(
    string Id,
    string Message,
    DateTimeOffset CreatedTime,
    string? PictureUrl,
    string? PermalinkUrl);

/// <summary>Reads recent posts of the brand Fanpage through the Facebook Graph API.</summary>
public sealed class FacebookPagePostClient
{
    private const long MaxImageBytes = 8L * 1024 * 1024;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AutoNewsSettings _settings;
    private readonly ILogger<FacebookPagePostClient> _logger;

    public FacebookPagePostClient(
        IHttpClientFactory httpClientFactory,
        IOptions<AutoNewsSettings> settings,
        ILogger<FacebookPagePostClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsConfigured => _settings.FacebookConfigured;

    public async Task<IReadOnlyList<FacebookPagePost>> GetRecentPostsAsync(int limit, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var version = string.IsNullOrWhiteSpace(_settings.GraphApiVersion) ? "v21.0" : _settings.GraphApiVersion.Trim();
        var url = $"https://graph.facebook.com/{version}/{Uri.EscapeDataString(_settings.FacebookPageId.Trim())}/posts" +
                  $"?fields=id,message,created_time,full_picture,permalink_url&limit={Math.Clamp(limit, 1, 50)}" +
                  $"&access_token={Uri.EscapeDataString(_settings.FacebookAccessToken.Trim())}";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            using var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // Never log the URL: it contains the access token.
                _logger.LogWarning("Facebook Graph API returned {StatusCode}: {Body}", (int)response.StatusCode, Truncate(body, 300));
                return [];
            }

            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var posts = new List<FacebookPagePost>();
            foreach (var item in data.EnumerateArray())
            {
                var id = GetString(item, "id");
                var message = GetString(item, "message");
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                posts.Add(new FacebookPagePost(
                    id,
                    message,
                    ParseFacebookTime(GetString(item, "created_time")),
                    NullIfEmpty(GetString(item, "full_picture")),
                    NullIfEmpty(GetString(item, "permalink_url"))));
            }

            return posts;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not read Fanpage posts.");
            return [];
        }
    }

    public async Task<byte[]?> DownloadImageAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            if (response.Content.Headers.ContentLength is > MaxImageBytes)
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return bytes.Length is > 0 and <= (int)MaxImageBytes ? bytes : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not download Fanpage image.");
            return null;
        }
    }

    private static DateTimeOffset ParseFacebookTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTimeOffset.UtcNow;
        }

        // Graph API format: 2026-11-17T03:00:00+0000 -> add the colon in the offset.
        var normalized = Regex.Replace(value.Trim(), @"([+-]\d{2})(\d{2})$", "$1:$2");
        return DateTimeOffset.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;
    }

    private static string? GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
