namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardEstadosOffVm
    {
        public int id_proyecto { get; set; }
        public int id_grupo_alerta { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;
        public string nombre_grupo_alerta { get; set; } = string.Empty;

        public List<ProyectoWizardEstadoOffEstadoVm> Estados { get; set; } = new();
        public List<ProyectoWizardEstadoOffAlertaVm> Alertas { get; set; } = new();
    }

    public class ProyectoWizardEstadoOffEstadoVm
    {
        public int id_estado { get; set; }
        public string nombre { get; set; } = string.Empty;
        public int orden { get; set; }
    }

    public class ProyectoWizardEstadoOffAlertaVm
    {
        public int id_grupo_alerta_dia { get; set; }

        public string nombre { get; set; } = string.Empty;
        public string rol { get; set; } = string.Empty;
        public string condicion { get; set; } = string.Empty;

        public List<int> estados_off_seleccionados { get; set; } = new();
    }
}