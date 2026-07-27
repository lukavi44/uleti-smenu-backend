namespace Infrastructure.FileService;

public static class UploadPathHelper
{
    public const string Prefix = "/uploads/";

    public static bool TryGetFileName(string? relativePath, out string fileName)
    {
        fileName = string.Empty;

        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        if (!relativePath.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        fileName = Path.GetFileName(relativePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return string.Equals(relativePath, Prefix + fileName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSafeFileName(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
        !fileName.Contains('/', StringComparison.Ordinal) &&
        !fileName.Contains('\\', StringComparison.Ordinal) &&
        !fileName.Contains("..", StringComparison.Ordinal);
}
