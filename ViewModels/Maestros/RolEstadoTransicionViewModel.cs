using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class RolEstadoTransicionViewModel
    {
        public int id_rol_estado_transicion { get; set; }

        [Required(ErrorMessage = "Debe indicar la transición de estado.")]
        public int id_estado_transicion { get; set; }

        [Required(ErrorMessage = "Debe indicar el rol.")]
        public int id_rol { get; set; }

        public bool activo { get; set; } = true;

        public string? nombre_transicion { get; set; }
        public string? nombre_rol { get; set; }

        public List<SelectListItem> EstadosTransicion { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();
    }
}