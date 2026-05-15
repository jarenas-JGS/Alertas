using Alertas.Services.Notificaciones.Config;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Alertas.Services.Notificaciones
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public async Task EnviarEmailAsync(
            string destinatario,
            string asunto,
            string htmlBody)
        {
            using var message = new MailMessage();

            message.From = new MailAddress(
                _settings.FromEmail,
                _settings.FromName);

            message.To.Add(destinatario);
            message.Subject = asunto;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var smtp = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(
                    _settings.User,
                    _settings.Password)
            };

            await smtp.SendMailAsync(message);
        }
    }
}