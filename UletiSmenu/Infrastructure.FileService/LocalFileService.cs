using Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.FileService;

public class LocalFileService : IFileService
{
    private readonly string _uploadPath;

    public LocalFileService(IConfiguration configuration)
    {
        _uploadPath = configuration["FileSettings:UploadPath"] ?? "wwwroot/uploads";
        if (!Directory.Exists(_uploadPath))
            Directory.CreateDirectory(_uploadPath);
    }

    public async Task<string> UploadImageAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        await using var jpeg = await ImageUploadProcessor.ProcessToJpegAsync(file, cancellationToken);

        var fileName = $"{Guid.NewGuid():N}.jpg";
        var filePath = Path.Combine(_uploadPath, fileName);

        try
        {
            await using var output = File.Create(filePath);
            await jpeg.CopyToAsync(output, cancellationToken);
        }
        catch
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            throw;
        }

        return $"{UploadPathHelper.Prefix}{fileName}";
    }

    public Task DeleteImageAsync(
        string? relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!UploadPathHelper.TryGetFileName(relativePath, out var fileName))
            return Task.CompletedTask;

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
