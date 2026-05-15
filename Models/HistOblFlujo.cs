using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("hist_obl_flujo")]
    public class HistOblFlujo
    {
        [Key]
        public int id_hist_obl_flujo { get; set; }

        [Required]
        public int id_reg_obl { get; set; }

        public int? id_estado_origen { get; set; }

        public int? id_estado_destino { get; set; }

        [MaxLength(100)]
        public string? accion { get; set; }

        [MaxLength(500)]
        public string? observacion { get; set; }

        [Required]
        public int id_usuario { get; set; }

        [Required]
        public DateTime fecha { get; set; }

        [MaxLength(50)]
        public string? rol_ejecutor { get; set; }

        public bool es_automatico { get; set; } = false;

        // =========================
        // Relaciones
        // =========================

        [ForeignKey(nameof(id_reg_obl))]
        public RegObl RegObl { get; set; } = null!;

        [ForeignKey(nameof(id_estado_origen))]
        public Estado? EstadoOrigen { get; set; }

        [ForeignKey(nameof(id_estado_destino))]
        public Estado? EstadoDestino { get; set; }

        [ForeignKey(nameof(id_usuario))]
        public Usuario Usuario { get; set; } = null!;
    }
}