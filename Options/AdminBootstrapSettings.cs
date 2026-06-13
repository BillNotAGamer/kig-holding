namespace KIGHolding.Options;

public sealed class AdminBootstrapSettings
{
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? Email { get; init; }
    public bool RemediateLegacySeed { get; init; }
}
