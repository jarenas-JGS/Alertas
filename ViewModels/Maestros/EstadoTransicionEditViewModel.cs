using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class EstadoTransicionEditViewModel
    {
        public int id_estado_transicion { get; set; }

        public int id_proyecto { get; set; }
        public string? nombre_proyecto { get; set; }

        public int id_estado_origen { get; set; }
        public string? estado_origen { get; set; }

        public int id_estado_destino { get; set; }
        public string? estado_destino { get; set; }

        [Required(ErrorMessage = "El nombre de la acción es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre de la acción no puede tener más de 100 caracteres.")]
        public string nombre_accion { get; set; } = string.Empty;

        public bool requiere_observacion { get; set; }
        public bool es_aprobacion { get; set; }
        public bool es_rechazo { get; set; }
        public bool es_anulacion { get; set; }
        public bool activo { get; set; }
        public int? orden { get; set; }

        public bool puedeReabrirWizard { get; set; }
    }
}