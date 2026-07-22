using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("configuracion_operativa")]
    public class ConfiguracionOperativa
    {
        [Key]
        [StringLength(100)]
        public string clave { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string valor { get; set; } = string.Empty;

        [StringLength(500)]
        public string? descripcion { get; set; }

        public DateTime fecha_actualizacion { get; set; }

        public int? id_usuario_actualizacion { get; set; }

        [ForeignKey(nameof(id_usuario_actualizacion))]
        public Usuario? UsuarioActualizacion { get; set; }
    }
}