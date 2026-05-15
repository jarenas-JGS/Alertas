using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("festivos")]
    public class Festivo
    {
        [Key]
        public int id_festivo { get; set; }

        [Required]
        public DateOnly fecha { get; set; }

        [Required]
        [MaxLength(150)]
        public string nombre { get; set; } = string.Empty;

        public bool activo { get; set; } = true;
    }
}