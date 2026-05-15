using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class UsuariosAreasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosAreasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var registros = await _context.UsuarioArea
                .Include(x => x.Usuario)
                .Include(x => x.Area)
                .OrderBy(x => x.Usuario.nombre)
                .ThenBy(x => x.Area.nombre)
                .ToListAsync();

            return View(registros);
        }

        public async Task<IActionResult> Details(int id)
        {
            var registro = await _context.UsuarioArea
                .Include(x => x.Usuario)
                .Include(x => x.Area)
                .FirstOrDefaultAsync(x => x.id_usuario_area == id);

            if (registro == null)
                return NotFound();

            return View(registro);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new UsuarioAreaViewModel
            {
                activo = true
            };

            await CargarCombos(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioAreaViewModel vm)
        {
            if (await ExisteUsuarioArea(vm.id_usuario, vm.id_area))
            {
                ModelState.AddModelError(nameof(vm.id_area), "Este usuario ya tiene asignada esta área.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombos(vm);
                return View(vm);
            }

            var registro = new UsuarioArea
            {
                id_usuario = vm.id_usuario,
                id_area = vm.id_area,
                activo = vm.activo,
                fecha_asignacion = DateTime.UtcNow,
                id_usuario_asignacion = ObtenerUsuarioActualId()
            };

            _context.UsuarioArea.Add(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Área asignada correctamente al usuario.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var registro = await _context.UsuarioArea
                .Include(x => x.Usuario)
                .Include(x => x.Area)
                .FirstOrDefaultAsync(x => x.id_usuario_area == id);

            if (registro == null)
                return NotFound();

            var vm = new UsuarioAreaViewModel
            {
                id_usuario_area = registro.id_usuario_area,
                id_usuario = registro.id_usuario,
                id_area = registro.id_area,
                activo = registro.activo,
                fecha_asignacion = registro.fecha_asignacion,
                id_usuario_asignacion = registro.id_usuario_asignacion,
                nombre_usuario = registro.Usuario?.nombre,
                nombre_area = registro.Area?.nombre
            };

            await CargarCombos(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioAreaViewModel vm)
        {
            if (id != vm.id_usuario_area)
                return BadRequest();

            var registro = await _context.UsuarioArea
                .Include(x => x.Usuario)
                .Include(x => x.Area)
                .FirstOrDefaultAsync(x => x.id_usuario_area == id);

            if (registro == null)
                return NotFound();

            if (await ExisteUsuarioArea(vm.id_usuario, vm.id_area, vm.id_usuario_area))
            {
                ModelState.AddModelError(nameof(vm.id_area), "Este usuario ya tiene asignada esta área.");
            }

            if (!ModelState.IsValid)
            {
                vm.nombre_usuario = registro.Usuario?.nombre;
                vm.nombre_area = registro.Area?.nombre;
                vm.fecha_asignacion = registro.fecha_asignacion;
                vm.id_usuario_asignacion = registro.id_usuario_asignacion;

                await CargarCombos(vm);
                return View(vm);
            }

            registro.id_usuario = vm.id_usuario;
            registro.id_area = vm.id_area;
            registro.activo = vm.activo;

            if (!registro.fecha_asignacion.HasValue)
                registro.fecha_asignacion = DateTime.UtcNow;

            if (!registro.id_usuario_asignacion.HasValue)
                registro.id_usuario_asignacion = ObtenerUsuarioActualId();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Asignación de área actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var registro = await _context.UsuarioArea
                .Include(x => x.Usuario)
                .Include(x => x.Area)
                .FirstOrDefaultAsync(x => x.id_usuario_area == id);

            if (registro == null)
                return NotFound();

            ViewBag.PuedeEliminar = true;

            return View(registro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var registro = await _context.UsuarioArea
                .FirstOrDefaultAsync(x => x.id_usuario_area == id);

            if (registro == null)
                return NotFound();

            _context.UsuarioArea.Remove(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Asignación de área eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCombos(UsuarioAreaViewModel vm)
        {
            vm.Usuarios = await _context.Usuarios
                .Where(u => u.activo)
                .OrderBy(u => u.nombre)
                .Select(u => new SelectListItem
                {
                    Value = u.id_usuario.ToString(),
                    Text = u.nombre + " (" + u.usuario + ")"
                })
                .ToListAsync();

            vm.Areas = await _context.Areas
                .OrderBy(a => a.nombre)
                .Select(a => new SelectListItem
                {
                    Value = a.id_area.ToString(),
                    Text = a.nombre
                })
                .ToListAsync();
        }

        private async Task<bool> ExisteUsuarioArea(int idUsuario, int idArea, int? idExcluir = null)
        {
            return await _context.UsuarioArea.AnyAsync(x =>
                x.id_usuario == idUsuario &&
                x.id_area == idArea &&
                (!idExcluir.HasValue || x.id_usuario_area != idExcluir.Value));
        }

        private int? ObtenerUsuarioActualId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (claim == null)
                return null;

            if (int.TryParse(claim.Value, out int idUsuario))
                return idUsuario;

            return null;
        }
    }
}