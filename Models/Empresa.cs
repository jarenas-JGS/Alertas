using Alertas.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("empresas")]
    public class Empresa
    {
        [Key]
        [Display(Name = "Id")]
        public int id_empresa { get; set; }

        [Required(ErrorMessage = "El nit de la empresa es obligatorio.")]
        [MaxLength(30, ErrorMessage = "El nit no puede tener más de 30 caracteres.")]
        [Display(Name = "Nit")]
        public string nit { get; set; }

        [Required(ErrorMessage = "El dígito de verificación es obligatorio")]
        [Display(Name = "DV")]
        public int dig_verif { get; set; }

        [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
        [MaxLength(100, ErrorMessage = "La empresa no puede tener más de 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [MaxLength(70, ErrorMessage = "El email no puede tener más de 70 caracteres.")]
        [Display(Name = "Email")]
        public string email { get; set; }

        [Required(ErrorMessage = "Debe indicar si el empleado está activo")]
        [Display(Name = "Activo S/N")]
        public Boolean activo { get; set; }

        [Required(ErrorMessage = "Debe indicar el Id del cliente")]
        [Display(Name = "Cliente")]
        public int id_cliente { get; set; }

        // Propiedades de navegación opcionales para no romper la validación

        [ForeignKey("id_cliente")]
        public Cliente? Cliente { get; set; }

        [InverseProperty("Empresa")]
        public ICollection<AreaEmpresa> AreasEmpresas { get; set; } = new List<AreaEmpresa>();

    }
}