using Alertas.Data;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class EstadosTransicionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EstadosTransicionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var transiciones = await _context.EstadosTransicion
                .Include(e => e.Proyecto)
                .Include(e => e.EstadoOrigen)
                .Include(e => e.EstadoDestino)
                .Where(e => e.Proyecto.configuracion_completa)
                .OrderBy(e => e.Proyecto.nombre)
                .ThenBy(e => e.orden)
                .ThenBy(e => e.EstadoOrigen.orden)
                .ThenBy(e => e.EstadoDestino.orden)
                .ToListAsync();

            return View(transiciones);
        }

        public async Task<IActionResult> Details(int id)
        {
            var transicion = await _context.EstadosTransicion
                .Include(e => e.Proyecto)
                .Include(e => e.EstadoOrigen)
                .Include(e => e.EstadoDestino)
                .Include(e => e.RolesEstadosTransicion)
                    .ThenInclude(r => r.Rol)
                .FirstOrDefaultAsync(e => e.id_estado_transicion == id);

            if (transicion == null)
                return NotFound();

            if (!transicion.Proyecto.configuracion_completa)
                return RedirectToAction(nameof(Index));

            return View(transicion);
        }

        public IActionResult Create()
        {
            TempData["Error"] = "Las transiciones de estado deben crearse desde el Wizard de configuración del proyecto.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var transicion = await _context.EstadosTransicion
                .Include(e => e.Proyecto)
                .Include(e => e.EstadoOrigen)
                .Include(e => e.EstadoDestino)
                .FirstOrDefaultAsync(e => e.id_estado_transicion == id);

            if (transicion == null)
                return NotFound();

            if (!transicion.Proyecto.configuracion_completa)
                return RedirectToAction(nameof(Index));

            var tieneObligaciones = await _context.RegObls
                .AnyAsync(o => o.id_proyecto == transicion.id_proyecto);

            var vm = new EstadoTransicionEditViewModel
            {
                id_estado_transicion = transicion.id_estado_transicion,
                id_proyecto = transicion.id_proyecto,
                nombre_proyecto = transicion.Proyecto.nombre,

                id_estado_origen = transicion.id_estado_origen,
                estado_origen = transicion.EstadoOrigen.nombre,

                id_estado_destino = transicion.id_estado_destino,
                estado_destino = transicion.EstadoDestino.nombre,

                nombre_accion = transicion.nombre_accion,

                requiere_observacion = transicion.requiere_observacion,
                es_aprobacion = transicion.es_aprobacion,
                es_rechazo = transicion.es_rechazo,
                es_anulacion = transicion.es_anulacion,
                activo = transicion.activo,
                orden = transicion.orden,

                puedeReabrirWizard = !tieneObligaciones
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EstadoTransicionEditViewModel vm)
        {
            if (id != vm.id_estado_transicion)
                return BadRequest();

            var transicion = await _context.EstadosTransicion
                .Include(e => e.Proyecto)
                .Include(e => e.EstadoOrigen)
                .Include(e => e.EstadoDestino)
                .FirstOrDefaultAsync(e => e.id_estado_transicion == id);

            if (transicion == null)
                return NotFound();

            if (!transicion.Proyecto.configuracion_completa)
                return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid)
            {
                vm.id_proyecto = transicion.id_proyecto;
                vm.nombre_proyecto = transicion.Proyecto.nombre;

                vm.id_estado_origen = transicion.id_estado_origen;
                vm.id_estado_destino = transicion.id_estado_destino;

                vm.requiere_observacion = transicion.requiere_observacion;
                vm.es_aprobacion = transicion.es_aprobacion;
                vm.es_rechazo = transicion.es_rechazo;
                vm.es_anulacion = transicion.es_anulacion;
                vm.activo = transicion.activo;
                vm.orden = transicion.orden;

                vm.puedeReabrirWizard = !await _context.RegObls
                    .AnyAsync(o => o.id_proyecto == transicion.id_proyecto);

                return View(vm);
            }

            transicion.nombre_accion = vm.nombre_accion.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "La transición de estado fue actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReabrirConfiguracionProyecto(int idProyecto)
        {
            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == idProyecto);

            if (proyecto == null)
                return NotFound();

            var tieneObligaciones = await _context.RegObls
                .AnyAsync(o => o.id_proyecto == idProyecto);

            if (tieneObligaciones)
            {
                TempData["Error"] = "No se puede reabrir la configuración porque el proyecto ya tiene obligaciones creadas.";
                return RedirectToAction(nameof(Index));
            }

            proyecto.configuracion_completa = false;

            await _context.SaveChangesAsync();

            TempData["Success"] = "El proyecto fue pasado a configuración incompleta. Puedes ajustarlo desde el Wizard.";

            return RedirectToAction("Continuar", "ProyectoWizard", new { idProyecto });
        }

        private async Task<bool> ExisteNombreAccionProyecto(string nombreAccion, int idProyecto, int idExcluir)
        {
            var nombreNormalizado = nombreAccion.Trim().ToLower();

            return await _context.EstadosTransicion.AnyAsync(e =>
                e.nombre_accion.ToLower() == nombreNormalizado &&
                e.id_proyecto == idProyecto &&
                e.id_estado_transicion != idExcluir);
        }

    }
}