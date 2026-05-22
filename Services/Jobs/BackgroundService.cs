using Alertas.Services.Jobs.Options;
using Microsoft.Extensions.Options;

namespace Alertas.Services.Jobs
{
    public class AlertasBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AlertasBackgroundService> _logger;
        private readonly IOptionsMonitor<AlertasJobOptions> _optionsMonitor;
        private readonly IWebHostEnvironment _environment;

        public AlertasBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AlertasBackgroundService> logger,
            IOptionsMonitor<AlertasJobOptions> optionsMonitor,
            IWebHostEnvironment environment)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _optionsMonitor = optionsMonitor;
            _environment = environment;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertasBackgroundService inicializado. Ambiente: {Ambiente}",
                _environment.EnvironmentName);

            var options = _optionsMonitor.CurrentValue;

            if (!options.EjecutarAlIniciar)
            {
                await EsperarIntervaloAsync(options.IntervaloMinutos, stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                options = _optionsMonitor.CurrentValue;

                try
                {
                    if (!DebeEjecutar(options))
                    {
                        _logger.LogInformation(
                            "Job de alertas no ejecutado. Habilitado: {Habilitado}, PermitirEnvioReal: {PermitirEnvioReal}, AmbienteActual: {AmbienteActual}, AmbientePermitido: {AmbientePermitido}",
                            options.Habilitado,
                            options.PermitirEnvioReal,
                            _environment.EnvironmentName,
                            options.AmbientePermitido);
                    }
                    else
                    {
                        await EjecutarJobAsync(options, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error general ejecutando el job automático de alertas.");
                }

                await EsperarIntervaloAsync(options.IntervaloMinutos, stoppingToken);
            }
        }

        private bool DebeEjecutar(AlertasJobOptions options)
        {
            if (!options.Habilitado)
                return false;

            if (!options.PermitirEnvioReal)
                return false;

            if (!string.Equals(
                    _environment.EnvironmentName,
                    options.AmbientePermitido,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private async Task EjecutarJobAsync(AlertasJobOptions options, CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Iniciando ejecución automática de alertas. Intervalo: {IntervaloMinutos} minutos. SoloProyectoId: {SoloProyectoId}",
                options.IntervaloMinutos,
                options.SoloProyectoId);

            using var scope = _serviceProvider.CreateScope();

            // Aquí luego conectaremos la lógica real de notificaciones.
            // Por ahora solo dejamos la estructura segura.

            await Task.CompletedTask;

            _logger.LogInformation("Finalizó ejecución automática de alertas.");
        }

        private static async Task EsperarIntervaloAsync(int intervaloMinutos, CancellationToken stoppingToken)
        {
            if (intervaloMinutos <= 0)
                intervaloMinutos = 60;

            await Task.Delay(TimeSpan.FromMinutes(intervaloMinutos), stoppingToken);
        }
    }
}