using Alertas.Data;
using Alertas.Services;
using Alertas.Services.RutinasActualizacion;
using Alertas.ViewModels.RutinasActualizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    [Authorize]
    public class RutinasActualizacionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SeguridadService _seguridad;
        private readonly IRutinasActualizacionService _service;

        private static readonly string[] RolesObligacion =
        {
            "Responsable",
            "Elaborador",
            "Autorizador",
            "Aprobador",
            "Vencimiento"
        };

        public RutinasActualizacionController(
            ApplicationDbContext context,
            SeguridadService seguridad,
            IRutinasActualizacionService service)
        {
            _context = context;
            _seguridad = seguridad;
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            if (!await _seguridad.UsuarioPuedeVerRutinasActualizacionAsync())
                return Forbid();

            var model = new RutinaParticipantesVm
            {
                Proyectos = await CargarProyectosAsync(),
                Roles = await CargarRolesObligacionAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(RutinaParticipantesVm model)
        {
            if (!await _seguridad.UsuarioPuedeVerRutinasActualizacionAsync())
                return Forbid();

            if (model.IdProyecto == null ||
                !await _seguridad.UsuarioPuedeAdministrarProyectoAsync(model.IdProyecto.Value))
                return Forbid();

            ValidarModeloPorAccion(model);

            if (!ModelState.IsValid)
            {
                model.Proyectos = await CargarProyectosAsync();
                model.Roles = await CargarRolesObligacionAsync();

                if (model.IdProyecto.HasValue)
                    model.Empresas = await CargarEmpresasProyectoAsync(model.IdProyecto.Value);

                return View("Index", model);
            }

            var preview = await _service.GenerarPreviewAsync(model);

            return View(preview);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(RutinaParticipantesVm model)
        {
            if (!await _seguridad.UsuarioPuedeVerRutinasActualizacionAsync())
                return Forbid();

            if (model.IdProyecto == null ||
                !await _seguridad.UsuarioPuedeAdministrarProyectoAsync(model.IdProyecto.Value))
                return Forbid();

            ValidarModeloPorAccion(model);

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "La información enviada no es válida.";
                return RedirectToAction(nameof(Index));
            }

            var resultado = await _service.EjecutarAsync(model);

            return View("Resultado", resultado);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerEmpresasProyecto(int idProyecto)
        {
            if (!await _seguridad.UsuarioPuedeAdministrarProyectoAsync(idProyecto))
                return Forbid();

            var empresas = await CargarEmpresasProyectoAsync(idProyecto);

            return Json(empresas.Select(x => new
            {
                id = x.Value,
                nombre = x.Text
            }));
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerUsuariosArea(int idProyecto)
        {
            if (!await _seguridad.UsuarioPuedeAdministrarProyectoAsync(idProyecto))
                return Forbid();

            var proyecto = await _context.Proyectos
                .Where(p => p.id_proyecto == idProyecto)
                .Select(p => new { p.id_area })
                .FirstOrDefaultAsync();

            if (proyecto == null)
                return Json(Array.Empty<object>());

            var usuarios = await _context.UsuarioArea
                .Where(ua => ua.id_area == proyecto.id_area && ua.activo)
                .Join(_context.Usuarios,
                    ua => ua.id_usuario,
                    u => u.id_usuario,
                    (ua, u) => u)
                .Where(u => u.activo)
                .OrderBy(u => u.nombre)
                .Select(u => new
                {
                    id = u.id_usuario,
                    nombre = u.nombre
                })
                .Distinct()
                .ToListAsync();

            return Json(usuarios);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerUsuariosRol(
            int idProyecto,
            int idEmpresa,
            int idRol)
        {
            if (!await _seguridad.UsuarioPuedeAdministrarProyectoAsync(idProyecto))
                return Forbid();

            var usuarios = await _context.UsuariosObligaciones
                .Where(uo => uo.activo && uo.id_rol == idRol)
                .Join(_context.RegObls,
                    uo => uo.id_reg_obl,
                    ro => ro.id_reg_obl,
                    (uo, ro) => new { uo, ro })
                .Where(x =>
                    x.ro.id_proyecto == idProyecto &&
                    x.ro.id_empresa == idEmpresa)
                .Join(_context.Usuarios,
                    x => x.uo.id_usuario,
                    u => u.id_usuario,
                    (x, u) => u)
                .Where(u => u.activo)
                .OrderBy(u => u.nombre)
                .Select(u => new
                {
                    id = u.id_usuario,
                    nombre = u.nombre
                })
                .Distinct()
                .ToListAsync();

            return Json(usuarios);
        }

        private async Task<List<SelectListItem>> CargarProyectosAsync()
        {
            var proyectos = await _seguridad.ObtenerProyectosAdministrablesAsync();

            return proyectos
                .Select(p => new SelectListItem
                {
                    Value = p.id_proyecto.ToString(),
                    Text = p.nombre_proyecto
                })
                .ToList();
        }

        private async Task<List<SelectListItem>> CargarRolesObligacionAsync()
        {
            return await _context.Roles
                .Where(r => RolesObligacion.Contains(r.nombre))
                .OrderBy(r => r.id_rol)
                .Select(r => new SelectListItem
                {
                    Value = r.id_rol.ToString(),
                    Text = r.nombre
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> CargarEmpresasProyectoAsync(int idProyecto)
        {
            return await _context.RegObls
                .Where(ro => ro.id_proyecto == idProyecto)
                .Join(_context.Empresas,
                    ro => ro.id_empresa,
                    e => e.id_empresa,
                    (ro, e) => e)
                .Distinct()
                .OrderBy(e => e.nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.id_empresa.ToString(),
                    Text = e.nombre
                })
                .ToListAsync();
        }

        private void ValidarModeloPorAccion(RutinaParticipantesVm model)
        {
            if (model.Accion == "INCLUIR")
            {
                if (model.IdUsuarioDestino == null)
                    ModelState.AddModelError(nameof(model.IdUsuarioDestino), "Seleccione el usuario a incluir.");
            }

            if (model.Accion == "ELIMINAR")
            {
                if (model.IdUsuarioOrigen == null)
                    ModelState.AddModelError(nameof(model.IdUsuarioOrigen), "Seleccione el usuario a eliminar.");
            }

            if (model.Accion == "CAMBIAR")
            {
                if (model.IdUsuarioOrigen == null)
                    ModelState.AddModelError(nameof(model.IdUsuarioOrigen), "Seleccione el usuario actual.");

                if (model.IdUsuarioDestino == null)
                    ModelState.AddModelError(nameof(model.IdUsuarioDestino), "Seleccione el usuario nuevo.");

                if (model.IdUsuarioOrigen != null &&
                    model.IdUsuarioDestino != null &&
                    model.IdUsuarioOrigen == model.IdUsuarioDestino)
                {
                    ModelState.AddModelError(nameof(model.IdUsuarioDestino), "El usuario nuevo debe ser diferente al usuario actual.");
                }
            }
        }
    }
}