using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class GrupoAlertaViewModel
    {
        public int id_grupo_alerta { get; set; }

        [Required(ErrorMessage = "El nombre del grupo de alerta es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el proyecto.")]
        public int id_proyecto { get; set; }

        public string? nombre_proyecto { get; set; }

        public bool activo { get; set; } = true;

        public List<SelectListItem> Proyectos { get; set; } = new();
    }
}