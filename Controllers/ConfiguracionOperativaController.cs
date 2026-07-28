using Alertas.Data;
using Alertas.Services.ConfiguracionOperativa;
using Alertas.Services.Jobs;
using Alertas.Services.Jobs.Options;
using Alertas.ViewModels.ConfiguracionOperativa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;


namespace Alertas.Controllers
{
    [Authorize]

    public class ConfiguracionOperativaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguracionOperativaService _configuracionService;
        private readonly AlertasJobOptions _jobOptions;
        private readonly IWebHostEnvironment _environment;
        private readonly AlertasBackgroundService _alertasBackgroundService;

        public ConfiguracionOperativaController(
            ApplicationDbContext context,
            IConfiguracionOperativaService configuracionService,
            IOptions<AlertasJobOptions> jobOptions,
            IWebHostEnvironment environment,
            AlertasBackgroundService alertasBackgroundService)
        {
            _context = context;
            _configuracionService = configuracionService;
            _jobOptions = jobOptions.Value;
            _environment = environment;
            _alertasBackgroundService = alertasBackgroundService;
        }
        public async Task<IActionResult> Index()
        {
            if (!User.HasClaim("EsSuperAdmin", "true"))
            {
                TempData["Error"] =
                    "No tienes permisos para acceder a la configuración operativa.";

                return RedirectToAction("Index", "Home");
            }

            var configuracion = await _context.ConfiguracionesOperativas
                .AsNoTracking()
                .Include(x => x.UsuarioActualizacion)
                .FirstOrDefaultAsync(x =>
                    x.clave ==
                    ClavesConfiguracionOperativa.AlertasAutomaticasHabilitadas);

            if (configuracion == null)
            {
                TempData["Error"] =
                    "No se encontró la configuración del envío automático de alertas.";

                return RedirectToAction("Index", "Monitoreo");
            }

            // Zona horaria del usuario
            var tzIana = Request.Cookies["tzIana"] ?? "America/Bogota";

            TimeZoneInfo userTimeZone;

            try
            {
                userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzIana);
            }
            catch
            {
                userTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            }

            DateTime? fechaActualizacionLocal = null;

            if (configuracion.fecha_actualizacion != default)
            {
                fechaActualizacionLocal =
                    TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(
                            configuracion.fecha_actualizacion,
                            DateTimeKind.Utc),
                        userTimeZone);
            }

            // Estado operativo almacenado en base de datos
            var habilitadoOperativamente =
                bool.TryParse(configuracion.valor, out var habilitado) &&
                habilitado;

            // Estado técnico leído desde appsettings / Railway
            var ambienteActual = _environment.EnvironmentName;

            var ambientePermitido = string.Equals(
                ambienteActual,
                _jobOptions.AmbientePermitido,
                StringComparison.OrdinalIgnoreCase);

            /*
             * Estado efectivo:
             * para poder enviar realmente deben cumplirse todos los candados.
             *
             * SimularEjecucion no se incluye aquí porque el job sí puede ejecutarse,
             * aunque no envíe correos reales.
             */
            var estadoEfectivo =
                _jobOptions.Habilitado &&
                _jobOptions.PermitirEnvioReal &&
                ambientePermitido &&
                habilitadoOperativamente;

            string estadoEfectivoMensaje;

            if (!_jobOptions.Habilitado)
            {
                estadoEfectivoMensaje =
                    "El job está deshabilitado por la configuración técnica del ambiente.";
            }
            else if (!_jobOptions.PermitirEnvioReal)
            {
                estadoEfectivoMensaje =
                    "El job está habilitado, pero el envío real no está permitido por configuración.";
            }
            else if (!ambientePermitido)
            {
                estadoEfectivoMensaje =
                    $"El ambiente actual '{ambienteActual}' no coincide con el ambiente permitido " +
                    $"'{_jobOptions.AmbientePermitido}'.";
            }
            else if (!habilitadoOperativamente)
            {
                estadoEfectivoMensaje =
                    "El job está técnicamente disponible, pero fue deshabilitado desde la aplicación.";
            }
            else if (_jobOptions.SimularEjecucion)
            {
                estadoEfectivoMensaje =
                    "El job está habilitado y puede ejecutarse, pero se encuentra en modo simulación. " +
                    "No enviará correos reales.";
            }
            else
            {
                estadoEfectivoMensaje =
                    "El envío automático está habilitado y listo para ejecutarse según la programación definida.";
            }

            var model = new ConfiguracionOperativaViewModel
            {
                // Configuración operativa
                AlertasAutomaticasHabilitadas = habilitadoOperativamente,
                FechaActualizacion = fechaActualizacionLocal,
                UsuarioActualizacion =
                    configuracion.UsuarioActualizacion?.nombre,
                Descripcion = configuracion.descripcion,

                // Configuración técnica
                AmbienteActual = ambienteActual,
                JobHabilitadoConfiguracion = _jobOptions.Habilitado,
                PermitirEnvioReal = _jobOptions.PermitirEnvioReal,
                SimularEjecucion = _jobOptions.SimularEjecucion,

                BloquearEnviosFueraDeProduccion =
                    _jobOptions.BloquearEnviosFueraDeProduccion,

                UsarHorarioDiario = _jobOptions.UsarHorarioDiario,
                HoraEjecucion = _jobOptions.HoraEjecucion,
                MinutoEjecucion = _jobOptions.MinutoEjecucion,
                ZonaHoraria = _jobOptions.ZonaHoraria,

                OmitirFestivos = _jobOptions.OmitirFestivos,
                IntervaloMinutos = _jobOptions.IntervaloMinutos,

                EmailDestinoPruebas = _jobOptions.EmailDestinoPruebas,

                AmbientePermitido = ambientePermitido,
                EstadoEfectivo = estadoEfectivo,
                EstadoEfectivoMensaje = estadoEfectivoMensaje
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(bool habilitar)
        {
            if (!User.HasClaim("EsSuperAdmin", "true"))
            {
                TempData["Error"] =
                    "No tienes permisos para modificar la configuración operativa.";

                return RedirectToAction("Index", "Home");
            }

            var idUsuario = ObtenerIdUsuarioActual();

            if (!idUsuario.HasValue)
            {
                TempData["Error"] =
                    "No fue posible identificar el usuario que realiza el cambio.";

                return RedirectToAction(nameof(Index));
            }

            await _configuracionService.CambiarEstadoAsync(
                ClavesConfiguracionOperativa.AlertasAutomaticasHabilitadas,
                habilitar,
                idUsuario.Value);

            TempData["Success"] = habilitar
                ? "El envío automático de alertas fue habilitado."
                : "El envío automático de alertas fue deshabilitado.";

            return RedirectToAction(nameof(Index));
        }

        private int? ObtenerIdUsuarioActual()
        {
            var valoresPosibles = new[]
            {
                HttpContext.Session.GetString("id_usuario"),
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue("id_usuario"),
                User.FindFirstValue("IdUsuario")
            };

            foreach (var valor in valoresPosibles)
            {
                if (int.TryParse(valor, out var idUsuario) && idUsuario > 0)
                {
                    return idUsuario;
                }
            }

            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EjecutarAhora(
            CancellationToken cancellationToken)
        {
            if (!User.HasClaim("EsSuperAdmin", "true"))
            {
                TempData["Error"] =
                    "No tienes permisos para ejecutar manualmente el job.";

                return RedirectToAction("Index", "Home");
            }

            if (!_jobOptions.Habilitado)
            {
                TempData["Error"] =
                    "El job está deshabilitado por la configuración técnica del ambiente.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _alertasBackgroundService.EjecutarAhoraAsync(
                    cancellationToken);

                TempData["Success"] =
                    "La ejecución del job finalizó. Consulta el resultado en Monitoreo de Jobs y en el Historial de Correos.";
            }
            catch (OperationCanceledException)
            {
                TempData["Error"] =
                    "La ejecución fue cancelada antes de finalizar.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    $"No fue posible ejecutar el job: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}