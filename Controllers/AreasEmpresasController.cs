using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace Alertas.Controllers
{
    public class AreasEmpresasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AreasEmpresasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // INDEX
        // =========================
        public async Task<IActionResult> Index()
        {
            var data = await _context.AreasEmpresas
                .Include(a => a.Area)
                .Include(a => a.Empresa)
                .OrderBy(a => a.Area.nombre)
                .ToListAsync();

            return View(data);
        }

        // =========================
        // CREATE
        // =========================
        public async Task<IActionResult> Create()
        {
            var vm = new AreaEmpresaViewModel();
            await CargarCombos(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AreaEmpresaViewModel vm)
        {
            if (await ExisteRegistro(vm))
            {
                ModelState.AddModelError("", "Ya existe un registro con esta Área, Empresa y Email.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombos(vm);
                return View(vm);
            }

            var entity = new AreaEmpresa
            {
                id_area = vm.id_area,
                id_empresa = vm.id_empresa,
                email = vm.email,
                activo = vm.activo
            };

            _context.AreasEmpresas.Add(entity);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Registro creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _context.AreasEmpresas.FindAsync(id);

            if (entity == null)
                return NotFound();

            var vm = new AreaEmpresaViewModel
            {
                id_area_empresa = entity.id_area_empresa,
                id_area = entity.id_area,
                id_empresa = entity.id_empresa,
                email = entity.email,
                activo = entity.activo
            };

            await CargarCombos(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AreaEmpresaViewModel vm)
        {
            if (id != vm.id_area_empresa)
                return BadRequest();

            if (await ExisteRegistro(vm, id))
            {
                ModelState.AddModelError("", "Ya existe otro registro con esta combinación.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombos(vm);
                return View(vm);
            }

            var entity = await _context.AreasEmpresas.FindAsync(id);

            if (entity == null)
                return NotFound();

            entity.id_area = vm.id_area;
            entity.id_empresa = vm.id_empresa;
            entity.email = vm.email;
            entity.activo = vm.activo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Registro actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DETAILS
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var data = await _context.AreasEmpresas
                .Include(a => a.Area)
                .Include(a => a.Empresa)
                .FirstOrDefaultAsync(a => a.id_area_empresa == id);

            if (data == null)
                return NotFound();

            return View(data);
        }

        // =========================
        // DELETE (lógico)
        // =========================
        public async Task<IActionResult> Delete(int id)
        {
            var data = await _context.AreasEmpresas
                .Include(a => a.Area)
                .Include(a => a.Empresa)
                .FirstOrDefaultAsync(a => a.id_area_empresa == id);

            if (data == null)
                return NotFound();

            return View(data);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entity = await _context.AreasEmpresas.FindAsync(id);

            if (entity == null)
                return NotFound();

            entity.activo = false; // 👈 eliminación lógica
            await _context.SaveChangesAsync();

            TempData["Success"] = "Registro inactivado.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // HELPERS
        // =========================
        private async Task CargarCombos(AreaEmpresaViewModel vm)
        {
            vm.Areas = await _context.Areas
                .Select(a => new SelectListItem
                {
                    Value = a.id_area.ToString(),
                    Text = a.nombre
                }).ToListAsync();

            vm.Empresas = await _context.Empresas
                .Select(e => new SelectListItem
                {
                    Value = e.id_empresa.ToString(),
                    Text = e.nombre
                }).ToListAsync();
        }

        private async Task<bool> ExisteRegistro(AreaEmpresaViewModel vm, int? idExcluir = null)
        {
            return await _context.AreasEmpresas.AnyAsync(x =>
                x.id_area == vm.id_area &&
                x.id_empresa == vm.id_empresa &&
                x.email.ToLower() == vm.email.ToLower() &&
                (!idExcluir.HasValue || x.id_area_empresa != idExcluir));
        }
    }
}