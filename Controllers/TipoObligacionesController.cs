using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class TipoObligacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TipoObligacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var registros = await _context.TipoObligaciones
                .Include(t => t.Area)
                .OrderBy(t => t.Area.nombre)
                .ThenBy(t => t.orden)
                .ThenBy(t => t.nombre)
                .ToListAsync();

            return View(registros);
        }

        public async Task<IActionResult> Details(int id)
        {
            var registro = await _context.TipoObligaciones
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.id_tipo_obligacion == id);

            if (registro == null)
                return NotFound();

            return View(registro);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new TipoObligacionViewModel
            {
                activo = true
            };

            await CargarAreas(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoObligacionViewModel vm)
        {
            if (await ExisteNombreArea(vm.nombre, vm.id_area))
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe un tipo de obligación con este nombre para el área.");
            }

            if (!ModelState.IsValid)
            {
                await CargarAreas(vm);
                return View(vm);
            }

            var registro = new TipoObligacion
            {
                nombre = vm.nombre.Trim(),
                orden = vm.orden,
                activo = vm.activo,
                id_area = vm.id_area
            };

            _context.TipoObligaciones.Add(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tipo de obligación creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var registro = await _context.TipoObligaciones
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.id_tipo_obligacion == id);

            if (registro == null)
                return NotFound();

            var vm = new TipoObligacionViewModel
            {
                id_tipo_obligacion = registro.id_tipo_obligacion,
                nombre = registro.nombre,
                orden = registro.orden,
                activo = registro.activo,
                id_area = registro.id_area,
                nombre_area = registro.Area?.nombre
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TipoObligacionViewModel vm)
        {
            if (id != vm.id_tipo_obligacion)
                return BadRequest();

            var registro = await _context.TipoObligaciones
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.id_tipo_obligacion == id);

            if (registro == null)
                return NotFound();

            // Seguridad backend: nunca permitir cambiar el área desde Edit.
            vm.id_area = registro.id_area;

            if (await ExisteNombreArea(vm.nombre, registro.id_area, registro.id_tipo_obligacion))
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe otro tipo de obligación con este nombre para el área.");
            }

            if (!ModelState.IsValid)
            {
                vm.id_area = registro.id_area;
                vm.nombre_area = registro.Area?.nombre;
                return View(vm);
            }

            registro.nombre = vm.nombre.Trim();
            registro.orden = vm.orden;
            registro.activo = vm.activo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Tipo de obligación actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var registro = await _context.TipoObligaciones
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.id_tipo_obligacion == id);

            if (registro == null)
                return NotFound();

            ViewBag.PuedeEliminar = !await TieneRelaciones(id);

            return View(registro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var registro = await _context.TipoObligaciones
                .FirstOrDefaultAsync(t => t.id_tipo_obligacion == id);

            if (registro == null)
                return NotFound();

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar el tipo de obligación porque tiene obligaciones asociadas.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.TipoObligaciones.Remove(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tipo de obligación eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarAreas(TipoObligacionViewModel vm)
        {
            vm.Areas = await _context.Areas
                .OrderBy(a => a.nombre)
                .Select(a => new SelectListItem
                {
                    Value = a.id_area.ToString(),
                    Text = a.nombre
                })
                .ToListAsync();
        }

        private async Task<bool> ExisteNombreArea(string nombre, int idArea, int? idExcluir = null)
        {
            var nombreNormalizado = nombre.Trim().ToLower();

            return await _context.TipoObligaciones.AnyAsync(t =>
                t.nombre.ToLower() == nombreNormalizado &&
                t.id_area == idArea &&
                (!idExcluir.HasValue || t.id_tipo_obligacion != idExcluir.Value));
        }

        private async Task<bool> TieneRelaciones(int id)
        {
            return await _context.RegObls
                .AnyAsync(r => r.id_tipo_obligacion == id);
        }
    }
}