using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("estados_transicion")]
    public class EstadoTransicion
    {
        [Key]
        public int id_estado_transicion { get; set; }

        [Required]
        public int id_proyecto { get; set; }

        [Required]
        public int id_estado_origen { get; set; }

        [Required]
        public int id_estado_destino { get; set; }

        [Required]
        [MaxLength(100)]
        public string nombre_accion { get; set; } = string.Empty;

        public bool requiere_observacion { get; set; } = false;

        public bool es_aprobacion { get; set; } = false;

        public bool es_rechazo { get; set; } = false;

        public bool es_anulacion { get; set; } = false;

        public bool activo { get; set; } = true;

        public int? orden { get; set; }

        [ForeignKey(nameof(id_proyecto))]
        public Proyecto Proyecto { get; set; } = null!;

        [ForeignKey(nameof(id_estado_origen))]
        public Estado EstadoOrigen { get; set; } = null!;

        [ForeignKey(nameof(id_estado_destino))]
        public Estado EstadoDestino { get; set; } = null!;

        public ICollection<RolEstadoTransicion> RolesEstadosTransicion { get; set; } = new List<RolEstadoTransicion>();
    }
}