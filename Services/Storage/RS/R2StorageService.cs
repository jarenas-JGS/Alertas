using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Alertas.Services.Storage.Interfaces;
using Alertas.Services.Storage.Models;
using Microsoft.Extensions.Options;

namespace Alertas.Services.Storage.R2
{
    public class R2StorageService : IFileStorageService
    {
        private readonly StorageSettings _settings;
        private readonly IAmazonS3 _s3Client;

        public R2StorageService(IOptions<StorageSettings> options)
        {
            _settings = options.Value;

            var credentials = new BasicAWSCredentials(
                _settings.AccessKey,
                _settings.SecretKey);

            var config = new AmazonS3Config
            {
                ServiceURL = _settings.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = "auto"
            };

            _s3Client = new AmazonS3Client(credentials, config);
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

            var fileName = $"{Guid.NewGuid()}{extension}";

            var objectKey = BuildObjectKey(folder, fileName);

            using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(request);

            return new FileUploadResult
            {
                ObjectKey = objectKey,
                BucketName = _settings.BucketName,
                MimeType = file.ContentType,
                FileSize = file.Length,
                OriginalFileName = file.FileName,
                Extension = extension
            };
        }

        public async Task<byte[]> DownloadAsync(string objectKey)
        {
            var request = new GetObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey
            };

            using var response = await _s3Client.GetObjectAsync(request);
            using var memoryStream = new MemoryStream();

            await response.ResponseStream.CopyToAsync(memoryStream);

            return memoryStream.ToArray();
        }

        public async Task DeleteAsync(string objectKey)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey
            };

            await _s3Client.DeleteObjectAsync(request);
        }
    }
}