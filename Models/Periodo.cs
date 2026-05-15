using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("periodos")]
    public class Periodo
    {
        [Key]
        [Display(Name = "Id")]
        public int id_periodo { get; set; }

        [Required(ErrorMessage = "El nombre del periodo es obligatoria.")]
        [MaxLength(100, ErrorMessage = "El nombre del periodo no puede tener más de 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

    }
}
