namespace KIGHolding.Options;

public sealed class ImageStorageSettings
{
    public const long DefaultMaxFileSizeBytes = 50L * 1024 * 1024;

    public string Provider { get; set; } = "LocalVolume";
    public string RootPath { get; set; } = "App_Data/uploads";
    public string PublicBasePath { get; set; } = "/uploads";
    public string AccountId { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string Region { get; set; } = "auto";
    public string DefaultPrefix { get; set; } = "general";
    public string MenuPagesPrefix { get; set; } = "menu-pages";
    public string BranchesPrefix { get; set; } = "branches";
    public string NewsPrefix { get; set; } = "news";
    public string BrandsPrefix { get; set; } = "brands";
    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;
    public bool UsePathStyle { get; set; }
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
}
