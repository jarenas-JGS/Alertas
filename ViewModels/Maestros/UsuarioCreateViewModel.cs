using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class UsuarioCreateViewModel
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

        [Required(ErrorMessage = "La clave es obligatoria.")]
        [MinLength(6, ErrorMessage = "La clave debe tener mínimo 6 caracteres.")]
        [DataType(DataType.Password)]
        public string clave { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la clave.")]
        [Compare(nameof(clave), ErrorMessage = "La clave y la confirmación no coinciden.")]
        [DataType(DataType.Password)]
        public string confirmar_clave { get; set; } = string.Empty;

        public bool activo { get; set; } = true;

        public bool es_super_admin { get; set; }
    }
}