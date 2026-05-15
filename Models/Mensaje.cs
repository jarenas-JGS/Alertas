using Alertas.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("mensajes")]
    public class Mensaje
    {
        [Key]
        public int id_mensaje { get; set; }

        [Required(ErrorMessage = "La prioridad es obligatoria.")]
        [Range(1, 3, ErrorMessage = "La prioridad debe estar entre 1 y 3.")]
        public int prioridad { get; set; }

        [Required(ErrorMessage = "El texto del mensaje es obligatorio.")]
        public string texto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del mensaje es obligatorio.")]
        [MaxLength(50, ErrorMessage = "El nombre no puede tener más de 50 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        public bool activo { get; set; }

        [InverseProperty(nameof(GrupoAlertaDia.Mensaje))]
        public ICollection<GrupoAlertaDia> GruposAlertasDias { get; set; } = new List<GrupoAlertaDia>();
    }
}