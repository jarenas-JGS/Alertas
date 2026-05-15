using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class EmpresaViewModel
    {
        public int id_empresa { get; set; }

        [Required(ErrorMessage = "El nit de la empresa es obligatorio.")]
        [MaxLength(30, ErrorMessage = "El nit no puede tener más de 30 caracteres.")]
        public string nit { get; set; } = string.Empty;

        [Required(ErrorMessage = "El dígito de verificación es obligatorio")]
        public int dig_verif { get; set; }

        [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
        [MaxLength(100, ErrorMessage = "La empresa no puede tener más de 100 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [MaxLength(70, ErrorMessage = "El email no puede tener más de 70 caracteres.")]
        [EmailAddress(ErrorMessage = "Debe ingresar un email válido.")]
        public string? email { get; set; }

        [Required(ErrorMessage = "Debe indicar si la empresa está activa")]
        public bool activo { get; set; } = true;

        [Required(ErrorMessage = "Debe indicar el cliente.")]
        public int id_cliente { get; set; }

        public List<SelectListItem> Clientes { get; set; } = new();
    }
}