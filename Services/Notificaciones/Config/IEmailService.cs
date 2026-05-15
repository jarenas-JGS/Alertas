namespace Alertas.Services.Notificaciones
{
    public interface IEmailService
    {
        Task EnviarEmailAsync(
            string destinatario,
            string asunto,
            string htmlBody);
    }
}