using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class ProyectoEditViewModel
    {
        public int id_proyecto { get; set; }

        [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
        [MaxLength(200, ErrorMessage = "El nombre del proyecto no puede tener más de 200 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de seguimiento es obligatorio.")]
        [MaxLength(150, ErrorMessage = "El nombre de seguimiento no puede tener más de 150 caracteres.")]
        public string nombre_seguimiento { get; set; } = string.Empty;

        public bool activo { get; set; }

        public int id_area { get; set; }
        public string? nombre_area { get; set; }

        public DateTime fecha_creacion { get; set; }
        public string? usuario_creacion { get; set; }

        public bool configuracion_completa { get; set; }

        public bool puedeReabrirWizard { get; set; }
    }
}