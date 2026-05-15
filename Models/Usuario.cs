using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        [Display(Name = "Id")]
        public int id_usuario { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [MaxLength(30, ErrorMessage = "El usuario no puede tener más de 30 caracteres.")]
        [Display(Name = "Usuario")]
        public string usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del usuario es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre del usuario no puede tener más de 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email del usuario es obligatorio.")]
        [MaxLength(70, ErrorMessage = "El email del usuario no puede tener más de 70 caracteres.")]
        [Display(Name = "Email")]
        public string email { get; set; } = string.Empty;

        [ScaffoldColumn(false)]
        [MaxLength(500)]
        [Display(Name = "Hash de la clave")]
        public string clave_hash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar si el usuario está activo")]
        [Display(Name = "Activo S/N")]
        public bool activo { get; set; }

        [Display(Name = "Fecha creación")]
        public DateTime? fecha_creacion { get; set; }

        [Display(Name = "Fecha último login")]
        public DateTime? ultimo_login { get; set; }

        [Display(Name = "El usuario debe cambiar el password")]
        public bool must_change_password { get; set; }

        [Display(Name = "Fecha cambio de password")]
        public DateTime? last_password_change_at { get; set; }

        [Display(Name = "Es super admin?")]
        public bool es_super_admin { get; set; }

        public ICollection<RegObl> RegImpsComoAutorizador { get; set; } = new List<RegObl>();

        public ICollection<RegObl> RegImpsComoAprobador { get; set; } = new List<RegObl>();

        public ICollection<Proyecto> ProyectosCreados { get; set; } = new List<Proyecto>();

        public ICollection<UsuarioObligacion> UsuariosObligaciones { get; set; } = new List<UsuarioObligacion>();

        public ICollection<UsuarioObligacion> UsuariosObligacionesAsignadas { get; set; } = new List<UsuarioObligacion>();

        public ICollection<UsuarioProyecto> UsuariosProyectos { get; set; } = new List<UsuarioProyecto>();

        public ICollection<UsuarioProyecto> UsuariosProyectosAsignados { get; set; } = new List<UsuarioProyecto>();

        [InverseProperty(nameof(UsuarioArea.Usuario))]
        public ICollection<UsuarioArea> UsuariosAreas { get; set; } = new List<UsuarioArea>();

        [InverseProperty(nameof(UsuarioArea.UsuarioAsignacion))]
        public ICollection<UsuarioArea> UsuariosAreasAsignadas { get; set; } = new List<UsuarioArea>();
    }
}