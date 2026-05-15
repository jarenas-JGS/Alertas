using Alertas.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("tipo_obligaciones")]
    public class TipoObligacion
    {
        [Key]
        [Display(Name = "Id")]
        public int id_tipo_obligacion { get; set; }

        [Required(ErrorMessage = "El nombre del tipo de impuesto es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El tipo de impuesto no puede tener más de 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [Display(Name = "Orden")]
        public int orden { get; set; }

        [Required(ErrorMessage = "Debe indicar si el empleado está activo")]
        [Display(Name = "Activo S/N")]
        public Boolean activo { get; set; }

        [Required]
        public int id_area { get; set; }

        [ForeignKey(nameof(id_area))]
        public Area? Area { get; set; }


    }
}