using System.Net.Mime;
using Azure.Storage.Blobs;
using Infrastructure.FileService;

namespace API.Endpoints;

public static class UploadsEndpoints
{
    public static RouteGroupBuilder MapUploads(this WebApplication app, FileSettings settings)
    {
        var group = app.MapGroup("/uploads");

        if (settings.UseAzureBlob)
        {
            group.MapGet("/{fileName}", async (
                string fileName,
                BlobContainerClient container,
                CancellationToken cancellationToken) =>
            {
                if (!UploadPathHelper.IsSafeFileName(fileName))
                    return Results.NotFound();

                var blob = container.GetBlobClient(fileName);
                if (!await blob.ExistsAsync(cancellationToken))
                    return Results.NotFound();

                var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
                return Results.File(
                    download.Value.Content,
                    MediaTypeNames.Image.Jpeg,
                    enableRangeProcessing: true);
            }).DisableRateLimiting();
        }

        return group;
    }
}
