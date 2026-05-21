namespace Alertas.Services.Storage.Models
{
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }

        public static FileValidationResult Ok() => new() { IsValid = true };

        public static FileValidationResult Error(string message) => new()
        {
            IsValid = false,
            ErrorMessage = message
        };
    }
}