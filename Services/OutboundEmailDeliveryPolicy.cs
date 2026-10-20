using KIGHolding.Options;
using Microsoft.Extensions.Hosting;

namespace KIGHolding.Services;

public static class OutboundEmailDeliveryPolicy
{
    public const string SuppressedPolicyLogMessage = "Outbound email suppressed by configured delivery policy.";

    public static bool IsLiveDeliveryAuthorized(IHostEnvironment? environment, ResendSettings? settings)
    {
        if (environment is null || !environment.IsProduction())
        {
            return false;
        }

        if (settings is null || !settings.EnableOutboundEmail)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            return false;
        }

        return true;
    }
}
