
using Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

public class FileService : IFileService
{
    private readonly string _uploadPath;

    public FileService(IConfiguration configuration)
    {
        _uploadPath = configuration["FileSettings:UploadPath"] ?? "wwwroot/uploads";
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Invalid file.");
        }

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{fileName}";
    }
}
