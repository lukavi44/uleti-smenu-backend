using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Infrastructure.Email;

public static class EmailInputSanitizer
{
    private static readonly Regex ControlChars = new(@"[\r\n\u0000-\u001F\u007F]", RegexOptions.Compiled);

    public static string SanitizeHeaderValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var cleaned = ControlChars.Replace(value.Trim(), " ");
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim();
        if (cleaned.Length > maxLength)
            cleaned = cleaned[..maxLength].Trim();

        return cleaned;
    }

    public static string SanitizeBody(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var cleaned = value.Replace("\0", string.Empty).Trim();
        if (cleaned.Length > maxLength)
            cleaned = cleaned[..maxLength];

        return cleaned;
    }

    public static bool LooksLikeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            _ = new MailAddress(value.Trim());
            return !ControlChars.IsMatch(value);
        }
        catch
        {
            return false;
        }
    }
}
