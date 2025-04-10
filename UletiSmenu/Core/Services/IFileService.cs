using Microsoft.AspNetCore.Http;

namespace Core.Services
{
    public interface IFileService
    {
        /// <summary>
        /// Uploads an image file and returns the relative path where it is stored.
        /// </summary>
        /// <param name="file">The image file to upload.</param>
        /// <returns>The relative path of the uploaded file.</returns>
        Task<string> UploadImageAsync(IFormFile file);
    }
}
