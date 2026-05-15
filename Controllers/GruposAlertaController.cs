using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class GruposAlertaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GruposAlertaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var grupos = await _context.GruposAlertas
                .Include(g => g.Proyecto)
                .Where(g => g.Proyecto.configuracion_completa)
                .OrderBy(g => g.Proyecto.nombre)
                .ThenBy(g => g.nombre)
                .ToListAsync();

            return View(grupos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var grupo = await _context.GruposAlertas
                .Include(g => g.Proyecto)
                .Include(g => g.GruposAlertasDias)
                .FirstOrDefaultAsync(g => g.id_grupo_alerta == id);

            if (grupo == null)
                return NotFound();

            return View(grupo);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new GrupoAlertaViewModel
            {
                activo = true
            };

            await CargarProyectos(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GrupoAlertaViewModel vm)
        {
            if (await ExisteGrupoActivoProyecto(vm.id_proyecto))
            {
                ModelState.AddModelError(nameof(vm.id_proyecto), "Este proyecto ya tiene un grupo de alertas activo.");
            }

            if (!ModelState.IsValid)
            {
                await CargarProyectos(vm);
                return View(vm);
            }

            var grupo = new GrupoAlerta
            {
                nombre = vm.nombre.Trim(),
                id_proyecto = vm.id_proyecto,
                activo = vm.activo
            };

            _context.GruposAlertas.Add(grupo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Grupo de alerta creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var grupo = await _context.GruposAlertas
                .Include(g => g.Proyecto)
                .FirstOrDefaultAsync(g => g.id_grupo_alerta == id);

            if (grupo == null)
                return NotFound();

            var vm = new GrupoAlertaViewModel
            {
                id_grupo_alerta = grupo.id_grupo_alerta,
                nombre = grupo.nombre,
                id_proyecto = grupo.id_proyecto,
                nombre_proyecto = grupo.Proyecto?.nombre,
                activo = grupo.activo
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GrupoAlertaViewModel vm)
        {
            if (id != vm.id_grupo_alerta)
                return BadRequest();

            var grupo = await _context.GruposAlertas
                .Include(g => g.Proyecto)
                .FirstOrDefaultAsync(g => g.id_grupo_alerta == id);

            if (grupo == null)
                return NotFound();

            if (vm.activo && await ExisteGrupoActivoProyecto(grupo.id_proyecto, grupo.id_grupo_alerta))
            {
                ModelState.AddModelError(nameof(vm.activo), "Este proyecto ya tiene otro grupo de alertas activo.");
            }

            if (!ModelState.IsValid)
            {
                vm.id_proyecto = grupo.id_proyecto;
                vm.nombre_proyecto = grupo.Proyecto?.nombre;
                return View(vm);
            }

            grupo.nombre = vm.nombre.Trim();
            grupo.activo = vm.activo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Grupo de alerta actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var grupo = await _context.GruposAlertas
                .Include(g => g.Proyecto)
                .FirstOrDefaultAsync(g => g.id_grupo_alerta == id);

            if (grupo == null)
                return NotFound();

            ViewBag.PuedeEliminar = !await TieneRelaciones(id);

            return View(grupo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grupo = await _context.GruposAlertas
                .FirstOrDefaultAsync(g => g.id_grupo_alerta == id);

            if (grupo == null)
                return NotFound();

            if (await TieneRelaciones(id))
            {
                TempData["Error"] = "No se puede eliminar el grupo de alerta porque tiene días asociados.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.GruposAlertas.Remove(grupo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Grupo de alerta eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarProyectos(GrupoAlertaViewModel vm)
        {
            vm.Proyectos = await _context.Proyectos
                .Where(p => p.activo && p.configuracion_completa)
                .OrderBy(p => p.nombre)
                .Select(p => new SelectListItem
                {
                    Value = p.id_proyecto.ToString(),
                    Text = p.nombre
                })
                .ToListAsync();
        }

        private async Task<bool> ExisteGrupoActivoProyecto(int idProyecto, int? idExcluir = null)
        {
            return await _context.GruposAlertas.AnyAsync(g =>
                g.id_proyecto == idProyecto &&
                g.activo &&
                (!idExcluir.HasValue || g.id_grupo_alerta != idExcluir.Value));
        }

        private async Task<bool> TieneRelaciones(int id)
        {
            return await _context.GruposAlertasDias
                .AnyAsync(d => d.id_grupo_alerta == id);
        }
    }
}