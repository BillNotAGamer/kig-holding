namespace KIGHolding.Options;

/// <summary>
/// Daily automatic news: imports new Fanpage posts (rewritten into SEO articles) and,
/// on days without a Fanpage post, publishes one evergreen Korean food/culture article.
/// Railway variables: AutoNews__FacebookPageId, AutoNews__FacebookAccessToken (Page access token).
/// </summary>
public sealed class AutoNewsSettings
{
    public bool Enabled { get; set; } = true;
    public string FacebookPageId { get; set; } = string.Empty;
    public string FacebookAccessToken { get; set; } = string.Empty;
    public string GraphApiVersion { get; set; } = "v21.0";

    /// <summary>Only Fanpage posts newer than this many days are imported.</summary>
    public int LookbackDays { get; set; } = 14;

    /// <summary>Maximum Fanpage posts imported per run.</summary>
    public int MaxPostsPerRun { get; set; } = 5;

    /// <summary>Vietnam hour after which the evergreen fallback article may be published.</summary>
    public int FallbackAfterHour { get; set; } = 10;

    public int RunIntervalHours { get; set; } = 3;

    public bool FacebookConfigured =>
        !string.IsNullOrWhiteSpace(FacebookPageId) && !string.IsNullOrWhiteSpace(FacebookAccessToken);
}
