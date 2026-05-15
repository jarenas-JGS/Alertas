using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class GrupoAlertaDiaViewModel
    {
        public int id_grupo_alerta_dia { get; set; }

        [Required(ErrorMessage = "El nombre de la alerta es obligatorio.")]
        [MaxLength(200, ErrorMessage = "El nombre no puede tener más de 200 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el grupo de alerta.")]
        public int id_grupo_alerta { get; set; }

        public int? id_grupo_alerta_original { get; set; }

        public string? nombre_grupo_alerta { get; set; }
        public string? nombre_proyecto { get; set; }

        [Required(ErrorMessage = "Debe indicar el tipo de control.")]
        [MaxLength(20)]
        public string tipo_control { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el operador.")]
        [MaxLength(2)]
        public string operador { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el valor en días.")]
        public int valor_dias { get; set; }

        [Required(ErrorMessage = "Debe indicar el rol.")]
        public int id_rol { get; set; }

        [Required(ErrorMessage = "Debe indicar el mensaje.")]
        public int id_mensaje { get; set; }

        public int? id_dependencia { get; set; }

        public bool activo { get; set; } = true;

        public bool tieneEstadosOff { get; set; }

        public List<SelectListItem> GruposAlertas { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();
        public List<SelectListItem> Mensajes { get; set; } = new();
        public List<SelectListItem> Dependencias { get; set; } = new();

        public List<SelectListItem> TiposControl { get; set; } = new()
        {
            new SelectListItem { Value = "VENCIMIENTO", Text = "Vencimiento" },
            new SelectListItem { Value = "SEGUIMIENTO", Text = "Seguimiento" }
        };

        public List<SelectListItem> Operadores { get; set; } = new()
        {
            new SelectListItem { Value = "<", Text = "Menor que" },
            new SelectListItem { Value = "<=", Text = "Menor o igual que" },
            new SelectListItem { Value = "=", Text = "Igual a" },
            new SelectListItem { Value = ">=", Text = "Mayor o igual que" },
            new SelectListItem { Value = ">", Text = "Mayor que" }
        };
    }
}