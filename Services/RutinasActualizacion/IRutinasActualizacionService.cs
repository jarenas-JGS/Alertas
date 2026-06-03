using Alertas.ViewModels.RutinasActualizacion;

namespace Alertas.Services.RutinasActualizacion
{
    public interface IRutinasActualizacionService
    {
        Task<RutinaParticipantesPreviewVm> GenerarPreviewAsync(
            RutinaParticipantesVm model);

        Task<RutinaParticipantesResultadoVm> EjecutarAsync(
            RutinaParticipantesVm model);
    }
}