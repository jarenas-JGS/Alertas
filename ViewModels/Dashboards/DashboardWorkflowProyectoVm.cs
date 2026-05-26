namespace Alertas.ViewModels.Dashboards
{
    public class DashboardWorkflowProyectoVm
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; } = string.Empty;

        public int TotalTransiciones { get; set; }
        public int ObligacionesConMovimiento { get; set; }
        public int TransicionesAutomaticas { get; set; }
        public int TransicionesManuales { get; set; }
        public TiempoEstadoWorkflowVm? MayorCuelloBotella { get; set; }

        public FiltrosDashboardOperativoVm Filtros { get; set; } = new();

        public List<SerieDashboardVm> TransicionesPorEstadoDestino { get; set; } = new();
        public List<SerieDashboardVm> TransicionesPorAccion { get; set; } = new();
        public List<SerieDashboardVm> TransicionesPorUsuario { get; set; } = new();
        public List<SerieDashboardVm> TransicionesPorMes { get; set; } = new();
        public List<TiempoEstadoWorkflowVm> TiempoPromedioPorEstado { get; set; } = new();
    }

    public class TiempoEstadoWorkflowVm
    {
        public int IdEstado { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int Orden { get; set; }
        public decimal DiasPromedio { get; set; }
        public int TotalMediciones { get; set; }
    }
}