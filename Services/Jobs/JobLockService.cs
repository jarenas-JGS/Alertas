using Alertas.Data;
using Alertas.Models;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Services.Jobs
{
    public class JobLockService : IJobLockService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobLockService> _logger;

        public JobLockService(
            ApplicationDbContext context,
            ILogger<JobLockService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IntentarTomarLockAsync(
            string nombreJob,
            string lockedBy,
            TimeSpan duracionLock,
            CancellationToken cancellationToken)
        {
            var ahora = DateTime.UtcNow;
            var nuevoLockedUntil = ahora.Add(duracionLock);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var lockActual = await _context.JobsLocks
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM jobs_locks
                    WHERE nombre_job = {nombreJob}
                    FOR UPDATE")
                .FirstOrDefaultAsync(cancellationToken);

            if (lockActual == null)
            {
                _context.JobsLocks.Add(new JobsLock
                {
                    nombre_job = nombreJob,
                    locked_by = lockedBy,
                    locked_until = nuevoLockedUntil,
                    fecha_creacion = ahora,
                    fecha_actualizacion = ahora
                });

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return true;
            }

            if (lockActual.locked_until > ahora)
            {
                _logger.LogInformation(
                    "No se toma lock para {NombreJob}. Está bloqueado por {LockedBy} hasta {LockedUntil}",
                    nombreJob,
                    lockActual.locked_by,
                    lockActual.locked_until);

                await transaction.CommitAsync(cancellationToken);
                return false;
            }

            lockActual.locked_by = lockedBy;
            lockActual.locked_until = nuevoLockedUntil;
            lockActual.fecha_actualizacion = ahora;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }

        public async Task LiberarLockAsync(
            string nombreJob,
            string lockedBy,
            CancellationToken cancellationToken)
        {
            var ahora = DateTime.UtcNow;

            var lockActual = await _context.JobsLocks
                .FirstOrDefaultAsync(x =>
                    x.nombre_job == nombreJob &&
                    x.locked_by == lockedBy,
                    cancellationToken);

            if (lockActual == null)
                return;

            lockActual.locked_until = ahora;
            lockActual.fecha_ult_ejecucion = ahora;
            lockActual.fecha_actualizacion = ahora;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}