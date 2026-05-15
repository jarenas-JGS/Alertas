using Alertas.ViewModels.CargaMasiva;

namespace Alertas.Services.CargaMasiva
{
    public interface IConfirmadorCargaObligacionesService
    {
        Task<ResultadoCargaObligacionesViewModel> ConfirmarAsync(
            CargaObligacionesTemporalViewModel carga,
            int idUsuarioActual);
    }
}