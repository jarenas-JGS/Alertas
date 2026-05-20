using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Alertas.Services.Notificaciones.Config;
using Microsoft.Extensions.Options;

namespace Alertas.Services.Notificaciones
{
    public class EmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly ResendSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            HttpClient httpClient,
            IOptions<ResendSettings> options,
            ILogger<EmailService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task EnviarEmailAsync(
            string destinatario,
            string asunto,
            string htmlBody)
        {
            var destinatarioFinal = destinatario;

            if (!string.IsNullOrWhiteSpace(_settings.RedirectAllTo))
            {
                asunto = $"[STAGING] {asunto}";
                destinatarioFinal = _settings.RedirectAllTo!;
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var body = new
            {
                from = _settings.FromEmail,
                to = destinatarioFinal,
                subject = asunto,
                html = htmlBody
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                "https://api.resend.com/emails",
                content);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Error Resend: {StatusCode} - {Body}",
                    response.StatusCode,
                    responseBody);

                throw new Exception(
                    $"Error enviando correo con Resend: {responseBody}");
            }

            _logger.LogInformation(
                "Correo enviado correctamente con Resend a {To}",
                destinatarioFinal);
        }
    }
}