using KIGHolding.Options;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services;

public sealed class AdminBootstrapConfigurationResolver
{
    private const int AdminUsernameMaxLength = 120;
    private const int AdminEmailMaxLength = 256;

    private readonly AdminBootstrapSettings _adminBootstrapSettings;

    public AdminBootstrapConfigurationResolver(IOptions<AdminBootstrapSettings> adminBootstrapSettings)
    {
        _adminBootstrapSettings = adminBootstrapSettings.Value;
    }

    public bool IsLegacyRemediationEnabled => _adminBootstrapSettings.RemediateLegacySeed;

    public BootstrapAdminConfiguration? TryGetConfiguration()
    {
        var username = _adminBootstrapSettings.Username?.Trim();
        var password = _adminBootstrapSettings.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        if (username.Length > AdminUsernameMaxLength)
        {
            throw new InvalidOperationException("AdminBootstrap__Username exceeds the maximum supported length.");
        }

        var passwordErrors = AdminPasswordPolicy.Validate(password);
        if (passwordErrors.Count > 0)
        {
            throw new InvalidOperationException("AdminBootstrap__Password does not satisfy the admin password policy.");
        }

        var email = string.IsNullOrWhiteSpace(_adminBootstrapSettings.Email)
            ? null
            : _adminBootstrapSettings.Email.Trim();

        if (!string.IsNullOrWhiteSpace(email) && email.Length > AdminEmailMaxLength)
        {
            throw new InvalidOperationException("AdminBootstrap__Email exceeds the maximum supported length.");
        }

        return new BootstrapAdminConfiguration(
            username,
            password,
            email,
            AdminEmailNormalizer.Normalize(email));
    }

    public BootstrapAdminConfiguration GetRequiredConfiguration(string missingConfigurationMessage)
    {
        return TryGetConfiguration()
            ?? throw new InvalidOperationException(missingConfigurationMessage);
    }

    public sealed record BootstrapAdminConfiguration(
        string Username,
        string Password,
        string? Email,
        string? NormalizedEmail);
}
