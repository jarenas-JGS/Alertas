using Alertas.Data;
using Alertas.Services;
using Alertas.Services.CargaMasiva;
using Alertas.ViewModels.CargaMasiva;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Alertas.Controllers
{
    [Authorize]
    public class ObligacionesCargaMasivaController : Controller
    {
        private readonly IPlantillaObligacionesService _plantillaService;
        private readonly SeguridadService _seguridadService;
        private readonly IExcelObligacionesReader _excelReader;
        private readonly ApplicationDbContext _context;
        private readonly IValidadorCargaObligacionesService _validadorCarga;
        private readonly IConfirmadorCargaObligacionesService _confirmadorCarga;
        private readonly IExcelErroresCargaService _excelErroresCarga;


        public ObligacionesCargaMasivaController(
            IPlantillaObligacionesService plantillaService,
            SeguridadService seguridadService,
            ApplicationDbContext context,
            IExcelObligacionesReader excelReader,
            IValidadorCargaObligacionesService validadorCarga,
            IConfirmadorCargaObligacionesService confirmadorCarga,
            IExcelErroresCargaService excelErroresCarga)


        {
            _plantillaService = plantillaService;
            _seguridadService = seguridadService;
            _context = context;
            _excelReader = excelReader;
            _validadorCarga = validadorCarga;
            _confirmadorCarga = confirmadorCarga;
            _excelErroresCarga = excelErroresCarga;
        }

        [HttpGet]
        public async Task<IActionResult> DescargarPlantilla(int idProyecto)
        {
            if (!await _seguridadService.UsuarioPuedeCrearObligacionesEnProyectoAsync(idProyecto))
                return RedirectToAction("AccessDenied", "Login");

            var idUsuarioActual = _seguridadService.ObtenerIdUsuario();

            if (idUsuarioActual == null)
                return RedirectToAction("Index", "Login");

            var archivo = await _plantillaService.GenerarPlantillaAsync(
                idProyecto,
                idUsuarioActual.Value);

            return File(
                archivo.Contenido,
                archivo.ContentType,
                archivo.NombreArchivo);
        }



        [HttpGet]
        public async Task<IActionResult> Upload(int idProyecto)
        {
            if (!await _seguridadService.UsuarioPuedeCrearObligacionesEnProyectoAsync(idProyecto))
                return RedirectToAction("AccessDenied", "Login");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p =>
                    p.id_proyecto == idProyecto &&
                    p.activo &&
                    p.configuracion_completa);

            if (proyecto == null)
                return RedirectToAction("AccessDenied", "Login");

            var model = new CargaObligacionesUploadViewModel
            {
                id_proyecto = proyecto.id_proyecto,
                nombre_proyecto = proyecto.nombre
            };


            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(CargaObligacionesUploadViewModel model)
        {
            if (!await _seguridadService.UsuarioPuedeCrearObligacionesEnProyectoAsync(model.id_proyecto))
                return RedirectToAction("AccessDenied", "Login");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p =>
                    p.id_proyecto == model.id_proyecto &&
                    p.activo &&
                    p.configuracion_completa);

            if (proyecto == null)
                return RedirectToAction("AccessDenied", "Login");

            model.nombre_proyecto = proyecto.nombre;

            if (!ModelState.IsValid)
                return View(model);

            if (model.archivo == null || model.archivo.Length == 0)
            {
                ModelState.AddModelError("archivo", "Debe seleccionar un archivo válido.");
                return View(model);
            }

            var extension = Path.GetExtension(model.archivo.FileName).ToLower();

            if (extension != ".xlsx")
            {
                ModelState.AddModelError("archivo", "Solo se permiten archivos Excel .xlsx.");
                return View(model);
            }

            var filas = await _excelReader.LeerArchivoAsync(model.archivo);

            var errores = await _validadorCarga.ValidarAsync(
                proyecto.id_proyecto,
                filas);

            var idCarga = Guid.NewGuid();

            var cargaTemporal = new CargaObligacionesTemporalViewModel
            {
                id_carga = idCarga,
                id_proyecto = proyecto.id_proyecto,
                nombre_proyecto = proyecto.nombre,
                filas = filas
            };

            HttpContext.Session.SetString(
                $"CargaObligaciones_{idCarga}",
                System.Text.Json.JsonSerializer.Serialize(cargaTemporal));

            var preview = new CargaObligacionesPreviewViewModel
            {
                id_carga = idCarga,
                id_proyecto = proyecto.id_proyecto,
                nombre_proyecto = proyecto.nombre,
                filas = filas,
                errores = errores
            };

            HttpContext.Session.SetString(
                $"PreviewErroresObligaciones_{idCarga}",
                System.Text.Json.JsonSerializer.Serialize(preview));

            return View("Preview", preview);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(Guid idCarga)
        {
            TempData["Debug"] = $"Entró a Confirmar. idCarga: {idCarga}";

            var key = $"CargaObligaciones_{idCarga}";
            var json = HttpContext.Session.GetString(key);

            if (string.IsNullOrWhiteSpace(json))
            {
                TempData["Error"] = $"No se encontró la carga temporal en sesión. Key: {key}";
                return RedirectToAction("Upload", new { idProyecto = _seguridadService.ObtenerIdProyectoActivo() });
            }

            var carga = System.Text.Json.JsonSerializer
                .Deserialize<CargaObligacionesTemporalViewModel>(json);

            if (carga == null)
            {
                TempData["Error"] = "No fue posible deserializar la carga temporal.";
                return RedirectToAction("Index", "RegObl");
            }

            var idUsuarioActual = _seguridadService.ObtenerIdUsuario();

            if (idUsuarioActual == null)
                return RedirectToAction("Index", "Login");

            try
            {
                var resultado = await _confirmadorCarga.ConfirmarAsync(
                    carga,
                    idUsuarioActual.Value);

                HttpContext.Session.Remove(key);

                TempData["Success"] = resultado.mensaje;

                return RedirectToAction("Index", "RegObl");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al confirmar cargue: {ex.Message}";
                return RedirectToAction("Upload", new { idProyecto = carga.id_proyecto });
            }
        }

        [HttpGet]
        public IActionResult DescargarErrores(Guid idCarga)
        {
            var json = HttpContext.Session.GetString($"PreviewErroresObligaciones_{idCarga}");

            if (string.IsNullOrWhiteSpace(json))
            {
                TempData["Error"] = "No se encontró el archivo de errores. Por favor vuelva a validar el Excel.";
                return RedirectToAction("Index", "RegObl");
            }

            var preview = System.Text.Json.JsonSerializer
                .Deserialize<CargaObligacionesPreviewViewModel>(json);

            if (preview == null || !preview.errores.Any())
            {
                TempData["Error"] = "No existen errores para descargar.";
                return RedirectToAction("Index", "RegObl");
            }

            var archivo = _excelErroresCarga.GenerarExcelErrores(preview);

            return File(
                archivo.Contenido,
                archivo.ContentType,
                archivo.NombreArchivo);
        }

        private static List<string> SepararUsuarios(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return new List<string>();

            return valor
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}