using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class EstadoEditNombreViewModel
    {
        public int id_estado { get; set; }

        public int id_proyecto { get; set; }

        public string? nombre_proyecto { get; set; }

        [Required(ErrorMessage = "El nombre del estado es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El estado no puede tener más de 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        public int orden { get; set; }

        public bool bloquea { get; set; }

        public bool control_vencimiento { get; set; }

        public bool control_seguimiento { get; set; }

        public bool activo { get; set; }

        public bool puedeReabrirWizard { get; set; }
    }
}