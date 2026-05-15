using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class JustifVarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JustifVarController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var registros = await _context.JustifVars
                .Include(j => j.Area)
                .OrderBy(j => j.Area.nombre)
                .ThenBy(j => j.nombre)
                .ToListAsync();

            return View(registros);
        }

        public async Task<IActionResult> Details(int id)
        {
            var registro = await _context.JustifVars
                .Include(j => j.Area)
                .FirstOrDefaultAsync(j => j.id_justif_var == id);

            if (registro == null)
                return NotFound();

            return View(registro);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new JustifVarViewModel();

            await CargarAreas(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JustifVarViewModel vm)
        {
            if (await ExisteNombreArea(vm.nombre, vm.id_area))
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe una justificación con este nombre para el área.");
            }

            if (!ModelState.IsValid)
            {
                await CargarAreas(vm);
                return View(vm);
            }

            var registro = new JustifVar
            {
                nombre = vm.nombre.Trim(),
                id_area = vm.id_area
            };

            _context.JustifVars.Add(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Justificación creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tieneRegistros = await TieneRelaciones(id);
            ViewBag.BloquearArea = tieneRegistros;

            var registro = await _context.JustifVars
                .Include(j => j.Area)
                .FirstOrDefaultAsync(j => j.id_justif_var == id);

            if (registro == null)
                return NotFound();

            var vm = new JustifVarViewModel
            {
                id_justif_var = registro.id_justif_var,
                nombre = registro.nombre,
                id_area = registro.id_area,
                nombre_area = registro.Area?.nombre
            };

            await CargarAreas(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JustifVarViewModel vm)
        {
            if (id != vm.id_justif_var)
                return BadRequest();

            var registro = await _context.JustifVars
                .FirstOrDefaultAsync(j => j.id_justif_var == id);

            if (registro == null)
                return NotFound();

            var tieneRegistros = await TieneRelaciones(id);

            if (tieneRegistros && vm.id_area != registro.id_area)
            {
                ModelState.AddModelError(nameof(vm.id_area), "No se puede cambiar el área porque esta justificación ya tiene obligaciones asociadas.");
            }

            if (await ExisteNombreArea(vm.nombre, vm.id_area, vm.id_justif_var))
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe otra justificación con este nombre para el área.");
            }

            if (!ModelState.IsValid)
            {
                await CargarAreas(vm);
                ViewBag.BloquearArea = tieneRegistros;
                return View(vm);
            }

            registro.nombre = vm.nombre.Trim();
            registro.id_area = vm.id_area;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Justificación actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var registro = await _context.JustifVars
                .Include(j => j.Area)
                .FirstOrDefaultAsync(j => j.id_justif_var == id);

            if (registro == null)
                return NotFound();

            ViewBag.PuedeEliminar = !await TieneRelaciones(id);

            return View(registro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var registro = await _context.JustifVars
                .FirstOrDefaultAsync(j => j.id_justif_var == id);

            if (registro == null)
                return NotFound();

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar la justificación porque tiene registros relacionados.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.JustifVars.Remove(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Justificación eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarAreas(JustifVarViewModel vm)
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

            return await _context.JustifVars.AnyAsync(j =>
                j.nombre.ToLower() == nombreNormalizado &&
                j.id_area == idArea &&
                (!idExcluir.HasValue || j.id_justif_var != idExcluir.Value));
        }

        private async Task<bool> TieneRelaciones(int id)
        {
            return await _context.RegObls
                .AnyAsync(r => r.id_justif_var == id);
        }
    }
}