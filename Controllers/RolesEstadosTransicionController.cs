using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class RolesEstadosTransicionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RolesEstadosTransicionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var registros = await _context.RolesEstadosTransicion
                .Include(r => r.Rol)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.Proyecto)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.EstadoOrigen)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.EstadoDestino)
                .Where(r => r.EstadoTransicion.Proyecto.configuracion_completa)
                .OrderBy(r => r.EstadoTransicion.Proyecto.nombre)
                .ThenBy(r => r.EstadoTransicion.orden)
                .ThenBy(r => r.Rol.id_rol)
                .ToListAsync();

            return View(registros);
        }

        public async Task<IActionResult> Details(int id)
        {
            var registro = await _context.RolesEstadosTransicion
                .Include(r => r.Rol)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.Proyecto)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.EstadoOrigen)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.EstadoDestino)
                .FirstOrDefaultAsync(r => r.id_rol_estado_transicion == id);

            if (registro == null)
                return NotFound();

            return View(registro);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new RolEstadoTransicionViewModel
            {
                activo = true
            };

            await CargarCombos(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RolEstadoTransicionViewModel vm)
        {
            await ValidarReglas(vm);

            if (!ModelState.IsValid)
            {
                await CargarCombos(vm);
                return View(vm);
            }

            var registro = new RolEstadoTransicion
            {
                id_estado_transicion = vm.id_estado_transicion,
                id_rol = vm.id_rol,
                activo = vm.activo
            };

            _context.RolesEstadosTransicion.Add(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rol por transición creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var registro = await _context.RolesEstadosTransicion
                .Include(r => r.Rol)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.Proyecto)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.EstadoOrigen)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.EstadoDestino)
                .FirstOrDefaultAsync(r => r.id_rol_estado_transicion == id);

            if (registro == null)
                return NotFound();

            var vm = new RolEstadoTransicionViewModel
            {
                id_rol_estado_transicion = registro.id_rol_estado_transicion,
                id_estado_transicion = registro.id_estado_transicion,
                id_rol = registro.id_rol,
                activo = registro.activo,
                nombre_rol = registro.Rol?.nombre,
                nombre_transicion = ObtenerTextoTransicion(registro.EstadoTransicion)
            };

            await CargarCombos(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RolEstadoTransicionViewModel vm)
        {
            if (id != vm.id_rol_estado_transicion)
                return BadRequest();

            var registro = await _context.RolesEstadosTransicion
                .FirstOrDefaultAsync(r => r.id_rol_estado_transicion == id);

            if (registro == null)
                return NotFound();

            await ValidarReglas(vm);

            if (!ModelState.IsValid)
            {
                await CargarCombos(vm);
                return View(vm);
            }

            registro.id_estado_transicion = vm.id_estado_transicion;
            registro.id_rol = vm.id_rol;
            registro.activo = vm.activo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Rol por transición actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var registro = await _context.RolesEstadosTransicion
                .Include(r => r.Rol)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.Proyecto)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.EstadoOrigen)
                .Include(r => r.EstadoTransicion)
                    .ThenInclude(e => e.EstadoDestino)
                .FirstOrDefaultAsync(r => r.id_rol_estado_transicion == id);

            if (registro == null)
                return NotFound();

            ViewBag.PuedeEliminar = true;

            return View(registro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var registro = await _context.RolesEstadosTransicion
                .FirstOrDefaultAsync(r => r.id_rol_estado_transicion == id);

            if (registro == null)
                return NotFound();

            _context.RolesEstadosTransicion.Remove(registro);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rol por transición eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCombos(RolEstadoTransicionViewModel vm)
        {
            vm.EstadosTransicion = await _context.EstadosTransicion
                .Include(e => e.Proyecto)
                .Include(e => e.EstadoOrigen)
                .Include(e => e.EstadoDestino)
                .Where(e => e.activo && e.Proyecto.configuracion_completa)
                .OrderBy(e => e.Proyecto.nombre)
                .ThenBy(e => e.orden)
                .Select(e => new SelectListItem
                {
                    Value = e.id_estado_transicion.ToString(),
                    Text = e.id_estado_transicion + " - " +
                           e.Proyecto.nombre + " / " +
                           e.EstadoOrigen.nombre + " → " +
                           e.EstadoDestino.nombre + " / " +
                           e.nombre_accion
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

        private async Task ValidarReglas(RolEstadoTransicionViewModel vm)
        {
            var transicion = await _context.EstadosTransicion
                .Include(e => e.Proyecto)
                .FirstOrDefaultAsync(e => e.id_estado_transicion == vm.id_estado_transicion);

            if (transicion == null)
            {
                ModelState.AddModelError(nameof(vm.id_estado_transicion), "La transición seleccionada no existe.");
                return;
            }

            if (!transicion.activo || !transicion.Proyecto.configuracion_completa)
            {
                ModelState.AddModelError(nameof(vm.id_estado_transicion), "La transición seleccionada no está activa o su proyecto no está completamente configurado.");
            }

            var rolValido = await _context.Roles
                .AnyAsync(r => r.id_rol == vm.id_rol && r.Activo);

            if (!rolValido)
            {
                ModelState.AddModelError(nameof(vm.id_rol), "Debe seleccionar un rol activo válido.");
            }

            var existeDuplicado = await _context.RolesEstadosTransicion.AnyAsync(r =>
                r.id_estado_transicion == vm.id_estado_transicion &&
                r.id_rol == vm.id_rol &&
                r.id_rol_estado_transicion != vm.id_rol_estado_transicion);

            if (existeDuplicado)
            {
                ModelState.AddModelError(nameof(vm.id_rol), "Este rol ya está asociado a la transición seleccionada.");
            }
        }

        private string ObtenerTextoTransicion(EstadoTransicion transicion)
        {
            return $"{transicion.id_estado_transicion} - {transicion.Proyecto?.nombre} / {transicion.EstadoOrigen?.nombre} → {transicion.EstadoDestino?.nombre} / {transicion.nombre_accion}";
        }
    }
}