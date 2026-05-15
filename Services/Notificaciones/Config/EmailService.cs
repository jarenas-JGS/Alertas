using Alertas.Services.Notificaciones.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Alertas.Services.Notificaciones
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        private readonly IWebHostEnvironment _env;

        public EmailService(
            IOptions<SmtpSettings> options,
            ILogger<EmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task EnviarEmailAsync(
            string destinatario,
            string asunto,
            string htmlBody)
        {
            _logger.LogInformation(
            "SMTP RedirectAllTo: {RedirectAllTo}. Destinatario original: {Destinatario}",
            _settings.RedirectAllTo,
            destinatario);

            var destinatarioFinal = destinatario;

            if (!string.IsNullOrWhiteSpace(_settings.RedirectAllTo))
            {
                asunto = $"[STAGING - REDIRIGIDO] {asunto}";
                htmlBody = $@"
                <div style='padding:10px;background:#fff3cd;border:1px solid #ffeeba;margin-bottom:15px;'>
                    <strong>Correo redirigido desde ambiente de pruebas.</strong><br/>
                    Destinatario original: {destinatario}
                </div>
                {htmlBody}";

                destinatario = _settings.RedirectAllTo;
            }

            using var message = new MailMessage();

            message.From = new MailAddress(
                _settings.FromEmail,
                _settings.FromName);

            message.To.Add(destinatarioFinal);

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

        private readonly ILogger<EmailService> _logger;

    }
}