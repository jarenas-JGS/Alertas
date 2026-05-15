using Alertas.Services.Notificaciones;
using Alertas.Services.Notificaciones.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alertas.Controllers
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly INotificacionesAlertasService _service;

        public NotificacionesController(INotificacionesAlertasService service)
        {
            _service = service;
        }

        public IActionResult Index(int? idProyecto = null, string? modo = null)
        {
            if (modo == "general")
            {
                ViewBag.IdProyecto = null;
                ViewBag.ModoGeneral = true;
                return View();
            }

            if (!idProyecto.HasValue || idProyecto.Value <= 0)
            {
                idProyecto = ObtenerIdProyectoActivo();
            }

            ViewBag.IdProyecto = idProyecto;
            ViewBag.ModoGeneral = false;

            return View();
        }

        private int? ObtenerIdProyectoActivo()
        {
            var idProyectoStr = HttpContext.Session.GetString("id_proyecto_activo");

            if (int.TryParse(idProyectoStr, out int idProyecto))
                return idProyecto;

            return null;
        }

        [HttpPost]
        public async Task<IActionResult> EjecutarManualTodos()
        {
            var resultado = await _service.PrepararAlertasAsync(
                TipoEjecucionNotificacion.Manual);

            return View("Resultado", resultado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EjecutarManualProyecto(int? idProyecto)
        {
            idProyecto ??= ObtenerIdProyectoActivo();

            if (!idProyecto.HasValue || idProyecto.Value <= 0)
            {
                TempData["Error"] = "No se recibió un proyecto válido para ejecutar las notificaciones.";
                return RedirectToAction("Index");
            }

            var resultado = await _service.PrepararAlertasAsync(
                TipoEjecucionNotificacion.Manual,
                idProyecto: idProyecto.Value);

            return View("Resultado", resultado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarManualTodos()
        {
            var resultado = await _service.EnviarAlertasAsync(
                TipoEjecucionNotificacion.Manual);

            TempData["MensajeExito"] =
                $"Proceso finalizado. Correos enviados: {resultado.CorreosEnviados}. " +
                $"Correos con error: {resultado.CorreosError}. " +
                $"Alertas generadas: {resultado.AlertasGeneradas}.";

            return View("Resultado", resultado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarManualProyecto(int? idProyecto)
        {
            idProyecto ??= ObtenerIdProyectoActivo();

            if (!idProyecto.HasValue || idProyecto.Value <= 0)
            {
                TempData["Error"] = "No se recibió un proyecto válido para enviar las notificaciones.";
                return RedirectToAction("Index");
            }

            var resultado = await _service.EnviarAlertasAsync(
                TipoEjecucionNotificacion.Manual,
                idProyecto: idProyecto.Value);

            TempData["MensajeExito"] =
                $"Proceso finalizado. Correos enviados: {resultado.CorreosEnviados}. " +
                $"Correos con error: {resultado.CorreosError}. " +
                $"Alertas generadas: {resultado.AlertasGeneradas}.";

            return View("Resultado", resultado);
        }
    }
}