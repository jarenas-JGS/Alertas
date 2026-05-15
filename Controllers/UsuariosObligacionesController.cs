using Alertas.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Controllers
{
    public class UsuariosObligacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosObligacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var registros = await _context.UsuariosObligaciones
                .Include(x => x.Usuario)
                .Include(x => x.Rol)
                .Include(x => x.RegObl)
                    .ThenInclude(o => o.Proyecto)
                .Include(x => x.UsuarioAsignacion)
                .OrderBy(x => x.RegObl.Proyecto.nombre)
                .ThenBy(x => x.id_reg_obl)
                .ThenBy(x => x.Rol.id_rol)
                .ThenBy(x => x.Usuario.nombre)
                .ToListAsync();

            return View(registros);
        }

        public async Task<IActionResult> Details(int id)
        {
            var registro = await _context.UsuariosObligaciones
                .Include(x => x.Usuario)
                .Include(x => x.Rol)
                .Include(x => x.RegObl)
                    .ThenInclude(o => o.Proyecto)
                .Include(x => x.UsuarioAsignacion)
                .FirstOrDefaultAsync(x => x.id_usuario_obligacion == id);

            if (registro == null)
                return NotFound();

            return View(registro);
        }

        public IActionResult Create()
        {
            TempData["Error"] = "La asignación de usuarios a obligaciones debe realizarse desde el manejo de la obligación.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            TempData["Error"] = "La edición de usuarios asociados a obligaciones debe realizarse desde el manejo de la obligación.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public IActionResult Delete(int id)
        {
            TempData["Error"] = "La eliminación de usuarios asociados a obligaciones debe realizarse desde el manejo de la obligación.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}