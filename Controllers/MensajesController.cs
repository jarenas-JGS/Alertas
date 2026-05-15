using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class MensajesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MensajesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var mensajes = await _context.Mensajes
                .OrderBy(m => m.prioridad)
                .ThenBy(m => m.nombre)
                .ToListAsync();

            return View(mensajes);
        }

        public async Task<IActionResult> Details(int id)
        {
            var mensaje = await _context.Mensajes
                .Include(m => m.GruposAlertasDias)
                .FirstOrDefaultAsync(m => m.id_mensaje == id);

            if (mensaje == null)
                return NotFound();

            return View(mensaje);
        }

        public IActionResult Create()
        {
            var vm = new MensajeViewModel
            {
                activo = true,
                prioridad = 1
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MensajeViewModel vm)
        {
            if (await ExisteNombre(vm.nombre))
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe un mensaje con este nombre.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var mensaje = new Mensaje
            {
                prioridad = vm.prioridad,
                texto = vm.texto.Trim(),
                nombre = vm.nombre.Trim(),
                activo = vm.activo
            };

            _context.Mensajes.Add(mensaje);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mensaje creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var mensaje = await _context.Mensajes
                .FirstOrDefaultAsync(m => m.id_mensaje == id);

            if (mensaje == null)
                return NotFound();

            var vm = new MensajeViewModel
            {
                id_mensaje = mensaje.id_mensaje,
                prioridad = mensaje.prioridad,
                texto = mensaje.texto,
                nombre = mensaje.nombre,
                activo = mensaje.activo
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MensajeViewModel vm)
        {
            if (id != vm.id_mensaje)
                return BadRequest();

            var mensaje = await _context.Mensajes
                .FirstOrDefaultAsync(m => m.id_mensaje == id);

            if (mensaje == null)
                return NotFound();

            if (await ExisteNombre(vm.nombre, vm.id_mensaje))
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe otro mensaje con este nombre.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            mensaje.prioridad = vm.prioridad;
            mensaje.texto = vm.texto.Trim();
            mensaje.nombre = vm.nombre.Trim();
            mensaje.activo = vm.activo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Mensaje actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var mensaje = await _context.Mensajes
                .FirstOrDefaultAsync(m => m.id_mensaje == id);

            if (mensaje == null)
                return NotFound();

            ViewBag.PuedeEliminar = !await TieneRelaciones(id);

            return View(mensaje);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mensaje = await _context.Mensajes
                .FirstOrDefaultAsync(m => m.id_mensaje == id);

            if (mensaje == null)
                return NotFound();

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar el mensaje porque tiene alertas por días asociadas.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Mensajes.Remove(mensaje);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mensaje eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ExisteNombre(string nombre, int? idExcluir = null)
        {
            var nombreNormalizado = nombre.Trim().ToLower();

            return await _context.Mensajes.AnyAsync(m =>
                m.nombre.ToLower() == nombreNormalizado &&
                (!idExcluir.HasValue || m.id_mensaje != idExcluir.Value));
        }

        private async Task<bool> TieneRelaciones(int id)
        {
            return await _context.GruposAlertasDias
                .AnyAsync(g => g.id_mensaje == id);
        }
    }
}