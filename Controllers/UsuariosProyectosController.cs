using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class UsuariosProyectosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosProyectosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var registros = await _context.UsuariosProyectos
                .Include(x => x.Usuario)
                .Include(x => x.Proyecto)
                    .ThenInclude(p => p.Area)
                .Include(x => x.Rol)
                .Include(x => x.UsuarioAsignacion)
                .Where(x => x.Proyecto.configuracion_completa)
                .OrderBy(x => x.Proyecto.nombre)
                .ThenBy(x => x.Rol.id_rol)
                .ThenBy(x => x.Usuario.nombre)
                .ToListAsync();

            return View(registros);
        }

        public async Task<IActionResult> Details(int id)
        {
            var registro = await _context.UsuariosProyectos
                .Include(x => x.Usuario)
                .Include(x => x.Proyecto)
                    .ThenInclude(p => p.Area)
                .Include(x => x.Rol)
                .Include(x => x.UsuarioAsignacion)
                .FirstOrDefaultAsync(x => x.id_usuario_proyecto == id);

            if (registro == null)
                return NotFound();

            return View(registro);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new UsuarioProyectoViewModel
            {
                activo = true
            };

            await CargarCombos(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioProyectoViewModel vm)
        {
            if (await ExisteUsuarioProyectoRol(vm.id_usuario, vm.id_proyecto, vm.id_rol))
            {
                ModelState.AddModelError(nameof(vm.id_rol), "Este usuario ya tiene este rol asignado en el proyecto.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombos(vm);
                return View(vm);
            }

            var registro = new UsuarioProyecto
            {
                id_usuario = vm.id_usuario,
                id_proyecto = vm.id_proyecto,
                id_rol = vm.id_rol,
                activo = vm.activo,
                fecha_asignacion = DateTime.UtcNow,
                id_usuario_asignacion = ObtenerUsuarioActualId()
            };

            _context.UsuariosProyectos.Add(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Usuario asignado correctamente al proyecto.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var registro = await _context.UsuariosProyectos
                .Include(x => x.Usuario)
                .Include(x => x.Proyecto)
                .Include(x => x.Rol)
                .Include(x => x.UsuarioAsignacion)
                .FirstOrDefaultAsync(x => x.id_usuario_proyecto == id);

            if (registro == null)
                return NotFound();

            var vm = new UsuarioProyectoViewModel
            {
                id_usuario_proyecto = registro.id_usuario_proyecto,
                id_usuario = registro.id_usuario,
                id_proyecto = registro.id_proyecto,
                id_rol = registro.id_rol,
                activo = registro.activo,
                fecha_asignacion = registro.fecha_asignacion,
                id_usuario_asignacion = registro.id_usuario_asignacion,
                nombre_usuario = registro.Usuario?.nombre,
                nombre_proyecto = registro.Proyecto?.nombre,
                nombre_rol = registro.Rol?.nombre,
                nombre_usuario_asignacion = registro.UsuarioAsignacion?.nombre
            };

            await CargarCombos(vm, vm.id_proyecto);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioProyectoViewModel vm)
        {
            if (id != vm.id_usuario_proyecto)
                return BadRequest();

            var registro = await _context.UsuariosProyectos
                .Include(x => x.Usuario)
                .Include(x => x.Proyecto)
                .Include(x => x.Rol)
                .Include(x => x.UsuarioAsignacion)
                .FirstOrDefaultAsync(x => x.id_usuario_proyecto == id);

            if (registro == null)
                return NotFound();

            if (await ExisteUsuarioProyectoRol(vm.id_usuario, vm.id_proyecto, vm.id_rol, vm.id_usuario_proyecto))
            {
                ModelState.AddModelError(nameof(vm.id_rol), "Este usuario ya tiene este rol asignado en el proyecto.");
            }

            if (!ModelState.IsValid)
            {
                vm.fecha_asignacion = registro.fecha_asignacion;
                vm.id_usuario_asignacion = registro.id_usuario_asignacion;
                vm.nombre_usuario = registro.Usuario?.nombre;
                vm.nombre_proyecto = registro.Proyecto?.nombre;
                vm.nombre_rol = registro.Rol?.nombre;
                vm.nombre_usuario_asignacion = registro.UsuarioAsignacion?.nombre;

                await CargarCombos(vm);
                return View(vm);
            }

            registro.id_usuario = vm.id_usuario;
            registro.id_proyecto = vm.id_proyecto;
            registro.id_rol = vm.id_rol;
            registro.activo = vm.activo;

            if (!registro.fecha_asignacion.HasValue)
                registro.fecha_asignacion = DateTime.UtcNow;

            if (!registro.id_usuario_asignacion.HasValue)
                registro.id_usuario_asignacion = ObtenerUsuarioActualId();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Asignación de usuario a proyecto actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var registro = await _context.UsuariosProyectos
                .Include(x => x.Usuario)
                .Include(x => x.Proyecto)
                .Include(x => x.Rol)
                .Include(x => x.UsuarioAsignacion)
                .FirstOrDefaultAsync(x => x.id_usuario_proyecto == id);

            if (registro == null)
                return NotFound();

            ViewBag.PuedeEliminar = true;

            return View(registro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var registro = await _context.UsuariosProyectos
                .FirstOrDefaultAsync(x => x.id_usuario_proyecto == id);

            if (registro == null)
                return NotFound();

            _context.UsuariosProyectos.Remove(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Asignación de usuario a proyecto eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCombos(
            UsuarioProyectoViewModel vm,
            int? idProyectoActual = null)
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

            vm.Proyectos = await _context.Proyectos
                .Include(p => p.Area)
                .Where(p =>
                    (p.activo && p.configuracion_completa)
                    || p.id_proyecto == idProyectoActual)
                .OrderBy(p => p.Area.nombre)
                .ThenBy(p => p.nombre)
                .Select(p => new SelectListItem
                {
                    Value = p.id_proyecto.ToString(),
                    Text = p.Area.nombre + " - " + p.nombre
                })
                .ToListAsync();

            vm.Roles = await _context.Roles
                .Where(r => r.Activo)
                .OrderBy(r => r.id_rol)
                .Select(r => new SelectListItem
                {
                    Value = r.id_rol.ToString(),
                    Text = r.id_rol + " - " + r.nombre
                })
                .ToListAsync();
        }

        private async Task<bool> ExisteUsuarioProyectoRol(int idUsuario, int idProyecto, int idRol, int? idExcluir = null)
        {
            return await _context.UsuariosProyectos.AnyAsync(x =>
                x.id_usuario == idUsuario &&
                x.id_proyecto == idProyecto &&
                x.id_rol == idRol &&
                (!idExcluir.HasValue || x.id_usuario_proyecto != idExcluir.Value));
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