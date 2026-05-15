using Alertas.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("grupos_alertas")]
    public class GrupoAlerta
    {
        [Key]
        public int id_grupo_alerta { get; set; }

        [Required(ErrorMessage = "El nombre del grupo de alerta es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el proyecto")]
        public int id_proyecto { get; set; }

        public bool activo { get; set; }

        [ForeignKey(nameof(id_proyecto))]
        public Proyecto Proyecto { get; set; } = null!;

        [InverseProperty(nameof(GrupoAlertaDia.GrupoAlerta))]
        public ICollection<GrupoAlertaDia> GruposAlertasDias { get; set; } = new List<GrupoAlertaDia>();
    }
}