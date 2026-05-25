using Alertas.Services;
using Alertas.Services.Dashboards;
using Alertas.ViewModels.Dashboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alertas.Controllers
{
    [Authorize]
    public class DashboardsController : Controller
    {
        private readonly IDashboardOperativoService _dashboardOperativoService;
        private readonly SeguridadService _seguridadService;

        public DashboardsController(
            IDashboardOperativoService dashboardOperativoService,
            SeguridadService seguridadService)
        {
            _dashboardOperativoService = dashboardOperativoService;
            _seguridadService = seguridadService;
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
    }
}