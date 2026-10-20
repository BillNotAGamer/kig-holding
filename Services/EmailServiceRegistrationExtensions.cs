using KIGHolding.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Resend;

namespace KIGHolding.Services;

public static class EmailServiceRegistrationExtensions
{
    public static IServiceCollection AddConfiguredEmailService(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var resendSection = configuration.GetSection("ResendSettings");
        services.Configure<ResendSettings>(resendSection);

        var settings = resendSection.Get<ResendSettings>() ?? new ResendSettings();

        if (OutboundEmailDeliveryPolicy.IsLiveDeliveryAuthorized(environment, settings))
        {
            services.AddResend(options =>
            {
                options.ApiToken = settings.ApiKey.Trim();
            });
            services.AddScoped<IEmailService, EmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, NoOpEmailService>();
        }

        return services;
    }
}
