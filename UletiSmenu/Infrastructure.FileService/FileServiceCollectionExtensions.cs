using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.FileService;

public static class FileServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileSettings>(configuration.GetSection(FileSettings.SectionName));

        var settings = configuration
            .GetSection(FileSettings.SectionName)
            .Get<FileSettings>() ?? new FileSettings();

        if (settings.UseAzureBlob)
        {
            if (string.IsNullOrWhiteSpace(settings.BlobConnectionString))
            {
                throw new InvalidOperationException(
                    "FileSettings:BlobConnectionString is required when Provider is AzureBlob.");
            }

            services.AddSingleton(_ => new BlobServiceClient(settings.BlobConnectionString));
            services.AddSingleton(sp =>
            {
                var blobService = sp.GetRequiredService<BlobServiceClient>();
                var container = blobService.GetBlobContainerClient(settings.BlobContainerName);
                container.CreateIfNotExists(PublicAccessType.None);
                return container;
            });
            services.AddScoped<IFileService, AzureBlobFileService>();
        }
        else
        {
            services.AddScoped<IFileService, LocalFileService>();
        }

        return services;
    }
}
