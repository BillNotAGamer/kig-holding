using System.Net;
using System.Text.Json;
using KIGHolding.Options;
using KIGHolding.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests.Storage;

public sealed class LiveCloudflareR2SmokeTests
{
    [LiveR2Fact]
    public async Task LiveR2Smoke_UploadsVerifiesPublicUrlAndDeletesSmokeObject()
    {
        var settings = LoadIgnoredLocalSettings();
        Assert.True(ImageStorageProviderKindParser.TryParse(settings.Provider, out var providerKind));
        Assert.Equal(ImageStorageProviderKind.CloudflareR2, providerKind);

        var objectKey = string.Empty;
        var bytes = TinyPngBytes();

        using var client = new CloudflareR2Client(Microsoft.Extensions.Options.Options.Create(settings));
        var storageProvider = new CloudflareR2ImageStorageProvider(
            Microsoft.Extensions.Options.Options.Create(settings),
            client,
            NullLogger<CloudflareR2ImageStorageProvider>.Instance);
        var cleanupAttempted = false;

        try
        {
            var url = await storageProvider.UploadAsync(
                CreateFormFile("r2-smoke.png", "image/png", bytes),
                ImageCategory.MenuPages,
                "menu-group-folder-smoke");

            Assert.Contains("/menu-pages/menu-group-folder-smoke/", url, StringComparison.Ordinal);
            Assert.True(storageProvider.TryExtractManagedObjectKey(url, ImageCategory.MenuPages, out objectKey));
            Assert.StartsWith("menu-pages/menu-group-folder-smoke/r2-smoke-", objectKey);
            Assert.True(await client.ObjectExistsAsync(settings.BucketName, objectKey));

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await storageProvider.DeleteAsync(url, ImageCategory.MenuPages);
            Assert.False(await client.ObjectExistsAsync(settings.BucketName, objectKey));
        }
        finally
        {
            cleanupAttempted = true;
            if (!string.IsNullOrWhiteSpace(objectKey) &&
                await client.ObjectExistsAsync(settings.BucketName, objectKey))
            {
                await client.DeleteObjectAsync(settings.BucketName, objectKey);
            }

            if (!string.IsNullOrWhiteSpace(objectKey))
            {
                var existsAfterDelete = await client.ObjectExistsAsync(settings.BucketName, objectKey);
                Assert.False(existsAfterDelete);
            }
        }

        Assert.True(cleanupAttempted);
    }

    private static ImageStorageSettings LoadIgnoredLocalSettings()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var candidates = new[]
        {
            Path.Combine(root, "appsettings.Development.json"),
            Path.Combine(root, "appsettings.json")
        };

        foreach (var settingsFile in candidates.Where(File.Exists))
        {
            var settings = ReadSettings(settingsFile);
            if (ImageStorageProviderKindParser.TryParse(settings.Provider, out var provider) &&
                provider == ImageStorageProviderKind.CloudflareR2)
            {
                return settings;
            }
        }

        throw new InvalidOperationException("A local ignored appsettings file must configure ImageStorage:Provider as CloudflareR2 to run the live R2 smoke test.");
    }

    private static ImageStorageSettings ReadSettings(string settingsFile)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(settingsFile));
        var section = document.RootElement.GetProperty("ImageStorage");

        return new ImageStorageSettings
        {
            Provider = GetString(section, "Provider"),
            AccountId = GetString(section, "AccountId"),
            BucketName = GetString(section, "BucketName"),
            AccessKeyId = GetString(section, "AccessKeyId"),
            SecretAccessKey = GetString(section, "SecretAccessKey"),
            ServiceUrl = GetString(section, "ServiceUrl"),
            PublicBaseUrl = GetString(section, "PublicBaseUrl"),
            Region = GetString(section, "Region"),
            DefaultPrefix = GetString(section, "DefaultPrefix"),
            MenuPagesPrefix = GetString(section, "MenuPagesPrefix"),
            BranchesPrefix = GetString(section, "BranchesPrefix"),
            NewsPrefix = GetString(section, "NewsPrefix"),
            BrandsPrefix = GetString(section, "BrandsPrefix"),
            MaxFileSizeBytes = GetLong(section, "MaxFileSizeBytes"),
            UsePathStyle = GetBool(section, "UsePathStyle"),
            AllowedExtensions = GetStringArray(section, "AllowedExtensions"),
            AllowedContentTypes = GetStringArray(section, "AllowedContentTypes")
        };
    }

    private static string GetString(JsonElement section, string name)
    {
        return section.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static long GetLong(JsonElement section, string name)
    {
        return section.TryGetProperty(name, out var value) && value.TryGetInt64(out var result)
            ? result
            : ImageStorageSettings.DefaultMaxFileSizeBytes;
    }

    private static bool GetBool(JsonElement section, string name)
    {
        return section.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
    }

    private static string[] GetStringArray(JsonElement section, string name)
    {
        return section.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToArray()
            : [];
    }

    private static byte[] TinyPngBytes()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] bytes)
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class LiveR2FactAttribute : FactAttribute
{
    public LiveR2FactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_LIVE_R2_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set RUN_LIVE_R2_TESTS=true to run the live Cloudflare R2 smoke test.";
        }
    }
}
