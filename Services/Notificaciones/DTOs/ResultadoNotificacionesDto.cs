namespace Alertas.Services.Notificaciones.DTOs
{
    public class ResultadoNotificacionesDto
    {
        public int ProyectosProcesados { get; set; }
        public int AlertasGeneradas { get; set; }
        public int CorreosPreparados { get; set; }
        public int CorreosEnviados { get; set; }
        public int CorreosError { get; set; }

        public List<GrupoAlertasUsuarioDto> Correos { get; set; } = new();

        public List<string> Errores { get; set; } = new();

    }
}