using System.Text;
using KIGHolding.Options;
using KIGHolding.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KIGHolding.Tests.Storage;

internal static class StorageTestHelpers
{
    public static ImageStorageSettings CreateR2Settings()
    {
        return new ImageStorageSettings
        {
            Provider = "CloudflareR2",
            AccountId = "account-placeholder",
            BucketName = "kig-test-bucket",
            AccessKeyId = "access-key-placeholder",
            SecretAccessKey = "secret-placeholder",
            ServiceUrl = "https://account-placeholder.r2.cloudflarestorage.com",
            PublicBaseUrl = "https://media.example.test",
            Region = "auto",
            DefaultPrefix = "general",
            MenuPagesPrefix = "menu-pages",
            BranchesPrefix = "branches",
            NewsPrefix = "news",
            BrandsPrefix = "brands",
            MaxFileSizeBytes = ImageStorageSettings.DefaultMaxFileSizeBytes,
            AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"],
            AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"]
        };
    }

    public static IOptions<ImageStorageSettings> Options(ImageStorageSettings settings)
    {
        return Microsoft.Extensions.Options.Options.Create(settings);
    }

    public static IFormFile CreateFormFile(
        string fileName = "sample.webp",
        string contentType = "image/webp",
        byte[]? bytes = null)
    {
        bytes ??= Encoding.UTF8.GetBytes("fake-image-bytes");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    public static TestWebHostEnvironment CreateEnvironment(string contentRootPath)
    {
        return new TestWebHostEnvironment
        {
            ContentRootPath = contentRootPath,
            WebRootPath = Path.Combine(contentRootPath, "wwwroot")
        };
    }
}

internal sealed class FakeCloudflareR2Client : ICloudflareR2Client
{
    public string? PutBucketName { get; private set; }
    public string? PutObjectKey { get; private set; }
    public string? PutContentType { get; private set; }
    public byte[]? PutContent { get; private set; }
    public CancellationToken PutCancellationToken { get; private set; }
    public string? DeletedBucketName { get; private set; }
    public string? DeletedObjectKey { get; private set; }
    public CancellationToken DeleteCancellationToken { get; private set; }
    public bool ThrowOnPut { get; set; }
    public bool ObjectExists { get; set; } = true;

    public async Task PutObjectAsync(
        string bucketName,
        string objectKey,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        PutCancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        if (ThrowOnPut)
        {
            throw new ApplicationException("simulated provider failure with sensitive marker");
        }

        PutBucketName = bucketName;
        PutObjectKey = objectKey;
        PutContentType = contentType;

        await using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken);
        PutContent = memoryStream.ToArray();
    }

    public Task DeleteObjectAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        DeleteCancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        DeletedBucketName = bucketName;
        DeletedObjectKey = objectKey;
        return Task.CompletedTask;
    }

    public Task<bool> ObjectExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ObjectExists);
    }
}

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "KIGHolding.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
