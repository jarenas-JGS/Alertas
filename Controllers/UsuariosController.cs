using Alertas.Data;
using Alertas.Helpers;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .OrderBy(u => u.nombre)
                .ToListAsync();

            return View(usuarios);
        }

        public async Task<IActionResult> Details(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.RegImpsComoAutorizador)
                .Include(u => u.RegImpsComoAprobador)
                .Include(u => u.ProyectosCreados)
                .Include(u => u.UsuariosObligaciones)
                .Include(u => u.UsuariosObligacionesAsignadas)
                .Include(u => u.UsuariosProyectos)
                .Include(u => u.UsuariosProyectosAsignados)
                .FirstOrDefaultAsync(u => u.id_usuario == id);

            if (usuario == null)
                return NotFound();

            return View(usuario);
        }

        public IActionResult Create()
        {
            var vm = new UsuarioCreateViewModel
            {
                activo = true
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateViewModel vm)
        {
            if (await ExisteUsuario(vm.usuario))
            {
                ModelState.AddModelError(nameof(vm.usuario), "Ya existe un usuario con este código.");
            }

            if (await ExisteEmail(vm.email))
            {
                ModelState.AddModelError(nameof(vm.email), "Ya existe un usuario con este email.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var usuario = new Usuario
            {
                usuario = vm.usuario.Trim(),
                nombre = vm.nombre.Trim(),
                email = vm.email.Trim(),
                activo = vm.activo,
                es_super_admin = vm.es_super_admin,
                fecha_creacion = DateTime.UtcNow,
                must_change_password = true,
                last_password_change_at = DateTime.UtcNow,            };

            usuario.clave_hash = SeguridadHelper.HashPassword(vm.clave);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Usuario creado correctamente. Deberá cambiar la clave en el próximo inicio de sesión.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.id_usuario == id);

            if (usuario == null)
                return NotFound();

            var vm = new UsuarioEditViewModel
            {
                id_usuario = usuario.id_usuario,
                usuario = usuario.usuario,
                nombre = usuario.nombre,
                email = usuario.email,
                activo = usuario.activo,
                es_super_admin = usuario.es_super_admin,
                fecha_creacion = usuario.fecha_creacion,
                ultimo_login = usuario.ultimo_login,
                must_change_password = usuario.must_change_password,
                last_password_change_at = usuario.last_password_change_at
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioEditViewModel vm)
        {
            if (id != vm.id_usuario)
                return BadRequest();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.id_usuario == id);

            if (usuario == null)
                return NotFound();

            if (await ExisteUsuario(vm.usuario, vm.id_usuario))
            {
                ModelState.AddModelError(nameof(vm.usuario), "Ya existe otro usuario con este código.");
            }

            if (await ExisteEmail(vm.email, vm.id_usuario))
            {
                ModelState.AddModelError(nameof(vm.email), "Ya existe otro usuario con este email.");
            }

            var cambiaClave = !string.IsNullOrWhiteSpace(vm.nueva_clave);

            if (cambiaClave && string.IsNullOrWhiteSpace(vm.confirmar_nueva_clave))
            {
                ModelState.AddModelError(nameof(vm.confirmar_nueva_clave), "Debe confirmar la nueva clave.");
            }

            if (!ModelState.IsValid)
            {
                vm.fecha_creacion = usuario.fecha_creacion;
                vm.ultimo_login = usuario.ultimo_login;
                vm.must_change_password = usuario.must_change_password;
                vm.last_password_change_at = usuario.last_password_change_at;

                return View(vm);
            }

            usuario.usuario = vm.usuario.Trim();
            usuario.nombre = vm.nombre.Trim();
            usuario.email = vm.email.Trim();
            usuario.activo = vm.activo;
            usuario.es_super_admin = vm.es_super_admin;

            if (cambiaClave)
            {
                usuario.clave_hash = SeguridadHelper.HashPassword(vm.nueva_clave!);
                usuario.must_change_password = true;
                usuario.last_password_change_at = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = cambiaClave
                ? "Usuario actualizado correctamente. Deberá cambiar la clave en el próximo inicio de sesión."
                : "Usuario actualizado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.id_usuario == id);

            if (usuario == null)
                return NotFound();

            ViewBag.PuedeEliminar = !await TieneRelacionesRegObl(id);

            return View(usuario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.id_usuario == id);

            if (usuario == null)
                return NotFound();

            if (await TieneRelacionesRegObl(id))
            {
                TempData["Error"] = "No se puede eliminar el usuario porque tiene obligaciones asociadas.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Usuario eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ExisteUsuario(string usuario, int? idExcluir = null)
        {
            var usuarioNormalizado = usuario.Trim().ToLower();

            return await _context.Usuarios.AnyAsync(u =>
                u.usuario.ToLower() == usuarioNormalizado &&
                (!idExcluir.HasValue || u.id_usuario != idExcluir.Value));
        }

        private async Task<bool> ExisteEmail(string email, int? idExcluir = null)
        {
            var emailNormalizado = email.Trim().ToLower();

            return await _context.Usuarios.AnyAsync(u =>
                u.email.ToLower() == emailNormalizado &&
                (!idExcluir.HasValue || u.id_usuario != idExcluir.Value));
        }

        private async Task<bool> TieneRelacionesRegObl(int idUsuario)
        {
            var tieneUsuarioObligacion = await _context.UsuariosObligaciones
                .AnyAsync(uo =>
                    uo.id_usuario == idUsuario ||
                    uo.id_usuario_asignacion == idUsuario);

            var tieneUsuarioProyecto = await _context.UsuariosProyectos
                .AnyAsync(up =>
                    up.id_usuario == idUsuario ||
                    up.id_usuario_asignacion == idUsuario);

            var tieneProyectosCreados = await _context.Proyectos
                .AnyAsync(p => p.id_usuario_creacion == idUsuario);

            return tieneUsuarioObligacion
                || tieneUsuarioProyecto
                || tieneProyectosCreados;
        }
    }
}