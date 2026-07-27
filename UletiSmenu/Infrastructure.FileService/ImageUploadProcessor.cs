using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Infrastructure.FileService;

internal static class ImageUploadProcessor
{
    public const long MaxUploadBytes = 5 * 1024 * 1024;
    private const int MaxSourceDimension = 8000;
    private const long MaxSourcePixels = 25_000_000;
    private const int MaxProfileDimension = 1024;

    private static readonly HashSet<string> AllowedFormats =
        new(StringComparer.OrdinalIgnoreCase) { "JPEG", "PNG", "WEBP" };

    public static async Task<MemoryStream> ProcessToJpegAsync(
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
        var decoderOptions = new DecoderOptions { MaxFrames = 2 };
        using var image = await Image.LoadAsync(decoderOptions, input, cancellationToken);
        if (image.Frames.Count != 1)
            throw new ArgumentException("Animated images are not allowed.");

        image.Mutate(context => context.AutoOrient());

        if (image.Width > MaxProfileDimension || image.Height > MaxProfileDimension)
        {
            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxProfileDimension, MaxProfileDimension)
            }));
        }

        var output = new MemoryStream();
        await image.SaveAsJpegAsync(
            output,
            new JpegEncoder { Quality = 85 },
            cancellationToken);
        output.Position = 0;
        return output;
    }
}
