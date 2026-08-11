using KIGHolding.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests.Storage;

public sealed class CloudflareR2ImageStorageProviderTests
{
    [Fact]
    public async Task UploadAsync_UsesConfiguredBucketKeyContentTypeAndStream()
    {
        var settings = StorageTestHelpers.CreateR2Settings();
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(settings, client);
        var bytes = new byte[] { 1, 2, 3, 4 };

        var url = await provider.UploadAsync(
            StorageTestHelpers.CreateFormFile("Menu Page.PNG", "image/png", bytes),
            ImageCategory.MenuPages);

        Assert.Equal(settings.BucketName, client.PutBucketName);
        Assert.StartsWith("menu-pages/menu-page-", client.PutObjectKey);
        Assert.Equal("image/png", client.PutContentType);
        Assert.Equal(bytes, client.PutContent);
        Assert.Equal($"https://media.example.test/{client.PutObjectKey}", url);
    }

    [Fact]
    public async Task UploadAsync_WithMenuPageScope_UsesNestedObjectKey()
    {
        var settings = StorageTestHelpers.CreateR2Settings();
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(settings, client);

        var url = await provider.UploadAsync(
            StorageTestHelpers.CreateFormFile("1.PNG", "image/png", [1, 2, 3]),
            ImageCategory.MenuPages,
            "truyen-thuyet-champong");

        Assert.StartsWith("menu-pages/truyen-thuyet-champong/1-", client.PutObjectKey);
        Assert.Equal($"https://media.example.test/{client.PutObjectKey}", url);
    }

    [Fact]
    public async Task UploadAsync_PropagatesCancellationToClient()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provider.UploadAsync(StorageTestHelpers.CreateFormFile(), ImageCategory.Posts, cancellationSource.Token));

        Assert.True(client.PutCancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task UploadAsync_RejectsOversizedFileBeforeNetworkInvocation()
    {
        var settings = StorageTestHelpers.CreateR2Settings();
        settings.MaxFileSizeBytes = 3;
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(settings, client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.UploadAsync(
                StorageTestHelpers.CreateFormFile(bytes: [1, 2, 3, 4]),
                ImageCategory.Posts));

        Assert.Null(client.PutBucketName);
    }

    [Fact]
    public async Task UploadAsync_RejectsUnsupportedExtension()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.UploadAsync(
                StorageTestHelpers.CreateFormFile(fileName: "script.gif", contentType: "image/webp"),
                ImageCategory.Posts));

        Assert.Null(client.PutBucketName);
    }

    [Fact]
    public async Task UploadAsync_RejectsUnsupportedMimeType()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.UploadAsync(
                StorageTestHelpers.CreateFormFile(fileName: "image.webp", contentType: "application/octet-stream"),
                ImageCategory.Posts));

        Assert.Null(client.PutBucketName);
    }

    [Fact]
    public async Task UploadAsync_SurfacesProviderFailureWithoutCredentialText()
    {
        var client = new FakeCloudflareR2Client { ThrowOnPut = true };
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.UploadAsync(StorageTestHelpers.CreateFormFile(), ImageCategory.Posts));

        Assert.DoesNotContain("sensitive marker", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_ConvertsConfiguredPublicUrlToObjectKey()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        await provider.DeleteAsync("https://media.example.test/menu-pages/sample.webp", ImageCategory.MenuPages);

        Assert.Equal("kig-test-bucket", client.DeletedBucketName);
        Assert.Equal("menu-pages/sample.webp", client.DeletedObjectKey);
    }

    [Fact]
    public async Task DeleteAsync_DeletesNestedMenuPageObjectKey()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        await provider.DeleteAsync(
            "https://media.example.test/menu-pages/truyen-thuyet-champong/sample.webp",
            ImageCategory.MenuPages);

        Assert.Equal("menu-pages/truyen-thuyet-champong/sample.webp", client.DeletedObjectKey);
    }

    [Fact]
    public async Task DeleteAsync_DeletesExplicitSameCategoryObjectKey()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        await provider.DeleteAsync("branches/sample.webp", ImageCategory.Branches);

        Assert.Equal("branches/sample.webp", client.DeletedObjectKey);
    }

    [Fact]
    public async Task DeleteAsync_IgnoresExternalAbsoluteUrl()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        await provider.DeleteAsync("https://external.example.test/menu-pages/sample.webp", ImageCategory.MenuPages);

        Assert.Null(client.DeletedObjectKey);
    }

    [Theory]
    [InlineData("/images/static.webp")]
    [InlineData("/favicon.ico")]
    public async Task DeleteAsync_IgnoresStaticLocalPaths(string path)
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        await provider.DeleteAsync(path, ImageCategory.MenuPages);

        Assert.Null(client.DeletedObjectKey);
    }

    [Fact]
    public async Task DeleteAsync_IgnoresWrongCategoryR2Url()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        await provider.DeleteAsync("https://media.example.test/news/sample.webp", ImageCategory.MenuPages);

        Assert.Null(client.DeletedObjectKey);
    }

    [Fact]
    public async Task DeleteAsync_RejectsTraversalLikeKey()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        await provider.DeleteAsync("https://media.example.test/menu-pages/../sample.webp", ImageCategory.MenuPages);

        Assert.Null(client.DeletedObjectKey);
    }

    [Fact]
    public async Task DeleteAsync_TreatsMissingObjectAsIdempotentAtProviderBoundary()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);

        await provider.DeleteAsync("https://media.example.test/menu-pages/missing.webp", ImageCategory.MenuPages);

        Assert.Equal("menu-pages/missing.webp", client.DeletedObjectKey);
    }

    [Fact]
    public async Task DeleteAsync_PropagatesCancellationToClient()
    {
        var client = new FakeCloudflareR2Client();
        var provider = CreateProvider(StorageTestHelpers.CreateR2Settings(), client);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provider.DeleteAsync(
                "https://media.example.test/menu-pages/cancel.webp",
                ImageCategory.MenuPages,
                cancellationSource.Token));

        Assert.True(client.DeleteCancellationToken.IsCancellationRequested);
    }

    private static CloudflareR2ImageStorageProvider CreateProvider(
        KIGHolding.Options.ImageStorageSettings settings,
        ICloudflareR2Client client)
    {
        return new CloudflareR2ImageStorageProvider(
            StorageTestHelpers.Options(settings),
            client,
            NullLogger<CloudflareR2ImageStorageProvider>.Instance);
    }
}
