namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardIndexItemVm
    {
        public int id_proyecto { get; set; }
        public string nombre { get; set; } = "";
        public string area { get; set; } = "";
        public DateTime fecha_creacion { get; set; }
        public string creado_por { get; set; } = "";
        public string paso_actual { get; set; } = "";
    }
}