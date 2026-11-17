using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services.AutoNews;

public sealed record AutoNewsRunResult(int ImportedFromFacebook, string? EvergreenTitle, bool FacebookConfigured, string Message);

/// <summary>
/// Publishes news automatically: new Fanpage posts (rewritten for SEO) and, when nothing
/// was published today, one evergreen Korean food/culture article.
/// </summary>
public sealed class AutoNewsPublisher
{
    private static readonly SemaphoreSlim RunLock = new(1, 1);

    private readonly AppDbContext _dbContext;
    private readonly FacebookPagePostClient _facebookClient;
    private readonly IImageStorageService _imageStorageService;
    private readonly IBlogHtmlSanitizer _sanitizer;
    private readonly INewsService _newsService;
    private readonly AutoNewsSettings _settings;
    private readonly ILogger<AutoNewsPublisher> _logger;

    public AutoNewsPublisher(
        AppDbContext dbContext,
        FacebookPagePostClient facebookClient,
        IImageStorageService imageStorageService,
        IBlogHtmlSanitizer sanitizer,
        INewsService newsService,
        IOptions<AutoNewsSettings> settings,
        ILogger<AutoNewsPublisher> logger)
    {
        _dbContext = dbContext;
        _facebookClient = facebookClient;
        _imageStorageService = imageStorageService;
        _sanitizer = sanitizer;
        _newsService = newsService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AutoNewsRunResult> RunAsync(bool manual, CancellationToken cancellationToken)
    {
        if (!_settings.Enabled && !manual)
        {
            return new AutoNewsRunResult(0, null, _facebookClient.IsConfigured, "Tự động đăng bài đang tắt.");
        }

        await RunLock.WaitAsync(cancellationToken);
        try
        {
            await RepairEvergreenLinksAsync(cancellationToken);
            var imported = await ImportFacebookPostsAsync(cancellationToken);

            string? evergreenTitle = null;
            var vietnamNow = VietnamClock.GetVietnamNow();
            if (manual || vietnamNow.Hour >= _settings.FallbackAfterHour)
            {
                if (!await HasPostPublishedTodayAsync(vietnamNow, cancellationToken))
                {
                    evergreenTitle = await PublishEvergreenAsync(cancellationToken);
                }
            }

            if (imported > 0 || evergreenTitle is not null)
            {
                _newsService.InvalidateNewsCache();
            }

            var message = imported > 0 || evergreenTitle is not null
                ? $"Đã đăng {imported} bài từ Fanpage" + (evergreenTitle is null ? "." : $" và 1 bài ẩm thực: \"{evergreenTitle}\".")
                : "Không có bài mới cần đăng (hôm nay đã có bài).";
            if (!_facebookClient.IsConfigured)
            {
                message += " Chưa kết nối Fanpage (thiếu AutoNews__FacebookPageId / AutoNews__FacebookAccessToken).";
            }

            _logger.LogInformation("Auto news run: {Message}", message);
            return new AutoNewsRunResult(imported, evergreenTitle, _facebookClient.IsConfigured, message);
        }
        finally
        {
            RunLock.Release();
        }
    }

    /// <summary>Rebuilds evergreen articles saved before internal links were made absolute.</summary>
    private async Task RepairEvergreenLinksAsync(CancellationToken cancellationToken)
    {
        var slugs = KoreanFoodArticleLibrary.Articles.Select(x => x.Slug).ToList();
        var marker = SeoArticleComposer.SiteBaseUrl + "/dat-ban";
        var posts = await _dbContext.Posts
            .Where(x => slugs.Contains(x.Slug) && !x.Content.Contains(marker))
            .ToListAsync(cancellationToken);
        if (posts.Count == 0)
        {
            return;
        }

        foreach (var post in posts)
        {
            var article = KoreanFoodArticleLibrary.Articles.First(x => x.Slug == post.Slug);
            post.Content = _sanitizer.Sanitize(article.BuildHtml());
            post.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _newsService.InvalidateNewsCache();
    }

    private async Task<int> ImportFacebookPostsAsync(CancellationToken cancellationToken)
    {
        if (!_facebookClient.IsConfigured)
        {
            return 0;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-Math.Max(1, _settings.LookbackDays));
        var posts = (await _facebookClient.GetRecentPostsAsync(25, cancellationToken))
            .Where(post => post.CreatedTime >= cutoff)
            .OrderBy(post => post.CreatedTime)
            .ToList();

        var imported = 0;
        foreach (var post in posts)
        {
            if (imported >= Math.Max(1, _settings.MaxPostsPerRun))
            {
                break;
            }

            var article = SeoArticleComposer.Compose(post);
            if (article is null)
            {
                continue;
            }

            var marker = SeoArticleComposer.SourceMarker(post);
            var existing = await _dbContext.Posts
                .FirstOrDefaultAsync(x => x.Slug == article.Slug || x.Content.Contains(marker), cancellationToken);
            if (existing is not null)
            {
                // Upgrade posts imported before full-resolution photos were supported (OgImageUrl was not set then).
                if (existing.OgImageUrl is null && existing.Content.Contains(marker))
                {
                    var (upgradedThumbnail, upgradedHtml) = await BuildImagesAsync(post, article, cancellationToken);
                    existing.Title = article.Title;
                    existing.SeoTitle = article.SeoTitle;
                    existing.OgTitle = article.SeoTitle;
                    existing.Content = _sanitizer.Sanitize(upgradedHtml);
                    existing.ThumbnailUrl = upgradedThumbnail;
                    existing.OgImageUrl = ToAbsoluteUrl(upgradedThumbnail);
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    imported++;
                }

                continue;
            }

            var (thumbnailUrl, htmlWithImages) = await BuildImagesAsync(post, article, cancellationToken);
            var content = _sanitizer.Sanitize(htmlWithImages);
            _dbContext.Posts.Add(new Post
            {
                Title = article.Title,
                Slug = await UniqueSlugAsync(article.Slug, cancellationToken),
                Excerpt = article.Excerpt,
                Content = content,
                ContentMode = PostContentMode.Html,
                ThumbnailUrl = thumbnailUrl,
                Category = article.Category,
                IsPublished = true,
                PublishedAt = post.CreatedTime.ToUniversalTime(),
                SeoTitle = article.SeoTitle,
                SeoDescription = article.SeoDescription,
                FocusKeyword = article.FocusKeyword,
                OgTitle = article.SeoTitle,
                OgDescription = article.SeoDescription,
                OgImageUrl = ToAbsoluteUrl(thumbnailUrl),
                RobotsIndex = true,
                RobotsFollow = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            imported++;
        }

        return imported;
    }

    /// <summary>
    /// Downloads every photo of the Fanpage post in full resolution (up to 2048px), stores them,
    /// uses the first as thumbnail and adds the others to the article as a gallery.
    /// </summary>
    private async Task<(string Thumbnail, string Html)> BuildImagesAsync(FacebookPagePost post, ComposedArticle article, CancellationToken cancellationToken)
    {
        var sources = new List<string>();
        foreach (var photoId in post.PhotoIds.Take(6))
        {
            var url = await _facebookClient.GetBestPhotoUrlAsync(photoId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(url))
            {
                sources.Add(url);
            }
        }

        if (sources.Count == 0 && !string.IsNullOrWhiteSpace(post.PictureUrl))
        {
            sources.Add(post.PictureUrl!);
        }

        var stored = new List<string>();
        for (var i = 0; i < sources.Count; i++)
        {
            var url = await TryUploadImageAsync(sources[i], i == 0 ? article.Slug : $"{article.Slug}-{i + 1}", cancellationToken);
            if (url is not null)
            {
                stored.Add(url);
            }
        }

        var thumbnail = stored.Count > 0 ? stored[0] : article.FallbackImagePath;
        var html = article.HtmlContent;
        if (stored.Count > 1)
        {
            var gallery = new System.Text.StringBuilder();
            for (var i = 1; i < stored.Count; i++)
            {
                gallery.Append("<figure><img src=\"")
                    .Append(System.Net.WebUtility.HtmlEncode(stored[i]))
                    .Append("\" alt=\"")
                    .Append(System.Net.WebUtility.HtmlEncode($"{article.Title} – ảnh {i + 1}"))
                    .Append("\" loading=\"lazy\" /></figure>");
            }

            var index = html.IndexOf("<h2>Về ", StringComparison.Ordinal);
            html = index >= 0 ? html.Insert(index, gallery.ToString()) : html + gallery.ToString();
        }

        return (thumbnail, html);
    }

    private static string ToAbsoluteUrl(string url)
    {
        return url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : SeoArticleComposer.SiteBaseUrl + url;
    }

    private async Task<bool> HasPostPublishedTodayAsync(DateTimeOffset vietnamNow, CancellationToken cancellationToken)
    {
        var startOfDayUtc = new DateTimeOffset(vietnamNow.Year, vietnamNow.Month, vietnamNow.Day, 0, 0, 0, vietnamNow.Offset).ToUniversalTime();
        return await _dbContext.Posts.AsNoTracking()
            .AnyAsync(x => x.IsPublished && x.PublishedAt != null && x.PublishedAt >= startOfDayUtc, cancellationToken);
    }

    private async Task<string?> PublishEvergreenAsync(CancellationToken cancellationToken)
    {
        var slugs = KoreanFoodArticleLibrary.Articles.Select(x => x.Slug).ToList();
        var used = await _dbContext.Posts.AsNoTracking()
            .Where(x => slugs.Contains(x.Slug))
            .Select(x => x.Slug)
            .ToListAsync(cancellationToken);

        var article = KoreanFoodArticleLibrary.Articles.FirstOrDefault(x => !used.Contains(x.Slug));
        if (article is null)
        {
            _logger.LogInformation("Auto news: all evergreen articles are already published.");
            return null;
        }

        var seoTitle = article.Title.Length <= 65 ? article.Title : article.Title[..65].TrimEnd();
        var seoDescription = article.Description.Length <= 160 ? article.Description : article.Description[..157].TrimEnd() + "…";
        _dbContext.Posts.Add(new Post
        {
            Title = article.Title,
            Slug = article.Slug,
            Excerpt = article.Description,
            Content = _sanitizer.Sanitize(article.BuildHtml()),
            ContentMode = PostContentMode.Html,
            ThumbnailUrl = article.ImagePath,
            Category = article.Category,
            IsPublished = true,
            PublishedAt = DateTimeOffset.UtcNow,
            SeoTitle = seoTitle,
            SeoDescription = seoDescription,
            FocusKeyword = article.FocusKeyword,
            OgTitle = seoTitle,
            OgDescription = seoDescription,
            RobotsIndex = true,
            RobotsFollow = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return article.Title;
    }

    private async Task<string> UniqueSlugAsync(string slug, CancellationToken cancellationToken)
    {
        var candidate = string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
        var suffix = 2;
        while (await _dbContext.Posts.AsNoTracking().AnyAsync(x => x.Slug == candidate, cancellationToken))
        {
            candidate = $"{slug}-{suffix++}";
        }

        return candidate;
    }

    private async Task<string?> TryUploadImageAsync(string pictureUrl, string slug, CancellationToken cancellationToken)
    {
        var bytes = await _facebookClient.DownloadImageAsync(pictureUrl, cancellationToken);
        if (bytes is null)
        {
            return null;
        }

        var (extension, contentType) = DetectImageType(bytes);
        if (extension is null || contentType is null)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(bytes);
            var file = new FormFile(stream, 0, bytes.Length, "file", $"{slug}{extension}")
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            return await _imageStorageService.UploadAsync(file, ImageCategory.Posts, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Auto news: could not store Fanpage image.");
            return null;
        }
    }

    private static (string? Extension, string? ContentType) DetectImageType(byte[] bytes)
    {
        if (bytes.Length > 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return (".jpg", "image/jpeg");
        }

        if (bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return (".png", "image/png");
        }

        if (bytes.Length > 12 && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
        {
            return (".webp", "image/webp");
        }

        return (null, null);
    }
}
