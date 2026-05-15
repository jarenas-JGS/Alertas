using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class PeriodoViewModel
    {
        public int id_periodo { get; set; }

        [Required(ErrorMessage = "El nombre del periodo es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre del periodo no puede tener más de 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;
    }
}