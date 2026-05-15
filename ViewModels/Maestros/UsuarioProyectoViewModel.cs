using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class UsuarioProyectoViewModel
    {
        public int id_usuario_proyecto { get; set; }

        [Required(ErrorMessage = "Debe indicar el usuario.")]
        public int id_usuario { get; set; }

        [Required(ErrorMessage = "Debe indicar el proyecto.")]
        public int id_proyecto { get; set; }

        [Required(ErrorMessage = "Debe indicar el rol.")]
        public int id_rol { get; set; }

        public bool activo { get; set; } = true;

        public DateTime? fecha_asignacion { get; set; }

        public int? id_usuario_asignacion { get; set; }

        public string? nombre_usuario { get; set; }
        public string? nombre_proyecto { get; set; }
        public string? nombre_rol { get; set; }
        public string? nombre_usuario_asignacion { get; set; }

        public List<SelectListItem> Usuarios { get; set; } = new();
        public List<SelectListItem> Proyectos { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();
    }
}