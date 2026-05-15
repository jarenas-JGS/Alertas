using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("clientes")]
    public class Cliente
    {
        [Key]
        [Display(Name = "Id")]
        public int id_cliente { get; set; }

        [Required(ErrorMessage = "El Nit del cliente es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El prestador no puede tener más de 100 caracteres.")]
        [Display(Name = "Nit")]
        public string nit { get; set; }

        [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El cliente no puede tener más de 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "El dígito de verificación es obligatorio")]
        [Display(Name = "DV")]
        public int dig_verif { get; set; }

        [Required(ErrorMessage = "Debe indicar si el empleado está activo")]
        [Display(Name = "Activo S/N")]
        public Boolean activo { get; set; }

    }
}
