using Alertas.Services;
using Alertas.Services.Dashboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alertas.Controllers
{
    [Authorize]
    public class DashboardsController : Controller
    {
        private readonly IDashboardOperativoService _dashboardOperativoService;
        private readonly SeguridadService _seguridadService;

        public DashboardsController(
            IDashboardOperativoService dashboardOperativoService,
            SeguridadService seguridadService)
        {
            _dashboardOperativoService = dashboardOperativoService;
            _seguridadService = seguridadService;
        }

        public async Task<IActionResult> OperativoProyecto()
        {
            var idProyecto = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
            {
                TempData["Error"] = "Debe seleccionar un proyecto.";
                return RedirectToAction("SeleccionarProyecto", "Login");
            }

            var esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");

            var tieneAccesoProyecto = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "PROYECTO");

            var tieneAccesoObligacion = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "OBLIGACION");

            if (!esSuperAdmin && !tieneAccesoProyecto && !tieneAccesoObligacion)
            {
                TempData["Error"] = "No tiene acceso al proyecto seleccionado.";
                return RedirectToAction("Index", "Home");
            }

            var vm = await _dashboardOperativoService
                .ObtenerDashboardProyectoAsync(idProyecto.Value);

            return View(vm);
        }
    }
}