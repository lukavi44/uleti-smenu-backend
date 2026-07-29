using Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Email;

public class EmailService : IEmailService
{
    /// <summary>
    /// MailKit default is ~120s; keep failures fast (e.g. Render Free blocks outbound 25/465/587).
    /// </summary>
    private static readonly TimeSpan SmtpTimeout = TimeSpan.FromSeconds(15);

    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public Task<bool> SendConfirmEmailAsync(
        string toEmail,
        string confirmationLink,
        CancellationToken cancellationToken = default) =>
        SendEmailAsync(
            toEmail,
            "Confirm Your Email",
            EmailTemplates.ConfirmEmail(confirmationLink),
            cancellationToken: cancellationToken);

    public Task<bool> SendPasswordResetAsync(
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken = default) =>
        SendEmailAsync(
            toEmail,
            "Reset your UletiSmenu password",
            EmailTemplates.PasswordReset(resetLink),
            cancellationToken: cancellationToken);

    public Task<bool> SendWelcomeEmployerAsync(
        string toEmail,
        string? displayName,
        CancellationToken cancellationToken = default) =>
        SendEmailAsync(
            toEmail,
            "Welcome to UletiSmenu",
            EmailTemplates.WelcomeEmployer(displayName),
            cancellationToken: cancellationToken);

    public Task<bool> SendWelcomeEmployeeAsync(
        string toEmail,
        string? firstName,
        CancellationToken cancellationToken = default) =>
        SendEmailAsync(
            toEmail,
            "Welcome to UletiSmenu",
            EmailTemplates.WelcomeEmployee(firstName),
            cancellationToken: cancellationToken);

    public Task<bool> SendFavouriteJobPostAsync(
        string toEmail,
        string jobTitle,
        CancellationToken cancellationToken = default) =>
        SendEmailAsync(
            toEmail,
            "New restaurant shift available",
            EmailTemplates.NewFavouriteJobPost(jobTitle),
            cancellationToken: cancellationToken);

    public Task<bool> SendContactFormAsync(
        string name,
        string fromEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        var safeName = EmailInputSanitizer.SanitizeHeaderValue(name, 120);
        var safeEmail = EmailInputSanitizer.SanitizeHeaderValue(fromEmail, 256);
        var safeSubject = EmailInputSanitizer.SanitizeHeaderValue(subject, 160);
        var safeMessage = EmailInputSanitizer.SanitizeBody(message, 4000);

        if (!EmailInputSanitizer.LooksLikeEmail(safeEmail)
            || string.IsNullOrWhiteSpace(safeName)
            || string.IsNullOrWhiteSpace(safeSubject)
            || string.IsNullOrWhiteSpace(safeMessage))
        {
            _logger.LogWarning("Contact email skipped: validation failed after sanitization.");
            return Task.FromResult(false);
        }

        var inbox = string.IsNullOrWhiteSpace(_settings.ContactInbox)
            ? "support@uletismenu.com"
            : _settings.ContactInbox.Trim();

        return SendEmailAsync(
            inbox,
            $"[Contact] {safeSubject}",
            EmailTemplates.ContactNotification(safeName, safeEmail, safeSubject, safeMessage),
            replyTo: safeEmail,
            cancellationToken: cancellationToken);
    }

    public Task<bool> SendSmtpTestAsync(
        string toEmail,
        CancellationToken cancellationToken = default) =>
        SendEmailAsync(
            toEmail,
            "UletiSmenu SMTP test",
            EmailTemplates.TestEmail(),
            cancellationToken: cancellationToken);

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? replyTo = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Email skipped: recipient is empty.");
            return false;
        }

        if (!_settings.IsFullyConfigured())
        {
            _logger.LogWarning(
                "Email skipped: SMTP is incomplete (Host/Username/Password/FromEmail/ReplyToEmail). To={To} Subject={Subject}",
                toEmail,
                subject);
            return false;
        }

        // Never silently fall back to Username or another address when FromEmail is blank —
        // IsFullyConfigured already requires FromEmail.
        var fromEmail = _settings.FromEmail.Trim();
        var fromName = string.IsNullOrWhiteSpace(_settings.FromName)
            ? "UletiSmenu"
            : _settings.FromName.Trim();
        var defaultReplyTo = _settings.ReplyToEmail.Trim();

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
            message.Subject = EmailInputSanitizer.SanitizeHeaderValue(subject, 200);

            var replyToAddress = string.IsNullOrWhiteSpace(replyTo)
                ? defaultReplyTo
                : EmailInputSanitizer.SanitizeHeaderValue(replyTo, 256);
            if (!string.IsNullOrWhiteSpace(replyToAddress) && EmailInputSanitizer.LooksLikeEmail(replyToAddress))
                message.ReplyTo.Add(MailboxAddress.Parse(replyToAddress));

            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient { Timeout = (int)SmtpTimeout.TotalMilliseconds };
            var secureSocketOptions = _settings.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            try
            {
                await client.ConnectAsync(_settings.Host, _settings.Port, secureSocketOptions, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Connectivity failed before AuthenticateAsync — common when outbound SMTP is blocked
                // (Render Free blocks ports 25, 465, and 587).
                _logger.LogError(
                    ex,
                    "SMTP network connectivity failed before authentication. Host={Host} Port={Port}. " +
                    "To={To} Subject={Subject}. Check firewall/outbound SMTP (Render Free blocks 25/465/587).",
                    _settings.Host,
                    _settings.Port,
                    toEmail,
                    subject);
                return false;
            }

            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email sent. To={To} Subject={Subject} From={From}",
                toEmail,
                subject,
                fromEmail);
            return true;
        }
        catch (Exception ex)
        {
            // Do not log username/password; MailKit exceptions may include host names only.
            _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}.", toEmail, subject);
            return false;
        }
    }
}
