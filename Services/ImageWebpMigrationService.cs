using System.Text.RegularExpressions;
using KIGHolding.Data;
using KIGHolding.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services;

public sealed record ImageMigrationPreview(
    bool IsAvailable,
    string? UnavailableReason,
    int TotalR2Images,
    int PngJpgCandidates,
    int LargeWebpCandidates,
    int AlreadyOptimized);

public sealed record ImageMigrationResult(
    int Processed,
    int ConvertedToWebp,
    int KeptOriginalFormat,
    int Failed,
    long BytesBefore,
    long BytesAfter,
    int UpdatedRecords,
    int Remaining,
    IReadOnlyList<string> Errors);

public interface IImageWebpMigrationService
{
    Task<ImageMigrationPreview> PreviewAsync(CancellationToken cancellationToken = default);

    Task<ImageMigrationResult> RunBatchAsync(int maxImages, CancellationToken cancellationToken = default);
}

/// <summary>
/// One-off conversion of images already stored on Cloudflare R2 to optimized WebP.
/// Each image is re-uploaded under a new "-opt" key and database references are repointed;
/// the original objects are never deleted, so the change can be reverted by restoring URLs.
/// </summary>
public sealed class ImageWebpMigrationService : IImageWebpMigrationService
{
    private const string OptimizedSuffix = "-opt";
    private const long LargeWebpThresholdBytes = 400 * 1024;
    private static readonly string[] RasterExtensions = [".png", ".jpg", ".jpeg"];

    private readonly AppDbContext _dbContext;
    private readonly ImageStorageSettings _settings;
    private readonly ICloudflareR2Client _r2Client;
    private readonly IImageOptimizer _imageOptimizer;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly INewsService _newsService;
    private readonly IBranchService _branchService;
    private readonly ISiteSettingService _siteSettingService;
    private readonly ILogger<ImageWebpMigrationService> _logger;

    public ImageWebpMigrationService(
        AppDbContext dbContext,
        IOptions<ImageStorageSettings> settings,
        ICloudflareR2Client r2Client,
        IImageOptimizer imageOptimizer,
        IHttpClientFactory httpClientFactory,
        INewsService newsService,
        IBranchService branchService,
        ISiteSettingService siteSettingService,
        ILogger<ImageWebpMigrationService> logger)
    {
        _dbContext = dbContext;
        _settings = settings.Value;
        _r2Client = r2Client;
        _imageOptimizer = imageOptimizer;
        _httpClientFactory = httpClientFactory;
        _newsService = newsService;
        _branchService = branchService;
        _siteSettingService = siteSettingService;
        _logger = logger;
    }

    public async Task<ImageMigrationPreview> PreviewAsync(CancellationToken cancellationToken = default)
    {
        var unavailableReason = GetUnavailableReason();
        if (unavailableReason is not null)
        {
            return new ImageMigrationPreview(false, unavailableReason, 0, 0, 0, 0);
        }

        var urls = await CollectR2UrlsAsync(cancellationToken);
        var alreadyOptimized = urls.Count(IsAlreadyOptimized);
        var candidates = await GetCandidatesAsync(urls, cancellationToken);

        return new ImageMigrationPreview(
            true,
            null,
            urls.Count,
            candidates.Count(url => IsRasterUrl(url)),
            candidates.Count(url => !IsRasterUrl(url)),
            alreadyOptimized);
    }

    public async Task<ImageMigrationResult> RunBatchAsync(int maxImages, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var unavailableReason = GetUnavailableReason();
        if (unavailableReason is not null)
        {
            return new ImageMigrationResult(0, 0, 0, 0, 0, 0, 0, 0, [unavailableReason]);
        }

        var urls = await CollectR2UrlsAsync(cancellationToken);
        var candidates = await GetCandidatesAsync(urls, cancellationToken);
        var batch = candidates.Take(Math.Clamp(maxImages, 1, 50)).ToList();

        var replacements = new Dictionary<string, string>(StringComparer.Ordinal);
        var converted = 0;
        var keptFormat = 0;
        var failed = 0;
        long bytesBefore = 0;
        long bytesAfter = 0;
        var httpClient = _httpClientFactory.CreateClient(nameof(ImageWebpMigrationService));
        httpClient.Timeout = TimeSpan.FromSeconds(90);

        foreach (var url in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var original = await httpClient.GetByteArrayAsync(url, cancellationToken);
                var extension = GetExtension(url);
                var objectKey = GetObjectKey(url);
                if (objectKey is null)
                {
                    failed++;
                    errors.Add($"Bỏ qua (đường dẫn không hợp lệ): {url}");
                    continue;
                }

                var maxDimension = objectKey.StartsWith($"{_settings.MenuPagesPrefix.Trim('/')}/", StringComparison.OrdinalIgnoreCase)
                    ? SkiaImageOptimizer.MenuPageMaxDimension
                    : SkiaImageOptimizer.DefaultMaxDimension;

                var optimized = _imageOptimizer.TryOptimize(original, extension, maxDimension);
                var newExtension = optimized is not null ? SkiaImageOptimizer.WebpExtension : extension;
                var newContent = optimized?.Content ?? original;
                var newContentType = optimized is not null ? SkiaImageOptimizer.WebpContentType : GetContentType(extension);

                // "-opt" marks the image as processed even when re-encoding gave no gain,
                // so later batches do not pick it up again.
                var newUrl = ReplaceExtension(url, OptimizedSuffix + newExtension);
                var newObjectKey = GetObjectKey(newUrl);
                if (newObjectKey is null)
                {
                    failed++;
                    errors.Add($"Bỏ qua (không tạo được khóa mới): {url}");
                    continue;
                }

                await using (var stream = new MemoryStream(newContent, writable: false))
                {
                    await _r2Client.PutObjectAsync(_settings.BucketName.Trim(), newObjectKey, newContentType, stream, cancellationToken);
                }

                replacements[url] = newUrl;
                bytesBefore += original.Length;
                bytesAfter += newContent.Length;

                if (optimized is not null)
                {
                    converted++;
                }
                else
                {
                    keptFormat++;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failed++;
                errors.Add($"Lỗi ({exception.GetType().Name}): {url}");
                _logger.LogWarning(
                    "WebP migration failed for {Url}. ExceptionType={ExceptionType}",
                    url,
                    exception.GetType().Name);
            }
        }

        var updatedRecords = replacements.Count > 0
            ? await ApplyReplacementsAsync(replacements, cancellationToken)
            : 0;

        return new ImageMigrationResult(
            batch.Count,
            converted,
            keptFormat,
            failed,
            bytesBefore,
            bytesAfter,
            updatedRecords,
            Math.Max(0, candidates.Count - replacements.Count),
            errors.Take(20).ToList());
    }

    private string? GetUnavailableReason()
    {
        if (ImageStorageProviderKindParser.ParseOrThrow(_settings.Provider) != ImageStorageProviderKind.CloudflareR2)
        {
            return "Kho ảnh hiện tại không phải Cloudflare R2.";
        }

        if (string.IsNullOrWhiteSpace(_settings.PublicBaseUrl) || string.IsNullOrWhiteSpace(_settings.BucketName))
        {
            return "Chưa cấu hình PublicBaseUrl hoặc BucketName cho R2.";
        }

        return null;
    }

    private string PublicBase => ImageStoragePathUtilities.NormalizePublicBaseUrl(_settings.PublicBaseUrl) + "/";

    private async Task<List<string>> CollectR2UrlsAsync(CancellationToken cancellationToken)
    {
        var values = new List<string?>();

        values.AddRange(await _dbContext.Posts.AsNoTracking().Select(x => x.ThumbnailUrl).ToListAsync(cancellationToken));
        values.AddRange(await _dbContext.Posts.AsNoTracking().Select(x => x.OgImageUrl).ToListAsync(cancellationToken));
        values.AddRange(await _dbContext.Branches.AsNoTracking().Select(x => x.ThumbnailUrl).ToListAsync(cancellationToken));
        values.AddRange(await _dbContext.MenuGroups.AsNoTracking().Select(x => x.CoverImageUrl).ToListAsync(cancellationToken));
        values.AddRange(await _dbContext.MenuPageImages.AsNoTracking().Select(x => x.ImageUrl).ToListAsync(cancellationToken));
        values.AddRange(await _dbContext.Reviews.AsNoTracking().Select(x => x.AvatarUrl).ToListAsync(cancellationToken));
        values.AddRange(await _dbContext.SiteSettings.AsNoTracking().Select(x => x.LogoUrl).ToListAsync(cancellationToken));

        var contents = await _dbContext.Posts.AsNoTracking().Select(x => x.Content).ToListAsync(cancellationToken);
        var urlPattern = new Regex(Regex.Escape(PublicBase) + @"[^\s""'<>()]+", RegexOptions.CultureInvariant);
        foreach (var content in contents)
        {
            if (string.IsNullOrEmpty(content))
            {
                continue;
            }

            values.AddRange(urlPattern.Matches(content).Select(match => match.Value));
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Where(value => value.StartsWith(PublicBase, StringComparison.OrdinalIgnoreCase))
            .Where(value => IsRasterUrl(value) || GetExtension(value) == SkiaImageOptimizer.WebpExtension)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<List<string>> GetCandidatesAsync(List<string> urls, CancellationToken cancellationToken)
    {
        var candidates = new List<string>();
        var httpClient = _httpClientFactory.CreateClient(nameof(ImageWebpMigrationService));
        httpClient.Timeout = TimeSpan.FromSeconds(20);

        foreach (var url in urls.Where(url => !IsAlreadyOptimized(url)))
        {
            if (IsRasterUrl(url))
            {
                candidates.Add(url);
                continue;
            }

            // WebP already: only worth re-encoding when it is large.
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength > LargeWebpThresholdBytes)
                {
                    candidates.Add(url);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogDebug("HEAD failed for {Url}: {ExceptionType}", url, exception.GetType().Name);
            }
        }

        return candidates;
    }

    private async Task<int> ApplyReplacementsAsync(IReadOnlyDictionary<string, string> replacements, CancellationToken cancellationToken)
    {
        var updated = 0;

        string? Swap(string? value)
        {
            if (value is null)
            {
                return null;
            }

            return replacements.TryGetValue(value.Trim(), out var replacement) ? replacement : value;
        }

        foreach (var post in await _dbContext.Posts.ToListAsync(cancellationToken))
        {
            var changed = false;
            var thumbnail = Swap(post.ThumbnailUrl) ?? string.Empty;
            if (!string.Equals(thumbnail, post.ThumbnailUrl, StringComparison.Ordinal))
            {
                post.ThumbnailUrl = thumbnail;
                changed = true;
            }

            var ogImage = Swap(post.OgImageUrl);
            if (!string.Equals(ogImage, post.OgImageUrl, StringComparison.Ordinal))
            {
                post.OgImageUrl = ogImage;
                changed = true;
            }

            if (!string.IsNullOrEmpty(post.Content))
            {
                var content = post.Content;
                foreach (var (oldUrl, newUrl) in replacements)
                {
                    content = content.Replace(oldUrl, newUrl, StringComparison.Ordinal);
                }

                if (!string.Equals(content, post.Content, StringComparison.Ordinal))
                {
                    post.Content = content;
                    changed = true;
                }
            }

            if (changed)
            {
                updated++;
            }
        }

        foreach (var branch in await _dbContext.Branches.ToListAsync(cancellationToken))
        {
            var thumbnail = Swap(branch.ThumbnailUrl) ?? string.Empty;
            if (!string.Equals(thumbnail, branch.ThumbnailUrl, StringComparison.Ordinal))
            {
                branch.ThumbnailUrl = thumbnail;
                updated++;
            }
        }

        foreach (var group in await _dbContext.MenuGroups.ToListAsync(cancellationToken))
        {
            var cover = Swap(group.CoverImageUrl);
            if (!string.Equals(cover, group.CoverImageUrl, StringComparison.Ordinal))
            {
                group.CoverImageUrl = cover;
                updated++;
            }
        }

        foreach (var page in await _dbContext.MenuPageImages.ToListAsync(cancellationToken))
        {
            var image = Swap(page.ImageUrl) ?? string.Empty;
            if (!string.Equals(image, page.ImageUrl, StringComparison.Ordinal))
            {
                page.ImageUrl = image;
                updated++;
            }
        }

        foreach (var review in await _dbContext.Reviews.ToListAsync(cancellationToken))
        {
            var avatar = Swap(review.AvatarUrl);
            if (!string.Equals(avatar, review.AvatarUrl, StringComparison.Ordinal))
            {
                review.AvatarUrl = avatar;
                updated++;
            }
        }

        foreach (var setting in await _dbContext.SiteSettings.ToListAsync(cancellationToken))
        {
            var logo = Swap(setting.LogoUrl) ?? string.Empty;
            if (!string.Equals(logo, setting.LogoUrl, StringComparison.Ordinal))
            {
                setting.LogoUrl = logo;
                updated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _newsService.InvalidateNewsCache();
        _branchService.InvalidateActiveBranchesCache();
        _siteSettingService.InvalidateCache();

        return updated;
    }

    private static bool IsAlreadyOptimized(string url)
    {
        var path = StripQuery(url);
        var extension = Path.GetExtension(path);
        var stem = path[..^extension.Length];
        return stem.EndsWith(OptimizedSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRasterUrl(string url)
    {
        return RasterExtensions.Contains(GetExtension(url), StringComparer.OrdinalIgnoreCase);
    }

    private static string GetExtension(string url)
    {
        return Path.GetExtension(StripQuery(url)).ToLowerInvariant();
    }

    private static string StripQuery(string url)
    {
        var index = url.IndexOfAny(['?', '#']);
        return index >= 0 ? url[..index] : url;
    }

    private static string ReplaceExtension(string url, string newSuffixAndExtension)
    {
        var path = StripQuery(url);
        var extension = Path.GetExtension(path);
        return path[..^extension.Length] + newSuffixAndExtension;
    }

    private string? GetObjectKey(string url)
    {
        if (!url.StartsWith(PublicBase, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var key = Uri.UnescapeDataString(StripQuery(url)[PublicBase.Length..]).Replace('\\', '/').TrimStart('/');
        return ImageStoragePathUtilities.IsSafeObjectKey(key) ? key : null;
    }

    private static string GetContentType(string extension)
    {
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => SkiaImageOptimizer.WebpContentType
        };
    }
}
