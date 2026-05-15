using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class EstadoViewModel
    {
        public int id_estado { get; set; }

        [Required(ErrorMessage = "El nombre del estado es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El estado no puede tener más de 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el proyecto.")]
        public int id_proyecto { get; set; }

        public int orden { get; set; }

        public bool bloquea { get; set; } = false;
        public bool control_vencimiento { get; set; } = false;
        public bool control_seguimiento { get; set; } = false;
        public bool activo { get; set; } = true;

        public bool confirmarCambioControlVencimiento { get; set; } = false;
        public bool confirmarCambioControlSeguimiento { get; set; } = false;
        public bool confirmarDesmarcarControlSeguimiento { get; set; } = false;

        public string? MensajeAdvertencia { get; set; }

        public List<SelectListItem> Proyectos { get; set; } = new();


    }
}