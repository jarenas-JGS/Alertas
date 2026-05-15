using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("dominios")]
    public class Dominio
    {
        [Key]
        [Display(Name = "Id")]
        public int id_dominio { get; set; }

        [Required(ErrorMessage = "El nombre del dominio es obligatoria.")]
        [MaxLength(100, ErrorMessage = "El dominio no puede tener más de 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

    }
}
