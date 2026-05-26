namespace Alertas.ViewModels.Dashboards
{
    public class DashboardVencimientosProyectoVm
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; } = string.Empty;

        public int TotalPendientes { get; set; }
        public int Vencidas { get; set; }
        public int Vencen7Dias { get; set; }
        public int Vencen30Dias { get; set; }

        public decimal AtrasoPromedioDias { get; set; }
        public int MayorAtrasoDias { get; set; }
        public string NivelRiesgo { get; set; } = "Bajo";
        public string RiesgoCssClass { get; set; } = "success";
        public string RiesgoIcono { get; set; } = "bi-check-circle";

        public FiltrosDashboardOperativoVm Filtros { get; set; } = new();

        public List<SerieDashboardVm> VencimientosPorMes { get; set; } = new();
        public List<SerieDashboardVm> VencidasPorEstado { get; set; } = new();
        public List<SerieDashboardVm> TopAutorizadoresVencidas { get; set; } = new();
        public List<SerieDashboardVm> VencidasPorEmpresa { get; set; } = new();
        public List<SerieDashboardVm> VencidasPorTipoObligacion { get; set; } = new();
    }
}