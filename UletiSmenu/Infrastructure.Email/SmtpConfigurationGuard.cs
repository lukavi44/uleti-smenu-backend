using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public static class SmtpConfigurationGuard
{
    public static void ValidateAtStartup(IHostEnvironment environment, IServiceProvider services)
    {
        var settings = services.GetRequiredService<IOptions<SmtpSettings>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SmtpConfiguration");

        if (settings.IsFullyConfigured())
        {
            logger.LogInformation(
                "SMTP configured. Host={Host} Username={Username} From={From} ReplyTo={ReplyTo}",
                settings.Host,
                settings.Username,
                settings.FromEmail,
                settings.ReplyToEmail);
            return;
        }

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(settings.Host)) missing.Add(nameof(SmtpSettings.Host));
        if (string.IsNullOrWhiteSpace(settings.Username)) missing.Add(nameof(SmtpSettings.Username));
        if (string.IsNullOrWhiteSpace(settings.Password)) missing.Add(nameof(SmtpSettings.Password));
        if (string.IsNullOrWhiteSpace(settings.FromEmail)) missing.Add(nameof(SmtpSettings.FromEmail));
        if (string.IsNullOrWhiteSpace(settings.ReplyToEmail)) missing.Add(nameof(SmtpSettings.ReplyToEmail));

        var detail = string.Join(", ", missing);

        if (environment.IsProduction())
        {
            logger.LogCritical(
                "SMTP is not fully configured for Production. Missing: {Missing}. Set SmtpSettings__* App Settings (password via Portal/Key Vault only).",
                detail);
            throw new InvalidOperationException(
                $"SMTP is not fully configured for Production. Missing: {detail}.");
        }

        logger.LogWarning(
            "SMTP is not fully configured. Missing: {Missing}. Emails will be skipped until settings are provided.",
            detail);
    }
}
