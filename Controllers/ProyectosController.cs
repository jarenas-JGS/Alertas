using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class ProyectosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProyectosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var proyectos = await _context.Proyectos
                .Include(p => p.Area)
                .Include(p => p.UsuarioCreacion)
                .Where(p => p.configuracion_completa)
                .OrderBy(p => p.Area.nombre)
                .ThenBy(p => p.nombre)
                .ToListAsync();

            return View(proyectos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.Area)
                .Include(p => p.UsuarioCreacion)
                .Include(p => p.Obligaciones)
                .Include(p => p.Estados)
                .Include(p => p.GruposAlertas)
                .Include(p => p.UsuariosProyectos)
                .Include(p => p.EstadosTransicion)
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            if (!proyecto.configuracion_completa)
                return RedirectToAction(nameof(Index));

            return View(proyecto);
        }

        public IActionResult Create()
        {
            return RedirectToAction("Create", "ProyectoWizard");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.Area)
                .Include(p => p.UsuarioCreacion)
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            if (!proyecto.configuracion_completa)
                return RedirectToAction(nameof(Index));

            var tieneObligaciones = await TieneObligaciones(id);

            var vm = new ProyectoEditViewModel
            {
                id_proyecto = proyecto.id_proyecto,
                nombre = proyecto.nombre,
                nombre_seguimiento = proyecto.nombre_seguimiento,
                activo = proyecto.activo,
                id_area = proyecto.id_area,
                nombre_area = proyecto.Area?.nombre,
                fecha_creacion = proyecto.fecha_creacion,
                usuario_creacion = proyecto.UsuarioCreacion?.nombre,
                configuracion_completa = proyecto.configuracion_completa,
                puedeReabrirWizard = !tieneObligaciones
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProyectoEditViewModel vm)
        {
            if (id != vm.id_proyecto)
                return BadRequest();

            var proyecto = await _context.Proyectos
                .Include(p => p.Area)
                .Include(p => p.UsuarioCreacion)
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            if (!proyecto.configuracion_completa)
                return RedirectToAction(nameof(Index));

            if (await ExisteNombre(vm.nombre, proyecto.id_proyecto))
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe otro proyecto con este nombre.");
            }

            if (!ModelState.IsValid)
            {
                vm.id_area = proyecto.id_area;
                vm.nombre_area = proyecto.Area?.nombre;
                vm.fecha_creacion = proyecto.fecha_creacion;
                vm.usuario_creacion = proyecto.UsuarioCreacion?.nombre;
                vm.configuracion_completa = proyecto.configuracion_completa;
                vm.puedeReabrirWizard = !await TieneObligaciones(proyecto.id_proyecto);

                return View(vm);
            }

            proyecto.nombre = vm.nombre.Trim();
            proyecto.nombre_seguimiento = vm.nombre_seguimiento.Trim();
            proyecto.activo = vm.activo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Proyecto actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.Area)
                .Include(p => p.UsuarioCreacion)
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            if (!proyecto.configuracion_completa)
                return RedirectToAction(nameof(Index));

            ViewBag.PuedeEliminar = !await TieneObligaciones(id);

            return View(proyecto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            if (await TieneObligaciones(id))
            {
                TempData["Error"] = "No se puede eliminar el proyecto porque tiene obligaciones asociadas.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Proyectos.Remove(proyecto);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Proyecto eliminado correctamente.";
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

            if (await TieneObligaciones(idProyecto))
            {
                TempData["Error"] = "No se puede reabrir la configuración porque el proyecto ya tiene obligaciones creadas.";
                return RedirectToAction(nameof(Edit), new { id = idProyecto });
            }

            proyecto.configuracion_completa = false;

            await _context.SaveChangesAsync();

            TempData["Success"] = "El proyecto fue pasado a configuración incompleta. Puedes ajustarlo desde el Wizard.";

            return RedirectToAction("Continuar", "ProyectoWizard", new { idProyecto });
        }

        private async Task<bool> TieneObligaciones(int idProyecto)
        {
            return await _context.RegObls
                .AnyAsync(o => o.id_proyecto == idProyecto);
        }

        private async Task<bool> ExisteNombre(string nombre, int idExcluir)
        {
            var nombreNormalizado = nombre.Trim().ToLower();

            return await _context.Proyectos.AnyAsync(p =>
                p.nombre.ToLower() == nombreNormalizado &&
                p.id_proyecto != idExcluir);
        }
    }
}