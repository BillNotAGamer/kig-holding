using KIGHolding.Services;
using Microsoft.Extensions.Options;

namespace KIGHolding.Options;

public sealed class ImageStorageSettingsValidator : IValidateOptions<ImageStorageSettings>
{
    public ValidateOptionsResult Validate(string? name, ImageStorageSettings options)
    {
        var failures = new List<string>();

        if (!ImageStorageProviderKindParser.TryParse(options.Provider, out var provider))
        {
            failures.Add("ImageStorage:Provider must be one of LocalVolume, Cloudinary, or CloudflareR2.");
        }

        ValidateShared(options, failures);

        if (provider == ImageStorageProviderKind.LocalVolume)
        {
            ValidateLocalVolume(options, failures);
        }
        else if (provider == ImageStorageProviderKind.CloudflareR2)
        {
            ValidateCloudflareR2(options, failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateShared(ImageStorageSettings options, List<string> failures)
    {
        if (options.MaxFileSizeBytes <= 0)
        {
            failures.Add("ImageStorage:MaxFileSizeBytes must be greater than zero.");
        }

        if (options.AllowedExtensions is null || options.AllowedExtensions.Length == 0)
        {
            failures.Add("ImageStorage:AllowedExtensions must contain at least one value.");
        }

        if (options.AllowedContentTypes is null || options.AllowedContentTypes.Length == 0)
        {
            failures.Add("ImageStorage:AllowedContentTypes must contain at least one value.");
        }
    }

    private static void ValidateLocalVolume(ImageStorageSettings options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.RootPath))
        {
            failures.Add("ImageStorage:RootPath is required when Provider=LocalVolume.");
        }

        var publicBasePath = ImageStoragePathUtilities.NormalizePublicPath(options.PublicBasePath);
        if (string.IsNullOrWhiteSpace(publicBasePath) ||
            publicBasePath.Contains("..", StringComparison.Ordinal) ||
            !publicBasePath.StartsWith('/'))
        {
            failures.Add("ImageStorage:PublicBasePath must be an absolute app path without traversal.");
        }
    }

    private static void ValidateCloudflareR2(ImageStorageSettings options, List<string> failures)
    {
        Require(options.AccountId, "ImageStorage:AccountId", failures);
        Require(options.BucketName, "ImageStorage:BucketName", failures);
        Require(options.AccessKeyId, "ImageStorage:AccessKeyId", failures);
        Require(options.SecretAccessKey, "ImageStorage:SecretAccessKey", failures);
        Require(options.Region, "ImageStorage:Region", failures);

        ValidateHttpsAbsoluteUrl(options.ServiceUrl, "ImageStorage:ServiceUrl", failures);
        ValidateHttpsAbsoluteUrl(options.PublicBaseUrl, "ImageStorage:PublicBaseUrl", failures);

        ValidatePrefix(options.DefaultPrefix, "ImageStorage:DefaultPrefix", failures);
        ValidatePrefix(options.MenuPagesPrefix, "ImageStorage:MenuPagesPrefix", failures);
        ValidatePrefix(options.BranchesPrefix, "ImageStorage:BranchesPrefix", failures);
        ValidatePrefix(options.NewsPrefix, "ImageStorage:NewsPrefix", failures);
        ValidatePrefix(options.BrandsPrefix, "ImageStorage:BrandsPrefix", failures);
    }

    private static void Require(string? value, string keyName, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{keyName} is required.");
        }
    }

    private static void ValidateHttpsAbsoluteUrl(string? value, string keyName, List<string> failures)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{keyName} must be an absolute HTTPS URL.");
        }
    }

    private static void ValidatePrefix(string? value, string keyName, List<string> failures)
    {
        if (!ImageStoragePathUtilities.IsSafePrefix(value))
        {
            failures.Add($"{keyName} must be non-empty and must not contain traversal.");
        }
    }
}
