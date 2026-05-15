using Alertas.Data;
using Alertas.Helpers;
using Alertas.Models;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace Alertas.Controllers
{
    public class EstadosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EstadosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var estados = await _context.Estados
                .Include(e => e.Proyecto)
                .Where(e => e.Proyecto.configuracion_completa)
                .OrderBy(e => e.Proyecto.nombre)
                .ThenBy(e => e.orden)
                .ThenBy(e => e.nombre)
                .ToListAsync();

            return View(estados);
        }

        public async Task<IActionResult> Details(int id)
        {
            var estado = await _context.Estados
                .Include(e => e.Proyecto)
                .FirstOrDefaultAsync(e => e.id_estado == id);

            if (estado == null)
                return NotFound();

            return View(estado);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var estado = await _context.Estados
                .Include(e => e.Proyecto)
                .FirstOrDefaultAsync(e => e.id_estado == id);

            if (estado == null)
                return NotFound();

            if (!estado.Proyecto.configuracion_completa)
                return RedirectToAction(nameof(Index));

            var tieneObligaciones = await _context.RegObls
                .AnyAsync(o => o.id_proyecto == estado.id_proyecto);

            var vm = new EstadoEditNombreViewModel
            {
                id_estado = estado.id_estado,
                id_proyecto = estado.id_proyecto,
                nombre_proyecto = estado.Proyecto.nombre,
                nombre = estado.nombre,
                orden = estado.orden,
                bloquea = estado.bloquea,
                control_vencimiento = estado.control_vencimiento,
                control_seguimiento = estado.control_seguimiento,
                activo = estado.activo,
                puedeReabrirWizard = !tieneObligaciones
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EstadoEditNombreViewModel vm)
        {
            if (id != vm.id_estado)
                return BadRequest();

            var estado = await _context.Estados
                .Include(e => e.Proyecto)
                .FirstOrDefaultAsync(e => e.id_estado == id);

            if (estado == null)
                return NotFound();

            if (!estado.Proyecto.configuracion_completa)
                return RedirectToAction(nameof(Index));

            if (await ExisteEstadoProyecto(vm.nombre, estado.id_proyecto, estado.id_estado))
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe otro estado con este nombre para el proyecto.");
            }

            if (!ModelState.IsValid)
            {
                vm.id_proyecto = estado.id_proyecto;
                vm.nombre_proyecto = estado.Proyecto.nombre;
                vm.orden = estado.orden;
                vm.bloquea = estado.bloquea;
                vm.control_vencimiento = estado.control_vencimiento;
                vm.control_seguimiento = estado.control_seguimiento;
                vm.activo = estado.activo;
                vm.puedeReabrirWizard = !await _context.RegObls.AnyAsync(o => o.id_proyecto == estado.id_proyecto);

                return View(vm);
            }

            estado.nombre = vm.nombre.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Nombre del estado actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }


        private async Task CargarCombos(EstadoViewModel vm)
        {
            vm.Proyectos = await _context.Proyectos
                .Where(p => p.activo)
                .OrderBy(p => p.nombre)
                .Select(p => new SelectListItem
                {
                    Value = p.id_proyecto.ToString(),
                    Text = p.nombre
                })
                .ToListAsync();
        }

        private async Task<bool> ExisteEstadoProyecto(string nombre, int idProyecto, int? idExcluir = null)
        {
            var nombreNormalizado = nombre.Trim().ToLower();

            return await _context.Estados.AnyAsync(e =>
                e.nombre.ToLower() == nombreNormalizado &&
                e.id_proyecto == idProyecto &&
                (!idExcluir.HasValue || e.id_estado != idExcluir.Value));
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
    }
}