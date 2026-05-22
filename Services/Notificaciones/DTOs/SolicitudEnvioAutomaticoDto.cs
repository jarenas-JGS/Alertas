using Alertas.Services.Notificaciones.Enums;

namespace Alertas.Services.Notificaciones.DTOs
{
    public class SolicitudEnvioAutomaticoDto
    {
        public ModoEjecucionNotificacion Modo { get; set; } = ModoEjecucionNotificacion.Automatico;

        public int? SoloProyectoId { get; set; }

        public int MaxCorreosPorEjecucion { get; set; } = 100;

        public int DelayEntreCorreosMs { get; set; } = 250;

        public bool PermitirEnvioReal { get; set; } = false;
        public string Ambiente { get; set; } = string.Empty;
        public bool BloquearEnviosFueraDeProduccion { get; set; } = true;
        public string? EmailDestinoPruebas { get; set; }
        public bool SimularEjecucion { get; set; } = true;
    }
}