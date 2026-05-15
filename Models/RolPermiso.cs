using Alertas.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("roles_permisos")]
    public class RolPermiso
    {
        [Key]
        [Display(Name = "Id")]
        public int id_rol_permiso { get; set; }

        [Required(ErrorMessage = "Debe indicar el rol")]
        [Display(Name = "Rol")]
        public int id_rol { get; set; }

        [Required(ErrorMessage = "Debe indicar el permiso")]
        [Display(Name = "Permiso")]
        public int id_permiso { get; set; }

        // Propiedades de navegación opcionales para no romper la validación

        [ForeignKey("id_rol")]
        public Rol? Rol { get; set; }

        [ForeignKey("id_permiso")]
        public Permiso? Permiso { get; set; }
    }
}