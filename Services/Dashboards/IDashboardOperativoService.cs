using Alertas.ViewModels.Dashboards;

namespace Alertas.Services.Dashboards
{
    public interface IDashboardOperativoService
    {
        Task<DashboardOperativoProyectoVm> ObtenerDashboardProyectoAsync(
            int idProyecto,
            FiltrosDashboardOperativoVm filtros);

        Task<DetalleDashboardOperativoVm> ObtenerDetalleOperativoAsync(
            int idProyecto,
            string tipo,
            FiltrosDashboardOperativoVm filtros,
            int? idEstadoDetalle);

        Task<DashboardWorkflowProyectoVm> ObtenerDashboardWorkflowAsync(
            int idProyecto,
            FiltrosDashboardOperativoVm filtros);

        Task<DashboardVencimientosProyectoVm> ObtenerDashboardVencimientosAsync(
            int idProyecto,
            FiltrosDashboardOperativoVm filtros);
    }
}