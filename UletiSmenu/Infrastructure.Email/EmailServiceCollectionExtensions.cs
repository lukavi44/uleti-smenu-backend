using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Email;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddUletiSmenuEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddTransient<IEmailService, EmailService>();
        return services;
    }
}
