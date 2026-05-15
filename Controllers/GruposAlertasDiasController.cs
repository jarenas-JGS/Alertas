using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class GruposAlertasDiasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GruposAlertasDiasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var alertas = await _context.GruposAlertasDias
                .Include(a => a.GrupoAlerta)
                    .ThenInclude(g => g.Proyecto)
                .Include(a => a.Rol)
                .Include(a => a.Mensaje)
                .Include(a => a.Dependencia)
                .Where(a => a.GrupoAlerta.Proyecto.configuracion_completa)
                .OrderBy(a => a.GrupoAlerta.Proyecto.nombre)
                .ThenBy(a => a.GrupoAlerta.nombre)
                .ThenBy(a => a.nombre)
                .ToListAsync();

            return View(alertas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var alerta = await _context.GruposAlertasDias
                .Include(a => a.GrupoAlerta)
                    .ThenInclude(g => g.Proyecto)
                .Include(a => a.Rol)
                .Include(a => a.Mensaje)
                .Include(a => a.Dependencia)
                .Include(a => a.EstadosOff)
                    .ThenInclude(e => e.Estado)
                .FirstOrDefaultAsync(a => a.id_grupo_alerta_dia == id);

            if (alerta == null)
                return NotFound();

            return View(alerta);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new GrupoAlertaDiaViewModel
            {
                activo = true
            };

            await CargarCombos(vm, null, null);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GrupoAlertaDiaViewModel vm)
        {
            await ValidarReglas(vm, esEdicion: false);

            if (!ModelState.IsValid)
            {
                await CargarCombos(vm, vm.id_grupo_alerta, null);
                return View(vm);
            }

            var alerta = new GrupoAlertaDia
            {
                nombre = vm.nombre.Trim(),
                id_grupo_alerta = vm.id_grupo_alerta,
                tipo_control = vm.tipo_control,
                operador = vm.operador,
                valor_dias = vm.valor_dias,
                id_rol = vm.id_rol,
                id_mensaje = vm.id_mensaje,
                id_dependencia = vm.id_dependencia,
                activo = vm.activo
            };

            _context.GruposAlertasDias.Add(alerta);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Alerta por días creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var alerta = await _context.GruposAlertasDias
                .Include(a => a.GrupoAlerta)
                    .ThenInclude(g => g.Proyecto)
                .FirstOrDefaultAsync(a => a.id_grupo_alerta_dia == id);

            if (alerta == null)
                return NotFound();

            var tieneEstadosOff = await TieneEstadosOff(id);

            var vm = new GrupoAlertaDiaViewModel
            {
                id_grupo_alerta_dia = alerta.id_grupo_alerta_dia,
                nombre = alerta.nombre,
                id_grupo_alerta = alerta.id_grupo_alerta,
                nombre_grupo_alerta = alerta.GrupoAlerta.nombre,
                nombre_proyecto = alerta.GrupoAlerta.Proyecto.nombre,
                tipo_control = alerta.tipo_control,
                operador = alerta.operador,
                valor_dias = alerta.valor_dias,
                id_rol = alerta.id_rol,
                id_mensaje = alerta.id_mensaje,
                id_dependencia = alerta.id_dependencia,
                activo = alerta.activo,
                tieneEstadosOff = tieneEstadosOff
            };

            await CargarCombos(vm, alerta.id_grupo_alerta, alerta.id_grupo_alerta_dia);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GrupoAlertaDiaViewModel vm)
        {
            if (id != vm.id_grupo_alerta_dia)
                return BadRequest();

            var alerta = await _context.GruposAlertasDias
                .Include(a => a.GrupoAlerta)
                    .ThenInclude(g => g.Proyecto)
                .FirstOrDefaultAsync(a => a.id_grupo_alerta_dia == id);

            if (alerta == null)
                return NotFound();

            var tieneEstadosOff = await TieneEstadosOff(id);

            if (tieneEstadosOff && vm.id_grupo_alerta != alerta.id_grupo_alerta)
            {
                ModelState.AddModelError(nameof(vm.id_grupo_alerta),
                    "No se puede cambiar el grupo de alerta porque esta alerta tiene estados off asociados.");
            }

            await ValidarReglas(vm, esEdicion: true);

            if (!ModelState.IsValid)
            {
                vm.tieneEstadosOff = tieneEstadosOff;

                if (tieneEstadosOff)
                {
                    vm.id_grupo_alerta = alerta.id_grupo_alerta;
                }

                vm.nombre_grupo_alerta = alerta.GrupoAlerta.nombre;
                vm.nombre_proyecto = alerta.GrupoAlerta.Proyecto.nombre;

                await CargarCombos(vm, vm.id_grupo_alerta, vm.id_grupo_alerta_dia);
                return View(vm);
            }

            alerta.nombre = vm.nombre.Trim();

            if (!tieneEstadosOff)
            {
                alerta.id_grupo_alerta = vm.id_grupo_alerta;
            }

            alerta.tipo_control = vm.tipo_control;
            alerta.operador = vm.operador;
            alerta.valor_dias = vm.valor_dias;
            alerta.id_rol = vm.id_rol;
            alerta.id_mensaje = vm.id_mensaje;
            alerta.id_dependencia = vm.id_dependencia;
            alerta.activo = vm.activo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Alerta por días actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var alerta = await _context.GruposAlertasDias
                .Include(a => a.GrupoAlerta)
                    .ThenInclude(g => g.Proyecto)
                .Include(a => a.Rol)
                .Include(a => a.Mensaje)
                .Include(a => a.Dependencia)
                .FirstOrDefaultAsync(a => a.id_grupo_alerta_dia == id);

            if (alerta == null)
                return NotFound();

            ViewBag.PuedeEliminar = !await TieneEstadosOff(id);

            return View(alerta);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var alerta = await _context.GruposAlertasDias
                .FirstOrDefaultAsync(a => a.id_grupo_alerta_dia == id);

            if (alerta == null)
                return NotFound();

            if (await TieneEstadosOff(id))
            {
                TempData["Error"] = "No se puede eliminar la alerta porque tiene estados off asociados.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.GruposAlertasDias.Remove(alerta);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Alerta por días eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerDependenciasPorGrupoAlerta(int idGrupoAlerta, int? idExcluir)
        {
            var dependencias = await ObtenerDependenciasPorGrupo(idGrupoAlerta, idExcluir);

            return Json(dependencias.Select(d => new
            {
                value = d.Value,
                text = d.Text
            }));
        }

        private async Task CargarCombos(GrupoAlertaDiaViewModel vm, int? idGrupoAlerta, int? idExcluirDependencia)
        {
            vm.GruposAlertas = await _context.GruposAlertas
                .Include(g => g.Proyecto)
                .Where(g => g.activo && g.Proyecto.configuracion_completa)
                .OrderBy(g => g.Proyecto.nombre)
                .ThenBy(g => g.nombre)
                .Select(g => new SelectListItem
                {
                    Value = g.id_grupo_alerta.ToString(),
                    Text = g.Proyecto.nombre + " - " + g.nombre
                })
                .ToListAsync();

            vm.Roles = await ObtenerRolesGenerales();

            vm.Mensajes = await _context.Mensajes
                .Where(m => m.activo)
                .OrderBy(m => m.nombre)
                .Select(m => new SelectListItem
                {
                    Value = m.id_mensaje.ToString(),
                    Text = m.nombre
                })
                .ToListAsync();

            if (idGrupoAlerta.HasValue && idGrupoAlerta.Value > 0)
            {
                vm.Dependencias = await ObtenerDependenciasPorGrupo(idGrupoAlerta.Value, idExcluirDependencia);
            }
            else
            {
                vm.Dependencias = new List<SelectListItem>();
            }
        }

        private async Task<List<SelectListItem>> ObtenerRolesGenerales()
        {
            var rolesPermitidos = new[] { 1, 2, 3, 4, 5 };

            return await _context.Roles
                .Where(r => rolesPermitidos.Contains(r.id_rol))
                .OrderBy(r => r.id_rol)
                .Select(r => new SelectListItem
                {
                    Value = r.id_rol.ToString(),
                    Text = r.id_rol + " - " + r.nombre
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> ObtenerDependenciasPorGrupo(int idGrupoAlerta, int? idExcluir)
        {
            return await _context.GruposAlertasDias
                .Include(a => a.Rol)
                .Where(a =>
                    a.id_grupo_alerta == idGrupoAlerta &&
                    a.activo &&
                    (!idExcluir.HasValue || a.id_grupo_alerta_dia != idExcluir.Value))
                .OrderBy(a => a.nombre)
                .Select(a => new SelectListItem
                {
                    Value = a.id_grupo_alerta_dia.ToString(),
                    Text = a.nombre + " - " + a.Rol.nombre
                })
                .ToListAsync();
        }

        private async Task<bool> TieneEstadosOff(int idGrupoAlertaDia)
        {
            return await _context.GruposAlertasDiasEstadosOff
                .AnyAsync(e => e.id_grupo_alerta_dia == idGrupoAlertaDia);
        }

        private async Task ValidarReglas(GrupoAlertaDiaViewModel vm, bool esEdicion)
        {
            var grupo = await _context.GruposAlertas
                .Include(g => g.Proyecto)
                .FirstOrDefaultAsync(g => g.id_grupo_alerta == vm.id_grupo_alerta);

            if (grupo == null)
            {
                ModelState.AddModelError(nameof(vm.id_grupo_alerta), "El grupo de alerta seleccionado no existe.");
                return;
            }

            if (!grupo.activo || !grupo.Proyecto.configuracion_completa)
            {
                ModelState.AddModelError(nameof(vm.id_grupo_alerta), "El grupo de alerta seleccionado no está activo o su proyecto no está completamente configurado.");
            }

            var rolesPermitidos = new[] { 1, 2, 3, 4, 5 };

            if (!rolesPermitidos.Contains(vm.id_rol))
            {
                ModelState.AddModelError(nameof(vm.id_rol), "Debe seleccionar un rol válido.");
            }

            if (vm.id_dependencia.HasValue)
            {
                var dependenciaValida = await _context.GruposAlertasDias.AnyAsync(a =>
                    a.id_grupo_alerta_dia == vm.id_dependencia.Value &&
                    a.id_grupo_alerta == vm.id_grupo_alerta &&
                    (!esEdicion || a.id_grupo_alerta_dia != vm.id_grupo_alerta_dia));

                if (!dependenciaValida)
                {
                    ModelState.AddModelError(nameof(vm.id_dependencia), "La dependencia debe pertenecer al mismo grupo de alerta.");
                }
            }
        }
    }
}