using Alertas.Services.Storage.Models;

namespace Alertas.Services.Storage.Interfaces
{
    public interface IFileStorageService
    {
        Task<FileUploadResult> UploadAsync(
            IFormFile file,
            string folder);

        Task<byte[]> DownloadAsync(
            string objectKey);

        Task DeleteAsync(
            string objectKey);

        string BuildObjectKey(
            string folder,
            string fileName);
    }
}