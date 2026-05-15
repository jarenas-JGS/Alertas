using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("usuarios_areas")]
    public class UsuarioArea
    {
        [Key]
        public int id_usuario_area { get; set; }

        [Required]
        public int id_usuario { get; set; }

        [Required]
        public int id_area { get; set; }

        public bool activo { get; set; }

        public DateTime? fecha_asignacion { get; set; }

        public int? id_usuario_asignacion { get; set; }

        [ForeignKey(nameof(id_usuario))]
        public Usuario Usuario { get; set; } = null!;

        [ForeignKey(nameof(id_area))]
        public Area Area { get; set; } = null!;

        [ForeignKey(nameof(id_usuario_asignacion))]
        public Usuario? UsuarioAsignacion { get; set; }
    }
}