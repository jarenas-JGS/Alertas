using Alertas.ViewModels.CargaMasiva;

namespace Alertas.Services.CargaMasiva
{
    public interface IValidadorCargaObligacionesService
    {
        Task<List<CargaObligacionesErrorViewModel>> ValidarAsync(
            int idProyecto,
            List<CargaObligacionesFilaViewModel> filas);
    }
}