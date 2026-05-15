using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("ciudades")]
    public class Ciudad
    {
        [Key]
        [Display(Name = "Id")]
        public int id_ciudad { get; set; }

        [Required(ErrorMessage = "El nombre de la ciudad es obligatoria.")]
        [MaxLength(100, ErrorMessage = "La ciudad no puede tener más de 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "El código Ciiu es obligatorio.")]
        [Display(Name = "Cod_Ciiu")]
        public int cod_ciiu { get; set; }

    }
}
