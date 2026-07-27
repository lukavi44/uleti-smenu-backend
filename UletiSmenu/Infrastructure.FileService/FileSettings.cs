namespace Infrastructure.FileService;

public class FileSettings
{
    public const string SectionName = "FileSettings";

    /// <summary>Local (disk) or AzureBlob.</summary>
    public string Provider { get; set; } = "Local";

    public string UploadPath { get; set; } = "uploads";

    public string? BlobConnectionString { get; set; }

    public string BlobContainerName { get; set; } = "uploads";

    public bool UseAzureBlob =>
        Provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase);
}
