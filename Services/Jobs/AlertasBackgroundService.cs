using Alertas.Data;
using Alertas.Models;
using Alertas.Services.Jobs.Options;
using Alertas.Services.Notificaciones;
using Alertas.Services.Notificaciones.DTOs;
using Microsoft.EntityFrameworkCore;
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

            _logger.LogInformation(
                "Configuración AlertasJob => Habilitado: {Habilitado}, PermitirEnvioReal: {PermitirEnvioReal}, IntervaloMinutos: {IntervaloMinutos}, AmbientePermitido: {AmbientePermitido}, BloquearFueraProduccion: {BloquearFueraProduccion}, SimularEjecucion: {SimularEjecucion}",
                options.Habilitado,
                options.PermitirEnvioReal,
                options.IntervaloMinutos,
                options.AmbientePermitido,
                options.SimularEjecucion,
                options.BloquearEnviosFueraDeProduccion);


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
            const string nombreJob = "ALERTAS_CORREO_AUTOMATICO";

            var lockedBy = $"{Environment.MachineName}-{Guid.NewGuid():N}";

            using var scope = _serviceProvider.CreateScope();



            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var lockService = scope.ServiceProvider.GetRequiredService<IJobLockService>();

            var debeEjecutarPorHorario = await DebeEjecutarPorHorarioAsync(
                options,
                context,
                nombreJob,
                stoppingToken);

            if (!debeEjecutarPorHorario)
                return;

            var tomoLock = await lockService.IntentarTomarLockAsync(
                nombreJob,
                lockedBy,
                TimeSpan.FromMinutes(options.IntervaloMinutos + 10),
                stoppingToken);

            if (!tomoLock)
            {
                _logger.LogInformation("El job {NombreJob} no se ejecutó porque ya existe otra ejecución activa.", nombreJob);
                return;
            }

            var ejecucion = new JobsEjecucion
            {
                nombre_job = nombreJob,
                ambiente = _environment.EnvironmentName,
                fecha_inicio = DateTime.UtcNow,
                estado = "EN_PROCESO"
            };

            context.JobsEjecuciones.Add(ejecucion);
            await context.SaveChangesAsync(stoppingToken);

            try
            {
                _logger.LogInformation("Iniciando ejecución automática de alertas. EjecucionId: {Id}",
                    ejecucion.id_job_ejecucion);

                var notificacionesService = scope.ServiceProvider
                    .GetRequiredService<INotificacionesAlertasService>();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                cts.CancelAfter(TimeSpan.FromMinutes(options.TimeoutMinutos));

                var resultado = await notificacionesService.EnviarAlertasAutomaticasAsync(
                    new SolicitudEnvioAutomaticoDto
                    {
                        SoloProyectoId = options.SoloProyectoId,
                        MaxCorreosPorEjecucion = options.MaxCorreosPorEjecucion,
                        DelayEntreCorreosMs = options.DelayEntreCorreosMs,
                        PermitirEnvioReal = options.PermitirEnvioReal,
                        Ambiente = _environment.EnvironmentName,
                        BloquearEnviosFueraDeProduccion = options.BloquearEnviosFueraDeProduccion,
                        EmailDestinoPruebas = options.EmailDestinoPruebas,
                        SimularEjecucion = options.SimularEjecucion
                    },
                    cts.Token);

                ejecucion.total_generadas = resultado.TotalGeneradas;
                ejecucion.total_enviadas = resultado.TotalEnviadas;
                ejecucion.total_error = resultado.TotalError;

                if (resultado.TotalOmitidasDuplicadas > 0)
                {
                    ejecucion.mensaje_error =
                        $"Correos omitidos por duplicado: {resultado.TotalOmitidasDuplicadas}";
                }

                if (resultado.TotalError > 0)
                {
                    var errores = string.Join(" | ", resultado.Errores.Take(10));

                    ejecucion.mensaje_error = string.IsNullOrWhiteSpace(ejecucion.mensaje_error)
                        ? errores
                        : ejecucion.mensaje_error + " | " + errores;
                }

                ejecucion.estado = "FINALIZADO";
                ejecucion.fecha_fin = DateTime.UtcNow;

                await context.SaveChangesAsync(cts.Token);

                _logger.LogInformation("Finalizó ejecución automática de alertas. EjecucionId: {Id}",
                    ejecucion.id_job_ejecucion);
            }

            catch (OperationCanceledException ex) when (!stoppingToken.IsCancellationRequested)
            {
                ejecucion.estado = "ERROR";
                ejecucion.fecha_fin = DateTime.UtcNow;
                ejecucion.mensaje_error = $"La ejecución superó el timeout configurado de {options.TimeoutMinutos} minutos.";

                await context.SaveChangesAsync(CancellationToken.None);

                _logger.LogError(ex,
                    "Timeout ejecutando job automático de alertas. EjecucionId: {Id}",
                    ejecucion.id_job_ejecucion);
            }

            catch (Exception ex)
            {
                ejecucion.estado = "ERROR";
                ejecucion.fecha_fin = DateTime.UtcNow;
                ejecucion.mensaje_error = ex.Message;

                await context.SaveChangesAsync(CancellationToken.None);

                _logger.LogError(ex, "Error ejecutando job automático de alertas. EjecucionId: {Id}",
                    ejecucion.id_job_ejecucion);
            }
            finally
            {
                await lockService.LiberarLockAsync(
                    nombreJob,
                    lockedBy,
                    CancellationToken.None);
            }
        }

        private static async Task EsperarIntervaloAsync(int intervaloMinutos, CancellationToken stoppingToken)
        {
            if (intervaloMinutos <= 0)
                intervaloMinutos = 60;

            await Task.Delay(TimeSpan.FromMinutes(intervaloMinutos), stoppingToken);
        }

        private async Task<bool> DebeEjecutarPorHorarioAsync(
            AlertasJobOptions options,
            ApplicationDbContext context,
            string nombreJob,
            CancellationToken cancellationToken)
        {
            if (!options.UsarHorarioDiario)
                return true;

            TimeZoneInfo zonaHoraria;

            try
            {
                zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(options.ZonaHoraria);
            }
            catch
            {
                zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            }

            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaHoraria);
            var fechaLocal = DateOnly.FromDateTime(ahoraLocal);

            if (ahoraLocal.DayOfWeek == DayOfWeek.Saturday ||
                ahoraLocal.DayOfWeek == DayOfWeek.Sunday)
            {
                _logger.LogInformation("Job no ejecutado: hoy es fin de semana. Fecha local: {Fecha}", fechaLocal);
                return false;
            }

            var horaProgramada = new TimeSpan(
                options.HoraEjecucion,
                options.MinutoEjecucion,
                0);

            if (ahoraLocal.TimeOfDay < horaProgramada)
            {
                _logger.LogInformation(
                    "Job no ejecutado: aún no llega la hora programada. Ahora: {Ahora}, Programada: {Programada}",
                    ahoraLocal.TimeOfDay,
                    horaProgramada);

                return false;
            }

            if (options.OmitirFestivos)
            {
                var esFestivo = await context.Festivos
                    .AsNoTracking()
                    .AnyAsync(f => f.fecha == fechaLocal, cancellationToken);

                if (esFestivo)
                {
                    _logger.LogInformation("Job no ejecutado: hoy es festivo. Fecha local: {Fecha}", fechaLocal);
                    return false;
                }
            }

            var inicioDiaLocal = fechaLocal.ToDateTime(TimeOnly.MinValue);
            var finDiaLocal = fechaLocal.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioDiaLocal, zonaHoraria);
            var finDiaUtc = TimeZoneInfo.ConvertTimeToUtc(finDiaLocal, zonaHoraria);

            var yaSeEjecutoHoy = await context.JobsEjecuciones
                .AsNoTracking()
                .AnyAsync(j =>
                    j.nombre_job == nombreJob &&
                    j.estado == "FINALIZADO" &&
                    j.fecha_inicio >= inicioDiaUtc &&
                    j.fecha_inicio < finDiaUtc,
                    cancellationToken);

            if (yaSeEjecutoHoy)
            {
                _logger.LogInformation("Job no ejecutado: ya existe ejecución finalizada para hoy. Fecha local: {Fecha}", fechaLocal);
                return false;
            }

            return true;
        }
    }
}