using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class CambiarPasswordViewModel
    {
        [Required(ErrorMessage = "La clave actual es obligatoria.")]
        [DataType(DataType.Password)]
        public string ClaveActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva clave es obligatoria.")]
        [MinLength(6, ErrorMessage = "La nueva clave debe tener mínimo 6 caracteres.")]
        [DataType(DataType.Password)]
        public string NuevaClave { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la nueva clave.")]
        [Compare(nameof(NuevaClave), ErrorMessage = "La confirmación no coincide con la nueva clave.")]
        [DataType(DataType.Password)]
        public string ConfirmarNuevaClave { get; set; } = string.Empty;
    }
}