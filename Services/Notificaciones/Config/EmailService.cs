using Alertas.Services.Notificaciones.Config;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Alertas.Services.Notificaciones
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<EmailService> _logger;

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
            var destinatarioFinal = destinatario;

            _logger.LogInformation(
                "SMTP RedirectAllTo: {RedirectAllTo}. Destinatario original: {Destinatario}",
                _settings.RedirectAllTo,
                destinatario);

            if (!string.IsNullOrWhiteSpace(_settings.RedirectAllTo))
            {
                asunto = $"[STAGING - REDIRIGIDO] {asunto}";

                htmlBody = $@"
                <div style='padding:10px;background:#fff3cd;border:1px solid #ffeeba;margin-bottom:15px;'>
                    <strong>Correo redirigido desde ambiente de pruebas.</strong><br/>
                    Destinatario original: {destinatario}
                </div>
                {htmlBody}";

                destinatarioFinal = _settings.RedirectAllTo;
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
                    _settings.Password),
                Timeout = 30000
            };

            try
            {
                _logger.LogInformation(
                    "SMTP enviando. Host={Host}, Port={Port}, User={User}, FromEmail={FromEmail}, To={To}",
                    _settings.Host,
                    _settings.Port,
                    _settings.User,
                    _settings.FromEmail,
                    destinatarioFinal);

                await smtp.SendMailAsync(message);

                _logger.LogInformation(
                    "SMTP enviado correctamente a {To}",
                    destinatarioFinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SMTP error enviando. Host={Host}, Port={Port}, User={User}, FromEmail={FromEmail}, To={To}",
                    _settings.Host,
                    _settings.Port,
                    _settings.User,
                    _settings.FromEmail,
                    destinatarioFinal);

                throw;
            }
        }
    }
}