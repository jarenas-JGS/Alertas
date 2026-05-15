using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("notificaciones_envios")]
    public class NotificacionEnvio
    {
        [Key]
        public int id_notificacion_envio { get; set; }

        [Required]
        public int id_proyecto { get; set; }

        [Required]
        public int id_usuario { get; set; }

        public DateTime fecha_envio { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(20)]
        public string tipo_ejecucion { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string estado_envio { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string asunto { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string destinatario_email { get; set; } = string.Empty;

        public string? error_mensaje { get; set; }

        public int? id_usuario_ejecucion { get; set; }

        [ForeignKey(nameof(id_proyecto))]
        public Proyecto Proyecto { get; set; } = null!;

        [ForeignKey(nameof(id_usuario))]
        public Usuario Usuario { get; set; } = null!;

        [ForeignKey(nameof(id_usuario_ejecucion))]
        public Usuario? UsuarioEjecucion { get; set; }

        public ICollection<NotificacionEnvioDetalle> Detalles { get; set; } = new List<NotificacionEnvioDetalle>();
    }
}