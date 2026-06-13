namespace KIGHolding.Options;

public sealed class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string FolderPrefix { get; set; } = "kig-holding";
}
