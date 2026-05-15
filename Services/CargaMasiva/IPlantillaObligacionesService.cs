using Alertas.ViewModels.CargaMasiva;
using Alertas.ViewModels.CargaMasiva;

namespace Alertas.Services.CargaMasiva
{
    public interface IPlantillaObligacionesService
    {
        Task<ArchivoGeneradoViewModel> GenerarPlantillaAsync(int idProyecto, int idUsuarioActual);
    }
}