using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class TipoObligacionViewModel
    {
        public int id_tipo_obligacion { get; set; }

        [Required(ErrorMessage = "El nombre del tipo de obligación es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El tipo de obligación no puede tener más de 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Display(Name = "Orden")]
        public int orden { get; set; }

        [Required(ErrorMessage = "Debe indicar si está activo.")]
        public bool activo { get; set; } = true;

        [Required(ErrorMessage = "Debe indicar el área.")]
        public int id_area { get; set; }

        public string? nombre_area { get; set; }

        public List<SelectListItem> Areas { get; set; } = new();
    }
}