using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("usuarios_obligaciones")]
    public class UsuarioObligacion
    {
        [Key]
        public int id_usuario_obligacion { get; set; }

        [Required]
        public int id_usuario { get; set; }

        [Required]
        public int id_reg_obl { get; set; }

        [Required]
        public int id_rol { get; set; }

        public bool activo { get; set; }

        public DateTime? fecha_asignacion { get; set; }

        public int? id_usuario_asignacion { get; set; }

        [ForeignKey(nameof(id_usuario))]
        [InverseProperty(nameof(Usuario.UsuariosObligaciones))]
        public Usuario Usuario { get; set; } = null!;

        [ForeignKey(nameof(id_reg_obl))]
        public RegObl RegObl { get; set; } = null!;

        [ForeignKey(nameof(id_rol))]
        public Rol Rol { get; set; } = null!;

        [ForeignKey(nameof(id_usuario_asignacion))]
        [InverseProperty(nameof(Usuario.UsuariosObligacionesAsignadas))]
        public Usuario? UsuarioAsignacion { get; set; }
    }
}