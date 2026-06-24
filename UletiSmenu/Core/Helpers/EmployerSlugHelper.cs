using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Helpers
{
    public static class EmployerSlugHelper
    {
        public static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "restaurant";
            }

            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
                else if (character is ' ' or '-' or '_')
                {
                    builder.Append('-');
                }
            }

            var slug = Regex.Replace(builder.ToString(), "-{2,}", "-").Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? "restaurant" : slug;
        }
    }
}
