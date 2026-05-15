using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("notificaciones_envios_detalle")]
    public class NotificacionEnvioDetalle
    {
        [Key]
        public int id_notificacion_envio_detalle { get; set; }

        [Required]
        public int id_notificacion_envio { get; set; }

        [Required]
        public int id_grupo_alerta_dia { get; set; }

        [Required]
        public int id_reg_obl { get; set; }

        [Required]
        public int id_mensaje { get; set; }

        [Required]
        [MaxLength(200)]
        public string nombre_alerta { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string nombre_mensaje { get; set; } = string.Empty;

        [Required]
        public int prioridad { get; set; }

        [Required]
        public DateOnly fecha_venc_obl { get; set; }

        [Required]
        public int dias_vencimiento_obl { get; set; }

        [Required]
        public DateOnly fecha_venc_seguimiento { get; set; }

        [Required]
        public int dias_vencimiento_seguimiento { get; set; }

        [ForeignKey(nameof(id_notificacion_envio))]
        public NotificacionEnvio NotificacionEnvio { get; set; } = null!;

        [ForeignKey(nameof(id_grupo_alerta_dia))]
        public GrupoAlertaDia GrupoAlertaDia { get; set; } = null!;

        [ForeignKey(nameof(id_reg_obl))]
        public RegObl RegObl { get; set; } = null!;

        [ForeignKey(nameof(id_mensaje))]
        public Mensaje Mensaje { get; set; } = null!;
    }
}