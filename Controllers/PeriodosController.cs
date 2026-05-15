using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class PeriodosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PeriodosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var periodos = await _context.Periodos
                .OrderBy(p => p.nombre)
                .ToListAsync();

            return View(periodos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var periodo = await _context.Periodos
                .FirstOrDefaultAsync(p => p.id_periodo == id);

            if (periodo == null)
                return NotFound();

            return View(periodo);
        }

        public IActionResult Create()
        {
            return View(new PeriodoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PeriodoViewModel vm)
        {
            if (await ExisteNombre(vm.nombre))
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe un periodo con este nombre.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var periodo = new Periodo
            {
                nombre = vm.nombre.Trim()
            };

            _context.Periodos.Add(periodo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Periodo creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var periodo = await _context.Periodos
                .FirstOrDefaultAsync(p => p.id_periodo == id);

            if (periodo == null)
                return NotFound();

            var vm = new PeriodoViewModel
            {
                id_periodo = periodo.id_periodo,
                nombre = periodo.nombre
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PeriodoViewModel vm)
        {
            if (id != vm.id_periodo)
                return BadRequest();

            var periodo = await _context.Periodos
                .FirstOrDefaultAsync(p => p.id_periodo == id);

            if (periodo == null)
                return NotFound();

            if (await ExisteNombre(vm.nombre, vm.id_periodo))
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe otro periodo con este nombre.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            periodo.nombre = vm.nombre.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Periodo actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var periodo = await _context.Periodos
                .FirstOrDefaultAsync(p => p.id_periodo == id);

            if (periodo == null)
                return NotFound();

            ViewBag.PuedeEliminar = !await TieneRelaciones(id);

            return View(periodo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var periodo = await _context.Periodos
                .FirstOrDefaultAsync(p => p.id_periodo == id);

            if (periodo == null)
                return NotFound();

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar el periodo porque tiene obligaciones asociadas.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Periodos.Remove(periodo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Periodo eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ExisteNombre(string nombre, int? idExcluir = null)
        {
            var nombreNormalizado = nombre.Trim().ToLower();

            return await _context.Periodos.AnyAsync(p =>
                p.nombre.ToLower() == nombreNormalizado &&
                (!idExcluir.HasValue || p.id_periodo != idExcluir.Value));
        }

        private async Task<bool> TieneRelaciones(int id)
        {
            return await _context.RegObls
                .AnyAsync(r => r.id_periodo == id);
        }
    }
}