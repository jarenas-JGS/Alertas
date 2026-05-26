using Alertas.Data;
using Alertas.Services;
using Alertas.Services.Dashboards;
using Alertas.ViewModels.Dashboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Alertas.Controllers
{
    [Authorize]
    public class DashboardsController : Controller
    {
        private readonly IDashboardOperativoService _dashboardOperativoService;
        private readonly SeguridadService _seguridadService;
        private readonly ApplicationDbContext _context;

        public DashboardsController(
            IDashboardOperativoService dashboardOperativoService,
            SeguridadService seguridadService,
            ApplicationDbContext context)
        {
            _dashboardOperativoService = dashboardOperativoService;
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
                return RedirectToAction("DetalleOperativo");

            var idUsuario = _seguridadService.ObtenerIdUsuario();
            var esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");
            var accesoProyecto = _seguridadService.EsAccesoProyectoActivoPorProyecto();

            if (!esSuperAdmin && !accesoProyecto)
            {
                var participa = entidad.UsuariosObligaciones.Any(uo =>
                    uo.id_usuario == idUsuario &&
                    uo.activo);

                if (!participa)
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
    }
}