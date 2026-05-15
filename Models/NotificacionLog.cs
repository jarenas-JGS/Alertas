using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("notificaciones_log")]
    public class NotificacionLog
    {
        [Key]
        public int id_notificacion_log { get; set; }

        public DateTime fecha_inicio { get; set; } = DateTime.Now;

        public DateTime? fecha_fin { get; set; }

        [Required]
        [MaxLength(20)]
        public string tipo_ejecucion { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string estado { get; set; } = string.Empty;

        public int proyectos_procesados { get; set; }

        public int correos_enviados { get; set; }

        public int correos_error { get; set; }

        public int alertas_generadas { get; set; }

        public string? mensaje_error { get; set; }

        public int? id_usuario_ejecucion { get; set; }

        [ForeignKey(nameof(id_usuario_ejecucion))]
        public Usuario? UsuarioEjecucion { get; set; }
    }
}