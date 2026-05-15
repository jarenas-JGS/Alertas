using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardEstadosVm
    {
        public int id_proyecto { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;

        public List<ProyectoWizardEstadoItemVm> Estados { get; set; } = new();
    }

    public class ProyectoWizardEstadoItemVm
    {
        public int? id_estado { get; set; }

        [Required(ErrorMessage = "El nombre del estado es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El orden es obligatorio.")]
        public int orden { get; set; }

        public bool bloquea { get; set; }

        public bool control_vencimiento { get; set; }

        public bool control_seguimiento { get; set; }

        public bool activo { get; set; } = true;

        public bool eliminar { get; set; } = false;
    }
}