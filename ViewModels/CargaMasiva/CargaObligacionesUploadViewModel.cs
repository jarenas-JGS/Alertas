using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels.CargaMasiva
{
    public class CargaObligacionesUploadViewModel
    {
        public int id_proyecto { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un archivo.")]
        public IFormFile? archivo { get; set; }
    }
}