using System.Net;
using System.Text;

namespace Infrastructure.Email;

public static class EmailTemplates
{
    private const string BrandPrimary = "#2563eb";
    private const string BrandDark = "#0f172a";
    private const string BrandMuted = "#64748b";
    private const string BrandBg = "#f8fafc";
    private const string SiteUrl = "https://www.uletismenu.com";
    private const string SupportEmail = "support@uletismenu.com";

    public static string Wrap(string title, string bodyHtml, string? ctaUrl = null, string? ctaLabel = null)
    {
        var button = string.Empty;
        if (!string.IsNullOrWhiteSpace(ctaUrl) && !string.IsNullOrWhiteSpace(ctaLabel))
        {
            var safeUrl = WebUtility.HtmlEncode(ctaUrl);
            var safeLabel = WebUtility.HtmlEncode(ctaLabel);
            button = $"""
                <table role="presentation" cellpadding="0" cellspacing="0" style="margin:24px 0 8px;">
                  <tr>
                    <td style="border-radius:10px;background:{BrandPrimary};">
                      <a href="{safeUrl}" style="display:inline-block;padding:12px 22px;color:#ffffff;text-decoration:none;font-weight:700;font-size:15px;">
                        {safeLabel}
                      </a>
                    </td>
                  </tr>
                </table>
                """;
        }

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{WebUtility.HtmlEncode(title)}</title>
            </head>
            <body style="margin:0;padding:0;background:{BrandBg};font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:{BrandDark};">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{BrandBg};padding:28px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e2e8f0;">
                      <tr>
                        <td style="padding:22px 28px;background:{BrandDark};color:#ffffff;">
                          <div style="font-size:20px;font-weight:800;letter-spacing:-0.02em;">UletiSmenu</div>
                          <div style="font-size:13px;opacity:0.85;margin-top:4px;">{WebUtility.HtmlEncode(title)}</div>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:28px;">
                          {bodyHtml}
                          {button}
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:18px 28px 26px;border-top:1px solid #e2e8f0;color:{BrandMuted};font-size:13px;line-height:1.55;">
                          <p style="margin:0 0 8px;">Regards,</p>
                          <p style="margin:0 0 8px;"><strong>UletiSmenu Support Team</strong></p>
                          <p style="margin:0 0 4px;">
                            <a href="mailto:{SupportEmail}" style="color:{BrandPrimary};text-decoration:none;">{SupportEmail}</a>
                          </p>
                          <p style="margin:0;">
                            <a href="{SiteUrl}" style="color:{BrandPrimary};text-decoration:none;">{SiteUrl}</a>
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    public static string ConfirmEmail(string confirmationLink) =>
        Wrap(
            "Confirm your email",
            """
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">Welcome to UletiSmenu.</p>
            <p style="margin:0;font-size:15px;line-height:1.6;color:#334155;">
              Please confirm your email address to finish setting up your account.
            </p>
            """,
            confirmationLink,
            "Confirm email");

    public static string PasswordReset(string resetLink) =>
        Wrap(
            "Reset your password",
            """
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">Password reset requested</p>
            <p style="margin:0;font-size:15px;line-height:1.6;color:#334155;">
              Click the button below to choose a new password. If you did not request this, you can ignore this email.
            </p>
            """,
            resetLink,
            "Reset password");

    public static string WelcomeEmployer(string? name)
    {
        var greeting = string.IsNullOrWhiteSpace(name)
            ? "Welcome to UletiSmenu"
            : $"Welcome, {WebUtility.HtmlEncode(name.Trim())}";

        return Wrap(
            "Welcome, employer",
            $"""
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">{greeting}</p>
            <p style="margin:0;font-size:15px;line-height:1.6;color:#334155;">
              Your restaurant account is ready. Complete your profile, then publish shifts when you need cover.
            </p>
            """,
            "https://app.uletismenu.com/login",
            "Go to UletiSmenu");
    }

    public static string WelcomeEmployee(string? firstName)
    {
        var greeting = string.IsNullOrWhiteSpace(firstName)
            ? "Welcome to UletiSmenu"
            : $"Welcome, {WebUtility.HtmlEncode(firstName.Trim())}";

        return Wrap(
            "Welcome, candidate",
            $"""
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">{greeting}</p>
            <p style="margin:0;font-size:15px;line-height:1.6;color:#334155;">
              Your candidate account is ready. Browse open shifts and apply when the timing works for you.
            </p>
            """,
            "https://app.uletismenu.com/login",
            "Go to UletiSmenu");
    }

    public static string NewFavouriteJobPost(string jobTitle) =>
        Wrap(
            "New shift available",
            $"""
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;">A restaurant you follow posted a new shift</p>
            <p style="margin:0;font-size:15px;line-height:1.6;color:#334155;">
              <strong>{WebUtility.HtmlEncode(jobTitle)}</strong> is now open for applications.
            </p>
            """,
            "https://app.uletismenu.com/oglasi-za-posao",
            "View job posts");

    public static string ContactNotification(string name, string email, string subject, string message)
    {
        var sb = new StringBuilder();
        sb.Append("<p style=\"margin:0 0 12px;font-size:16px;line-height:1.6;\">New contact form message</p>");
        sb.Append("<p style=\"margin:0 0 8px;font-size:14px;color:#334155;\"><strong>Name:</strong> ");
        sb.Append(WebUtility.HtmlEncode(name));
        sb.Append("</p>");
        sb.Append("<p style=\"margin:0 0 8px;font-size:14px;color:#334155;\"><strong>Email:</strong> ");
        sb.Append(WebUtility.HtmlEncode(email));
        sb.Append("</p>");
        sb.Append("<p style=\"margin:0 0 8px;font-size:14px;color:#334155;\"><strong>Subject:</strong> ");
        sb.Append(WebUtility.HtmlEncode(subject));
        sb.Append("</p>");
        sb.Append("<p style=\"margin:16px 0 0;font-size:14px;line-height:1.6;color:#334155;white-space:pre-wrap;\">");
        sb.Append(WebUtility.HtmlEncode(message));
        sb.Append("</p>");
        return Wrap("Contact form", sb.ToString());
    }

    public static string TestEmail() =>
        Wrap(
            "SMTP test",
            """
            <p style="margin:0;font-size:15px;line-height:1.6;color:#334155;">
              This message confirms that UletiSmenu SMTP (Zoho Mail) is configured correctly.
            </p>
            """);
}
