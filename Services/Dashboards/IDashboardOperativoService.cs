using Alertas.ViewModels.Dashboards;

namespace Alertas.Services.Dashboards
{
    public interface IDashboardOperativoService
    {
        Task<DashboardOperativoProyectoVm> ObtenerDashboardProyectoAsync(int idProyecto);
    }
}