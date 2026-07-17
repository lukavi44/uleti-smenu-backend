
using Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

public class FileService : IFileService
{
    public const long MaxUploadBytes = 5 * 1024 * 1024;
    private const int MaxSourceDimension = 8000;
    private const long MaxSourcePixels = 25_000_000;
    private const int MaxProfileDimension = 1024;
    private static readonly HashSet<string> AllowedFormats =
        new(StringComparer.OrdinalIgnoreCase) { "JPEG", "PNG", "WEBP" };

    private readonly string _uploadPath;

    public FileService(IConfiguration configuration)
    {
        _uploadPath = configuration["FileSettings:UploadPath"] ?? "wwwroot/uploads";
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> UploadImageAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Invalid file.");

        if (file.Length > MaxUploadBytes)
            throw new ArgumentException("Image must be 5 MB or smaller.");

        await using var input = file.OpenReadStream();

        ImageInfo imageInfo;
        IImageFormat detectedFormat;
        try
        {
            imageInfo = await Image.IdentifyAsync(input, cancellationToken)
                ?? throw new ArgumentException("The uploaded file is not a valid image.");
            detectedFormat = imageInfo.Metadata.DecodedImageFormat
                ?? throw new ArgumentException("The uploaded file format could not be detected.");
        }
        catch (UnknownImageFormatException)
        {
            throw new ArgumentException("Only JPEG, PNG, and WebP images are allowed.");
        }
        catch (InvalidImageContentException)
        {
            throw new ArgumentException("The uploaded image is invalid or corrupted.");
        }

        if (!AllowedFormats.Contains(detectedFormat.Name))
            throw new ArgumentException("Only JPEG, PNG, and WebP images are allowed.");

        if (imageInfo.Width <= 0 ||
            imageInfo.Height <= 0 ||
            imageInfo.Width > MaxSourceDimension ||
            imageInfo.Height > MaxSourceDimension ||
            (long)imageInfo.Width * imageInfo.Height > MaxSourcePixels)
        {
            throw new ArgumentException("Image dimensions are too large.");
        }

        input.Position = 0;
        using var image = await Image.LoadAsync(input, cancellationToken);
        image.Mutate(context => context.AutoOrient());

        if (image.Width > MaxProfileDimension || image.Height > MaxProfileDimension)
        {
            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxProfileDimension, MaxProfileDimension)
            }));
        }

        var fileName = $"{Guid.NewGuid():N}.jpg";
        var filePath = Path.Combine(_uploadPath, fileName);

        try
        {
            await image.SaveAsJpegAsync(
                filePath,
                new JpegEncoder { Quality = 85 },
                cancellationToken);
        }
        catch
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            throw;
        }

        return $"/uploads/{fileName}";
    }

    public Task DeleteImageAsync(
        string? relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.CompletedTask;

        const string uploadPrefix = "/uploads/";
        if (!relativePath.StartsWith(uploadPrefix, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var fileName = Path.GetFileName(relativePath);
        if (string.IsNullOrWhiteSpace(fileName) ||
            !string.Equals(relativePath, uploadPrefix + fileName, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var fullUploadPath = Path.GetFullPath(_uploadPath);
        var fullFilePath = Path.GetFullPath(Path.Combine(fullUploadPath, fileName));
        if (!fullFilePath.StartsWith(
                fullUploadPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(fullFilePath))
            File.Delete(fullFilePath);

        return Task.CompletedTask;
    }
}
