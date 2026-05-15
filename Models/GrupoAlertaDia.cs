using Alertas.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("grupos_alertas_dias")]
    public class GrupoAlertaDia
    {
        [Key]
        public int id_grupo_alerta_dia { get; set; }

        [Required(ErrorMessage = "El nombre de la alerta es obligatorio.")]
        [MaxLength(200, ErrorMessage = "El nombre no puede tener más de 200 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el grupo de alerta.")]
        public int id_grupo_alerta { get; set; }

        [Required(ErrorMessage = "Debe indicar el tipo de control.")]
        [MaxLength(20)]
        public string tipo_control { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el operador.")]
        [MaxLength(2)]
        public string operador { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el valor en días.")]
        public int valor_dias { get; set; }

        [Required(ErrorMessage = "Debe indicar el rol.")]
        public int id_rol { get; set; }

        [Required(ErrorMessage = "Debe indicar el mensaje.")]
        public int id_mensaje { get; set; }

        public int? id_dependencia { get; set; }

        public bool activo { get; set; }

        [ForeignKey(nameof(id_grupo_alerta))]
        public GrupoAlerta GrupoAlerta { get; set; } = null!;

        [ForeignKey(nameof(id_rol))]
        public Rol Rol { get; set; } = null!;

        [ForeignKey(nameof(id_mensaje))]
        public Mensaje Mensaje { get; set; } = null!;

        [ForeignKey(nameof(id_dependencia))]
        public GrupoAlertaDia? Dependencia { get; set; }

        [InverseProperty(nameof(Dependencia))]
        public ICollection<GrupoAlertaDia> Dependientes { get; set; } = new List<GrupoAlertaDia>();

        [InverseProperty(nameof(GrupoAlertaDiaEstadoOff.GrupoAlertaDia))]
        public ICollection<GrupoAlertaDiaEstadoOff> EstadosOff { get; set; } = new List<GrupoAlertaDiaEstadoOff>();
    }
}