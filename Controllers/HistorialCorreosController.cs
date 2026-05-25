using Alertas.Data;
using Alertas.ViewModels.Notificaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    [Authorize]
    public class HistorialCorreosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HistorialCorreosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.HasClaim("EsSuperAdmin", "true"))
            {
                TempData["Error"] = "No tienes permisos para acceder al historial de correos.";
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

            var correosRaw = await _context.NotificacionesEnvios
                .AsNoTracking()
                .Include(x => x.Proyecto)
                .Include(x => x.Usuario)
                .Include(x => x.Detalles)
                .OrderByDescending(x => x.fecha_envio)
                .Take(300)
                .ToListAsync();

            var correos = correosRaw
                .Select(x => new HistorialCorreoItemViewModel
                {
                    IdNotificacionEnvio = x.id_notificacion_envio,
                    FechaEnvio = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(x.fecha_envio, DateTimeKind.Utc),
                        userTimeZone),
                    TipoEjecucion = x.tipo_ejecucion,
                    EstadoEnvio = x.estado_envio,
                    NombreProyecto = x.Proyecto?.nombre,
                    NombreUsuario = x.Usuario?.nombre,
                    DestinatarioEmail = x.destinatario_email,
                    Asunto = x.asunto,
                    CantidadAlertas = x.Detalles.Count,
                    ErrorMensaje = x.error_mensaje
                })
                .ToList();

            return View(new HistorialCorreosViewModel
            {
                Correos = correos
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            if (!User.HasClaim("EsSuperAdmin", "true"))
            {
                TempData["Error"] = "No tienes permisos para acceder al detalle del correo.";
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

            var envio = await _context.NotificacionesEnvios
                .AsNoTracking()
                .Include(x => x.Proyecto)
                .Include(x => x.Usuario)
                .Include(x => x.Detalles)
                .FirstOrDefaultAsync(x => x.id_notificacion_envio == id);

            if (envio == null)
                return NotFound();

            var model = new HistorialCorreoDetalleViewModel
            {
                IdNotificacionEnvio = envio.id_notificacion_envio,
                FechaEnvio = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(envio.fecha_envio, DateTimeKind.Utc),
                    userTimeZone),
                TipoEjecucion = envio.tipo_ejecucion,
                EstadoEnvio = envio.estado_envio,
                NombreProyecto = envio.Proyecto?.nombre,
                NombreUsuario = envio.Usuario?.nombre,
                DestinatarioEmail = envio.destinatario_email,
                Asunto = envio.asunto,
                ErrorMensaje = envio.error_mensaje,
                Alertas = envio.Detalles
                    .OrderBy(x => x.prioridad)
                    .ThenBy(x => x.nombre_alerta)
                    .Select(x => new HistorialCorreoAlertaDetalleViewModel
                    {
                        NombreAlerta = x.nombre_alerta,
                        NombreMensaje = x.nombre_mensaje,
                        Prioridad = x.prioridad,
                        IdRegObl = x.id_reg_obl,
                        FechaVencObl = x.fecha_venc_obl,
                        DiasVencimientoObl = x.dias_vencimiento_obl,
                        FechaVencSeguimiento = x.fecha_venc_seguimiento,
                        DiasVencimientoSeguimiento = x.dias_vencimiento_seguimiento
                    })
                    .ToList()
            };

            return View(model);
        }
    }
}