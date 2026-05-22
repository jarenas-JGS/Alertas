using Alertas.Services.Notificaciones.DTOs;

namespace Alertas.Services.Notificaciones
{
    public interface INotificacionesAlertasService
    {
        Task<ResultadoNotificacionesDto> PrepararAlertasAsync(
            string tipoEjecucion,
            int? idUsuarioEjecucion = null,
            int? idProyecto = null);

        Task<ResultadoNotificacionesDto> EnviarAlertasAsync(
            string tipoEjecucion,
            int? idUsuarioEjecucion = null,
            int? idProyecto = null);

        Task<ResultadoEnvioAutomaticoDto> EnviarAlertasAutomaticasAsync(
            SolicitudEnvioAutomaticoDto solicitud,
            CancellationToken cancellationToken);
    }
}