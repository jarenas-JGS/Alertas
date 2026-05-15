using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class AreaEmpresaViewModel
    {
        public int? id_area_empresa { get; set; }

        [Required(ErrorMessage = "El área es obligatoria.")]
        public int id_area { get; set; }

        [Required(ErrorMessage = "La empresa es obligatoria.")]
        public int id_empresa { get; set; }

        [Required(ErrorMessage = "El email es obligatorio.")]
        [MaxLength(150)]
        [EmailAddress(ErrorMessage = "Debe ingresar un email válido.")]
        public string email { get; set; } = string.Empty;

        public bool activo { get; set; } = true;

        // Combos
        public List<SelectListItem> Areas { get; set; } = new();
        public List<SelectListItem> Empresas { get; set; } = new();
    }
}