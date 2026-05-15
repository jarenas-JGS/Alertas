using Alertas.Services.Notificaciones.DTOs;

namespace Alertas.Services.Notificaciones
{
    public interface IPlantillaCorreoAlertasService
    {
        string GenerarHtml(GrupoAlertasUsuarioDto correo);
    }
}