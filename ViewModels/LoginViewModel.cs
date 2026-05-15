using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [Display(Name = "Usuario")]
        public string usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string password { get; set; } = string.Empty;

        [Display(Name = "Recordarme")]
        public bool recordarme { get; set; }
    }
}