using Alertas.Services.Storage.Models;

namespace Alertas.Services.Storage
{
    public class FileUploadValidator
    {
        private readonly long _maxBytes = 2 * 1024 * 1024;

        private static readonly Dictionary<string, string[]> AllowedMimeTypes = new()
        {
            { ".pdf",  new[] { "application/pdf" } },
            { ".jpg",  new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".png",  new[] { "image/png" } },
            { ".doc",  new[] { "application/msword" } },
            { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
            { ".xls",  new[] { "application/vnd.ms-excel" } },
            { ".xlsx", new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } }
        };

        public FileValidationResult Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return FileValidationResult.Error("Debe seleccionar un archivo.");

            if (file.Length > _maxBytes)
                return FileValidationResult.Error("El archivo no puede superar 2 MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension))
                return FileValidationResult.Error("El archivo no tiene una extensión válida.");

            if (!AllowedMimeTypes.ContainsKey(extension))
                return FileValidationResult.Error(
                    "Tipo de archivo no permitido. Solo se permiten PDF, Word, Excel e imágenes JPG/PNG.");

            var contentType = file.ContentType?.ToLowerInvariant() ?? "";

            if (!AllowedMimeTypes[extension]
                .Any(m => m.Equals(contentType, StringComparison.OrdinalIgnoreCase)))
            {
                return FileValidationResult.Error(
                    $"El tipo de archivo no coincide con la extensión {extension}.");
            }

            return FileValidationResult.Ok();
        }
    }
}