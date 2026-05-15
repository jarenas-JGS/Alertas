using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardAlertasDiasVm
    {
        public int id_proyecto { get; set; }

        public int id_grupo_alerta { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;

        public string nombre_grupo_alerta { get; set; } = string.Empty;

        public List<SelectListItem> Roles { get; set; } = new();

        public List<SelectListItem> Mensajes { get; set; } = new();

        public List<SelectListItem> Dependencias { get; set; } = new();

        public List<ProyectoWizardAlertaDiaItemVm> AlertasDias { get; set; } = new();
    }

    public class ProyectoWizardAlertaDiaItemVm
    {
        public int? id_grupo_alerta_dia { get; set; }

        [Required(ErrorMessage = "El nombre de la alerta es obligatorio.")]
        [MaxLength(200)]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el tipo de control.")]
        public string tipo_control { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el operador.")]
        public string operador { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el valor de días.")]
        public int valor_dias { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el rol.")]
        public int? id_rol { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el mensaje.")]
        public int? id_mensaje { get; set; }

        public int? id_dependencia { get; set; }

        public bool activo { get; set; } = true;

        public bool eliminar { get; set; } = false;
    }
}