using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class FestivosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FestivosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var festivos = await _context.Festivos
                .OrderBy(f => f.fecha)
                .ToListAsync();

            return View(festivos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var festivo = await _context.Festivos
                .FirstOrDefaultAsync(f => f.id_festivo == id);

            if (festivo == null)
                return NotFound();

            return View(festivo);
        }

        public IActionResult Create()
        {
            var vm = new FestivoViewModel
            {
                activo = true
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FestivoViewModel vm)
        {
            if (await ExisteFestivoFecha(vm.fecha))
            {
                ModelState.AddModelError(nameof(vm.fecha), "Ya existe un festivo registrado para esta fecha.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var festivo = new Festivo
            {
                fecha = vm.fecha,
                nombre = vm.nombre.Trim(),
                activo = vm.activo
            };

            _context.Festivos.Add(festivo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Festivo creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var festivo = await _context.Festivos
                .FirstOrDefaultAsync(f => f.id_festivo == id);

            if (festivo == null)
                return NotFound();

            var vm = new FestivoViewModel
            {
                id_festivo = festivo.id_festivo,
                fecha = festivo.fecha,
                nombre = festivo.nombre,
                activo = festivo.activo
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FestivoViewModel vm)
        {
            if (id != vm.id_festivo)
                return BadRequest();

            var festivo = await _context.Festivos
                .FirstOrDefaultAsync(f => f.id_festivo == id);

            if (festivo == null)
                return NotFound();

            if (await ExisteFestivoFecha(vm.fecha, vm.id_festivo))
            {
                ModelState.AddModelError(nameof(vm.fecha), "Ya existe otro festivo registrado para esta fecha.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            festivo.fecha = vm.fecha;
            festivo.nombre = vm.nombre.Trim();
            festivo.activo = vm.activo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Festivo actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var festivo = await _context.Festivos
                .FirstOrDefaultAsync(f => f.id_festivo == id);

            if (festivo == null)
                return NotFound();

            ViewBag.PuedeEliminar = true;
            ViewBag.MensajeNoEliminar = "";

            return View(festivo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var festivo = await _context.Festivos
                .FirstOrDefaultAsync(f => f.id_festivo == id);

            if (festivo == null)
                return NotFound();

            _context.Festivos.Remove(festivo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Festivo eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ExisteFestivoFecha(DateOnly fecha, int? idExcluir = null)
        {
            return await _context.Festivos.AnyAsync(f =>
                f.fecha == fecha &&
                (!idExcluir.HasValue || f.id_festivo != idExcluir.Value));
        }
    }
}