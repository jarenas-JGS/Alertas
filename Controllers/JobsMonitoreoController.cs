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

            var ejecuciones = await _context.JobsEjecuciones
                .AsNoTracking()
                .OrderByDescending(x => x.fecha_inicio)
                .Take(100)
                .Select(x => new JobEjecucionItemViewModel
                {
                    IdJobEjecucion = x.id_job_ejecucion,
                    NombreJob = x.nombre_job,
                    Ambiente = x.ambiente,
                    FechaInicio = x.fecha_inicio,
                    FechaFin = x.fecha_fin,
                    Estado = x.estado,
                    TotalGeneradas = x.total_generadas,
                    TotalEnviadas = x.total_enviadas,
                    TotalError = x.total_error,
                    MensajeError = x.mensaje_error
                })
                .ToListAsync();

            var locks = await _context.JobsLocks
                .AsNoTracking()
                .OrderBy(x => x.nombre_job)
                .Select(x => new JobLockItemViewModel
                {
                    NombreJob = x.nombre_job,
                    LockedUntil = x.locked_until,
                    LockedBy = x.locked_by,
                    FechaUltEjecucion = x.fecha_ult_ejecucion,
                    FechaActualizacion = x.fecha_actualizacion
                })
                .ToListAsync();

            var model = new JobsMonitoreoIndexViewModel
            {
                Ejecuciones = ejecuciones,
                Locks = locks
            };

            return View(model);
        }
    }
}