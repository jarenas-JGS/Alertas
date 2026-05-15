using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class RolesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles
                .OrderBy(r => r.id_rol)
                .ToListAsync();

            return View(roles);
        }

        public async Task<IActionResult> Details(int id)
        {
            var rol = await _context.Roles
                .Include(r => r.RolesEstadosTransicion)
                .Include(r => r.UsuariosObligaciones)
                .Include(r => r.UsuariosProyectos)
                .Include(r => r.GruposAlertasDias)
                .FirstOrDefaultAsync(r => r.id_rol == id);

            if (rol == null)
                return NotFound();

            return View(rol);
        }

        public IActionResult Create()
        {
            TempData["Error"] = "Los roles del sistema no deben crearse desde este CRUD porque son usados por la lógica interna de la aplicación.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var rol = await _context.Roles
                .FirstOrDefaultAsync(r => r.id_rol == id);

            if (rol == null)
                return NotFound();

            var vm = new RolEditViewModel
            {
                id_rol = rol.id_rol,
                nombre = rol.nombre,
                Activo = rol.Activo
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RolEditViewModel vm)
        {
            if (id != vm.id_rol)
                return BadRequest();

            var rol = await _context.Roles
                .FirstOrDefaultAsync(r => r.id_rol == id);

            if (rol == null)
                return NotFound();

            rol.Activo = vm.Activo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Rol actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var rol = await _context.Roles
                .FirstOrDefaultAsync(r => r.id_rol == id);

            if (rol == null)
                return NotFound();

            ViewBag.PuedeEliminar = !await TieneRelaciones(id);

            return View(rol);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rol = await _context.Roles
                .FirstOrDefaultAsync(r => r.id_rol == id);

            if (rol == null)
                return NotFound();

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar el rol porque tiene registros relacionados.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Roles.Remove(rol);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rol eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> TieneRelaciones(int id)
        {
            var tieneRolEstadoTransicion = await _context.RolesEstadosTransicion
                .AnyAsync(r => r.id_rol == id);

            var tieneUsuariosObligaciones = await _context.UsuariosObligaciones
                .AnyAsync(r => r.id_rol == id);

            var tieneUsuariosProyectos = await _context.UsuariosProyectos
                .AnyAsync(r => r.id_rol == id);

            return tieneRolEstadoTransicion
                || tieneUsuariosObligaciones
                || tieneUsuariosProyectos;
        }
    }
}