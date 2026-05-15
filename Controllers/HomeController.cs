using Alertas.Data;
using Alertas.Services;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Alertas.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SeguridadService _seguridadService;

        public HomeController(ApplicationDbContext context, SeguridadService seguridadService)
        {
            _context = context;
            _seguridadService = seguridadService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var idUsuario = _seguridadService.ObtenerIdUsuario();

            if (idUsuario == null)
                return RedirectToAction("Index", "Login");

            bool esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");

            var usuarioClaim = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var nombreClaim = User.FindFirstValue("NombreCompleto") ?? string.Empty;
            var emailClaim = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var model = new HomeViewModel
            {
                Usuario = usuarioClaim,
                Nombre = nombreClaim,
                Email = emailClaim,
                EsSuperAdmin = esSuperAdmin
            };

            if (esSuperAdmin)
            {
                model.PuedeVerSeguimientoProyecto = true;
                model.PuedeVerReportes = true;
                model.PuedeVerCreacionProyecto = true;
                model.PuedeVerCarguePlantilla = true;
                model.PuedeVerNotificaciones = true;
                model.TieneProyectosDisponibles = await _context.Proyectos.AnyAsync(p => p.activo);
                model.TieneProyectosComoAdministrador = true;

                if (!model.TieneProyectosDisponibles)
                {
                    model.MensajeInformativo = "No hay proyectos activos disponibles en el sistema.";
                }

                return View(model);
            }

            var accesosProyecto = await _seguridadService.ObtenerAccesosProyectoUsuarioAsync(idUsuario.Value);

            bool tieneAccesosProyecto = accesosProyecto.Any();

            bool esAdministradorEnAlgunProyecto = accesosProyecto.Any(a =>
                a.tipo_acceso != null &&
                a.tipo_acceso.Trim().ToUpper() == "Administrador"
            );

            model.TieneProyectosDisponibles = tieneAccesosProyecto;
            model.TieneProyectosComoAdministrador = esAdministradorEnAlgunProyecto;

            model.PuedeVerSeguimientoProyecto = tieneAccesosProyecto;
            model.PuedeVerReportes = tieneAccesosProyecto;
            model.PuedeVerCreacionProyecto = false;
            model.PuedeVerCarguePlantilla = esAdministradorEnAlgunProyecto;
            model.PuedeVerNotificaciones = false;

            if (!tieneAccesosProyecto)
            {
                model.MensajeInformativo = "Actualmente no tiene proyectos asignados o participación operativa habilitada.";
            }

            return View(model);
        }
    }
}