using Alertas.ViewModels.Dashboards;

namespace Alertas.Services.Dashboards
{
    public interface IDashboardHistoricoCumplimientoService
    {
        Task<DashboardHistoricoCumplimientoVm>
            ObtenerDashboardAsync(
                int idProyecto,
                FiltrosDashboardHistoricoCumplimientoVm filtros);

        Task<DetalleHistoricoCumplimientoVm>
            ObtenerDetalleAsync(
                int idProyecto,
                string tipo,
                FiltrosDashboardHistoricoCumplimientoVm filtros,
                int? idEmpresaDetalle = null,
                int? idAutorizadorDetalle = null,
                int? idTipoObligacionDetalle = null);
    }
}