namespace Infrastructure.Email;

public class SmtpSettings
{
    public const string SectionName = "SmtpSettings";

    /// <summary>Zoho SMTP host (EU): smtppro.zoho.eu</summary>
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    /// <summary>SMTP auth username. Production: support@uletismenu.com</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Zoho app password. Never commit a real value.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Envelope From. Must be an allowed Zoho "Send Mail As" alias (noreply@...).
    /// Never leave empty in Production — there is no silent fallback to Username.
    /// </summary>
    public string FromEmail { get; set; } = "noreply@uletismenu.com";

    public string FromName { get; set; } = "UletiSmenu";

    /// <summary>Default Reply-To for transactional mail.</summary>
    public string ReplyToEmail { get; set; } = "support@uletismenu.com";

    /// <summary>Fixed recipient for the public contact form.</summary>
    public string ContactInbox { get; set; } = "support@uletismenu.com";

    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Required for POST /api/v1/debug/test-email outside Development.
    /// Send as header X-Email-Debug-Key. Never commit a real value.
    /// </summary>
    public string DebugApiKey { get; set; } = string.Empty;

    public bool IsFullyConfigured() =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(FromEmail)
        && !string.IsNullOrWhiteSpace(ReplyToEmail);
}
