using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class FestivoViewModel
    {
        public int id_festivo { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly fecha { get; set; }

        [Required(ErrorMessage = "El nombre del festivo es obligatorio.")]
        [MaxLength(150, ErrorMessage = "El nombre no puede tener más de 150 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        public bool activo { get; set; } = true;
    }
}