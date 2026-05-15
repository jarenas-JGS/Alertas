namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardRolesTransicionVm
    {
        public int id_proyecto { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;

        public List<ProyectoWizardRolVm> Roles { get; set; } = new();

        public List<ProyectoWizardRolTransicionItemVm> Transiciones { get; set; } = new();
    }

    public class ProyectoWizardRolVm
    {
        public int id_rol { get; set; }

        public string nombre { get; set; } = string.Empty;
    }

    public class ProyectoWizardRolTransicionItemVm
    {
        public int id_estado_transicion { get; set; }

        public string estado_origen { get; set; } = string.Empty;

        public string estado_destino { get; set; } = string.Empty;

        public string nombre_accion { get; set; } = string.Empty;

        public bool es_aprobacion { get; set; }

        public bool es_rechazo { get; set; }

        public bool es_anulacion { get; set; }

        public List<int> roles_seleccionados { get; set; } = new();
    }
}