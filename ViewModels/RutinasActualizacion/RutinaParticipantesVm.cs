using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Alertas.ViewModels.RutinasActualizacion
{
    public class RutinaParticipantesVm
    {
        [Required(ErrorMessage = "Seleccione un proyecto.")]
        public int? IdProyecto { get; set; }

        [Required(ErrorMessage = "Seleccione una empresa.")]
        public int? IdEmpresa { get; set; }

        [Required(ErrorMessage = "Seleccione un rol.")]
        public int? IdRol { get; set; }

        [Required(ErrorMessage = "Seleccione una acción.")]
        public string Accion { get; set; } = string.Empty;

        public int? IdUsuarioOrigen { get; set; }

        public int? IdUsuarioDestino { get; set; }

        public List<SelectListItem> Proyectos { get; set; } = new();

        public List<SelectListItem> Empresas { get; set; } = new();

        public List<SelectListItem> Roles { get; set; } = new();

        public List<SelectListItem> UsuariosOrigen { get; set; } = new();

        public List<SelectListItem> UsuariosDestino { get; set; } = new();
    }
}