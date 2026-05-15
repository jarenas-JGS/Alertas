using Alertas.Data;
using Alertas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Alertas.Helpers;

namespace Alertas.Controllers
{
    public class AreasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AreasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var areas = await _context.Areas
                .OrderBy(a => a.nombre)
                .ToListAsync();

            return View(areas);
        }

        public IActionResult Create()
        {
            return View(new Area());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Area area)
        {
            if (await ExisteNombreArea(area.nombre))
            {
                ModelState.AddModelError(nameof(area.nombre), "Ya existe un área con este nombre.");
            }

            if (!ModelState.IsValid)
            {
                return View(area);
            }

            _context.Areas.Add(area);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Área creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var area = await _context.Areas.FindAsync(id);

            if (area == null)
            {
                return NotFound();
            }

            return View(area);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Area area)
        {
            if (id != area.id_area)
            {
                return BadRequest();
            }

            if (await ExisteNombreArea(area.nombre, area.id_area))
            {
                ModelState.AddModelError(nameof(area.nombre), "Ya existe otra área con este nombre.");
            }

            if (!ModelState.IsValid)
            {
                return View(area);
            }

            try
            {
                _context.Areas.Update(area);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Área actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Areas.AnyAsync(a => a.id_area == id))
                {
                    return NotFound();
                }

                throw;
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var area = await _context.Areas
                .FirstOrDefaultAsync(a => a.id_area == id);

            if (area == null)
            {
                return NotFound();
            }

            ViewBag.PuedeEliminar = !await TieneRelaciones(id);

            return View(area);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var area = await _context.Areas
                .FirstOrDefaultAsync(a => a.id_area == id);

            if (area == null)
            {
                return NotFound();
            }

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar el área porque tiene registros relacionados.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Areas.Remove(area);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Área eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ExisteNombreArea(string nombre, int? idAreaExcluir = null)
        {
            var nombreNormalizado = nombre.Trim().ToLower();

            return await _context.Areas.AnyAsync(a =>
                a.nombre.ToLower() == nombreNormalizado &&
                (!idAreaExcluir.HasValue || a.id_area != idAreaExcluir.Value));
        }

        private async Task<bool> TieneRelaciones(int idArea)
        {
            return await CrudRelationHelper.TieneRelaciones(
                    _context.Proyectos,
                    x => x.id_area == idArea
                )
                || await CrudRelationHelper.TieneRelaciones(
                    _context.AreasEmpresas,
                    x => x.id_area == idArea
                )
                || await CrudRelationHelper.TieneRelaciones(
                    _context.TipoObligaciones,
                    x => x.id_area == idArea
                );
        }

        public async Task<IActionResult> Details(int id)
        {
            var area = await _context.Areas
                .Include(a => a.Proyectos)
                .Include(a => a.AreasEmpresas)
                .Include(a => a.TiposObligacion)
                .FirstOrDefaultAsync(a => a.id_area == id);

            if (area == null)
            {
                return NotFound();
            }

            return View(area);
        }
    }
}