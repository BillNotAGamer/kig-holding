using KIGHolding.Options;
using KIGHolding.Services;

namespace KIGHolding.Tests.Storage;

public sealed class ImageStorageConfigurationTests
{
    [Theory]
    [InlineData("LocalVolume", ImageStorageProviderKind.LocalVolume)]
    [InlineData(" localvolume ", ImageStorageProviderKind.LocalVolume)]
    [InlineData("Cloudinary", ImageStorageProviderKind.Cloudinary)]
    [InlineData(" cloudinary ", ImageStorageProviderKind.Cloudinary)]
    [InlineData("CloudflareR2", ImageStorageProviderKind.CloudflareR2)]
    [InlineData(" cloudflarer2 ", ImageStorageProviderKind.CloudflareR2)]
    public void ProviderParser_ParsesSupportedProvidersCaseInsensitively(string value, ImageStorageProviderKind expected)
    {
        Assert.True(ImageStorageProviderKindParser.TryParse(value, out var provider));
        Assert.Equal(expected, provider);
    }

    [Fact]
    public void ProviderParser_RejectsUnknownProvider()
    {
        Assert.False(ImageStorageProviderKindParser.TryParse("unknown", out _));
    }

    [Theory]
    [InlineData("BucketName")]
    [InlineData("AccessKeyId")]
    [InlineData("SecretAccessKey")]
    public void R2Validation_FailsWhenRequiredCredentialOrBucketKeyIsMissing(string propertyName)
    {
        var settings = StorageTestHelpers.CreateR2Settings();
        typeof(ImageStorageSettings).GetProperty(propertyName)!.SetValue(settings, string.Empty);

        var result = Validate(settings);

        Assert.False(result.Succeeded);
        Assert.Contains($"ImageStorage:{propertyName}", FailureText(result));
    }

    [Theory]
    [InlineData("ServiceUrl", "http://example.test")]
    [InlineData("ServiceUrl", "not-a-url")]
    [InlineData("PublicBaseUrl", "http://media.example.test")]
    [InlineData("PublicBaseUrl", "not-a-url")]
    public void R2Validation_FailsForInvalidHttpsUrls(string propertyName, string value)
    {
        var settings = StorageTestHelpers.CreateR2Settings();
        typeof(ImageStorageSettings).GetProperty(propertyName)!.SetValue(settings, value);

        var result = Validate(settings);

        Assert.False(result.Succeeded);
        Assert.Contains($"ImageStorage:{propertyName}", FailureText(result));
    }

    [Theory]
    [InlineData("DefaultPrefix")]
    [InlineData("MenuPagesPrefix")]
    [InlineData("BranchesPrefix")]
    [InlineData("NewsPrefix")]
    [InlineData("BrandsPrefix")]
    public void R2Validation_FailsForTraversalPrefixes(string propertyName)
    {
        var settings = StorageTestHelpers.CreateR2Settings();
        typeof(ImageStorageSettings).GetProperty(propertyName)!.SetValue(settings, "../escape");

        var result = Validate(settings);

        Assert.False(result.Succeeded);
        Assert.Contains($"ImageStorage:{propertyName}", FailureText(result));
    }

    [Fact]
    public void R2Validation_FailsForNonPositiveMaxFileSize()
    {
        var settings = StorageTestHelpers.CreateR2Settings();
        settings.MaxFileSizeBytes = 0;

        var result = Validate(settings);

        Assert.False(result.Succeeded);
        Assert.Contains("ImageStorage:MaxFileSizeBytes", FailureText(result));
    }

    [Fact]
    public void R2Validation_FailsForEmptyExtensionAllowlist()
    {
        var settings = StorageTestHelpers.CreateR2Settings();
        settings.AllowedExtensions = [];

        var result = Validate(settings);

        Assert.False(result.Succeeded);
        Assert.Contains("ImageStorage:AllowedExtensions", FailureText(result));
    }

    [Fact]
    public void R2Validation_FailsForEmptyContentTypeAllowlist()
    {
        var settings = StorageTestHelpers.CreateR2Settings();
        settings.AllowedContentTypes = [];

        var result = Validate(settings);

        Assert.False(result.Succeeded);
        Assert.Contains("ImageStorage:AllowedContentTypes", FailureText(result));
    }

    [Fact]
    public void CanonicalMaxFileSizeDefault_Is50Megabytes()
    {
        Assert.Equal(52_428_800, ImageStorageSettings.DefaultMaxFileSizeBytes);
        Assert.Equal(ImageStorageSettings.DefaultMaxFileSizeBytes, new ImageStorageSettings().MaxFileSizeBytes);
    }

    [Fact]
    public void SafeTemplate_DocumentsCanonicalMaxFileSizeOnly()
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "appsettings.example.json"));

        Assert.Contains("\"MaxFileSizeBytes\"", text);
        Assert.DoesNotContain("MaxUploadBytes", text);
    }

    private static Microsoft.Extensions.Options.ValidateOptionsResult Validate(ImageStorageSettings settings)
    {
        return new ImageStorageSettingsValidator().Validate(null, settings);
    }

    private static string FailureText(Microsoft.Extensions.Options.ValidateOptionsResult result)
    {
        return string.Join('|', result.Failures ?? []);
    }
}
