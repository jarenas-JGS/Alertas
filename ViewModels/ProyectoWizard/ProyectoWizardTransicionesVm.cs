using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardTransicionesVm
    {
        public int id_proyecto { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;

        public List<SelectListItem> Estados { get; set; } = new();

        public List<ProyectoWizardTransicionItemVm> Transiciones { get; set; } = new();
    }

    public class ProyectoWizardTransicionItemVm
    {
        public int? id_estado_transicion { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el estado origen.")]
        public int? id_estado_origen { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el estado destino.")]
        public int? id_estado_destino { get; set; }

        [Required(ErrorMessage = "El nombre de la acción es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre de la acción no puede superar 100 caracteres.")]
        public string nombre_accion { get; set; } = string.Empty;

        public bool requiere_observacion { get; set; }

        public bool es_aprobacion { get; set; }

        public bool es_rechazo { get; set; }

        public bool es_anulacion { get; set; }

        public bool activo { get; set; } = true;

        public int? orden { get; set; }

        public bool eliminar { get; set; } = false;
    }
}