using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class GrupoAlertaDiaEstadoOffViewModel
    {
        public int id_grupo_alerta_dia_estado_off { get; set; }

        [Required(ErrorMessage = "Debe indicar el detalle de alerta.")]
        public int id_grupo_alerta_dia { get; set; }

        public string? nombre_grupo_alerta_dia { get; set; }
        public string? nombre_grupo_alerta { get; set; }
        public string? nombre_proyecto { get; set; }

        [Required(ErrorMessage = "Debe indicar el estado.")]
        public int id_estado { get; set; }

        public List<SelectListItem> GruposAlertasDias { get; set; } = new();
        public List<SelectListItem> Estados { get; set; } = new();
    }
}