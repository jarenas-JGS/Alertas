using Alertas.Data;
using Alertas.Helpers;
using Alertas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Alertas.Helpers;

namespace Alertas.Controllers
{
    public class DominiosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DominiosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dominios = await _context.Dominios
                .OrderBy(d => d.nombre)
                .ToListAsync();

            return View(dominios);
        }

        public async Task<IActionResult> Details(int id)
        {
            var dominio = await _context.Dominios.FindAsync(id);

            if (dominio == null)
                return NotFound();

            return View(dominio);
        }

        public IActionResult Create()
        {
            return View(new Dominio());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Dominio dominio)
        {
            if (await ExisteNombre(dominio.nombre))
            {
                ModelState.AddModelError(nameof(dominio.nombre), "Ya existe un dominio con este nombre.");
            }

            if (!ModelState.IsValid)
                return View(dominio);

            dominio.nombre = dominio.nombre.Trim();

            _context.Dominios.Add(dominio);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Dominio creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var dominio = await _context.Dominios.FindAsync(id);

            if (dominio == null)
                return NotFound();

            return View(dominio);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Dominio dominio)
        {
            if (id != dominio.id_dominio)
                return BadRequest();

            if (await ExisteNombre(dominio.nombre, dominio.id_dominio))
            {
                ModelState.AddModelError(nameof(dominio.nombre), "Ya existe otro dominio con este nombre.");
            }

            if (!ModelState.IsValid)
                return View(dominio);

            dominio.nombre = dominio.nombre.Trim();

            _context.Dominios.Update(dominio);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Dominio actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var dominio = await _context.Dominios.FindAsync(id);

            if (dominio == null)
                return NotFound();

            ViewBag.TieneRelaciones = await TieneRelaciones(id);

            return View(dominio);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dominio = await _context.Dominios.FindAsync(id);

            if (dominio == null)
                return NotFound();

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar el dominio porque tiene registros asociados.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Dominios.Remove(dominio);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Dominio eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ExisteNombre(string nombre, int? idExcluir = null)
        {
            var nombreNormalizado = nombre.Trim().ToLower();

            return await _context.Dominios.AnyAsync(d =>
                d.nombre.ToLower() == nombreNormalizado &&
                (!idExcluir.HasValue || d.id_dominio != idExcluir.Value));
        }

        private async Task<bool> TieneRelaciones(int idDominio)
        {
            return await CrudRelationHelper.TieneRelaciones(
                _context.RegObls,
                x => x.id_dominio == idDominio
            );
        }
    }
}