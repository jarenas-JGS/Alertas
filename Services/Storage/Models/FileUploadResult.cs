namespace Alertas.Services.Storage.Models
{
    public class FileUploadResult
    {
        public string ObjectKey { get; set; } = string.Empty;

        public string BucketName { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;
    }
}