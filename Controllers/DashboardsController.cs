using Alertas.Data;
using Alertas.Services;
using Alertas.Services.Dashboards;
using Alertas.ViewModels.Dashboards;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace Alertas.Controllers
{
    [Authorize]
    public class DashboardsController : Controller
    {
        private readonly IDashboardOperativoService _dashboardOperativoService;
        private readonly SeguridadService _seguridadService;
        private readonly ApplicationDbContext _context;
        private readonly IDashboardHistoricoCumplimientoService _dashboardHistoricoCumplimientoService;

        public DashboardsController(
            IDashboardOperativoService dashboardOperativoService,
            IDashboardHistoricoCumplimientoService dashboardHistoricoCumplimientoService,
            SeguridadService seguridadService,
            ApplicationDbContext context)
        {
            _dashboardOperativoService = dashboardOperativoService;
            _dashboardHistoricoCumplimientoService = dashboardHistoricoCumplimientoService;
            _seguridadService = seguridadService;
            _context = context;
        }

        public async Task<IActionResult> OperativoProyecto(
            int? idCliente,
            int? idEmpresa,
            int? idCiudad,
            int? idEstado,
            int? idTipoObligacion,
            int? anio,
            int? mes,
            int? idResponsable,
            int? idElaborador,
            int? idAutorizador,
            int? idAprobador,
            int? idUsuarioVencimiento)
        {
            var idProyecto = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
            {
                TempData["Error"] = "Debe seleccionar un proyecto.";
                return RedirectToAction("SeleccionarProyecto", "Login");
            }

            var esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");

            var tieneAccesoProyecto = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "PROYECTO");

            var tieneAccesoObligacion = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "OBLIGACION");

            if (!esSuperAdmin && !tieneAccesoProyecto && !tieneAccesoObligacion)
            {
                TempData["Error"] = "No tiene acceso al proyecto seleccionado.";
                return RedirectToAction("Index", "Home");
            }

            var filtros = new FiltrosDashboardOperativoVm
            {
                IdCliente = idCliente,
                IdEmpresa = idEmpresa,
                IdCiudad = idCiudad,
                IdEstado = idEstado,
                IdTipoObligacion = idTipoObligacion,
                Anio = anio,
                Mes = mes,
                IdResponsable = idResponsable,
                IdElaborador = idElaborador,
                IdAutorizador = idAutorizador,
                IdAprobador = idAprobador,
                IdUsuarioVencimiento = idUsuarioVencimiento
            };

            var vm = await _dashboardOperativoService.ObtenerDashboardProyectoAsync(idProyecto.Value, filtros);

            return View(vm);
        }

        public async Task<IActionResult> DetalleOperativo(
            string tipo,
            int? idCliente,
            int? idEmpresa,
            int? idCiudad,
            int? idEstado,
            int? idEstadoDetalle,
            int? idTipoObligacion,
            int? anio,
            int? mes,
            int? idResponsable,
            int? idElaborador,
            int? idAutorizador,
            int? idAprobador,
            int? idUsuarioVencimiento)
        {
            var idProyecto = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
            {
                TempData["Error"] = "Debe seleccionar un proyecto.";
                return RedirectToAction("SeleccionarProyecto", "Login");
            }

            var esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");

            var tieneAccesoProyecto = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "PROYECTO");

            var tieneAccesoObligacion = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "OBLIGACION");

            if (!esSuperAdmin && !tieneAccesoProyecto && !tieneAccesoObligacion)
            {
                TempData["Error"] = "No tiene acceso al proyecto seleccionado.";
                return RedirectToAction("Index", "Home");
            }

            var filtros = new FiltrosDashboardOperativoVm
            {
                IdCliente = idCliente,
                IdEmpresa = idEmpresa,
                IdCiudad = idCiudad,
                IdEstado = idEstado,
                IdTipoObligacion = idTipoObligacion,
                Anio = anio,
                Mes = mes,
                IdResponsable = idResponsable,
                IdElaborador = idElaborador,
                IdAutorizador = idAutorizador,
                IdAprobador = idAprobador,
                IdUsuarioVencimiento = idUsuarioVencimiento
            };

            var vm = await _dashboardOperativoService
                .ObtenerDetalleOperativoAsync(
                    idProyecto.Value,
                    tipo,
                    filtros,
                    idEstadoDetalle);

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportarDetalleOperativo(
            string tipo,
            int? idEstadoDetalle,
            int? idCliente,
            int? idEmpresa,
            int? idCiudad,
            int? idEstado,
            int? idTipoObligacion,
            int? anio,
            int? mes,
            int? idResponsable,
            int? idElaborador,
            int? idAutorizador,
            int? idAprobador,
            int? idUsuarioVencimiento)
        {
            var idProyecto = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
                return RedirectToAction("SeleccionarProyecto", "Login");

            var filtros = new FiltrosDashboardOperativoVm
            {
                IdCliente = idCliente,
                IdEmpresa = idEmpresa,
                IdCiudad = idCiudad,
                IdEstado = idEstado,
                IdTipoObligacion = idTipoObligacion,
                Anio = anio,
                Mes = mes,

                IdResponsable = idResponsable,
                IdElaborador = idElaborador,
                IdAutorizador = idAutorizador,
                IdAprobador = idAprobador,
                IdUsuarioVencimiento = idUsuarioVencimiento
            };

            var vm = await _dashboardOperativoService
                .ObtenerDetalleOperativoAsync(
                    idProyecto.Value,
                    tipo,
                    filtros,
                    idEstadoDetalle);

            using var workbook = new ClosedXML.Excel.XLWorkbook();

            var ws = workbook.Worksheets.Add("Detalle");

            ws.Cell(1, 1).Value = "Obligación";
            ws.Cell(1, 2).Value = "Código";
            ws.Cell(1, 3).Value = "Cliente";
            ws.Cell(1, 4).Value = "Empresa";
            ws.Cell(1, 5).Value = "Tipo";
            ws.Cell(1, 6).Value = "Estado";
            ws.Cell(1, 7).Value = "Vencimiento";
            ws.Cell(1, 8).Value = "Seguimiento";
            ws.Cell(1, 9).Value = "Aprobada";

            int row = 2;

            foreach (var item in vm.Obligaciones)
            {
                ws.Cell(row, 1).Value = item.Nombre;
                ws.Cell(row, 2).Value = item.CodigoObligacion;
                ws.Cell(row, 3).Value = item.Cliente;
                ws.Cell(row, 4).Value = item.Empresa;
                ws.Cell(row, 5).Value = item.TipoObligacion;
                ws.Cell(row, 6).Value = item.Estado;
                ws.Cell(row, 7).Value = item.FechaVencimiento;
                ws.Cell(row, 8).Value = item.FechaSeguimiento;
                ws.Cell(row, 9).Value = item.Aprobado ? "Sí" : "No";

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DetalleDashboard_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        public async Task<IActionResult> ConsultaObligacion(int id, string? returnUrl = null)
        {
            var idProyecto = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
            {
                TempData["Error"] = "Debe seleccionar un proyecto.";
                return RedirectToAction("SeleccionarProyecto", "Login");
            }

            var idUsuario = _seguridadService.ObtenerIdUsuario();

            if (idUsuario == null)
                return RedirectToAction("Index", "Login");

            var entidad = await _context.RegObls
                .AsNoTracking()
                .Include(x => x.Proyecto)
                .Include(x => x.Cliente)
                .Include(x => x.Empresa)
                .Include(x => x.TipoObligacion)
                .Include(x => x.Ciudad)
                .Include(x => x.Dominio)
                .Include(x => x.Periodo)
                .Include(x => x.Estado)
                .Include(x => x.JustifVar)
                .Include(x => x.AprobadoPor)
                .Include(x => x.UsuariosObligaciones)
                    .ThenInclude(uo => uo.Usuario)
                .Include(x => x.UsuariosObligaciones)
                    .ThenInclude(uo => uo.Rol)
                .FirstOrDefaultAsync(x =>
                    x.id_reg_obl == id &&
                    x.id_proyecto == idProyecto.Value);

            if (entidad == null)
                return RedirectToAction("AccessDenied", "Login");

            bool esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");

            bool tieneAccesoProyecto = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "PROYECTO");

            bool tieneAccesoObligacionEnProyecto = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "OBLIGACION");

            if (!esSuperAdmin && !tieneAccesoProyecto && !tieneAccesoObligacionEnProyecto)
            {
                return RedirectToAction("AccessDenied", "Login");
            }

            string? Fecha(DateOnly? fecha) =>
                fecha.HasValue ? fecha.Value.ToString("dd/MM/yyyy") : null;

            var vm = new ConsultaObligacionDashboardVm
            {
                IdRegObl = entidad.id_reg_obl,
                Nombre = entidad.nombre,
                CodigoObligacion = entidad.cod_obligacion,

                Proyecto = entidad.Proyecto.nombre,
                Cliente = entidad.Cliente?.nombre ?? "",
                Empresa = entidad.Empresa?.nombre ?? "",
                TipoObligacion = entidad.TipoObligacion?.nombre ?? "",
                Ciudad = entidad.Ciudad?.nombre,
                Dominio = entidad.Dominio?.nombre ?? "",
                Periodo = entidad.Periodo?.nombre ?? "",
                Estado = entidad.Estado?.nombre ?? "",

                FechaCreacion = Fecha(entidad.fecha_creac) ?? "-",
                FechaVencimiento = entidad.fecha_venc_obl.ToString("dd/MM/yyyy"),
                FechaSeguimiento = entidad.fecha_venc_seguimiento.ToString("dd/MM/yyyy"),
                FechaSeguimientoEjecutada = Fecha(entidad.fecha_seguimiento_ejecutada),
                FechaVencimientoEjecutada = Fecha(entidad.fecha_vencimiento_ejecutada),
                FechaAprobadoFinal = Fecha(entidad.fecha_aprobado_final),

                DiasAtrasoSeguimiento = entidad.dias_atraso_seguimiento,
                DiasAtrasoVencimiento = entidad.dias_atraso_vencimiento,

                ValorAprox = entidad.vlr_aprox,
                ValorReal = entidad.vlr_real,
                Diferencia = entidad.diferencia,
                Variacion = entidad.variacion,
                SaldoFavor = entidad.saldo_favor,

                Justificacion = entidad.JustifVar?.nombre,
                Observaciones = entidad.observaciones,

                Aprobado = entidad.aprobado,
                AprobadoPor = entidad.AprobadoPor?.nombre,

                Responsables = entidad.UsuariosObligaciones
                    .Where(x => x.activo && x.Rol.nombre == "Responsable")
                    .Select(x => x.Usuario.nombre)
                    .Distinct()
                    .ToList(),

                Elaboradores = entidad.UsuariosObligaciones
                    .Where(x => x.activo && x.Rol.nombre == "Elaborador")
                    .Select(x => x.Usuario.nombre)
                    .Distinct()
                    .ToList(),

                Autorizadores = entidad.UsuariosObligaciones
                    .Where(x => x.activo && x.Rol.nombre == "Autorizador")
                    .Select(x => x.Usuario.nombre)
                    .Distinct()
                    .ToList(),

                Aprobadores = entidad.UsuariosObligaciones
                    .Where(x => x.activo && x.Rol.nombre == "Aprobador")
                    .Select(x => x.Usuario.nombre)
                    .Distinct()
                    .ToList(),

                UsuariosVencimiento = entidad.UsuariosObligaciones
                    .Where(x => x.activo && x.Rol.nombre == "Vencimiento")
                    .Select(x => x.Usuario.nombre)
                    .Distinct()
                    .ToList(),

                ReturnUrl = returnUrl
            };

            return View(vm);
        }

        public async Task<IActionResult> WorkflowProyecto(
            int? idCliente,
            int? idEmpresa,
            int? idCiudad,
            int? idEstado,
            int? idTipoObligacion,
            int? anio,
            int? mes,
            int? idResponsable,
            int? idElaborador,
            int? idAutorizador,
            int? idAprobador,
            int? idUsuarioVencimiento)
        {
            var idProyecto = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
            {
                TempData["Error"] = "Debe seleccionar un proyecto.";
                return RedirectToAction("SeleccionarProyecto", "Login");
            }

            var esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");

            var tieneAccesoProyecto = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "PROYECTO");

            var tieneAccesoObligacion = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "OBLIGACION");

            if (!esSuperAdmin && !tieneAccesoProyecto && !tieneAccesoObligacion)
            {
                TempData["Error"] = "No tiene acceso al proyecto seleccionado.";
                return RedirectToAction("Index", "Home");
            }

            var filtros = new FiltrosDashboardOperativoVm
            {
                IdCliente = idCliente,
                IdEmpresa = idEmpresa,
                IdCiudad = idCiudad,
                IdEstado = idEstado,
                IdTipoObligacion = idTipoObligacion,
                Anio = anio,
                Mes = mes,

                IdResponsable = idResponsable,
                IdElaborador = idElaborador,
                IdAutorizador = idAutorizador,
                IdAprobador = idAprobador,
                IdUsuarioVencimiento = idUsuarioVencimiento
            };

            var vm = await _dashboardOperativoService
                .ObtenerDashboardWorkflowAsync(idProyecto.Value, filtros);

            return View(vm);
        }

        public async Task<IActionResult> VencimientosProyecto(
            int? idCliente,
            int? idEmpresa,
            int? idCiudad,
            int? idEstado,
            int? idTipoObligacion,
            int? anio,
            int? mes,
            int? idResponsable,
            int? idElaborador,
            int? idAutorizador,
            int? idAprobador,
            int? idUsuarioVencimiento)
        {
            var idProyecto = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
            {
                TempData["Error"] = "Debe seleccionar un proyecto.";
                return RedirectToAction("SeleccionarProyecto", "Login");
            }

            var esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");

            var tieneAccesoProyecto = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "PROYECTO");

            var tieneAccesoObligacion = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "OBLIGACION");

            if (!esSuperAdmin && !tieneAccesoProyecto && !tieneAccesoObligacion)
            {
                TempData["Error"] = "No tiene acceso al proyecto seleccionado.";
                return RedirectToAction("Index", "Home");
            }

            var filtros = new FiltrosDashboardOperativoVm
            {
                IdCliente = idCliente,
                IdEmpresa = idEmpresa,
                IdCiudad = idCiudad,
                IdEstado = idEstado,
                IdTipoObligacion = idTipoObligacion,
                Anio = anio,
                Mes = mes,

                IdResponsable = idResponsable,
                IdElaborador = idElaborador,
                IdAutorizador = idAutorizador,
                IdAprobador = idAprobador,
                IdUsuarioVencimiento = idUsuarioVencimiento
            };

            var vm = await _dashboardOperativoService
                .ObtenerDashboardVencimientosAsync(idProyecto.Value, filtros);

            return View(vm);
        }

        public async Task<IActionResult> HistoricoCumplimientoProyecto(
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            int? idCliente,
            int? idEmpresa,
            int? idCiudad,
            int? idEstado,
            int? idTipoObligacion,
            int? idResponsable,
            int? idElaborador,
            int? idAutorizador,
            int? idAprobador,
            int? idUsuarioVencimiento,
            string? clasificacion)
        {
            var idProyecto = _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
            {
                TempData["Error"] = "Debe seleccionar un proyecto.";
                return RedirectToAction("SeleccionarProyecto", "Login");
            }

            var esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");

            var tieneAccesoProyecto = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "PROYECTO");

            var tieneAccesoObligacion = await _seguridadService
                .UsuarioTieneAccesoProyectoAsync(idProyecto.Value, "OBLIGACION");

            if (!esSuperAdmin &&
                !tieneAccesoProyecto &&
                !tieneAccesoObligacion)
            {
                TempData["Error"] = "No tiene acceso al proyecto seleccionado.";
                return RedirectToAction("Index", "Home");
            }

            var filtros = new FiltrosDashboardHistoricoCumplimientoVm
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,

                IdCliente = idCliente,
                IdEmpresa = idEmpresa,
                IdCiudad = idCiudad,
                IdEstado = idEstado,
                IdTipoObligacion = idTipoObligacion,

                IdResponsable = idResponsable,
                IdElaborador = idElaborador,
                IdAutorizador = idAutorizador,
                IdAprobador = idAprobador,
                IdUsuarioVencimiento = idUsuarioVencimiento,

                Clasificacion = clasificacion
            };

            var vm =
                await _dashboardHistoricoCumplimientoService
                    .ObtenerDashboardAsync(
                        idProyecto.Value,
                        filtros);

            return View(vm);
        }

        public async Task<IActionResult> DetalleHistoricoCumplimiento(
            string? tipo,
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            int? idCliente,
            int? idEmpresa,
            int? idCiudad,
            int? idEstado,
            int? idTipoObligacion,
            int? idResponsable,
            int? idElaborador,
            int? idAutorizador,
            int? idAprobador,
            int? idUsuarioVencimiento,
            string? clasificacion,
            int? idEmpresaDetalle,
            int? idAutorizadorDetalle,
            int? idTipoObligacionDetalle)
        {
            var idProyecto =
                _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
            {
                TempData["Error"] =
                    "Debe seleccionar un proyecto.";

                return RedirectToAction(
                    "SeleccionarProyecto",
                    "Login");
            }

            var esSuperAdmin =
                User.HasClaim("EsSuperAdmin", "true");

            var tieneAccesoProyecto =
                await _seguridadService
                    .UsuarioTieneAccesoProyectoAsync(
                        idProyecto.Value,
                        "PROYECTO");

            var tieneAccesoObligacion =
                await _seguridadService
                    .UsuarioTieneAccesoProyectoAsync(
                        idProyecto.Value,
                        "OBLIGACION");

            if (!esSuperAdmin &&
                !tieneAccesoProyecto &&
                !tieneAccesoObligacion)
            {
                TempData["Error"] =
                    "No tiene acceso al proyecto seleccionado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var filtros =
                new FiltrosDashboardHistoricoCumplimientoVm
                {
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    IdCliente = idCliente,
                    IdEmpresa = idEmpresa,
                    IdCiudad = idCiudad,
                    IdEstado = idEstado,
                    IdTipoObligacion = idTipoObligacion,
                    IdResponsable = idResponsable,
                    IdElaborador = idElaborador,
                    IdAutorizador = idAutorizador,
                    IdAprobador = idAprobador,
                    IdUsuarioVencimiento =
                        idUsuarioVencimiento,
                    Clasificacion = clasificacion
                };

            var vm =
                await _dashboardHistoricoCumplimientoService
                    .ObtenerDetalleAsync(
                        idProyecto: idProyecto.Value,
                        tipo: tipo ?? "total",
                        filtros: filtros,
                        idEmpresaDetalle: idEmpresaDetalle,
                        idAutorizadorDetalle:
                            idAutorizadorDetalle,
                        idTipoObligacionDetalle:
                            idTipoObligacionDetalle);

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportarHistoricoCumplimientoExcel(
    DateOnly? fechaDesde,
    DateOnly? fechaHasta,
    int? idCliente,
    int? idEmpresa,
    int? idCiudad,
    int? idEstado,
    int? idTipoObligacion,
    int? idResponsable,
    int? idElaborador,
    int? idAutorizador,
    int? idAprobador,
    int? idUsuarioVencimiento,
    string? clasificacion)
        {
            var idProyecto =
                _seguridadService.ObtenerIdProyectoActivo();

            if (idProyecto == null)
            {
                TempData["Error"] =
                    "Debe seleccionar un proyecto.";

                return RedirectToAction(
                    "SeleccionarProyecto",
                    "Login");
            }

            var esSuperAdmin =
                User.HasClaim("EsSuperAdmin", "true");

            var tieneAccesoProyecto =
                await _seguridadService
                    .UsuarioTieneAccesoProyectoAsync(
                        idProyecto.Value,
                        "PROYECTO");

            var tieneAccesoObligacion =
                await _seguridadService
                    .UsuarioTieneAccesoProyectoAsync(
                        idProyecto.Value,
                        "OBLIGACION");

            if (!esSuperAdmin &&
                !tieneAccesoProyecto &&
                !tieneAccesoObligacion)
            {
                TempData["Error"] =
                    "No tiene acceso al proyecto seleccionado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var filtros =
                new FiltrosDashboardHistoricoCumplimientoVm
                {
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,

                    IdCliente = idCliente,
                    IdEmpresa = idEmpresa,
                    IdCiudad = idCiudad,
                    IdEstado = idEstado,
                    IdTipoObligacion = idTipoObligacion,

                    IdResponsable = idResponsable,
                    IdElaborador = idElaborador,
                    IdAutorizador = idAutorizador,
                    IdAprobador = idAprobador,
                    IdUsuarioVencimiento =
                        idUsuarioVencimiento,

                    Clasificacion = clasificacion
                };

            /*
             * Usamos el detalle tipo "total" para exportar todas
             * las obligaciones resultantes de los filtros generales.
             */
            var detalle =
                await _dashboardHistoricoCumplimientoService
                    .ObtenerDetalleAsync(
                        idProyecto: idProyecto.Value,
                        tipo: "total",
                        filtros: filtros);

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add(
                    "Histórico cumplimiento");

            ConstruirExcelHistoricoCumplimiento(
                worksheet,
                detalle);

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            var fechaArchivo =
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss",
                    CultureInfo.InvariantCulture);

            var nombreArchivo =
                $"Historico_Cumplimiento_{fechaArchivo}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreArchivo);
        }

        private static void ConstruirExcelHistoricoCumplimiento(
            IXLWorksheet worksheet,
            DetalleHistoricoCumplimientoVm detalle)
        {
            var filaActual = 1;

            /*
             * ============================================================
             * TÍTULO
             * ============================================================
             */

            worksheet.Cell(filaActual, 1)
                .Value =
                    "Dashboard Histórico de Cumplimiento";

            worksheet.Range(
                    filaActual,
                    1,
                    filaActual,
                    13)
                .Merge();

            worksheet.Cell(filaActual, 1)
                .Style.Font.Bold = true;

            worksheet.Cell(filaActual, 1)
                .Style.Font.FontSize = 16;

            worksheet.Cell(filaActual, 1)
                .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

            worksheet.Cell(filaActual, 1)
                .Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#0D6EFD");

            worksheet.Cell(filaActual, 1)
                .Style.Font.FontColor =
                    XLColor.White;

            worksheet.Row(filaActual)
                .Height = 26;

            filaActual += 2;

            /*
             * ============================================================
             * INFORMACIÓN GENERAL
             * ============================================================
             */

            worksheet.Cell(filaActual, 1)
                .Value = "Proyecto:";

            worksheet.Cell(filaActual, 2)
                .Value = detalle.NombreProyecto;

            worksheet.Cell(filaActual, 4)
                .Value = "Fecha inicial:";

            if (detalle.FechaDesde.HasValue)
            {
                worksheet.Cell(filaActual, 5)
                    .Value = detalle.FechaDesde.Value.ToDateTime(
                        TimeOnly.MinValue);

            }
            else
            {
                worksheet.Cell(filaActual, 5)
                    .Value = "No definida";
            }

            worksheet.Cell(filaActual, 7)
                .Value = "Fecha final:";

            if (detalle.FechaHasta.HasValue)
            {
                worksheet.Cell(filaActual, 8)
                    .Value = detalle.FechaHasta.Value.ToDateTime(
                        TimeOnly.MinValue);

            }
            else
            {
                worksheet.Cell(filaActual, 8)
                    .Value = "No definida";
            }

            worksheet.Cell(filaActual, 10)
                .Value = "Registros:";

            worksheet.Cell(filaActual, 11)
                .Value = detalle.Obligaciones.Count;

            worksheet.Cell(filaActual, 1)
                .Style.Font.Bold = true;

            worksheet.Cell(filaActual, 4)
                .Style.Font.Bold = true;

            worksheet.Cell(filaActual, 7)
                .Style.Font.Bold = true;

            worksheet.Cell(filaActual, 10)
                .Style.Font.Bold = true;

            worksheet.Cell(filaActual, 5)
                .Style.DateFormat.Format =
                    "dd/MM/yyyy";

            worksheet.Cell(filaActual, 8)
                .Style.DateFormat.Format =
                    "dd/MM/yyyy";

            filaActual += 2;

            /*
             * ============================================================
             * ENCABEZADOS
             * ============================================================
             */

            var encabezados = new[]
            {
                "ID obligación",
                "Obligación",
                "Código",
                "Cliente",
                "Empresa",
                "Tipo de obligación",
                "Ciudad",
                "Estado actual",
                "Fecha de vencimiento",
                "Fecha de cumplimiento",
                "Días de atraso",
                "Clasificación",
                "Autorizador(es)"
            };

            for (var columna = 0;
                 columna < encabezados.Length;
                 columna++)
            {
                worksheet.Cell(
                        filaActual,
                        columna + 1)
                    .Value =
                        encabezados[columna];
            }

            var rangoEncabezados =
                worksheet.Range(
                    filaActual,
                    1,
                    filaActual,
                    encabezados.Length);

            rangoEncabezados.Style.Font.Bold = true;

            rangoEncabezados.Style.Font.FontColor =
                XLColor.White;

            rangoEncabezados.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#212529");

            rangoEncabezados.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            rangoEncabezados.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            rangoEncabezados.Style.Border.BottomBorder =
                XLBorderStyleValues.Thin;

            worksheet.Row(filaActual)
                .Height = 28;

            var filaEncabezado = filaActual;

            filaActual++;

            /*
             * ============================================================
             * DATOS
             * ============================================================
             */

            foreach (var item in detalle.Obligaciones)
            {
                worksheet.Cell(filaActual, 1)
                    .Value = item.IdRegObl;

                worksheet.Cell(filaActual, 2)
                    .Value = item.Nombre;

                worksheet.Cell(filaActual, 3)
                    .Value =
                        string.IsNullOrWhiteSpace(
                            item.CodigoObligacion)
                            ? "-"
                            : item.CodigoObligacion;

                worksheet.Cell(filaActual, 4)
                    .Value = item.Cliente;

                worksheet.Cell(filaActual, 5)
                    .Value = item.Empresa;

                worksheet.Cell(filaActual, 6)
                    .Value = item.TipoObligacion;

                worksheet.Cell(filaActual, 7)
                    .Value =
                        string.IsNullOrWhiteSpace(
                            item.Ciudad)
                            ? "Sin ciudad"
                            : item.Ciudad;

                worksheet.Cell(filaActual, 8)
                    .Value = item.EstadoActual;

                worksheet.Cell(filaActual, 9)
                    .Value =
                        item.FechaVencimiento
                            .ToDateTime(
                                TimeOnly.MinValue);

                if (item.FechaCumplimiento.HasValue)
                {
                    worksheet.Cell(filaActual, 10)
                        .Value =
                            item.FechaCumplimiento
                                .Value
                                .ToDateTime(
                                    TimeOnly.MinValue);
                }
                else
                {
                    worksheet.Cell(filaActual, 10)
                        .Value =
                            "Sin cumplimiento";
                }

                worksheet.Cell(filaActual, 11)
                    .Value = item.DiasVencida;

                worksheet.Cell(filaActual, 12)
                    .Value =
                        ObtenerTextoClasificacionExcel(
                            item.Clasificacion);

                worksheet.Cell(filaActual, 13)
                    .Value =
                        item.AutorizadoresTexto;

                worksheet.Cell(filaActual, 9)
                    .Style.DateFormat.Format =
                        "dd/MM/yyyy";

                if (item.FechaCumplimiento.HasValue)
                {
                    worksheet.Cell(filaActual, 10)
                        .Style.DateFormat.Format =
                            "dd/MM/yyyy";
                }

                AplicarEstiloClasificacionExcel(
                    worksheet.Cell(filaActual, 12),
                    item.Clasificacion);

                filaActual++;
            }

            /*
             * ============================================================
             * TABLA Y FORMATO
             * ============================================================
             */

            if (detalle.Obligaciones.Count > 0)
            {
                var rangoDatos =
                    worksheet.Range(
                        filaEncabezado,
                        1,
                        filaActual - 1,
                        encabezados.Length);

                var tabla =
                    rangoDatos.CreateTable(
                        "TablaHistoricoCumplimiento");

                tabla.Theme =
                    XLTableTheme.TableStyleMedium2;

                tabla.ShowAutoFilter = true;

                worksheet.SheetView
                    .FreezeRows(filaEncabezado);

                worksheet.SheetView
                    .FreezeColumns(2);
            }

            /*
             * ============================================================
             * ANCHOS
             * ============================================================
             */

            worksheet.Column(1).Width = 13;
            worksheet.Column(2).Width = 38;
            worksheet.Column(3).Width = 16;
            worksheet.Column(4).Width = 28;
            worksheet.Column(5).Width = 28;
            worksheet.Column(6).Width = 25;
            worksheet.Column(7).Width = 18;
            worksheet.Column(8).Width = 18;
            worksheet.Column(9).Width = 20;
            worksheet.Column(10).Width = 21;
            worksheet.Column(11).Width = 15;
            worksheet.Column(12).Width = 25;
            worksheet.Column(13).Width = 35;

            worksheet.Columns(2, 13)
                .Style.Alignment.WrapText = true;

            worksheet.Columns(1, 13)
                .Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

            worksheet.Column(1)
                .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

            worksheet.Columns(9, 12)
                .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

            var rangoUsado = worksheet.RangeUsed();

            if (rangoUsado != null)
            {
                rangoUsado.Style.Border.InsideBorder =
                    XLBorderStyleValues.Hair;

                rangoUsado.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;
            }
        }

        private static string ObtenerTextoClasificacionExcel(
            string? clasificacion)
        {
            var valor =
                (clasificacion ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

            if (valor.Contains("OPORTUN"))
                return "Cumplida oportunamente";

            if (valor.Contains("ATRAS"))
                return "Cumplida con atraso";

            if (valor.Contains("VENCID") ||
                valor.Contains("PENDIENT"))
            {
                return "Sigue vencida";
            }

            return string.IsNullOrWhiteSpace(
                clasificacion)
                    ? "Sin clasificación"
                    : clasificacion;
        }

        private static void AplicarEstiloClasificacionExcel(
    IXLCell celda,
    string? clasificacion)
        {
            var valor =
                (clasificacion ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

            celda.Style.Font.Bold = true;

            celda.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            if (valor.Contains("OPORTUN"))
            {
                celda.Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#D1E7DD");

                celda.Style.Font.FontColor =
                    XLColor.FromHtml("#0F5132");

                return;
            }

            if (valor.Contains("ATRAS"))
            {
                celda.Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#FFF3CD");

                celda.Style.Font.FontColor =
                    XLColor.FromHtml("#664D03");

                return;
            }

            if (valor.Contains("VENCID") ||
                valor.Contains("PENDIENT"))
            {
                celda.Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#F8D7DA");

                celda.Style.Font.FontColor =
                    XLColor.FromHtml("#842029");

                return;
            }

            celda.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#E2E3E5");

            celda.Style.Font.FontColor =
                XLColor.FromHtml("#41464B");
        }

    }
}