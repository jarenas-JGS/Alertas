using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class UsuarioAreaViewModel
    {
        public int id_usuario_area { get; set; }

        [Required(ErrorMessage = "Debe indicar el usuario.")]
        public int id_usuario { get; set; }

        [Required(ErrorMessage = "Debe indicar el área.")]
        public int id_area { get; set; }

        public bool activo { get; set; } = true;

        public DateTime? fecha_asignacion { get; set; }

        public int? id_usuario_asignacion { get; set; }

        public string? nombre_usuario { get; set; }

        public string? nombre_area { get; set; }

        public string? nombre_usuario_asignacion { get; set; }

        public List<SelectListItem> Usuarios { get; set; } = new();

        public List<SelectListItem> Areas { get; set; } = new();
    }
}