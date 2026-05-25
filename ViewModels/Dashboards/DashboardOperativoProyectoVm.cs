namespace Alertas.ViewModels.Dashboards
{
    public class DashboardOperativoProyectoVm
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; } = string.Empty;

        public int TotalObligaciones { get; set; }
        public int ObligacionesVencidas { get; set; }
        public int ProximasAVencer { get; set; }
        public List<EstadoResumenDashboardVm> ResumenPorEstados { get; set; } = new();

        public decimal PorcentajeCumplimiento { get; set; }
        public decimal PorcentajeAprobadas { get; set; }
        public decimal DiasPromedioCierre { get; set; }

        public List<SerieDashboardVm> ObligacionesPorEstado { get; set; } = new();
        public List<SerieDashboardVm> ObligacionesPorMes { get; set; } = new();
        public List<SerieDashboardVm> TendenciaCumplimientoMensual { get; set; } = new();
        public List<SerieDashboardVm> TopTiposObligacion { get; set; } = new();
        public List<SerieDashboardVm> ObligacionesPorCiudad { get; set; } = new();
        public List<SerieDashboardVm> ObligacionesPorEmpresaCliente { get; set; } = new();
        public FiltrosDashboardOperativoVm Filtros { get; set; } = new();
    }

    public class SerieDashboardVm
    {
        public int? Id { get; set; }
        public int? Anio { get; set; }
        public int? Mes { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }
}