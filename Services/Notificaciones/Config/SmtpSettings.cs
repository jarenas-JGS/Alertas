namespace Alertas.Services.Notificaciones.Config
{
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "Sistema de Alertas";
        public bool EnableSsl { get; set; } = true;

        // Solo se usa en Staging para redirigir correos a un buzón de pruebas
        public string? RedirectAllTo { get; set; }
    }
}