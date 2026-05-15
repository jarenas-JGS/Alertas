using Alertas.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("grupos_alertas_dias_estados_off")]
    public class GrupoAlertaDiaEstadoOff
    {
        [Key]
        public int id_grupo_alerta_dia_estado_off { get; set; }

        [Required(ErrorMessage = "Debe indicar el detalle de alerta.")]
        public int id_grupo_alerta_dia { get; set; }

        [Required(ErrorMessage = "Debe indicar el estado.")]
        public int id_estado { get; set; }

        [ForeignKey(nameof(id_grupo_alerta_dia))]
        public GrupoAlertaDia GrupoAlertaDia { get; set; } = null!;

        [ForeignKey(nameof(id_estado))]
        public Estado Estado { get; set; } = null!;
    }
}