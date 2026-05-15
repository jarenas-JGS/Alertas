using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("roles")]
    public class Rol
    {
        [Key]
        [Display(Name = "Id")]
        public int id_rol { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatoria.")]
        [MaxLength(100, ErrorMessage = "El nombre del rol no puede tener más de 100 caracteres.")]
        [Display(Name = "Rol")]
        public string nombre { get; set; } = string.Empty;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [InverseProperty(nameof(UsuarioObligacion.Rol))]
        public ICollection<UsuarioObligacion> UsuariosObligaciones { get; set; } = new List<UsuarioObligacion>();

        [InverseProperty(nameof(GrupoAlertaDia.Rol))]
        public ICollection<GrupoAlertaDia> GruposAlertasDias { get; set; } = new List<GrupoAlertaDia>();
        public ICollection<UsuarioProyecto> UsuariosProyectos { get; set; } = new List<UsuarioProyecto>();

        public ICollection<RolEstadoTransicion> RolesEstadosTransicion { get; set; } = new List<RolEstadoTransicion>();
    }
}