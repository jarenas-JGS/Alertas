using Alertas.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("estados")]
    public class Estado
    {
        [Key]
        public int id_estado { get; set; }

        [Required(ErrorMessage = "El nombre del estado es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El estado no puede tener más de 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el proyecto")]
        public int id_proyecto { get; set; }

        public int orden { get; set; }

        public bool bloquea { get; set; } = false;

        public bool control_vencimiento { get; set; } = false;

        public bool control_seguimiento { get; set; } = false;

        public bool activo { get; set; } = true;

        [ForeignKey(nameof(id_proyecto))]
        public Proyecto Proyecto { get; set; } = null!;

        [InverseProperty(nameof(RegObl.Estado))]
        public ICollection<RegObl> Obligaciones { get; set; } = new List<RegObl>();

        [InverseProperty(nameof(GrupoAlertaDiaEstadoOff.Estado))]
        public ICollection<GrupoAlertaDiaEstadoOff> GruposAlertasDiasEstadosOff { get; set; } = new List<GrupoAlertaDiaEstadoOff>();

        [InverseProperty(nameof(EstadoTransicion.EstadoOrigen))]
        public ICollection<EstadoTransicion> TransicionesComoOrigen { get; set; } = new List<EstadoTransicion>();

        [InverseProperty(nameof(EstadoTransicion.EstadoDestino))]
        public ICollection<EstadoTransicion> TransicionesComoDestino { get; set; } = new List<EstadoTransicion>();
    }
}
