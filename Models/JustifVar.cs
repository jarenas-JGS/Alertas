using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("justif_var")]
    public class JustifVar
    {
        [Key]
        public int id_justif_var { get; set; }

        [Required]
        [MaxLength(100)]
        public string nombre { get; set; } = string.Empty;

        [Required]
        public int id_area { get; set; }

        [ForeignKey(nameof(id_area))]
        public Area? Area { get; set; }
    }
}