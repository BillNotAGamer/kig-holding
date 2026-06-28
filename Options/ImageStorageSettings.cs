namespace KIGHolding.Options;

public sealed class ImageStorageSettings
{
    public string Provider { get; set; } = "LocalVolume";
    public string RootPath { get; set; } = "App_Data/uploads";
    public string PublicBasePath { get; set; } = "/uploads";
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
}
