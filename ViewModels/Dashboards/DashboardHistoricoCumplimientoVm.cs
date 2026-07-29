namespace Alertas.ViewModels.Dashboards
{
    public class DashboardHistoricoCumplimientoVm
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; } = string.Empty;

        public DateOnly? FechaDesde { get; set; }
        public DateOnly? FechaHasta { get; set; }

        // KPIs
        public int TotalObligacionesPeriodo { get; set; }
        public int CumplidasOportunamente { get; set; }
        public int CumplidasConAtraso { get; set; }
        public int SiguenVencidas { get; set; }

        public decimal PorcentajeCumplimientoOportuno { get; set; }
        public decimal PromedioDiasAtraso { get; set; }
        public int MayorAtrasoDias { get; set; }

        public FiltrosDashboardHistoricoCumplimientoVm Filtros { get; set; } = new();

        // Gráficos
        public List<SerieDashboardVm> DistribucionCumplimiento { get; set; } = new();
        public List<SerieDashboardVm> VencidasPorEmpresa { get; set; } = new();
        public List<SerieDashboardVm> VencidasPorAutorizador { get; set; } = new();
        public List<SerieDashboardVm> VencidasPorTipoObligacion { get; set; } = new();
        public List<SerieDashboardVm> RangosAtraso { get; set; } = new();

        // Dataset utilizado por el detalle y la exportación.
        public List<HistoricoCumplimientoItemVm> Obligaciones { get; set; } = new();
    }
}