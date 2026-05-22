namespace Alertas.Services.Notificaciones.DTOs
{
    public class ResultadoEnvioAutomaticoDto
    {
        public int TotalGeneradas { get; set; }
        public int TotalEnviadas { get; set; }
        public int TotalError { get; set; }
        public int TotalOmitidasDuplicadas { get; set; }

        public List<string> Errores { get; set; } = new();
    }
}