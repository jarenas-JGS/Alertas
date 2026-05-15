using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardGrupoAlertasVm
    {
        public int id_proyecto { get; set; }

        public int? id_grupo_alerta { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del grupo de alertas es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        public bool activo { get; set; } = true;
    }
}