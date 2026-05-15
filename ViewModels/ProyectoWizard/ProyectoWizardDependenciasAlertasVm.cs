using Microsoft.AspNetCore.Mvc.Rendering;

namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardDependenciasAlertasVm
    {
        public int id_proyecto { get; set; }

        public int id_grupo_alerta { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;

        public string nombre_grupo_alerta { get; set; } = string.Empty;

        public List<ProyectoWizardDependenciaAlertaItemVm> Alertas { get; set; } = new();
    }

    public class ProyectoWizardDependenciaAlertaItemVm
    {
        public int id_grupo_alerta_dia { get; set; }

        public string nombre { get; set; } = string.Empty;

        public string rol { get; set; } = string.Empty;

        public string tipo_control { get; set; } = string.Empty;

        public string condicion { get; set; } = string.Empty;

        public int? id_dependencia { get; set; }

        public List<SelectListItem> DependenciasDisponibles { get; set; } = new();
    }
}