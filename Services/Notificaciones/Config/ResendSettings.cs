namespace Alertas.Services.Notificaciones.Config
{
    public class ResendSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string? RedirectAllTo { get; set; }
    }
}