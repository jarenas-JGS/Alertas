using Alertas.Data;
using Alertas.Models;
using Alertas.Services;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeZoneConverter;
using Alertas.Services.Storage.Interfaces;

namespace Alertas.Controllers
{
    [Authorize]
    public class RegOblController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SeguridadService _seguridadService;
        private const long MaxArchivoBytes = 2 * 1024 * 1024; // 2 MB
        private readonly IFileStorageService _fileStorageService;

        public RegOblController(
            ApplicationDbContext context,
            SeguridadService seguridadService,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _seguridadService = seguridadService;
            _fileStorageService = fileStorageService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var idProyectoNullable = _seguridadService.ObtenerIdProyectoActivo();
            if (idProyectoNullable == null)
                return RedirectToAction("SeleccionarProyecto", "Login");

            int idProyecto = idProyectoNullable.Value;

            var proyectoActivo = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == idProyecto && p.activo);

            if (proyectoActivo == null)
                return RedirectToAction("SeleccionarProyecto", "Login");

            if (!proyectoActivo.configuracion_completa)
            {
                TempData["Error"] = "Este proyecto aún no está completamente configurado.";
                return RedirectToAction("SeleccionarProyecto", "Login");
            }

            int? idUsuario = _seguridadService.ObtenerIdUsuario();

            if (idUsuario == null)
                return RedirectToAction("Index", "Login");

            bool esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");
            bool accesoProyecto = _seguridadService.EsAccesoProyectoActivoPorProyecto();

            IQueryable<RegObl> query = _context.RegObls
                .Include(x => x.Cliente)
                .Include(x => x.Empresa)
                .Include(x => x.TipoObligacion)
                .Include(x => x.Estado)
                .Where(x => x.id_proyecto == idProyecto);

            if (!esSuperAdmin && !accesoProyecto)
            {
                query = query.Where(x => x.UsuariosObligaciones.Any(uo =>
                    uo.activo && uo.id_usuario == idUsuario.Value));
            }

            ViewBag.PuedeCrearObligaciones =
                await _seguridadService.UsuarioPuedeCrearObligacionesEnProyectoAsync(idProyecto);

            ViewBag.IdProyectoActivo = idProyecto;

            var data = await query
                .OrderByDescending(x => x.id_reg_obl)
                .ToListAsync();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var idProyectoNullable = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyectoNullable == null)
                return RedirectToAction("SeleccionarProyecto", "Login");

            int idProyecto = idProyectoNullable.Value;

            if (!await _seguridadService.UsuarioPuedeCrearObligacionesEnProyectoAsync(idProyecto))
            {
                return RedirectToAction("AccessDenied", "Login");
            }

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var model = new RegOblViewModel
            {
                fecha_creac = hoy,
                fecha_venc_seguimiento = hoy,
                fecha_venc_obl = hoy,
                anio = DateTime.Today.Year,
                mes = DateTime.Today.Month,
                dia = DateTime.Today.Day,
                vigencia = DateTime.Today.Year,
                id_proyecto = idProyecto
            };

            await CargarCombosAsync(idProyecto, model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegOblViewModel model)
        {
            var idProyectoNullable = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyectoNullable == null)
                return RedirectToAction("SeleccionarProyecto", "Login");

            int idProyecto = idProyectoNullable.Value;

            if (!await _seguridadService.UsuarioPuedeCrearObligacionesEnProyectoAsync(idProyecto))
                return RedirectToAction("AccessDenied", "Login");

            var idUsuarioActual = _seguridadService.ObtenerIdUsuario();
            model.id_proyecto = idProyecto;

            // =====================================================
            // VALIDACIONES MANUALES ADICIONALES
            // =====================================================

            if (model.fecha_venc_seguimiento > model.fecha_venc_obl)
            {
                ModelState.AddModelError("fecha_venc_seguimiento",
                    "La fecha de vencimiento de seguimiento no puede ser mayor a la fecha de vencimiento de la obligación.");
            }

            if (model.ids_responsables == null || !model.ids_responsables.Any())
            {
                ModelState.AddModelError("ids_responsables", "Debe seleccionar al menos un Responsable.");
            }

            if (model.ids_elaboradores == null || !model.ids_elaboradores.Any())
            {
                ModelState.AddModelError("ids_elaboradores", "Debe seleccionar al menos un Elaborador.");
            }

            if (model.ids_autorizadores == null || !model.ids_autorizadores.Any())
            {
                ModelState.AddModelError("ids_autorizadores", "Debe seleccionar al menos un Autorizador.");
            }

            if (model.ids_aprobadores == null || !model.ids_aprobadores.Any())
            {
                ModelState.AddModelError("ids_aprobadores", "Debe seleccionar al menos un Aprobador.");
            }

            if (model.ids_vencimiento == null || !model.ids_vencimiento.Any())
            {
                ModelState.AddModelError("ids_vencimiento", "Debe seleccionar al menos un Usuario de Vencimiento.");
            }

            int? diferencia = null;
            decimal? variacion = null;

            if (model.vlr_aprox.HasValue && model.vlr_real.HasValue)
            {
                int vlrAprox = model.vlr_aprox.Value;
                int vlrReal = model.vlr_real.Value;

                diferencia = vlrReal - vlrAprox;

                if (vlrAprox > 0)
                {
                    variacion = Math.Round(
                        ((decimal)(vlrReal - vlrAprox) / vlrAprox) * 100m, 2);
                }
                else
                {
                    variacion = null;
                }

                if (diferencia != 0 && model.id_justif_var == null)
                {
                    ModelState.AddModelError("id_justif_var",
                        "Debe seleccionar una justificación cuando exista diferencia.");
                }
            }

            // Resolver estado inicial automáticamente
            var estadoInicial = await _context.Estados
                .Where(x => x.id_proyecto == idProyecto && x.activo)
                .OrderBy(x => x.orden)
                .ThenBy(x => x.nombre)
                .FirstOrDefaultAsync();

            if (estadoInicial == null)
            {
                ModelState.AddModelError(string.Empty,
                    "El proyecto no tiene estados activos configurados.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(idProyecto, model);
                return View(model);
            }

            // Cálculo automático de fecha derivada
            int anio = model.fecha_venc_obl.Year;
            int mes = model.fecha_venc_obl.Month;
            int dia = model.fecha_venc_obl.Day;

            var entidad = new RegObl
            {
                id_proyecto = idProyecto,
                id_cliente = model.id_cliente!.Value,
                id_empresa = model.id_empresa!.Value,
                id_tipo_obligacion = model.id_tipo_obligacion!.Value,
                id_ciudad = model.id_ciudad,
                id_dominio = model.id_dominio!.Value,
                id_periodo = model.id_periodo!.Value,
                cod_obligacion = model.cod_obligacion,
                fecha_venc_seguimiento = model.fecha_venc_seguimiento,
                fecha_venc_obl = model.fecha_venc_obl,
                vigencia = model.vigencia,
                anio = anio,
                mes = mes,
                dia = dia,
                vlr_aprox = model.vlr_aprox,
                vlr_real = model.vlr_real,
                diferencia = diferencia,
                variacion = variacion,
                saldo_favor = model.saldo_favor,
                id_justif_var = model.id_justif_var,
                id_estado = estadoInicial!.id_estado,
                fecha_creac = model.fecha_creac,
                fecha_seguimiento_ejecutada = null,
                fecha_vencimiento_ejecutada = null,
                fecha_aprobado_final = null,
                dias_atraso_seguimiento = null,
                dias_atraso_vencimiento = null,
                fecha_ult_modif = DateTime.UtcNow,
                id_usuario_ult_modif = idUsuarioActual,
                aprobado = null,
                observaciones = model.observaciones,
                cc_empleador = model.cc_empleador?.Trim(),
                cc_empleado = model.cc_empleado?.Trim(),
                nombre = model.nombre,
                nombre_empleador = model.nombre_empleador,
                nombre_empleado = model.nombre_empleado,
                id_autorizado_por = null,
                id_aprobado_por = null
            };

            _context.RegObls.Add(entidad);
            await _context.SaveChangesAsync();

            await AgregarUsuariosObligacionAsync(entidad.id_reg_obl, model.ids_responsables ?? new List<int>(), "Responsable", idUsuarioActual);
            await AgregarUsuariosObligacionAsync(entidad.id_reg_obl, model.ids_elaboradores ?? new List<int>(), "Elaborador", idUsuarioActual);
            await AgregarUsuariosObligacionAsync(entidad.id_reg_obl, model.ids_autorizadores ?? new List<int>(), "Autorizador", idUsuarioActual);
            await AgregarUsuariosObligacionAsync(entidad.id_reg_obl, model.ids_aprobadores ?? new List<int>(), "Aprobador", idUsuarioActual);
            await AgregarUsuariosObligacionAsync(entidad.id_reg_obl, model.ids_vencimiento ?? new List<int>(), "Vencimiento", idUsuarioActual);

            // Aquí luego conviene registrar historial inicial de flujo
            // Ejemplo futuro:
            // await _auditoriaService.RegistrarCreacionObligacionAsync(...);

            if (!await _seguridadService.UsuarioPuedeCrearObligacionesEnProyectoAsync(model.id_proyecto.Value))
            {
                return RedirectToAction("AccessDenied", "Login");
            }

            await _context.SaveChangesAsync();

            TempData["Ok"] = "Registro creado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerEmpresasPorCliente(int idCliente)
        {
            var idProyecto = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
                return Json(new List<object>());

            var proyecto = await _context.Proyectos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.id_proyecto == idProyecto.Value);

            if (proyecto == null)
                return Json(new List<object>());

            var empresas = await _context.AreasEmpresas
                .Where(ae => ae.id_area == proyecto.id_area && ae.activo)
                .Join(_context.Empresas,
                    ae => ae.id_empresa,
                    e => e.id_empresa,
                    (ae, e) => e)
                .Where(e => e.id_cliente == idCliente)
                .OrderBy(e => e.nombre)
                .Select(e => new
                {
                    value = e.id_empresa,
                    text = e.nombre
                })
                .Distinct()
                .ToListAsync();

            return Json(empresas);
        }

        private async Task<int?> ObtenerIdRolAsync(string nombreRol)
        {
            return await _context.Roles
                .Where(r => r.nombre == nombreRol)
                .Select(r => (int?)r.id_rol)
                .FirstOrDefaultAsync();
        }

        private async Task CargarCombosAsync(int idProyecto, RegOblViewModel? model = null)
        {
            var proyecto = await _context.Proyectos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.id_proyecto == idProyecto);

            if (proyecto == null)
                return;

            int idArea = proyecto.id_area;

            ViewBag.Clientes = new SelectList(
                await _context.AreasEmpresas
                    .Where(ae => ae.id_area == idArea && ae.activo)
                    .Join(_context.Empresas,
                        ae => ae.id_empresa,
                        e => e.id_empresa,
                        (ae, e) => e)
                    .Join(_context.Clientes,
                        e => e.id_cliente,
                        c => c.id_cliente,
                        (e, c) => c)
                    .Distinct()
                    .OrderBy(c => c.nombre)
                    .ToListAsync(),
                "id_cliente",
                "nombre",
                model?.id_cliente
            );

            if (model?.id_cliente != null && model.id_cliente > 0)
            {
                ViewBag.Empresas = new SelectList(
                    await _context.AreasEmpresas
                        .Where(ae => ae.id_area == idArea && ae.activo)
                        .Join(_context.Empresas,
                            ae => ae.id_empresa,
                            e => e.id_empresa,
                            (ae, e) => e)
                        .Where(e => e.id_cliente == model.id_cliente)
                        .OrderBy(e => e.nombre)
                        .ToListAsync(),
                    "id_empresa",
                    "nombre",
                    model?.id_empresa
                );
            }
            else
            {
                ViewBag.Empresas = new SelectList(
                    Enumerable.Empty<SelectListItem>(),
                    "Value",
                    "Text"
                );
            }

            ViewBag.TiposObligaciones = new SelectList(
                await _context.TipoObligaciones
                    .Where(x => x.id_area == idArea && (x.activo == null || x.activo == true))
                    .OrderBy(x => x.nombre)
                    .ToListAsync(),
                "id_tipo_obligacion",
                "nombre",
                model?.id_tipo_obligacion
            );

            ViewBag.Ciudades = new SelectList(
                await _context.Ciudades
                    .OrderBy(x => x.nombre)
                    .ToListAsync(),
                "id_ciudad",
                "nombre",
                model?.id_ciudad
            );

            ViewBag.Dominios = new SelectList(
                await _context.Dominios
                    .OrderBy(x => x.nombre)
                    .ToListAsync(),
                "id_dominio",
                "nombre",
                model?.id_dominio
            );

            ViewBag.Periodos = new SelectList(
                await _context.Periodos
                    .OrderBy(x => x.nombre)
                    .ToListAsync(),
                "id_periodo",
                "nombre",
                model?.id_periodo
            );

            ViewBag.Estados = new SelectList(
                await _context.Estados
                    .Where(x => x.id_proyecto == idProyecto && x.activo)
                    .OrderBy(x => x.orden)
                    .ThenBy(x => x.nombre)
                    .ToListAsync(),
                "id_estado",
                "nombre",
                model?.id_estado
            );

            ViewBag.Justificaciones = new SelectList(
                await _context.JustifVars
                    .Where(j => j.id_area == idArea)
                    .OrderBy(j => j.nombre)
                    .ToListAsync(),
                "id_justif_var",
                "nombre",
                model?.id_justif_var
            );

            var usuariosArea = await _context.UsuarioArea
                .Where(ua => ua.id_area == idArea && ua.activo)
                .Join(_context.Usuarios,
                    ua => ua.id_usuario,
                    u => u.id_usuario,
                    (ua, u) => u)
                .Where(u => u.activo)
                .OrderBy(u => u.nombre)
                .ToListAsync();

            ViewBag.Responsables = new MultiSelectList(usuariosArea, "id_usuario", "nombre", model?.ids_responsables);
            ViewBag.Elaboradores = new MultiSelectList(usuariosArea, "id_usuario", "nombre", model?.ids_elaboradores);
            ViewBag.Autorizadores = new MultiSelectList(usuariosArea, "id_usuario", "nombre", model?.ids_autorizadores);
            ViewBag.Aprobadores = new MultiSelectList(usuariosArea, "id_usuario", "nombre", model?.ids_aprobadores);
            ViewBag.UsuariosVencimiento = new MultiSelectList(usuariosArea, "id_usuario", "nombre", model?.ids_vencimiento);
        }

        private async Task AgregarUsuariosObligacionAsync(
            int idRegObl,
            IEnumerable<int> idsUsuarios,
            string nombreRol,
            int? idUsuarioAsignacion)
        {
            var idRol = await _context.Roles
                .Where(r => r.nombre == nombreRol)
                .Select(r => (int?)r.id_rol)
                .FirstOrDefaultAsync();

            if (!idRol.HasValue)
                return;

            foreach (var idUsuario in idsUsuarios.Distinct())
            {
                _context.UsuariosObligaciones.Add(new UsuarioObligacion
                {
                    id_usuario = idUsuario,
                    id_reg_obl = idRegObl,
                    id_rol = idRol.Value,
                    activo = true,
                    fecha_asignacion = DateTime.UtcNow,
                    id_usuario_asignacion = idUsuarioAsignacion
                });
            }
        }
        private async Task CargarCombosSeguimientoAsync(int idProyecto, RegOblSeguimientoViewModel model)
        {
            var proyecto = await _context.Proyectos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.id_proyecto == idProyecto);

            if (proyecto == null)
                return;

            int idArea = proyecto.id_area;

            var usuariosArea = await _context.UsuarioArea
                .Where(ua => ua.id_area == idArea && ua.activo)
                .Join(_context.Usuarios,
                    ua => ua.id_usuario,
                    u => u.id_usuario,
                    (ua, u) => u)
                .Where(u => u.activo)
                .OrderBy(u => u.nombre)
                .ToListAsync();

            model.responsablesDisponibles = usuariosArea
                .Select(u => new SelectListItem
                {
                    Value = u.id_usuario.ToString(),
                    Text = u.nombre,
                    Selected = model.ids_responsables.Contains(u.id_usuario)
                })
                .ToList();

            model.elaboradoresDisponibles = usuariosArea
                .Select(u => new SelectListItem
                {
                    Value = u.id_usuario.ToString(),
                    Text = u.nombre,
                    Selected = model.ids_elaboradores.Contains(u.id_usuario)
                })
                .ToList();

            model.autorizadoresDisponibles = usuariosArea
                .Select(u => new SelectListItem
                {
                    Value = u.id_usuario.ToString(),
                    Text = u.nombre,
                    Selected = model.ids_autorizadores.Contains(u.id_usuario)
                })
                .ToList();

            model.aprobadoresDisponibles = usuariosArea
                .Select(u => new SelectListItem
                {
                    Value = u.id_usuario.ToString(),
                    Text = u.nombre,
                    Selected = model.ids_aprobadores.Contains(u.id_usuario)
                })
                .ToList();

            model.vencimientoDisponibles = usuariosArea
                .Select(u => new SelectListItem
                {
                    Value = u.id_usuario.ToString(),
                    Text = u.nombre,
                    Selected = model.ids_vencimiento.Contains(u.id_usuario)
                })
                .ToList();

            model.justificacionesDisponibles = await _context.JustifVars
                .Where(j => j.id_area == idArea)
                .OrderBy(j => j.nombre)
                .Select(j => new SelectListItem
                {
                    Value = j.id_justif_var.ToString(),
                    Text = j.nombre,
                    Selected = model.id_justif_var == j.id_justif_var
                })
                .ToListAsync();
        }

        private async Task<(bool Ok, IActionResult? ErrorResult)> AplicarCambiosSeguimientoAsync(
            RegOblSeguimientoViewModel model,
            RegObl entidad,
            int idProyecto,
            int idUsuario)
        {
            var form = Request.HasFormContentType ? Request.Form : null;

            bool traeVlrAprox = form?.ContainsKey(nameof(model.vlr_aprox)) == true;
            bool traeVlrReal = form?.ContainsKey(nameof(model.vlr_real)) == true;
            bool traeSaldoFavor = form?.ContainsKey(nameof(model.saldo_favor)) == true;
            bool traeJustif = form?.ContainsKey(nameof(model.id_justif_var)) == true;
            bool traeObservaciones = form?.ContainsKey(nameof(model.observaciones)) == true;

            bool traeIdsResponsables = form?.ContainsKey(nameof(model.ids_responsables)) == true;
            bool traeIdsElaboradores = form?.ContainsKey(nameof(model.ids_elaboradores)) == true;
            bool traeIdsAutorizadores = form?.ContainsKey(nameof(model.ids_autorizadores)) == true;
            bool traeIdsAprobadores = form?.ContainsKey(nameof(model.ids_aprobadores)) == true;
            bool traeIdsVencimiento = form?.ContainsKey(nameof(model.ids_vencimiento)) == true;

            bool esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");
            bool accesoPorProyecto = _seguridadService.EsAccesoProyectoActivoPorProyecto();
            bool esAdminProyecto = _seguridadService.EsAdministradorProyectoActivo();

            var fase = await ObtenerFaseEstadoAsync(idProyecto, entidad.Estado);

            bool estadoFinal = fase.EsFinal;
            bool esPresentada = fase.EsPresentada;
            bool esAntesDeSeguimiento = fase.EsAntesDeSeguimiento;
            bool esAntesDePresentada = fase.EsAntesDePresentada;

            // Participantes actuales
            var responsables = entidad.UsuariosObligaciones
                .Where(x => x.activo && x.Rol != null && x.Rol.nombre == "Responsable")
                .Select(x => x.id_usuario)
                .Distinct()
                .ToList();

            var elaboradores = entidad.UsuariosObligaciones
                .Where(x => x.activo && x.Rol != null && x.Rol.nombre == "Elaborador")
                .Select(x => x.id_usuario)
                .Distinct()
                .ToList();

            var autorizadores = entidad.UsuariosObligaciones
                .Where(x => x.activo && x.Rol != null && x.Rol.nombre == "Autorizador")
                .Select(x => x.id_usuario)
                .Distinct()
                .ToList();

            var aprobadores = entidad.UsuariosObligaciones
                .Where(x => x.activo && x.Rol != null && x.Rol.nombre == "Aprobador")
                .Select(x => x.id_usuario)
                .Distinct()
                .ToList();

            bool esElaborador = elaboradores.Contains(idUsuario);
            bool esAutorizador = autorizadores.Contains(idUsuario);

            bool puedeEditarDatosGenerales =
                !estadoFinal &&
                !esPresentada &&
                (esSuperAdmin || esAdminProyecto);

            bool puedeEditarParticipantes =
                !estadoFinal &&
                !esPresentada &&
                (esSuperAdmin || esAdminProyecto);

            bool puedeEditarFechasBase =
                !estadoFinal &&
                !esPresentada &&
                esAntesDeSeguimiento &&
                (esSuperAdmin || esAdminProyecto);

            bool puedeEditarValores =
                !estadoFinal &&
                !esPresentada &&
                esAntesDePresentada &&
                (esSuperAdmin || esAdminProyecto || esElaborador || esAutorizador);

            bool puedeEditarJustificacion = puedeEditarValores;

            bool puedeEditarObservaciones =
                !estadoFinal &&
                !esPresentada &&
                (esSuperAdmin || esAdminProyecto || esElaborador || esAutorizador);

            // ============================
            // Validaciones
            // ============================

            if (puedeEditarFechasBase && model.fecha_venc_seguimiento > model.fecha_venc_obl)
            {
                TempData["Error"] = "La fecha de vencimiento de seguimiento no puede ser mayor a la fecha de vencimiento de la obligación.";
                return (false, RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl }));
            }

            int? nuevaDiferencia = entidad.diferencia;
            decimal? nuevaVariacion = entidad.variacion;

            int? vlrAproxCalc = traeVlrAprox ? model.vlr_aprox : entidad.vlr_aprox;
            int? vlrRealCalc = traeVlrReal ? model.vlr_real : entidad.vlr_real;

            if (puedeEditarValores)
            {
                if (vlrRealCalc.HasValue)
                {
                    nuevaDiferencia = vlrRealCalc.Value - (vlrAproxCalc ?? 0);

                    if ((vlrAproxCalc ?? 0) > 0)
                    {
                        nuevaVariacion = Math.Round(
                            ((decimal)(vlrRealCalc.Value - (vlrAproxCalc ?? 0)) / (vlrAproxCalc ?? 1)) * 100m, 2);
                    }
                    else
                    {
                        nuevaVariacion = null;
                    }

                    int? justifActual = traeJustif ? model.id_justif_var : entidad.id_justif_var;

                    int? justificacionEfectiva = model.id_justif_var ?? entidad.id_justif_var;

                    if (nuevaDiferencia.HasValue && nuevaDiferencia.Value != 0 && justificacionEfectiva == null)
                    {
                        TempData["Error"] = "Debe seleccionar una justificación cuando exista diferencia.";
                        return (false, RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl }));
                    }
                }
                else
                {
                    nuevaDiferencia = null;
                    nuevaVariacion = null;
                }
            }

            bool traeParticipantes =
                traeIdsResponsables ||
                traeIdsElaboradores ||
                traeIdsAutorizadores ||
                traeIdsAprobadores ||
                traeIdsVencimiento;

            if (puedeEditarParticipantes && traeParticipantes)
            {
                if (model.ids_responsables == null || !model.ids_responsables.Any())
                {
                    TempData["Error"] = "Debe seleccionar al menos un Responsable.";
                    return (false, RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl }));
                }

                if (model.ids_elaboradores == null || !model.ids_elaboradores.Any())
                {
                    TempData["Error"] = "Debe seleccionar al menos un Elaborador.";
                    return (false, RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl }));
                }

                if (model.ids_autorizadores == null || !model.ids_autorizadores.Any())
                {
                    TempData["Error"] = "Debe seleccionar al menos un Autorizador.";
                    return (false, RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl }));
                }

                if (model.ids_aprobadores == null || !model.ids_aprobadores.Any())
                {
                    TempData["Error"] = "Debe seleccionar al menos un Aprobador.";
                    return (false, RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl }));
                }

                if (model.ids_vencimiento == null || !model.ids_vencimiento.Any())
                {
                    TempData["Error"] = "Debe seleccionar al menos un Usuario de Vencimiento.";
                    return (false, RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl }));
                }
            }

            // ============================
            // Auditoría
            // ============================

            var fechaHoraUtc = ObtenerFechaHoraUtc();
            var auditorias = new List<HistOblCampo>();

            void RegistrarCambio(string campo, string? anterior, string? nuevo)
            {
                if ((anterior ?? string.Empty) != (nuevo ?? string.Empty))
                {
                    auditorias.Add(new HistOblCampo
                    {
                        id_reg_obl = entidad.id_reg_obl,
                        campo = campo,
                        valor_anterior = anterior,
                        valor_nuevo = nuevo,
                        id_usuario = idUsuario,
                        fecha = fechaHoraUtc,
                        id_estado_en_momento = entidad.id_estado,
                        tipo_cambio = "EDICION"
                    });
                }
            }

            // ============================
            // Aplicar cambios
            // ============================

            if (puedeEditarDatosGenerales)
            {
                RegistrarCambio("nombre", entidad.nombre, model.nombre);
                entidad.nombre = model.nombre;

                RegistrarCambio("cod_obligacion", entidad.cod_obligacion, model.cod_obligacion);
                entidad.cod_obligacion = model.cod_obligacion;

                RegistrarCambio("vigencia", entidad.vigencia.ToString(), model.vigencia.ToString());
                entidad.vigencia = model.vigencia;

                RegistrarCambio("cc_empleador", entidad.cc_empleador, model.cc_empleador?.Trim());
                entidad.cc_empleador = model.cc_empleador?.Trim();

                RegistrarCambio("nombre_empleador", entidad.nombre_empleador, model.nombre_empleador);
                entidad.nombre_empleador = model.nombre_empleador;

                RegistrarCambio("cc_empleado", entidad.cc_empleado, model.cc_empleado?.Trim());
                entidad.cc_empleado = model.cc_empleado?.Trim();

                RegistrarCambio("nombre_empleado", entidad.nombre_empleado, model.nombre_empleado);
                entidad.nombre_empleado = model.nombre_empleado;
            }

            if (puedeEditarFechasBase)
            {
                RegistrarCambio("fecha_venc_obl", entidad.fecha_venc_obl.ToString("yyyy-MM-dd"), model.fecha_venc_obl.ToString("yyyy-MM-dd"));
                entidad.fecha_venc_obl = model.fecha_venc_obl;

                RegistrarCambio("fecha_venc_seguimiento", entidad.fecha_venc_seguimiento.ToString("yyyy-MM-dd"), model.fecha_venc_seguimiento.ToString("yyyy-MM-dd"));
                entidad.fecha_venc_seguimiento = model.fecha_venc_seguimiento;

                entidad.anio = model.fecha_venc_obl.Year;
                entidad.mes = model.fecha_venc_obl.Month;
                entidad.dia = model.fecha_venc_obl.Day;
            }

            if (puedeEditarValores)
            {
                if (traeVlrAprox)
                {
                    RegistrarCambio("vlr_aprox", entidad.vlr_aprox?.ToString(), model.vlr_aprox?.ToString());
                    entidad.vlr_aprox = model.vlr_aprox;
                }

                if (traeVlrReal)
                {
                    RegistrarCambio("vlr_real", entidad.vlr_real?.ToString(), model.vlr_real?.ToString());
                    entidad.vlr_real = model.vlr_real;
                }

                if (entidad.diferencia != nuevaDiferencia)
                {
                    RegistrarCambio(
                        "diferencia",
                        entidad.diferencia?.ToString(),
                        nuevaDiferencia?.ToString());
                }

                entidad.diferencia = nuevaDiferencia;

                decimal? variacionAnteriorNorm = entidad.variacion.HasValue
                    ? Math.Round(entidad.variacion.Value, 4)
                    : null;

                decimal? variacionNuevaNorm = nuevaVariacion.HasValue
                    ? Math.Round(nuevaVariacion.Value, 4)
                    : null;

                if (variacionAnteriorNorm != variacionNuevaNorm)
                {
                    RegistrarCambio(
                        "variacion",
                        variacionAnteriorNorm?.ToString("0.####"),
                        variacionNuevaNorm?.ToString("0.####"));
                }

                entidad.variacion = nuevaVariacion;

                if (traeSaldoFavor)
                {
                    RegistrarCambio("saldo_favor", entidad.saldo_favor?.ToString(), model.saldo_favor?.ToString());
                    entidad.saldo_favor = model.saldo_favor;
                }
            }

            if (puedeEditarJustificacion)
            {
                int? justificacionNueva;

                if (nuevaDiferencia.HasValue && nuevaDiferencia.Value != 0)
                {
                    justificacionNueva = model.id_justif_var ?? entidad.id_justif_var;
                }
                else
                {
                    justificacionNueva = null;
                }

                if (entidad.id_justif_var != justificacionNueva)
                {
                    RegistrarCambio(
                        "id_justif_var",
                        entidad.id_justif_var?.ToString(),
                        justificacionNueva?.ToString());
                }

                entidad.id_justif_var = justificacionNueva;
            }

            if (puedeEditarObservaciones && traeObservaciones)
            {
                RegistrarCambio("observaciones", entidad.observaciones, model.observaciones);
                entidad.observaciones = model.observaciones;
            }

            if (puedeEditarParticipantes && traeParticipantes)
            {
                await ActualizarUsuariosObligacionAsync(entidad.id_reg_obl, model.ids_responsables ?? new List<int>(), "Responsable", idUsuario);
                await ActualizarUsuariosObligacionAsync(entidad.id_reg_obl, model.ids_elaboradores ?? new List<int>(), "Elaborador", idUsuario);
                await ActualizarUsuariosObligacionAsync(entidad.id_reg_obl, model.ids_autorizadores ?? new List<int>(), "Autorizador", idUsuario);
                await ActualizarUsuariosObligacionAsync(entidad.id_reg_obl, model.ids_aprobadores ?? new List<int>(), "Aprobador", idUsuario);
                await ActualizarUsuariosObligacionAsync(entidad.id_reg_obl, model.ids_vencimiento ?? new List<int>(), "Vencimiento", idUsuario);
            }

            entidad.fecha_ult_modif = fechaHoraUtc;
            entidad.id_usuario_ult_modif = idUsuario;

            if (auditorias.Any())
            {
                _context.HistOblCampos.AddRange(auditorias);
            }

            return (true, null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarSeguimiento(RegOblSeguimientoViewModel model)
        {
            var idProyectoNullable = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyectoNullable == null)
                return RedirectToAction("SeleccionarProyecto", "Login");

            int idProyecto = idProyectoNullable.Value;
            int? idUsuario = _seguridadService.ObtenerIdUsuario();

            if (idUsuario == null)
                return RedirectToAction("Index", "Login");

            var entidad = await _context.RegObls
                .Include(x => x.Estado)
                .Include(x => x.Proyecto)
                .Include(x => x.UsuariosObligaciones)
                    .ThenInclude(uo => uo.Rol)
                .FirstOrDefaultAsync(x => x.id_reg_obl == model.id_reg_obl && x.id_proyecto == idProyecto);

            if (entidad == null)
                return RedirectToAction(nameof(Index));

            var resultado = await AplicarCambiosSeguimientoAsync(model, entidad, idProyecto, idUsuario.Value);

            if (!resultado.Ok)
                return resultado.ErrorResult!;

            await _context.SaveChangesAsync();

            TempData["Ok"] = "Cambios guardados correctamente.";

            return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
        }

        private async Task ActualizarUsuariosObligacionAsync(
            int idRegObl,
            IEnumerable<int> idsUsuariosNuevos,
            string nombreRol,
            int idUsuarioActual)
        {
            var idRol = await _context.Roles
                .Where(r => r.nombre == nombreRol)
                .Select(r => (int?)r.id_rol)
                .FirstOrDefaultAsync();

            if (!idRol.HasValue)
                return;

            var registrosActuales = await _context.UsuariosObligaciones
                .Where(x => x.id_reg_obl == idRegObl && x.id_rol == idRol.Value)
                .ToListAsync();

            var idsActualesActivos = registrosActuales
                .Where(x => x.activo)
                .Select(x => x.id_usuario)
                .ToHashSet();

            var idsNuevos = idsUsuariosNuevos.Distinct().ToHashSet();

            // Desactivar los que ya no deben estar
            foreach (var registro in registrosActuales.Where(x => x.activo && !idsNuevos.Contains(x.id_usuario)))
            {
                registro.activo = false;
            }

            // Reactivar si existía pero estaba inactivo
            foreach (var registro in registrosActuales.Where(x => !x.activo && idsNuevos.Contains(x.id_usuario)))
            {
                registro.activo = true;
                registro.fecha_asignacion = DateTime.UtcNow;
                registro.id_usuario_asignacion = idUsuarioActual;
            }

            // Agregar los nuevos que no existen
            var idsExistentes = registrosActuales.Select(x => x.id_usuario).ToHashSet();

            foreach (var idUsuario in idsNuevos.Where(x => !idsExistentes.Contains(x)))
            {
                _context.UsuariosObligaciones.Add(new UsuarioObligacion
                {
                    id_usuario = idUsuario,
                    id_reg_obl = idRegObl,
                    id_rol = idRol.Value,
                    activo = true,
                    fecha_asignacion = DateTime.UtcNow,
                    id_usuario_asignacion = idUsuarioActual
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Seguimiento(int id)
        {

            var idProyectoNullable = _seguridadService.ObtenerIdProyectoActivo();
            
            if (idProyectoNullable == null)
                return RedirectToAction("SeleccionarProyecto", "Login");

            int idProyecto = idProyectoNullable.Value;

            var proyectoActivo = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == idProyecto && p.activo);

            if (proyectoActivo == null)
                return RedirectToAction("SeleccionarProyecto", "Login");

            if (!proyectoActivo.configuracion_completa)
            {
                TempData["Error"] = "Este proyecto aún no está completamente configurado.";
                return RedirectToAction("SeleccionarProyecto", "Login");
            }

            int? idUsuario = _seguridadService.ObtenerIdUsuario();
            if (idUsuario == null)
                return RedirectToAction("Index", "Login");

            bool esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");
            bool accesoPorProyecto = _seguridadService.EsAccesoProyectoActivoPorProyecto();
            bool esAdminProyecto = _seguridadService.EsAdministradorProyectoActivo();

            var entidad = await _context.RegObls
                .Include(x => x.Cliente)
                .Include(x => x.Empresa)
                .Include(x => x.TipoObligacion)
                .Include(x => x.Dominio)
                .Include(x => x.Ciudad)
                .Include(x => x.Periodo)
                .Include(x => x.Proyecto)
                .Include(x => x.Estado)
                .Include(x => x.JustifVar)
                .Include(x => x.AprobadoPor)
                .Include(x => x.UsuarioSoportePostCierre)
                .Include(x => x.Adjuntos)
                .Include(x => x.UsuariosObligaciones)
                    .ThenInclude(uo => uo.Usuario)
                .Include(x => x.UsuariosObligaciones)
                    .ThenInclude(uo => uo.Rol)
                .FirstOrDefaultAsync(x => x.id_reg_obl == id && x.id_proyecto == idProyecto);


            if (entidad == null)
                return RedirectToAction(nameof(Index));

            // =====================================
            // VALIDACIÓN DE ACCESO A LA OBLIGACIÓN
            // =====================================
            if (!esSuperAdmin && !accesoPorProyecto)
            {
                bool participa = entidad.UsuariosObligaciones.Any(uo =>
                    uo.id_usuario == idUsuario.Value &&
                    uo.activo);

                if (!participa)
                    return RedirectToAction("AccessDenied", "Login");
            }

            // ============================
            // Participantes
            // ============================
            var responsables = entidad.UsuariosObligaciones
                .Where(x => x.activo && x.Rol.nombre == "Responsable")
                .Select(x => x.id_usuario)
                .Distinct()
                .ToList();

            var elaboradores = entidad.UsuariosObligaciones
                .Where(x => x.activo && x.Rol.nombre == "Elaborador")
                .Select(x => x.id_usuario)
                .Distinct()
                .ToList();

            var autorizadores = entidad.UsuariosObligaciones
                .Where(x => x.activo && x.Rol.nombre == "Autorizador")
                .Select(x => x.id_usuario)
                .Distinct()
                .ToList();

            var aprobadores = entidad.UsuariosObligaciones
                .Where(x => x.activo && x.Rol.nombre == "Aprobador")
                .Select(x => x.id_usuario)
                .Distinct()
                .ToList();

            var vencimiento = entidad.UsuariosObligaciones
                .Where(x => x.activo && x.Rol.nombre == "Vencimiento")
                .Select(x => x.id_usuario)
                .Distinct()
                .ToList();

            bool esElaborador = elaboradores.Contains(idUsuario.Value);
            bool esAutorizador = autorizadores.Contains(idUsuario.Value);
            bool esAprobador = aprobadores.Contains(idUsuario.Value);

            // ============================
            // Fase lógica del estado
            // ============================
            var fase = await ObtenerFaseEstadoAsync(idProyecto, entidad.Estado);

            bool estadoFinal = fase.EsFinal;
            bool esPresentada = fase.EsPresentada;
            bool esEnSeguimiento = fase.EsEnSeguimiento;
            bool esAntesDeSeguimiento = fase.EsAntesDeSeguimiento;
            bool esAntesDePresentada = fase.EsAntesDePresentada;

            bool proyectoUsaSoportePostCierre =
                entidad.Proyecto.usa_soporte_post_cierre &&
                !string.IsNullOrWhiteSpace(entidad.Proyecto.nombre_soporte_post_cierre);

            bool yaPasoControlVencimiento =
                entidad.Estado.control_vencimiento ||
                entidad.fecha_vencimiento_ejecutada != null;

            bool estaAnulada =
                entidad.Estado.bloquea &&
                !entidad.Estado.control_vencimiento &&
                !entidad.Estado.control_seguimiento &&
                entidad.Estado.nombre.ToUpper().Contains("ANUL");

            bool mostrarSoportePostCierre =
                proyectoUsaSoportePostCierre &&
                yaPasoControlVencimiento &&
                !estaAnulada;

            var ultimoSoportePostCierre = entidad.Adjuntos
                .Where(a =>
                    a.tipo_soporte == "POST_CIERRE" &&
                    a.activo &&
                    !a.eliminado)
                .OrderByDescending(a => a.fecha_carga)
                .FirstOrDefault();

            // ============================
            // Indicadores
            // ============================
            var hoy = ObtenerFechaBogota();

            bool estaVencida = !estadoFinal
                && entidad.fecha_venc_obl < hoy
                && entidad.fecha_vencimiento_ejecutada == null;

            bool estaPorVencer = !estadoFinal
                && entidad.fecha_venc_obl >= hoy
                && entidad.fecha_venc_obl <= hoy.AddDays(5)
                && entidad.fecha_vencimiento_ejecutada == null;

            // ============================
            // ViewModel
            // ============================
            var model = new RegOblSeguimientoViewModel
            {
                id_reg_obl = entidad.id_reg_obl,
                id_proyecto = entidad.id_proyecto,
                nombre_proyecto = entidad.Proyecto.nombre,

                nombre = entidad.nombre,
                cod_obligacion = entidad.cod_obligacion,

                id_cliente = entidad.id_cliente,
                nombre_cliente = entidad.Cliente?.nombre,

                id_empresa = entidad.id_empresa,
                nombre_empresa = entidad.Empresa?.nombre,

                id_tipo_obligacion = entidad.id_tipo_obligacion,
                nombre_tipo_obligacion = entidad.TipoObligacion?.nombre,

                id_dominio = entidad.id_dominio,
                nombre_dominio = entidad.Dominio?.nombre,

                id_ciudad = entidad.id_ciudad,
                nombre_ciudad = entidad.Ciudad?.nombre,

                id_periodo = entidad.id_periodo,
                nombre_periodo = entidad.Periodo?.nombre,

                vigencia = entidad.vigencia,
                anio = entidad.anio,
                mes = entidad.mes,
                dia = entidad.dia,

                fecha_creac = entidad.fecha_creac,
                fecha_venc_obl = entidad.fecha_venc_obl,
                fecha_venc_seguimiento = entidad.fecha_venc_seguimiento,
                fecha_seguimiento_ejecutada = entidad.fecha_seguimiento_ejecutada,
                fecha_vencimiento_ejecutada = entidad.fecha_vencimiento_ejecutada,
                dias_atraso_seguimiento = entidad.dias_atraso_seguimiento,
                dias_atraso_vencimiento = entidad.dias_atraso_vencimiento,
                fecha_aprobado_final = entidad.fecha_aprobado_final,

                vlr_aprox = entidad.vlr_aprox,
                vlr_real = entidad.vlr_real,
                diferencia = entidad.diferencia,
                variacion = entidad.variacion,
                saldo_favor = entidad.saldo_favor,

                id_justif_var = entidad.id_justif_var,
                nombre_justif_var = entidad.JustifVar?.nombre,

                cc_empleador = entidad.cc_empleador,
                nombre_empleador = entidad.nombre_empleador,
                cc_empleado = entidad.cc_empleado,
                nombre_empleado = entidad.nombre_empleado,
                observaciones = entidad.observaciones,

                id_estado = entidad.id_estado,
                nombre_estado = entidad.Estado?.nombre ?? string.Empty,
                aprobado = entidad.aprobado,
                id_aprobado_por = entidad.id_aprobado_por,
                nombre_aprobado_por = entidad.AprobadoPor?.nombre,

                ids_responsables = responsables,
                ids_elaboradores = elaboradores,
                ids_autorizadores = autorizadores,
                ids_aprobadores = aprobadores,
                ids_vencimiento = vencimiento,

                Adjuntos = entidad.Adjuntos
                    .Where(a => a.activo && !a.eliminado)
                    .OrderByDescending(a => a.fecha_carga)
                .Select(a => new OblAdjunto
                {
                    id_obl_adjunto = a.id_obl_adjunto,
                    nombre_orig = a.nombre_orig,
                    object_key = a.object_key,
                    fecha_carga = a.fecha_carga,
                    fecha_carga_local = ConvertirUtcABogota(a.fecha_carga).ToString("yyyy-MM-dd HH:mm")
                })
                .ToList(),

                EstaVencida = estaVencida,
                EstaPorVencer = estaPorVencer,
                EsCerrada = estadoFinal && !entidad.Estado.control_vencimiento,
                EsAnulada = entidad.Estado.bloquea && !entidad.Estado.control_vencimiento && !entidad.Estado.control_seguimiento,

                UsaSoportePostCierre = proyectoUsaSoportePostCierre,
                NombreSoportePostCierre = entidad.Proyecto.nombre_soporte_post_cierre,
                MostrarSoportePostCierre = mostrarSoportePostCierre,
                SoportePostCierreCumplido = entidad.soporte_post_cierre_cumplido,
                FechaSoportePostCierre = entidad.fecha_soporte_post_cierre,
                FechaHoraSoportePostCierreLocal = ultimoSoportePostCierre != null
                    ? ConvertirUtcABogota(ultimoSoportePostCierre.fecha_carga).ToString("yyyy-MM-dd HH:mm")
                    : null,
                NombreUsuarioSoportePostCierre = entidad.UsuarioSoportePostCierre?.nombre,
                UltimoSoportePostCierre = ultimoSoportePostCierre?.nombre_orig,
                IdUltimoSoportePostCierre = ultimoSoportePostCierre?.id_obl_adjunto,
                PuedeCargarSoportePostCierre = mostrarSoportePostCierre && (esSuperAdmin || esAdminProyecto || esElaborador || esAutorizador || esAprobador)
            };

            // ============================
            // Transiciones disponibles
            // ============================
            model.TransicionesDisponibles = await ObtenerTransicionesDisponiblesAsync(
                entidad,
                idUsuario.Value,
                esSuperAdmin,
                esAdminProyecto
            );

            // ============================
            // Permisos por fase lógica
            // ============================
            model.PuedeEditarDatosGenerales =
                !estadoFinal &&
                !esPresentada &&
                (esSuperAdmin || esAdminProyecto);

            model.PuedeEditarParticipantes =
                !estadoFinal &&
                !esPresentada &&
                (esSuperAdmin || esAdminProyecto);

            model.PuedeEditarFechasBase =
                !estadoFinal &&
                !esPresentada &&
                esAntesDeSeguimiento &&
                (esSuperAdmin || esAdminProyecto);

            model.PuedeEditarValores =
                !estadoFinal &&
                !esPresentada &&
                esAntesDePresentada &&
                (esSuperAdmin || esAdminProyecto || esElaborador || esAutorizador);

            model.PuedeEditarJustificacion = model.PuedeEditarValores;

            model.PuedeEditarObservaciones =
                !estadoFinal &&
                !esPresentada &&
                (esSuperAdmin || esAdminProyecto || esElaborador || esAutorizador);

            model.PuedeCargarSoporte =
                !estadoFinal &&
                !esPresentada &&
                esAntesDePresentada &&
                (esSuperAdmin || esAdminProyecto || esElaborador || esAutorizador);


            await CargarCombosSeguimientoAsync(entidad.id_proyecto, model);

            var historialFlujo = await _context.HistOblFlujos
                .Where(h => h.id_reg_obl == entidad.id_reg_obl)
                .Include(h => h.Usuario)
                .Include(h => h.EstadoOrigen)
                .Include(h => h.EstadoDestino)
                .OrderByDescending(h => h.fecha)
                .ToListAsync();

            var historialCampos = await _context.HistOblCampos
                .Where(h => h.id_reg_obl == entidad.id_reg_obl)
                .Include(h => h.Usuario)
                .OrderByDescending(h => h.fecha)
                .ToListAsync();

            var historial = new List<HistorialItemViewModel>();

            historial.AddRange(historialFlujo.Select(h => new HistorialItemViewModel
            {
                Fecha = ConvertirUtcABogota(h.fecha),
                Usuario = h.Usuario?.nombre ?? "Sistema",
                Tipo = "Flujo",
                Titulo = h.accion ?? "Cambio de estado",
                Detalle = string.IsNullOrWhiteSpace(h.observacion)
                    ? $"De '{h.EstadoOrigen?.nombre ?? "-"}' a '{h.EstadoDestino?.nombre ?? "-"}'"
                    : h.observacion
            }));

            historial.AddRange(historialCampos.Select(h => new HistorialItemViewModel
            {
                Fecha = ConvertirUtcABogota(h.fecha),
                Usuario = h.Usuario?.nombre ?? "Sistema",
                Tipo = "Campo",
                Titulo = $"Cambio en {h.campo}",
                Detalle = $"Antes: {h.valor_anterior ?? "-"} | Ahora: {h.valor_nuevo ?? "-"}"
            }));

            if (!historial.Any() && entidad.fecha_creac.HasValue)
            {
                historial.Add(new HistorialItemViewModel
                {
                    Fecha = entidad.fecha_creac.Value.ToDateTime(TimeOnly.MinValue),
                    Usuario = "Sistema",
                    Tipo = "Flujo",
                    Titulo = "Creación de la obligación",
                    Detalle = $"Estado inicial: {model.nombre_estado}"
                });
            }

            model.Historial = historial
                .OrderByDescending(x => x.Fecha)
                .ToList();

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> VerAdjunto(int idAdjunto)
        {
            var idProyectoNullable = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyectoNullable == null)
                return RedirectToAction("SeleccionarProyecto", "Login");

            int idProyecto = idProyectoNullable.Value;

            int? idUsuario = _seguridadService.ObtenerIdUsuario();
            if (idUsuario == null)
                return RedirectToAction("Index", "Login");

            var adjunto = await _context.OblAdjuntos
                .Include(a => a.RegObl)
                    .ThenInclude(o => o.UsuariosObligaciones)
                .FirstOrDefaultAsync(a =>
                    a.id_obl_adjunto == idAdjunto &&
                    a.activo &&
                    !a.eliminado);

            if (adjunto == null)
                return NotFound();

            if (adjunto.RegObl == null || adjunto.RegObl.id_proyecto != idProyecto)
                return RedirectToAction("AccessDenied", "Login");

            bool esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");
            bool accesoPorProyecto = _seguridadService.EsAccesoProyectoActivoPorProyecto();

            if (!esSuperAdmin && !accesoPorProyecto)
            {
                bool participa = adjunto.RegObl.UsuariosObligaciones.Any(uo =>
                    uo.id_usuario == idUsuario.Value &&
                    uo.activo);

                if (!participa)
                    return RedirectToAction("AccessDenied", "Login");
            }

            byte[] bytes;

            try
            {
                bytes = await _fileStorageService.DownloadAsync(adjunto.object_key);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }

            var mimeType = string.IsNullOrWhiteSpace(adjunto.mime_type)
                ? "application/octet-stream"
                : adjunto.mime_type;

            var nombreDescarga = string.IsNullOrWhiteSpace(adjunto.nombre_orig)
                ? Path.GetFileName(adjunto.object_key)
                : adjunto.nombre_orig;

            return File(bytes, mimeType, nombreDescarga);
        }



        private async Task<List<TransicionDisponibleViewModel>> ObtenerTransicionesDisponiblesAsync(
            RegObl entidad,
            int idUsuario,
            bool esSuperAdmin,
            bool esAdminProyecto)
        {
            int idEstadoActual = entidad.id_estado;

            // Roles del usuario en esta obligación
            var rolesUsuario = entidad.UsuariosObligaciones
                .Where(x => x.activo && x.id_usuario == idUsuario)
                .Select(x => x.id_rol)
                .Distinct()
                .ToList();

            // Traer transiciones desde el estado actual
            var query = _context.EstadosTransicion
                .Include(t => t.EstadoDestino)
                .Include(t => t.RolesEstadosTransicion)
                .Where(t =>
                    t.activo &&
                    t.id_estado_origen == idEstadoActual);

            var transiciones = await query.ToListAsync();

            // Filtrar por rol (excepto super admin)
            if (!esSuperAdmin && !esAdminProyecto)
            {
                transiciones = transiciones
                    .Where(t =>
                        t.RolesEstadosTransicion.Any(rt =>
                            rt.activo &&
                            rolesUsuario.Contains(rt.id_rol))
                    )
                    .ToList();
            }

            return transiciones.Select(t => new TransicionDisponibleViewModel
            {
                id_estado_transicion = t.id_estado_transicion,
                nombre_accion = t.nombre_accion,
                requiere_observacion = t.requiere_observacion,
                es_aprobacion = t.es_aprobacion,
                es_rechazo = t.es_rechazo,
                es_anulacion = t.es_anulacion,
                id_estado_destino = t.id_estado_destino,
                nombre_estado_destino = t.EstadoDestino.nombre
            }).OrderBy(t => t.id_estado_transicion).ToList();
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarSeguimiento(
            RegOblSeguimientoViewModel model,
            string accion,
            int? idEstadoTransicion,
            int? idAdjunto,
            IFormFile? archivo,
            IFormFile? archivoPostCierre,
            string? observacion)
        {


            var idProyectoNullable = _seguridadService.ObtenerIdProyectoActivo();
            if (idProyectoNullable == null)
                return RedirectToAction("SeleccionarProyecto", "Login");

            int idProyecto = idProyectoNullable.Value;

            int? idUsuario = _seguridadService.ObtenerIdUsuario();
            if (idUsuario == null)
                return RedirectToAction("Index", "Login");

            var entidad = await _context.RegObls
                .Include(x => x.Proyecto)
                .Include(x => x.Estado)
                .Include(x => x.Adjuntos)
                .Include(x => x.UsuariosObligaciones)
                    .ThenInclude(uo => uo.Rol)
                .FirstOrDefaultAsync(x => x.id_reg_obl == model.id_reg_obl && x.id_proyecto == idProyecto);

            if (entidad == null)
                return RedirectToAction(nameof(Index));

            var resultado = await AplicarCambiosSeguimientoAsync(model, entidad, idProyecto, idUsuario.Value);
            if (!resultado.Ok)
                return resultado.ErrorResult!;

            switch (accion)
            {
                case "guardar":
                    TempData["Ok"] = "Cambios guardados correctamente.";
                    break;

                case "transicion":
                    if (!idEstadoTransicion.HasValue)
                    {
                        TempData["Error"] = "No se encontró la transición.";
                        return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
                    }

                    var transicion = await _context.EstadosTransicion
                        .FirstOrDefaultAsync(t => t.id_estado_transicion == idEstadoTransicion.Value && t.activo);

                    if (transicion == null)
                    {
                        TempData["Error"] = "No se encontró la transición.";
                        return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
                    }

                    if (transicion.requiere_observacion && string.IsNullOrWhiteSpace(observacion))
                    {
                        TempData["Error"] = "Debe ingresar una observación para ejecutar esta acción.";
                        return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
                    }

                    await EjecutarTransicionInterno(entidad, transicion, observacion, idUsuario.Value);
                    TempData["Ok"] = "Transición ejecutada correctamente.";
                    break;

                case "subir":
                    if (archivo == null || archivo.Length == 0)
                    {
                        TempData["Error"] = "Debe seleccionar un archivo.";
                        return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
                    }

                    if (archivo.Length > MaxArchivoBytes)
                    {
                        TempData["Error"] = "El archivo no puede superar 2 MB.";
                        return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
                    }

                    await SubirAdjuntoInterno(entidad, archivo, idUsuario.Value);
                    TempData["Ok"] = "Soporte cargado correctamente.";
                    break;

                case "eliminar":
                    if (!idAdjunto.HasValue)
                    {
                        TempData["Error"] = "No se encontró el adjunto.";
                        return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
                    }

                    await EliminarAdjuntoInterno(entidad, idAdjunto.Value, idUsuario.Value);
                    TempData["Ok"] = "Adjunto eliminado correctamente.";
                    break;

                case "post_cierre":
                    if (archivoPostCierre == null || archivoPostCierre.Length == 0)
                    {
                        TempData["Error"] = "Debe seleccionar un archivo.";
                        return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
                    }

                    if (archivoPostCierre.Length > 2 * 1024 * 1024)
                    {
                        TempData["Error"] = "El archivo no puede superar 2 MB.";
                        return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
                    }

                    await SubirSoportePostCierreInterno(entidad, archivoPostCierre, idUsuario.Value);
                    TempData["Ok"] = "Soporte registrado correctamente.";
                    break;

                default:
                    TempData["Error"] = "Acción no válida.";
                    return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Seguimiento), new { id = entidad.id_reg_obl });
        }

        private async Task<string?> SubirSoportePostCierreInterno(
            RegObl entidad,
            IFormFile archivo,
            int idUsuario)
        {
            if (archivo == null || archivo.Length == 0)
                return "Debe seleccionar un archivo.";

            if (archivo.Length > 2 * 1024 * 1024)
                return "El archivo no puede superar 2 MB.";

            var folder = $"proyectos/{entidad.id_proyecto}/obligaciones/{entidad.id_reg_obl}/post-cierre/{DateTime.UtcNow:yyyy/MM}";

            var resultado = await _fileStorageService.UploadAsync(archivo, folder);

            var fechaHoraUtc = ObtenerFechaHoraUtc();
            var fechaBogota = ObtenerFechaBogota();

            string nombreControl =
                entidad.Proyecto?.nombre_soporte_post_cierre
                ?? "Soporte post cierre";

            var soporteAnterior = entidad.Adjuntos
                .Where(a =>
                    a.tipo_soporte == "POST_CIERRE" &&
                    a.activo &&
                    !a.eliminado)
                .OrderByDescending(a => a.fecha_carga)
                .FirstOrDefault();

            _context.OblAdjuntos.Add(new OblAdjunto
            {
                id_reg_obl = entidad.id_reg_obl,
                nombre_orig = resultado.OriginalFileName,
                object_key = resultado.ObjectKey,
                bucket_name = resultado.BucketName,
                mime_type = resultado.MimeType,
                extension = resultado.Extension,
                tamano_bytes = resultado.FileSize,
                fecha_carga = fechaHoraUtc,
                id_usuario = idUsuario,
                tipo_soporte = "POST_CIERRE",
                activo = true,
                eliminado = false
            });

            bool valorAnteriorCheck =
                entidad.soporte_post_cierre_cumplido;

            entidad.soporte_post_cierre_cumplido = true;
            entidad.fecha_soporte_post_cierre = fechaBogota;
            entidad.id_usuario_soporte_post_cierre = idUsuario;
            entidad.fecha_ult_modif = fechaHoraUtc;
            entidad.id_usuario_ult_modif = idUsuario;

            _context.HistOblCampos.Add(new HistOblCampo
            {
                id_reg_obl = entidad.id_reg_obl,
                campo = nombreControl,
                valor_anterior = valorAnteriorCheck ? "Cumplido" : "No cumplido",
                valor_nuevo = "Cumplido",
                id_usuario = idUsuario,
                fecha = fechaHoraUtc,
                id_estado_en_momento = entidad.id_estado,
                tipo_cambio = "SOPORTE_POST_CIERRE"
            });

            _context.HistOblCampos.Add(new HistOblCampo
            {
                id_reg_obl = entidad.id_reg_obl,
                campo = "Soporte post cierre",
                valor_anterior = soporteAnterior?.nombre_orig,
                valor_nuevo = resultado.OriginalFileName,
                id_usuario = idUsuario,
                fecha = fechaHoraUtc,
                id_estado_en_momento = entidad.id_estado,
                tipo_cambio = "SOPORTE_POST_CIERRE"
            });

            return null;
        }

        private async Task EjecutarTransicionInterno(
            RegObl entidad,
            EstadoTransicion transicion,
            string? observacion,
            int idUsuario)
        {

            if (transicion == null)
                return;

            var estadoOrigen = await _context.Estados.FirstAsync(e => e.id_estado == entidad.id_estado);
            var estadoDestino = await _context.Estados.FirstAsync(e => e.id_estado == transicion.id_estado_destino);

            var fechaBogota = ObtenerFechaBogota();
            var fechaHoraUtc = ObtenerFechaHoraUtc();

            // 🔹 Limpieza seguimiento si vuelve atrás
            if (estadoOrigen.control_seguimiento && estadoDestino.orden < estadoOrigen.orden)
            {
                entidad.fecha_seguimiento_ejecutada = null;
                entidad.dias_atraso_seguimiento = null;
            }

            // 🔹 Primera vez en seguimiento
            if (estadoDestino.control_seguimiento && entidad.fecha_seguimiento_ejecutada == null)
            {
                entidad.fecha_seguimiento_ejecutada = fechaBogota;

                if (entidad.fecha_seguimiento_ejecutada > entidad.fecha_venc_seguimiento)
                {
                    entidad.dias_atraso_seguimiento =
                        (entidad.fecha_seguimiento_ejecutada.Value.ToDateTime(TimeOnly.MinValue)
                        - entidad.fecha_venc_seguimiento.ToDateTime(TimeOnly.MinValue)).Days;
                }
                else
                {
                    entidad.dias_atraso_seguimiento = 0;
                }
            }

            // 🔹 Presentada
            if (estadoDestino.control_vencimiento)
            {
                entidad.fecha_vencimiento_ejecutada = fechaBogota;

                if (entidad.fecha_vencimiento_ejecutada > entidad.fecha_venc_obl)
                {
                    entidad.dias_atraso_vencimiento =
                        (entidad.fecha_vencimiento_ejecutada.Value.ToDateTime(TimeOnly.MinValue)
                        - entidad.fecha_venc_obl.ToDateTime(TimeOnly.MinValue)).Days;
                }
                else
                {
                    entidad.dias_atraso_vencimiento = 0;
                }

                entidad.id_autorizado_por = idUsuario;
            }

            // 🔹 Rechazo
            if (transicion.es_rechazo)
            {
                entidad.fecha_vencimiento_ejecutada = null;
                entidad.dias_atraso_vencimiento = null;
                entidad.id_autorizado_por = null;
            }

            // 🔹 Aprobación
            if (transicion.es_aprobacion)
            {
                entidad.aprobado = true;
                entidad.fecha_aprobado_final = fechaBogota;
                entidad.id_aprobado_por = idUsuario;
            }


            entidad.id_estado = transicion.id_estado_destino;
            entidad.fecha_ult_modif = fechaHoraUtc;
            entidad.id_usuario_ult_modif = idUsuario;

            _context.HistOblFlujos.Add(new HistOblFlujo
            {
                id_reg_obl = entidad.id_reg_obl,
                id_estado_origen = estadoOrigen.id_estado,
                id_estado_destino = estadoDestino.id_estado,
                accion = transicion.nombre_accion,
                observacion = observacion,
                id_usuario = idUsuario,
                fecha = fechaHoraUtc
            });
        }

        private async Task SubirAdjuntoInterno(RegObl entidad, IFormFile archivo, int idUsuario)
        {
            if (archivo == null || archivo.Length == 0)
                return;

            if (archivo.Length > 2 * 1024 * 1024)
                throw new Exception("El archivo no puede superar 2 MB.");

            var folder = $"proyectos/{entidad.id_proyecto}/obligaciones/{entidad.id_reg_obl}/soportes/{DateTime.UtcNow:yyyy/MM}";

            var resultado = await _fileStorageService.UploadAsync(archivo, folder);

            var fechaHoraUtc = ObtenerFechaHoraUtc();

            _context.OblAdjuntos.Add(new OblAdjunto
            {
                id_reg_obl = entidad.id_reg_obl,
                nombre_orig = resultado.OriginalFileName,
                object_key = resultado.ObjectKey,
                bucket_name = resultado.BucketName,
                mime_type = resultado.MimeType,
                extension = resultado.Extension,
                tamano_bytes = resultado.FileSize,
                fecha_carga = fechaHoraUtc,
                id_usuario = idUsuario,
                tipo_soporte = "NORMAL",
                activo = true,
                eliminado = false
            });

            _context.HistOblCampos.Add(new HistOblCampo
            {
                id_reg_obl = entidad.id_reg_obl,
                campo = "Adjunto",
                valor_anterior = null,
                valor_nuevo = resultado.OriginalFileName,
                id_usuario = idUsuario,
                fecha = fechaHoraUtc,
                id_estado_en_momento = entidad.id_estado,
                tipo_cambio = "SOPORTE"
            });
        }

        private async Task EliminarAdjuntoInterno(RegObl entidad, int idAdjunto, int idUsuario)
        {
            var adjunto = await _context.OblAdjuntos
                .FirstOrDefaultAsync(a =>
                    a.id_obl_adjunto == idAdjunto &&
                    a.id_reg_obl == entidad.id_reg_obl &&
                    a.activo &&
                    !a.eliminado);

            if (adjunto == null)
                return;

            var fechaHoraUtc = ObtenerFechaHoraUtc();

            adjunto.activo = false;
            adjunto.eliminado = true;
            adjunto.fecha_eliminacion = fechaHoraUtc;
            adjunto.id_usuario_eliminacion = idUsuario;
            adjunto.motivo_eliminacion = "Eliminado desde seguimiento de obligación";

            _context.HistOblCampos.Add(new HistOblCampo
            {
                id_reg_obl = entidad.id_reg_obl,
                campo = adjunto.tipo_soporte == "POST_CIERRE"
                    ? "Soporte post cierre"
                    : "Adjunto",
                valor_anterior = adjunto.nombre_orig,
                valor_nuevo = null,
                id_usuario = idUsuario,
                fecha = fechaHoraUtc,
                id_estado_en_momento = entidad.id_estado,
                tipo_cambio = adjunto.tipo_soporte == "POST_CIERRE"
                    ? "ELIMINACION_SOPORTE_POST_CIERRE"
                    : "ELIMINACION_SOPORTE"
            });
        }

        private static TimeZoneInfo ObtenerZonaBogota()
        {
            return TZConvert.GetTimeZoneInfo("America/Bogota");
        }

        private static DateTime ObtenerFechaHoraUtc()
        {
            return DateTime.UtcNow;
        }

        private static DateTime ObtenerFechaHoraBogota()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ObtenerZonaBogota());
        }

        private static DateOnly ObtenerFechaBogota()
        {
            return DateOnly.FromDateTime(ObtenerFechaHoraBogota());
        }

        private static DateTime ConvertirUtcABogota(DateTime fechaUtc)
        {
            var utc = fechaUtc.Kind == DateTimeKind.Utc
                ? fechaUtc
                : DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utc, ObtenerZonaBogota());
        }

        private async Task<(bool EsFinal, bool EsPresentada, bool EsEnSeguimiento, bool EsAntesDeSeguimiento, bool EsAntesDePresentada)>
            ObtenerFaseEstadoAsync(int idProyecto, Estado estadoActual)
        {
            var estadosProyecto = await _context.Estados
                .Where(e => e.id_proyecto == idProyecto && e.activo)
                .OrderBy(e => e.orden)
                .ToListAsync();

            var estadoSeguimiento = estadosProyecto.FirstOrDefault(e => e.control_seguimiento);
            var estadoPresentada = estadosProyecto.FirstOrDefault(e => e.control_vencimiento);

            int ordenActual = estadoActual.orden;
            int? ordenSeguimiento = estadoSeguimiento?.orden;
            int? ordenPresentada = estadoPresentada?.orden;

            bool esPresentada = estadoActual.control_vencimiento;
            bool esEnSeguimiento = estadoActual.control_seguimiento;

            bool esFinal = estadoActual.bloquea && !estadoActual.control_vencimiento && !estadoActual.control_seguimiento;

            bool esAntesDeSeguimiento = ordenSeguimiento.HasValue && ordenActual < ordenSeguimiento.Value;
            bool esAntesDePresentada = ordenPresentada.HasValue && ordenActual < ordenPresentada.Value;

            return (esFinal, esPresentada, esEnSeguimiento, esAntesDeSeguimiento, esAntesDePresentada);
        }

    }




}