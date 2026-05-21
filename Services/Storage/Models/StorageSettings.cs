namespace Alertas.Services.Storage.Models
{
    public class StorageSettings
    {
        public string Provider { get; set; } = "Local";
        public string BucketName { get; set; } = string.Empty;
        public string ServiceUrl { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public int MaxFileSizeMb { get; set; } = 2;
    }
}