using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("permisos")]
    public class Permiso
    {
        [Key]
        [Display(Name = "Id")]
        public int id_permiso { get; set; }

        [Required(ErrorMessage = "El nombre del permiso es obligatorio.")]
        [MaxLength(50, ErrorMessage = "El nombre del permiso no puede tener más de 50 caracteres.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

    }
}