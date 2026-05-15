using Alertas.Data;
using Alertas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Alertas.Helpers;

namespace Alertas.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var clientes = await _context.Clientes
                .OrderBy(c => c.nombre)
                .ToListAsync();

            return View(clientes);
        }

        public async Task<IActionResult> Details(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            return View(cliente);
        }

        public IActionResult Create()
        {
            return View(new Cliente { activo = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cliente cliente)
        {
            if (await ExisteNit(cliente.nit))
            {
                ModelState.AddModelError(nameof(cliente.nit), "Ya existe un cliente con este NIT.");
            }

            if (!ModelState.IsValid)
                return View(cliente);

            cliente.nit = cliente.nit.Trim();
            cliente.nombre = cliente.nombre.Trim();

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cliente creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            return View(cliente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cliente cliente)
        {
            if (id != cliente.id_cliente)
                return BadRequest();

            if (await ExisteNit(cliente.nit, cliente.id_cliente))
            {
                ModelState.AddModelError(nameof(cliente.nit), "Ya existe otro cliente con este NIT.");
            }

            if (!ModelState.IsValid)
                return View(cliente);

            cliente.nit = cliente.nit.Trim();
            cliente.nombre = cliente.nombre.Trim();

            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cliente actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.id_cliente == id);

            if (cliente == null)
                return NotFound();

            ViewBag.PuedeEliminar = !await TieneRelaciones(id);

            return View(cliente);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar el cliente porque tiene registros relacionados.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cliente eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ExisteNit(string nit, int? idExcluir = null)
        {
            var nitNormalizado = nit.Trim().ToLower();

            return await _context.Clientes.AnyAsync(c =>
                c.nit.ToLower() == nitNormalizado &&
                (!idExcluir.HasValue || c.id_cliente != idExcluir.Value));
        }

        private async Task<bool> TieneRelaciones(int idCliente)
        {
            return await CrudRelationHelper.TieneRelaciones(
                _context.Empresas,
                e => e.id_cliente == idCliente
            );
        }
    }
}