using Infrastructure.Email;

namespace UletiSmenu.Tests.Email;

public class EmailInputSanitizerTests
{
    [Theory]
    [InlineData("Alice\r\nBcc: evil@x.com", "Alice Bcc: evil@x.com")]
    [InlineData("Bob\nX-Inject: 1", "Bob X-Inject: 1")]
    [InlineData("  Trim me  ", "Trim me")]
    public void SanitizeHeaderValue_strips_crlf(string input, string expected)
    {
        Assert.Equal(expected, EmailInputSanitizer.SanitizeHeaderValue(input, 120));
    }

    [Fact]
    public void SanitizeHeaderValue_enforces_max_length()
    {
        var value = new string('a', 200);
        Assert.Equal(10, EmailInputSanitizer.SanitizeHeaderValue(value, 10).Length);
    }

    [Fact]
    public void SanitizeBody_caps_length_and_strips_nulls()
    {
        var body = "hello\0world" + new string('x', 50);
        var sanitized = EmailInputSanitizer.SanitizeBody(body, 20);
        Assert.DoesNotContain('\0', sanitized);
        Assert.True(sanitized.Length <= 20);
    }

    [Theory]
    [InlineData("support@uletismenu.com", true)]
    [InlineData("bad\r\naddr@x.com", false)]
    [InlineData("not-an-email", false)]
    [InlineData("", false)]
    public void LooksLikeEmail_validates(string input, bool expected)
    {
        Assert.Equal(expected, EmailInputSanitizer.LooksLikeEmail(input));
    }
}

public class SmtpSettingsTests
{
    [Fact]
    public void IsFullyConfigured_requires_from_and_reply_to()
    {
        var settings = new SmtpSettings
        {
            Host = "smtppro.zoho.eu",
            Username = "support@uletismenu.com",
            Password = "secret",
            FromEmail = "",
            ReplyToEmail = "support@uletismenu.com"
        };

        Assert.False(settings.IsFullyConfigured());

        settings.FromEmail = "noreply@uletismenu.com";
        Assert.True(settings.IsFullyConfigured());
    }
}

public class EmailTemplatesTests
{
    [Fact]
    public void Templates_include_support_footer_and_branding()
    {
        var html = EmailTemplates.ConfirmEmail("https://example.com/confirm");
        Assert.Contains("UletiSmenu", html);
        Assert.Contains("support@uletismenu.com", html);
        Assert.Contains("https://example.com/confirm", html);

        Assert.Contains("Welcome", EmailTemplates.WelcomeEmployer("Gradska"));
        Assert.Contains("Welcome", EmailTemplates.WelcomeEmployee("Ana"));
        Assert.Contains("Reset", EmailTemplates.PasswordReset("https://example.com/reset"));
        Assert.Contains("shift", EmailTemplates.NewFavouriteJobPost("Bartender"));
        Assert.Contains("Contact form", EmailTemplates.ContactNotification("A", "a@b.com", "Hi", "Body"));
        Assert.Contains("SMTP test", EmailTemplates.TestEmail());
    }
}
