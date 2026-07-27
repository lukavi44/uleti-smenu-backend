using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Core.Services;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.FileService;

public class AzureBlobFileService : IFileService
{
    private readonly BlobContainerClient _container;

    public AzureBlobFileService(BlobContainerClient container) =>
        _container = container;

    public async Task<string> UploadImageAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        await using var jpeg = await ImageUploadProcessor.ProcessToJpegAsync(file, cancellationToken);

        var fileName = $"{Guid.NewGuid():N}.jpg";
        var blob = _container.GetBlobClient(fileName);
        await blob.UploadAsync(
            jpeg,
            new BlobHttpHeaders { ContentType = "image/jpeg" },
            cancellationToken: cancellationToken);

        return $"{UploadPathHelper.Prefix}{fileName}";
    }

    public async Task DeleteImageAsync(
        string? relativePath,
        CancellationToken cancellationToken = default)
    {
        if (!UploadPathHelper.TryGetFileName(relativePath, out var fileName))
            return;

        await _container.GetBlobClient(fileName).DeleteIfExistsAsync(
            cancellationToken: cancellationToken);
    }
}
