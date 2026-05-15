using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class GruposAlertasDiasEstadosOffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GruposAlertasDiasEstadosOffController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var registros = await _context.GruposAlertasDiasEstadosOff
                .Include(x => x.GrupoAlertaDia)
                    .ThenInclude(d => d.GrupoAlerta)
                        .ThenInclude(g => g.Proyecto)
                .Include(x => x.Estado)
                .Where(x => x.GrupoAlertaDia.GrupoAlerta.Proyecto.configuracion_completa)
                .OrderBy(x => x.GrupoAlertaDia.GrupoAlerta.Proyecto.nombre)
                .ThenBy(x => x.GrupoAlertaDia.GrupoAlerta.nombre)
                .ThenBy(x => x.GrupoAlertaDia.nombre)
                .ThenBy(x => x.Estado.orden)
                .ToListAsync();

            return View(registros);
        }

        public async Task<IActionResult> Details(int id)
        {
            var registro = await _context.GruposAlertasDiasEstadosOff
                .Include(x => x.GrupoAlertaDia)
                    .ThenInclude(d => d.GrupoAlerta)
                        .ThenInclude(g => g.Proyecto)
                .Include(x => x.Estado)
                .FirstOrDefaultAsync(x => x.id_grupo_alerta_dia_estado_off == id);

            if (registro == null)
                return NotFound();

            return View(registro);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new GrupoAlertaDiaEstadoOffViewModel();

            await CargarCombos(vm, null);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GrupoAlertaDiaEstadoOffViewModel vm)
        {
            await ValidarReglas(vm);

            if (!ModelState.IsValid)
            {
                await CargarCombos(vm, vm.id_grupo_alerta_dia);
                return View(vm);
            }

            var registro = new GrupoAlertaDiaEstadoOff
            {
                id_grupo_alerta_dia = vm.id_grupo_alerta_dia,
                id_estado = vm.id_estado
            };

            _context.GruposAlertasDiasEstadosOff.Add(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Estado off creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var registro = await _context.GruposAlertasDiasEstadosOff
                .Include(x => x.GrupoAlertaDia)
                    .ThenInclude(d => d.GrupoAlerta)
                        .ThenInclude(g => g.Proyecto)
                .Include(x => x.Estado)
                .FirstOrDefaultAsync(x => x.id_grupo_alerta_dia_estado_off == id);

            if (registro == null)
                return NotFound();

            var vm = new GrupoAlertaDiaEstadoOffViewModel
            {
                id_grupo_alerta_dia_estado_off = registro.id_grupo_alerta_dia_estado_off,
                id_grupo_alerta_dia = registro.id_grupo_alerta_dia,
                id_estado = registro.id_estado,
                nombre_grupo_alerta_dia = $"{registro.GrupoAlertaDia.id_grupo_alerta_dia} - {registro.GrupoAlertaDia.nombre}",
                nombre_grupo_alerta = registro.GrupoAlertaDia.GrupoAlerta.nombre,
                nombre_proyecto = registro.GrupoAlertaDia.GrupoAlerta.Proyecto.nombre
            };

            await CargarCombos(vm, registro.id_grupo_alerta_dia);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GrupoAlertaDiaEstadoOffViewModel vm)
        {
            if (id != vm.id_grupo_alerta_dia_estado_off)
                return BadRequest();

            var registro = await _context.GruposAlertasDiasEstadosOff
                .Include(x => x.GrupoAlertaDia)
                    .ThenInclude(d => d.GrupoAlerta)
                        .ThenInclude(g => g.Proyecto)
                .FirstOrDefaultAsync(x => x.id_grupo_alerta_dia_estado_off == id);

            if (registro == null)
                return NotFound();

            vm.id_grupo_alerta_dia = registro.id_grupo_alerta_dia;

            await ValidarReglas(vm);

            if (!ModelState.IsValid)
            {
                vm.nombre_grupo_alerta_dia = $"{registro.GrupoAlertaDia.id_grupo_alerta_dia} - {registro.GrupoAlertaDia.nombre}";
                vm.nombre_grupo_alerta = registro.GrupoAlertaDia.GrupoAlerta.nombre;
                vm.nombre_proyecto = registro.GrupoAlertaDia.GrupoAlerta.Proyecto.nombre;

                await CargarCombos(vm, registro.id_grupo_alerta_dia);
                return View(vm);
            }

            registro.id_estado = vm.id_estado;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Estado off actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var registro = await _context.GruposAlertasDiasEstadosOff
                .Include(x => x.GrupoAlertaDia)
                    .ThenInclude(d => d.GrupoAlerta)
                        .ThenInclude(g => g.Proyecto)
                .Include(x => x.Estado)
                .FirstOrDefaultAsync(x => x.id_grupo_alerta_dia_estado_off == id);

            if (registro == null)
                return NotFound();

            ViewBag.PuedeEliminar = true;

            return View(registro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var registro = await _context.GruposAlertasDiasEstadosOff
                .FirstOrDefaultAsync(x => x.id_grupo_alerta_dia_estado_off == id);

            if (registro == null)
                return NotFound();

            _context.GruposAlertasDiasEstadosOff.Remove(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Estado off eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerEstadosPorGrupoAlertaDia(int idGrupoAlertaDia)
        {
            var estados = await ObtenerEstadosPorAlertaDia(idGrupoAlertaDia);

            return Json(estados.Select(e => new
            {
                value = e.Value,
                text = e.Text
            }));
        }

        private async Task CargarCombos(GrupoAlertaDiaEstadoOffViewModel vm, int? idGrupoAlertaDia)
        {
            vm.GruposAlertasDias = await _context.GruposAlertasDias
                .Include(d => d.GrupoAlerta)
                    .ThenInclude(g => g.Proyecto)
                .Where(d => d.activo && d.GrupoAlerta.Proyecto.configuracion_completa)
                .OrderBy(d => d.GrupoAlerta.Proyecto.nombre)
                .ThenBy(d => d.GrupoAlerta.nombre)
                .ThenBy(d => d.id_grupo_alerta_dia)
                .Select(d => new SelectListItem
                {
                    Value = d.id_grupo_alerta_dia.ToString(),
                    Text = d.id_grupo_alerta_dia + " - " + d.nombre + " / " + d.GrupoAlerta.Proyecto.nombre + " - " + d.GrupoAlerta.nombre
                })
                .ToListAsync();

            if (idGrupoAlertaDia.HasValue && idGrupoAlertaDia.Value > 0)
            {
                vm.Estados = await ObtenerEstadosPorAlertaDia(idGrupoAlertaDia.Value);
            }
            else
            {
                vm.Estados = new List<SelectListItem>();
            }
        }

        private async Task<List<SelectListItem>> ObtenerEstadosPorAlertaDia(int idGrupoAlertaDia)
        {
            var alertaDia = await _context.GruposAlertasDias
                .Include(d => d.GrupoAlerta)
                .FirstOrDefaultAsync(d => d.id_grupo_alerta_dia == idGrupoAlertaDia);

            if (alertaDia == null)
                return new List<SelectListItem>();

            return await _context.Estados
                .Where(e =>
                    e.id_proyecto == alertaDia.GrupoAlerta.id_proyecto &&
                    e.activo)
                .OrderBy(e => e.orden)
                .ThenBy(e => e.nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.id_estado.ToString(),
                    Text = e.orden + " - " + e.nombre
                })
                .ToListAsync();
        }

        private async Task ValidarReglas(GrupoAlertaDiaEstadoOffViewModel vm)
        {
            var alertaDia = await _context.GruposAlertasDias
                .Include(d => d.GrupoAlerta)
                    .ThenInclude(g => g.Proyecto)
                .FirstOrDefaultAsync(d => d.id_grupo_alerta_dia == vm.id_grupo_alerta_dia);

            if (alertaDia == null)
            {
                ModelState.AddModelError(nameof(vm.id_grupo_alerta_dia), "La alerta seleccionada no existe.");
                return;
            }

            if (!alertaDia.activo || !alertaDia.GrupoAlerta.Proyecto.configuracion_completa)
            {
                ModelState.AddModelError(nameof(vm.id_grupo_alerta_dia), "La alerta seleccionada no está activa o su proyecto no está completamente configurado.");
            }

            var estadoValido = await _context.Estados.AnyAsync(e =>
                e.id_estado == vm.id_estado &&
                e.id_proyecto == alertaDia.GrupoAlerta.id_proyecto &&
                e.activo);

            if (!estadoValido)
            {
                ModelState.AddModelError(nameof(vm.id_estado), "El estado seleccionado no pertenece al proyecto del grupo de alerta.");
            }

            var existeDuplicado = await _context.GruposAlertasDiasEstadosOff.AnyAsync(x =>
                x.id_grupo_alerta_dia == vm.id_grupo_alerta_dia &&
                x.id_estado == vm.id_estado &&
                x.id_grupo_alerta_dia_estado_off != vm.id_grupo_alerta_dia_estado_off);

            if (existeDuplicado)
            {
                ModelState.AddModelError(nameof(vm.id_estado), "Este estado ya está asociado como estado off para esta alerta.");
            }
        }
    }
}