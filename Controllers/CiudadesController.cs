using Alertas.Data;
using Alertas.Helpers;
using Alertas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Alertas.Controllers
{
    public class CiudadesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CiudadesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================
        // INDEX
        // ======================
        public async Task<IActionResult> Index()
        {
            var data = await _context.Ciudades
                .OrderBy(c => c.nombre)
                .ToListAsync();

            return View(data);
        }

        // ======================
        // CREATE
        // ======================
        public IActionResult Create()
        {
            return View(new Ciudad());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ciudad model)
        {
            if (await ExisteCiudad(model.nombre))
            {
                ModelState.AddModelError(nameof(model.nombre), "Ya existe una ciudad con este nombre.");
            }

            if (!ModelState.IsValid)
                return View(model);

            _context.Ciudades.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ciudad creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ======================
        // EDIT
        // ======================
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _context.Ciudades.FindAsync(id);
            if (data == null) return NotFound();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ciudad model)
        {
            if (id != model.id_ciudad)
                return BadRequest();

            if (await ExisteCiudad(model.nombre, id))
            {
                ModelState.AddModelError(nameof(model.nombre), "Ya existe otra ciudad con este nombre.");
            }

            if (!ModelState.IsValid)
                return View(model);

            _context.Ciudades.Update(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ciudad actualizada.";
            return RedirectToAction(nameof(Index));
        }

        // ======================
        // DETAILS
        // ======================
        public async Task<IActionResult> Details(int id)
        {
            var data = await _context.Ciudades.FindAsync(id);
            if (data == null) return NotFound();

            return View(data);
        }

        // ======================
        // DELETE
        // ======================
        public async Task<IActionResult> Delete(int id)
        {
            var ciudad = await _context.Ciudades
                .FirstOrDefaultAsync(c => c.id_ciudad == id);

            if (ciudad == null)
                return NotFound();

            ViewBag.TieneRelaciones = await TieneRelaciones(id);

            return View(ciudad);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ciudad = await _context.Ciudades.FindAsync(id);

            if (ciudad == null)
                return NotFound();

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar la ciudad porque tiene registros asociados.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Ciudades.Remove(ciudad);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ciudad eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> TieneRelaciones(int idCiudad)
        {
            return await CrudRelationHelper.TieneRelaciones(
                _context.RegObls,
                x => x.id_ciudad == idCiudad
            );
        }

        // ======================
        // VALIDACIÓN
        // ======================
        private async Task<bool> ExisteCiudad(string nombre, int? idExcluir = null)
        {
            var nombreNormalizado = nombre.Trim().ToLower();

            return await _context.Ciudades.AnyAsync(c =>
                c.nombre.ToLower() == nombreNormalizado &&
                (!idExcluir.HasValue || c.id_ciudad != idExcluir.Value));
        }


    }
}