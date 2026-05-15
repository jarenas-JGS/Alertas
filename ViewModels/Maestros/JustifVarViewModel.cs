using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class JustifVarViewModel
    {
        public int id_justif_var { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el área.")]
        public int id_area { get; set; }

        public string? nombre_area { get; set; }

        public List<SelectListItem> Areas { get; set; } = new();
    }
}