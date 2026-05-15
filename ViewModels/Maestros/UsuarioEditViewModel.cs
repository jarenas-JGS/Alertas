using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class UsuarioEditViewModel
    {
        public int id_usuario { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [MaxLength(30, ErrorMessage = "El usuario no puede tener más de 30 caracteres.")]
        public string usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del usuario es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [MaxLength(70, ErrorMessage = "El email no puede tener más de 70 caracteres.")]
        [EmailAddress(ErrorMessage = "Debe ingresar un email válido.")]
        public string email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La clave debe tener mínimo 6 caracteres.")]
        public string? nueva_clave { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(nueva_clave), ErrorMessage = "La clave y la confirmación no coinciden.")]
        public string? confirmar_nueva_clave { get; set; }

        public bool activo { get; set; }

        public bool es_super_admin { get; set; }

        public DateTime? fecha_creacion { get; set; }

        public DateTime? ultimo_login { get; set; }

        public bool must_change_password { get; set; }

        public DateTime? last_password_change_at { get; set; }
    }
}