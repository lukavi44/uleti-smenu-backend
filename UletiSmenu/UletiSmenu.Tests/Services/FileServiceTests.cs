using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace UletiSmenu.Tests.Services
{
    public class FileServiceTests : IDisposable
    {
        private static readonly byte[] OnePixelPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        private readonly string _uploadPath =
            Path.Combine(Path.GetTempPath(), "uletismenu-file-tests", Guid.NewGuid().ToString("N"));

        [Fact]
        public async Task UploadImageAsync_DecodesAndReencodesAllowedImage()
        {
            var service = CreateService();
            await using var stream = new MemoryStream(OnePixelPng);
            var file = new FormFile(stream, 0, stream.Length, "file", "payload.html")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/html"
            };

            var relativePath = await service.UploadImageAsync(file);

            Assert.StartsWith("/uploads/", relativePath);
            Assert.EndsWith(".jpg", relativePath);
            var storedPath = Path.Combine(_uploadPath, Path.GetFileName(relativePath));
            Assert.True(File.Exists(storedPath));
            var signature = await File.ReadAllBytesAsync(storedPath);
            Assert.True(signature.Length >= 2);
            Assert.Equal(0xFF, signature[0]);
            Assert.Equal(0xD8, signature[1]);
        }

        [Fact]
        public async Task UploadImageAsync_RejectsNonImageContent()
        {
            var service = CreateService();
            await using var stream = new MemoryStream("not an image"u8.ToArray());
            var file = new FormFile(stream, 0, stream.Length, "file", "attack.svg");

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.UploadImageAsync(file));

            Assert.Contains("JPEG, PNG, and WebP", exception.Message);
            Assert.Empty(Directory.GetFiles(_uploadPath));
        }

        [Fact]
        public async Task UploadImageAsync_RejectsFilesOverFiveMegabytes()
        {
            var service = CreateService();
            await using var stream = new MemoryStream(new byte[1]);
            var file = new FormFile(
                stream,
                0,
                FileService.MaxUploadBytes + 1,
                "file",
                "large.png");

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.UploadImageAsync(file));

            Assert.Contains("5 MB", exception.Message);
        }

        [Fact]
        public async Task UploadImageAsync_RejectsAnimatedImages()
        {
            var service = CreateService();
            await using var stream = new MemoryStream();
            using (var image = new Image<Rgba32>(2, 2))
            using (var secondFrame = new Image<Rgba32>(2, 2))
            {
                image.Frames.AddFrame(secondFrame.Frames.RootFrame);
                await image.SaveAsync(stream, new WebpEncoder());
            }

            stream.Position = 0;
            var file = new FormFile(stream, 0, stream.Length, "file", "animated.webp");

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.UploadImageAsync(file));

            Assert.Contains("Animated images", exception.Message);
            Assert.Empty(Directory.GetFiles(_uploadPath));
        }

        [Fact]
        public async Task DeleteImageAsync_OnlyDeletesDirectUploadFiles()
        {
            var service = CreateService();
            var storedPath = Path.Combine(_uploadPath, "owned.jpg");
            await File.WriteAllBytesAsync(storedPath, OnePixelPng);
            var outsidePath = Path.Combine(Path.GetDirectoryName(_uploadPath)!, "outside.jpg");
            await File.WriteAllBytesAsync(outsidePath, OnePixelPng);

            await service.DeleteImageAsync("/uploads/../outside.jpg");
            await service.DeleteImageAsync("/uploads/owned.jpg");

            Assert.True(File.Exists(outsidePath));
            Assert.False(File.Exists(storedPath));
            File.Delete(outsidePath);
        }

        private FileService CreateService()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FileSettings:UploadPath"] = _uploadPath
                })
                .Build();

            return new FileService(configuration);
        }

        public void Dispose()
        {
            if (Directory.Exists(_uploadPath))
                Directory.Delete(_uploadPath, recursive: true);
        }
    }
}
