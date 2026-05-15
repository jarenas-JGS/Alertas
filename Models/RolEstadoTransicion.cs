using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("rol_estados_transicion")]
    public class RolEstadoTransicion
    {
        [Key]
        public int id_rol_estado_transicion { get; set; }

        [Required]
        public int id_estado_transicion { get; set; }

        [Required]
        public int id_rol { get; set; }

        public bool activo { get; set; } = true;

        [ForeignKey(nameof(id_estado_transicion))]
        public EstadoTransicion EstadoTransicion { get; set; } = null!;

        [ForeignKey(nameof(id_rol))]
        public Rol Rol { get; set; } = null!;
    }
}