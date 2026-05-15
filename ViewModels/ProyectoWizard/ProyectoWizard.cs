using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardDatosGeneralesVm
    {
        public int? id_proyecto { get; set; }

        [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
        [MaxLength(200, ErrorMessage = "El nombre del proyecto no puede tener más de 200 caracteres.")]
        [Display(Name = "Nombre del proyecto")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar el área.")]
        [Display(Name = "Área")]
        public int? id_area { get; set; }

        [Required(ErrorMessage = "El nombre de seguimiento es obligatorio.")]
        [MaxLength(150, ErrorMessage = "El nombre de seguimiento no puede tener más de 150 caracteres.")]
        [Display(Name = "Nombre de seguimiento")]
        public string nombre_seguimiento { get; set; } = string.Empty;

        public List<SelectListItem> Areas { get; set; } = new();

    }
}