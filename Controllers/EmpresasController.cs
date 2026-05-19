using Alertas.Data;
using Alertas.Helpers;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace Alertas.Controllers
{
    public class EmpresasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmpresasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var empresas = await _context.Empresas
                .Include(e => e.Cliente)
                .OrderBy(e => e.nombre)
                .ToListAsync();

            return View(empresas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Cliente)
                .FirstOrDefaultAsync(e => e.id_empresa == id);

            if (empresa == null)
                return NotFound();

            return View(empresa);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new EmpresaViewModel { activo = true };
            await CargarCombos(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmpresaViewModel vm)
        {
            if (await ExisteNit(vm.nit))
            {
                ModelState.AddModelError(nameof(vm.nit), "Ya existe una empresa con este NIT.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombos(vm);
                return View(vm);
            }

            var empresa = new Empresa
            {
                nit = vm.nit.Trim(),
                ult_dig = ObtenerUltimoDigitoNit(vm.nit),
                nombre = vm.nombre.Trim(),
                email = vm.email?.Trim(),
                activo = vm.activo,
                id_cliente = vm.id_cliente
            };

            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Empresa creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);

            if (empresa == null)
                return NotFound();

            var vm = new EmpresaViewModel
            {
                id_empresa = empresa.id_empresa,
                nit = empresa.nit,
                ult_dig = empresa.ult_dig,
                nombre = empresa.nombre,
                email = empresa.email,
                activo = empresa.activo,
                id_cliente = empresa.id_cliente
            };

            await CargarCombos(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmpresaViewModel vm)
        {
            if (id != vm.id_empresa)
                return BadRequest();

            if (await ExisteNit(vm.nit, vm.id_empresa))
            {
                ModelState.AddModelError(nameof(vm.nit), "Ya existe otra empresa con este NIT.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombos(vm);
                return View(vm);
            }

            var empresa = await _context.Empresas.FindAsync(id);

            if (empresa == null)
                return NotFound();

            empresa.nit = vm.nit.Trim();
            empresa.ult_dig = ObtenerUltimoDigitoNit(vm.nit);
            empresa.nombre = vm.nombre.Trim();
            empresa.email = vm.email?.Trim();
            empresa.activo = vm.activo;
            empresa.id_cliente = vm.id_cliente;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Empresa actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Cliente)
                .FirstOrDefaultAsync(e => e.id_empresa == id);

            if (empresa == null)
                return NotFound();

            ViewBag.PuedeEliminar = !await TieneRelaciones(id);

            return View(empresa);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);

            if (empresa == null)
                return NotFound();

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar la empresa porque tiene registros relacionados.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Empresas.Remove(empresa);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Empresa eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCombos(EmpresaViewModel vm)
        {
            vm.Clientes = await _context.Clientes
                .Where(c => c.activo)
                .OrderBy(c => c.nombre)
                .Select(c => new SelectListItem
                {
                    Value = c.id_cliente.ToString(),
                    Text = c.nombre
                })
                .ToListAsync();
        }

        private async Task<bool> ExisteNit(string nit, int? idExcluir = null)
        {
            var nitNormalizado = nit.Trim().ToLower();

            return await _context.Empresas.AnyAsync(e =>
                e.nit.ToLower() == nitNormalizado &&
                (!idExcluir.HasValue || e.id_empresa != idExcluir.Value));
        }

        private async Task<bool> TieneRelaciones(int idEmpresa)
        {
            return await CrudRelationHelper.TieneRelaciones(
                    _context.RegObls,
                    x => x.id_empresa == idEmpresa
                )
                || await CrudRelationHelper.TieneRelaciones(
                    _context.AreasEmpresas,
                    x => x.id_empresa == idEmpresa
                );
        }

        private int ObtenerUltimoDigitoNit(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return 0;

            var soloDigitos = new string(nit.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(soloDigitos))
                return 0;

            return int.Parse(soloDigitos[^1].ToString());
        }
    }
}