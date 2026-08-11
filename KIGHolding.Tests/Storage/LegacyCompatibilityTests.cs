using KIGHolding.Options;
using KIGHolding.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests.Storage;

public sealed class LegacyCompatibilityTests
{
    [Fact]
    public async Task LocalVolumeUpload_ReturnsUploadsPath()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var settings = new ImageStorageSettings
            {
                Provider = "LocalVolume",
                RootPath = root,
                PublicBasePath = "/uploads",
                MaxFileSizeBytes = ImageStorageSettings.DefaultMaxFileSizeBytes
            };
            var provider = new LocalVolumeImageStorageProvider(
                StorageTestHelpers.CreateEnvironment(Directory.GetCurrentDirectory()),
                StorageTestHelpers.Options(settings),
                NullLogger<LocalVolumeImageStorageProvider>.Instance);

            var url = await provider.UploadAsync(StorageTestHelpers.CreateFormFile("branch.webp"), ImageCategory.Branches);

            Assert.StartsWith("/uploads/branches/branch-", url);
            Assert.True(Directory.EnumerateFiles(Path.Combine(root, "branches")).Any());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LocalVolumeUpload_WithMenuPageScope_ReturnsNestedUploadsPath()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var settings = new ImageStorageSettings
            {
                Provider = "LocalVolume",
                RootPath = root,
                PublicBasePath = "/uploads",
                MaxFileSizeBytes = ImageStorageSettings.DefaultMaxFileSizeBytes
            };
            var provider = new LocalVolumeImageStorageProvider(
                StorageTestHelpers.CreateEnvironment(Directory.GetCurrentDirectory()),
                StorageTestHelpers.Options(settings),
                NullLogger<LocalVolumeImageStorageProvider>.Instance);

            var url = await provider.UploadAsync(
                StorageTestHelpers.CreateFormFile("1.webp"),
                ImageCategory.MenuPages,
                "truyen-thuyet-champong");

            Assert.StartsWith("/uploads/menu-pages/truyen-thuyet-champong/1-", url);
            var files = Directory.EnumerateFiles(Path.Combine(root, "menu-pages", "truyen-thuyet-champong")).ToList();
            Assert.Single(files);
            Assert.StartsWith(Path.GetFullPath(root), Path.GetFullPath(files[0]), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LocalVolumeUpload_WithTraversalScope_CreatesNoFile()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var settings = new ImageStorageSettings
            {
                Provider = "LocalVolume",
                RootPath = root,
                PublicBasePath = "/uploads",
                MaxFileSizeBytes = ImageStorageSettings.DefaultMaxFileSizeBytes
            };
            var provider = new LocalVolumeImageStorageProvider(
                StorageTestHelpers.CreateEnvironment(Directory.GetCurrentDirectory()),
                StorageTestHelpers.Options(settings),
                NullLogger<LocalVolumeImageStorageProvider>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                provider.UploadAsync(
                    StorageTestHelpers.CreateFormFile("1.webp"),
                    ImageCategory.MenuPages,
                    "../news"));

            Assert.Empty(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LocalVolumeDelete_MissingFileIsIdempotent()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var settings = new ImageStorageSettings
            {
                Provider = "LocalVolume",
                RootPath = root,
                PublicBasePath = "/uploads"
            };
            var provider = new LocalVolumeImageStorageProvider(
                StorageTestHelpers.CreateEnvironment(Directory.GetCurrentDirectory()),
                StorageTestHelpers.Options(settings),
                NullLogger<LocalVolumeImageStorageProvider>.Instance);

            await provider.DeleteAsync("/uploads/branches/missing.webp", ImageCategory.Branches);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LocalVolumeDelete_SupportsLegacyAndNestedMenuPagePaths()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var settings = new ImageStorageSettings
            {
                Provider = "LocalVolume",
                RootPath = root,
                PublicBasePath = "/uploads"
            };
            var legacyFolder = Path.Combine(root, "menu-pages");
            var nestedFolder = Path.Combine(root, "menu-pages", "gogi-maru");
            Directory.CreateDirectory(legacyFolder);
            Directory.CreateDirectory(nestedFolder);
            var legacyFile = Path.Combine(legacyFolder, "legacy.webp");
            var nestedFile = Path.Combine(nestedFolder, "nested.webp");
            await File.WriteAllTextAsync(legacyFile, "legacy");
            await File.WriteAllTextAsync(nestedFile, "nested");

            var provider = new LocalVolumeImageStorageProvider(
                StorageTestHelpers.CreateEnvironment(Directory.GetCurrentDirectory()),
                StorageTestHelpers.Options(settings),
                NullLogger<LocalVolumeImageStorageProvider>.Instance);

            await provider.DeleteAsync("/uploads/menu-pages/legacy.webp", ImageCategory.MenuPages);
            await provider.DeleteAsync("/uploads/menu-pages/gogi-maru/nested.webp", ImageCategory.MenuPages);

            Assert.False(File.Exists(legacyFile));
            Assert.False(File.Exists(nestedFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CloudinaryProvider_RecognizesManagedCloudinaryUrlWithoutLiveApiCall()
    {
        var provider = new CloudinaryImageStorageProvider(
            Microsoft.Extensions.Options.Options.Create(new CloudinarySettings
            {
                CloudName = "cloud",
                ApiKey = "key",
                ApiSecret = "secret",
                FolderPrefix = "kig-holding"
            }),
            StorageTestHelpers.Options(StorageTestHelpers.CreateR2Settings()),
            NullLogger<CloudinaryImageStorageProvider>.Instance);

        Assert.True(provider.CanDelete(
            "https://res.cloudinary.com/demo/image/upload/v123/kig-holding/posts/sample.webp",
            ImageCategory.Posts));
    }

    [Fact]
    public async Task ActiveR2Provider_DoesNotPreventRecognizedLocalVolumeDeleteRouting()
    {
        var local = new TrackingProvider(ImageStorageProviderKind.LocalVolume, value => value.StartsWith("/uploads/"));
        var cloudinary = new TrackingProvider(ImageStorageProviderKind.Cloudinary, _ => false);
        var r2 = new TrackingProvider(ImageStorageProviderKind.CloudflareR2, _ => false);
        var settings = StorageTestHelpers.CreateR2Settings();
        var service = new ImageStorageService([local, cloudinary, r2], StorageTestHelpers.Options(settings));

        await service.DeleteAsync("/uploads/branches/old.webp", ImageCategory.Branches);

        Assert.True(local.DeleteCalled);
        Assert.False(r2.DeleteCalled);
    }

    [Fact]
    public async Task ActiveR2Provider_DoesNotPreventRecognizedCloudinaryDeleteRouting()
    {
        var local = new TrackingProvider(ImageStorageProviderKind.LocalVolume, _ => false);
        var cloudinary = new TrackingProvider(ImageStorageProviderKind.Cloudinary, value => value.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase));
        var r2 = new TrackingProvider(ImageStorageProviderKind.CloudflareR2, _ => false);
        var settings = StorageTestHelpers.CreateR2Settings();
        var service = new ImageStorageService([local, cloudinary, r2], StorageTestHelpers.Options(settings));

        await service.DeleteAsync("https://res.cloudinary.com/demo/image/upload/v123/kig-holding/posts/sample.webp", ImageCategory.Posts);

        Assert.True(cloudinary.DeleteCalled);
        Assert.False(r2.DeleteCalled);
    }

    [Fact]
    public async Task ImageStorageService_UnscopedUploadRemainsFunctional()
    {
        var r2 = new TrackingProvider(ImageStorageProviderKind.CloudflareR2, _ => false);
        var service = new ImageStorageService([r2], StorageTestHelpers.Options(StorageTestHelpers.CreateR2Settings()));

        await service.UploadAsync(StorageTestHelpers.CreateFormFile(), ImageCategory.Posts);

        Assert.True(r2.UploadCalled);
        Assert.Equal(ImageCategory.Posts, r2.UploadCategory);
        Assert.Null(r2.UploadStorageScope);
    }

    [Fact]
    public async Task ImageStorageService_PassesMenuPageScopeToActiveProvider()
    {
        var r2 = new TrackingProvider(ImageStorageProviderKind.CloudflareR2, _ => false);
        var service = new ImageStorageService([r2], StorageTestHelpers.Options(StorageTestHelpers.CreateR2Settings()));

        await service.UploadAsync(StorageTestHelpers.CreateFormFile(), ImageCategory.MenuPages, "gogi-maru");

        Assert.True(r2.UploadCalled);
        Assert.Equal(ImageCategory.MenuPages, r2.UploadCategory);
        Assert.Equal("gogi-maru", r2.UploadStorageScope);
    }

    [Fact]
    public async Task ImageStorageService_RejectsScopeForOtherCategoriesBeforeProvider()
    {
        var r2 = new TrackingProvider(ImageStorageProviderKind.CloudflareR2, _ => false);
        var service = new ImageStorageService([r2], StorageTestHelpers.Options(StorageTestHelpers.CreateR2Settings()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(StorageTestHelpers.CreateFormFile(), ImageCategory.Branches, "gogi-maru"));

        Assert.False(r2.UploadCalled);
    }

    [Fact]
    public async Task ImageStorageService_PropagatesUploadCancellation()
    {
        var r2 = new TrackingProvider(ImageStorageProviderKind.CloudflareR2, _ => false);
        var service = new ImageStorageService([r2], StorageTestHelpers.Options(StorageTestHelpers.CreateR2Settings()));
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.UploadAsync(
                StorageTestHelpers.CreateFormFile(),
                ImageCategory.MenuPages,
                "kbb-cook",
                cancellationSource.Token));

        Assert.True(r2.UploadCancellationToken.IsCancellationRequested);
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "kig-storage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TrackingProvider : IImageStorageProvider
    {
        private readonly Func<string, bool> _canDelete;

        public TrackingProvider(ImageStorageProviderKind provider, Func<string, bool> canDelete)
        {
            Provider = provider;
            _canDelete = canDelete;
        }

        public ImageStorageProviderKind Provider { get; }
        public bool DeleteCalled { get; private set; }
        public bool UploadCalled { get; private set; }
        public ImageCategory? UploadCategory { get; private set; }
        public string? UploadStorageScope { get; private set; }
        public CancellationToken UploadCancellationToken { get; private set; }

        public Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default)
        {
            return UploadAsync(file, category, storageScope: null, cancellationToken);
        }

        public Task<string> UploadAsync(IFormFile file, ImageCategory category, string? storageScope, CancellationToken cancellationToken = default)
        {
            UploadCalled = true;
            UploadCategory = category;
            UploadStorageScope = storageScope;
            UploadCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult("unused");
        }

        public bool CanDelete(string imageUrlOrPath, ImageCategory category)
        {
            return _canDelete(imageUrlOrPath);
        }

        public Task DeleteAsync(string imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }
}
