using Alertas.Data;
using Alertas.Services.Storage.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Services.Storage.Cleanup
{
    public class StorageCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StorageCleanupBackgroundService> _logger;

        public StorageCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<StorageCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Espera inicial para no competir con el arranque de la app
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EjecutarLimpiezaAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ejecutando limpieza física de soportes.");
                }

                // Ejecutar una vez al día
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private async Task EjecutarLimpiezaAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

            var fechaLimite = DateTime.UtcNow.AddDays(-30);

            var adjuntos = await context.OblAdjuntos
                .Where(a =>
                    a.eliminado &&
                    a.fecha_eliminacion != null &&
                    a.fecha_eliminacion <= fechaLimite &&
                    !a.eliminado_fisicamente)
                .OrderBy(a => a.fecha_eliminacion)
                .Take(50)
                .ToListAsync(cancellationToken);

            if (!adjuntos.Any())
            {
                _logger.LogInformation("Limpieza de soportes: no hay archivos pendientes.");
                return;
            }

            _logger.LogInformation(
                "Limpieza de soportes: {Cantidad} archivos pendientes.",
                adjuntos.Count);

            foreach (var adjunto in adjuntos)
            {
                try
                {
                    await storage.DeleteAsync(adjunto.object_key);

                    adjunto.eliminado_fisicamente = true;
                    adjunto.fecha_eliminacion_fisica = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Soporte eliminado físicamente. IdAdjunto={IdAdjunto}, ObjectKey={ObjectKey}",
                        adjunto.id_obl_adjunto,
                        adjunto.object_key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error eliminando físicamente soporte. IdAdjunto={IdAdjunto}, ObjectKey={ObjectKey}",
                        adjunto.id_obl_adjunto,
                        adjunto.object_key);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}