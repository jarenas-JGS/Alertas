using Alertas.Services.Storage.Interfaces;
using Alertas.Services.Storage.Models;

namespace Alertas.Services.Storage.Local
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;

        public LocalFileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string BuildObjectKey(string folder, string fileName)
        {
            return Path.Combine(folder, fileName)
                .Replace("\\", "/");
        }

        public async Task<FileUploadResult> UploadAsync(
            IFormFile file,
            string folder)
        {
            var extension = Path.GetExtension(file.FileName);

            var fileName =
                $"{Guid.NewGuid()}{extension}";

            var objectKey =
                BuildObjectKey(folder, fileName);

            var uploadsPath = Path.Combine(
                _env.WebRootPath,
                "uploads");

            var fullPath = Path.Combine(
                uploadsPath,
                objectKey);

            var directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return new FileUploadResult
            {
                ObjectKey = objectKey,
                BucketName = "local",
                MimeType = file.ContentType,
                FileSize = file.Length,
                OriginalFileName = file.FileName,
                Extension = extension
            };
        }

        public async Task<byte[]> DownloadAsync(string objectKey)
        {
            var uploadsPath = Path.Combine(
                _env.WebRootPath,
                "uploads");

            var fullPath = Path.Combine(
                uploadsPath,
                objectKey);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException();

            return await File.ReadAllBytesAsync(fullPath);
        }

        public async Task DeleteAsync(string objectKey)
        {
            var uploadsPath = Path.Combine(
                _env.WebRootPath,
                "uploads");

            var fullPath = Path.Combine(
                uploadsPath,
                objectKey);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            await Task.CompletedTask;
        }
    }
}