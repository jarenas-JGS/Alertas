using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("area_empresa")]
    public class AreaEmpresa
    {
        [Key]
        public int id_area_empresa { get; set; }

        [Required]
        public int id_area { get; set; }

        [Required]
        public int id_empresa { get; set; }

        [Required(ErrorMessage = "El email es obligatorio.")]
        [MaxLength(150)]
        [EmailAddress(ErrorMessage = "Debe ingresar un email válido.")]
        public string email { get; set; } = string.Empty;

        [Required]
        public bool activo { get; set; } = true;

        [ForeignKey(nameof(id_area))]
        public Area? Area { get; set; }

        [ForeignKey(nameof(id_empresa))]
        public Empresa? Empresa { get; set; }
    }
}