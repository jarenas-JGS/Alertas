namespace Alertas.Services.Notificaciones.DTOs
{
    public class GrupoAlertasUsuarioDto
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; } = string.Empty;

        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string EmailUsuario { get; set; } = string.Empty;

        public List<AlertaObligacionDto> Alertas { get; set; } = new();
    }
}