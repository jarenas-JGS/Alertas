using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("usuarios_proyectos")]
    public class UsuarioProyecto
    {
        [Key]
        public int id_usuario_proyecto { get; set; }

        [Required]
        public int id_usuario { get; set; }

        [Required]
        public int id_proyecto { get; set; }

        [Required]
        public int id_rol { get; set; }

        public bool activo { get; set; }

        public DateTime? fecha_asignacion { get; set; }

        public int? id_usuario_asignacion { get; set; }

        [ForeignKey(nameof(id_usuario))]
        [InverseProperty(nameof(Usuario.UsuariosProyectos))]
        public Usuario Usuario { get; set; } = null!;

        [ForeignKey(nameof(id_proyecto))]
        public Proyecto Proyecto { get; set; } = null!;

        [ForeignKey(nameof(id_rol))]
        public Rol Rol { get; set; } = null!;

        [ForeignKey(nameof(id_usuario_asignacion))]
        [InverseProperty(nameof(Usuario.UsuariosProyectosAsignados))]
        public Usuario? UsuarioAsignacion { get; set; }
    }
}