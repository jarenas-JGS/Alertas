using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardSoportePostCierreVm
    {
        public int id_proyecto { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;

        [Display(Name = "Permitir registro posterior al cierre")]
        public bool usa_soporte_post_cierre { get; set; }

        [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
        [Display(Name = "Nombre de la opción posterior al cierre")]
        public string? nombre_soporte_post_cierre { get; set; }
    }
}