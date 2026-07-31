namespace Core.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an HTML email. Returns false on failure; never throws for SMTP/transport errors.
    /// </summary>
    Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? replyTo = null,
        CancellationToken cancellationToken = default);

    Task<bool> SendConfirmEmailAsync(
        string toEmail,
        string confirmationLink,
        CancellationToken cancellationToken = default);

    Task<bool> SendPasswordResetAsync(
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken = default);

    Task<bool> SendWelcomeEmployerAsync(
        string toEmail,
        string? displayName,
        CancellationToken cancellationToken = default);

    Task<bool> SendWelcomeEmployeeAsync(
        string toEmail,
        string? firstName,
        CancellationToken cancellationToken = default);

    Task<bool> SendFavouriteJobPostAsync(
        string toEmail,
        string jobTitle,
        CancellationToken cancellationToken = default);

    Task<bool> SendApplicationReceivedAsync(
        string toEmail,
        string applicantName,
        string jobTitle,
        CancellationToken cancellationToken = default);

    Task<bool> SendContactFormAsync(
        string name,
        string fromEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default);

    Task<bool> SendSmtpTestAsync(
        string toEmail,
        CancellationToken cancellationToken = default);
}
