using Alertas.Data;
using Alertas.ViewModels.Monitoreo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    [Authorize]
    public class MonitoreoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MonitoreoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.HasClaim("EsSuperAdmin", "true"))
            {
                TempData["Error"] = "No tienes permisos para acceder al monitoreo.";
                return RedirectToAction("Index", "Home");
            }

            var tzIana = Request.Cookies["tzIana"] ?? "America/Bogota";
            TimeZoneInfo userTimeZone;

            try
            {
                userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzIana);
            }
            catch
            {
                userTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            }

            var hoyLocal = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone));

            var inicioDiaLocal = hoyLocal.ToDateTime(TimeOnly.MinValue);
            var finDiaLocal = hoyLocal.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioDiaLocal, userTimeZone);
            var finDiaUtc = TimeZoneInfo.ConvertTimeToUtc(finDiaLocal, userTimeZone);

            var ultimaEjecucion = await _context.JobsEjecuciones
                .AsNoTracking()
                .OrderByDescending(x => x.fecha_inicio)
                .FirstOrDefaultAsync();

            var ejecucionesHoy = await _context.JobsEjecuciones
                .AsNoTracking()
                .Where(x => x.fecha_inicio >= inicioDiaUtc && x.fecha_inicio < finDiaUtc)
                .ToListAsync();

            var correosHoy = await _context.NotificacionesEnvios
                .AsNoTracking()
                .Where(x => x.fecha_envio >= inicioDiaUtc && x.fecha_envio < finDiaUtc)
                .ToListAsync();

            var lockJob = await _context.JobsLocks
                .AsNoTracking()
                .OrderBy(x => x.nombre_job)
                .FirstOrDefaultAsync();

            var model = new MonitoreoDashboardViewModel
            {
                UltimaEjecucionInicio = ultimaEjecucion != null
                    ? TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(ultimaEjecucion.fecha_inicio, DateTimeKind.Utc),
                        userTimeZone)
                    : null,

                UltimaEjecucionFin = ultimaEjecucion?.fecha_fin != null
                    ? TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(ultimaEjecucion.fecha_fin.Value, DateTimeKind.Utc),
                        userTimeZone)
                    : null,

                UltimaEjecucionEstado = ultimaEjecucion?.estado,
                UltimaEjecucionAmbiente = ultimaEjecucion?.ambiente,

                AlertasGeneradasHoy = ejecucionesHoy.Sum(x => x.total_generadas),
                CorreosEnviadosHoy = correosHoy.Count(x => x.estado_envio == "ENVIADO"),
                CorreosErrorHoy = correosHoy.Count(x => x.estado_envio == "ERROR"),

                JobsFinalizadosHoy = ejecucionesHoy.Count(x => x.estado == "FINALIZADO"),
                JobsErrorHoy = ejecucionesHoy.Count(x => x.estado == "ERROR"),

                LockNombreJob = lockJob?.nombre_job,

                LockHasta = lockJob != null
                    ? TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(lockJob.locked_until, DateTimeKind.Utc),
                        userTimeZone)
                    : null,

                LockUltimaEjecucion = lockJob?.fecha_ult_ejecucion != null
                    ? TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(lockJob.fecha_ult_ejecucion.Value, DateTimeKind.Utc),
                        userTimeZone)
                    : null
            };

            if (model.UltimaEjecucionEstado == "ERROR")
                model.Advertencias.Add("La última ejecución del job terminó con error.");

            if (model.CorreosErrorHoy > 0)
                model.Advertencias.Add($"Hoy hay {model.CorreosErrorHoy} correos con error.");

            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);

            if (model.LockHasta.HasValue &&
                model.LockHasta.Value > ahoraLocal.AddMinutes(30))
            {
                model.Advertencias.Add(
                    "Existe un lock activo con vencimiento futuro. Validar si el job quedó bloqueado.");
            }

            var jobsEnProceso = await _context.JobsEjecuciones
                .AsNoTracking()
                .Where(x => x.estado == "EN_PROCESO")
                .ToListAsync();

            foreach (var job in jobsEnProceso)
            {
                var inicioLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(job.fecha_inicio, DateTimeKind.Utc),
                    userTimeZone);

                var duracion = ahoraLocal - inicioLocal;

                if (duracion.TotalMinutes > 60)
                {
                    model.Advertencias.Add(
                        $"El job '{job.nombre_job}' lleva más de 60 minutos en ejecución.");
                }
            }

            var hoySemana = ahoraLocal.DayOfWeek;

            bool esDiaHabil =
                hoySemana != DayOfWeek.Saturday &&
                hoySemana != DayOfWeek.Sunday;

            if (esDiaHabil &&
                ahoraLocal.Hour >= 9 &&
                model.JobsFinalizadosHoy == 0)
            {
                model.Advertencias.Add(
                    "No existen ejecuciones finalizadas hoy después de las 9:00 AM.");
            }

            return View(model);
        }
    }
}