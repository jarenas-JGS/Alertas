using Alertas.Data;
using Alertas.ViewModels.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    [Authorize]
    public class JobsMonitoreoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobsMonitoreoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.HasClaim("EsSuperAdmin", "true"))
            {
                TempData["Error"] = "No tienes permisos para acceder al monitoreo de jobs.";
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

            var ejecucionesRaw = await _context.JobsEjecuciones
                .AsNoTracking()
                .OrderByDescending(x => x.fecha_inicio)
                .Take(100)
                .ToListAsync();

            var ejecuciones = ejecucionesRaw
                .Select(x => new JobEjecucionItemViewModel
                {
                    IdJobEjecucion = x.id_job_ejecucion,
                    NombreJob = x.nombre_job,
                    Ambiente = x.ambiente,
                    FechaInicio = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(x.fecha_inicio, DateTimeKind.Utc),
                        userTimeZone),
                    FechaFin = x.fecha_fin.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.SpecifyKind(x.fecha_fin.Value, DateTimeKind.Utc),
                            userTimeZone)
                        : null,
                    Estado = x.estado,
                    TotalGeneradas = x.total_generadas,
                    TotalEnviadas = x.total_enviadas,
                    TotalError = x.total_error,
                    MensajeError = x.mensaje_error
                })
                .ToList();

            var locksRaw = await _context.JobsLocks
                .AsNoTracking()
                .OrderBy(x => x.nombre_job)
                .ToListAsync();

            var locks = locksRaw
                .Select(x => new JobLockItemViewModel
                {
                    NombreJob = x.nombre_job,
                    LockedUntil = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(x.locked_until, DateTimeKind.Utc),
                        userTimeZone),
                    LockedBy = x.locked_by,
                    FechaUltEjecucion = x.fecha_ult_ejecucion.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.SpecifyKind(x.fecha_ult_ejecucion.Value, DateTimeKind.Utc),
                            userTimeZone)
                        : null,
                    FechaActualizacion = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(x.fecha_actualizacion, DateTimeKind.Utc),
                        userTimeZone)
                })
                .ToList();

            var model = new JobsMonitoreoIndexViewModel
            {
                Ejecuciones = ejecuciones,
                Locks = locks
            };

            return View(model);
        }
    }
}