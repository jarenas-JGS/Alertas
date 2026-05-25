using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels.Dashboards;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Services.Dashboards
{
    public class DashboardOperativoService : IDashboardOperativoService
    {
        private readonly ApplicationDbContext _context;

        public DashboardOperativoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardOperativoProyectoVm> ObtenerDashboardProyectoAsync(
            int idProyecto,
            FiltrosDashboardOperativoVm filtros)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var fechaLimiteProximas = hoy.AddDays(30);

            var proyecto = await _context.Proyectos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.id_proyecto == idProyecto);

            if (proyecto == null)
                throw new Exception("Proyecto no encontrado.");

            var obligaciones = _context.RegObls
                .AsNoTracking()
                .Where(o => o.id_proyecto == idProyecto);

            if (filtros.IdCliente.HasValue)
                obligaciones = obligaciones.Where(o => o.id_cliente == filtros.IdCliente.Value);

            if (filtros.IdEmpresa.HasValue)
                obligaciones = obligaciones.Where(o => o.id_empresa == filtros.IdEmpresa.Value);

            if (filtros.IdCiudad.HasValue)
                obligaciones = obligaciones.Where(o => o.id_ciudad == filtros.IdCiudad.Value);

            if (filtros.IdEstado.HasValue)
                obligaciones = obligaciones.Where(o => o.id_estado == filtros.IdEstado.Value);

            if (filtros.IdTipoObligacion.HasValue)
                obligaciones = obligaciones.Where(o => o.id_tipo_obligacion == filtros.IdTipoObligacion.Value);

            if (filtros.Anio.HasValue)
                obligaciones = obligaciones.Where(o => o.anio == filtros.Anio.Value);

            if (filtros.Mes.HasValue)
                obligaciones = obligaciones.Where(o => o.mes == filtros.Mes.Value);

            if (filtros.IdResponsable.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdResponsable.Value &&
                        uo.id_rol == 1 &&
                        uo.activo));

            if (filtros.IdElaborador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdElaborador.Value &&
                        uo.id_rol == 2 &&
                        uo.activo));

            if (filtros.IdAutorizador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAutorizador.Value &&
                        uo.id_rol == 3 &&
                        uo.activo));

            if (filtros.IdAprobador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAprobador.Value &&
                        uo.id_rol == 4 &&
                        uo.activo));

            if (filtros.IdUsuarioVencimiento.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdUsuarioVencimiento.Value &&
                        uo.id_rol == 5 &&
                        uo.activo));

            var total = await obligaciones.CountAsync();

            var vencidas = await obligaciones
                .CountAsync(o =>
                    o.fecha_venc_obl < hoy &&
                    (o.aprobado == null || o.aprobado == false));

            var proximasAVencer = await obligaciones
                .CountAsync(o =>
                    o.fecha_venc_obl >= hoy &&
                    o.fecha_venc_obl <= fechaLimiteProximas &&
                    (o.aprobado == null || o.aprobado == false));

            var estadosControlVencimiento = await _context.Estados
                .AsNoTracking()
                .Where(e =>
                    e.id_proyecto == idProyecto &&
                    e.control_vencimiento &&
                    e.activo)
                .Select(e => e.id_estado)
                .ToListAsync();

            var cumplidas = await obligaciones
                .CountAsync(o => estadosControlVencimiento.Contains(o.id_estado));

            var fechasCierre = await obligaciones
                .Where(o => o.fecha_creac.HasValue && o.fecha_aprobado_final.HasValue)
                .Select(o => new
                {
                    FechaCreacion = o.fecha_creac!.Value,
                    FechaAprobacion = o.fecha_aprobado_final!.Value
                })
                .ToListAsync();

            var diasPromedioCierre = fechasCierre.Any()
                ? fechasCierre
                    .Average(x => x.FechaAprobacion.DayNumber - x.FechaCreacion.DayNumber)
                : 0;

            var obligacionesPorMesRaw = await obligaciones
                .GroupBy(o => new
                {
                    o.anio,
                    o.mes
                })
                .Select(g => new
                {
                    Anio = g.Key.anio,
                    Mes = g.Key.mes,
                    Total = g.Count()
                })
                .OrderBy(x => x.Anio)
                .ThenBy(x => x.Mes)
                .ToListAsync();

            var obligacionesPorMes = obligacionesPorMesRaw
                .Select(x => new SerieDashboardVm
                {
                    Anio = x.Anio,
                    Mes = x.Mes,
                    Label = $"{x.Anio}-{x.Mes:00}",
                    Valor = x.Total
                })
                .ToList();

            var tendenciaCumplimientoRaw = await obligaciones
                .Where(o => o.fecha_aprobado_final.HasValue)
                .GroupBy(o => new
                {
                    Anio = o.fecha_aprobado_final!.Value.Year,
                    Mes = o.fecha_aprobado_final!.Value.Month
                })
                .Select(g => new
                {
                    Anio = g.Key.Anio,
                    Mes = g.Key.Mes,
                    Total = g.Count()
                })
                .OrderBy(x => x.Anio)
                .ThenBy(x => x.Mes)
                .ToListAsync();

            var tendenciaCumplimientoMensual = tendenciaCumplimientoRaw
                .Select(x => new SerieDashboardVm
                {
                    Label = $"{x.Anio}-{x.Mes:00}",
                    Valor = x.Total
                })
                .ToList();

            filtros.Clientes = await _context.RegObls
                .AsNoTracking()
                .Where(o => o.id_proyecto == idProyecto && o.Cliente != null)
                .Select(o => new SelectListItem
                {
                    Value = o.id_cliente.ToString(),
                    Text = o.Cliente.nombre
                })
                .Distinct()
                .OrderBy(x => x.Text)
                .ToListAsync();

            filtros.Empresas = await _context.RegObls
                .AsNoTracking()
                .Where(o => o.id_proyecto == idProyecto)
                .Select(o => new SelectListItem
                {
                    Value = o.id_empresa.ToString(),
                    Text = o.Empresa.nombre
                })
                .Distinct()
                .OrderBy(x => x.Text)
                .ToListAsync();

            filtros.Ciudades = await _context.RegObls
                .AsNoTracking()
                .Where(o => o.id_proyecto == idProyecto && o.id_ciudad != null)
                .Select(o => new SelectListItem
                {
                    Value = o.id_ciudad!.Value.ToString(),
                    Text = o.Ciudad!.nombre
                })
                .Distinct()
                .OrderBy(x => x.Text)
                .ToListAsync();

            filtros.Estados = await _context.Estados
                .AsNoTracking()
                .Where(e => e.id_proyecto == idProyecto && e.activo)
                .OrderBy(e => e.orden)
                .Select(e => new SelectListItem
                {
                    Value = e.id_estado.ToString(),
                    Text = e.nombre
                })
                .ToListAsync();

            filtros.TiposObligacion = await _context.RegObls
                .AsNoTracking()
                .Where(o => o.id_proyecto == idProyecto)
                .Select(o => new SelectListItem
                {
                    Value = o.id_tipo_obligacion.ToString(),
                    Text = o.TipoObligacion.nombre
                })
                .Distinct()
                .OrderBy(x => x.Text)
                .ToListAsync();

            filtros.Anios = await _context.RegObls
                .AsNoTracking()
                .Where(o => o.id_proyecto == idProyecto)
                .Select(o => o.anio)
                .Distinct()
                .OrderByDescending(x => x)
                .Select(x => new SelectListItem
                {
                    Value = x.ToString(),
                    Text = x.ToString()
                })
                .ToListAsync();

            filtros.Meses = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(),
                    Text = new DateTime(2000, m, 1).ToString("MMMM")
                })
                .ToList();

            var conteoPorEstado = await obligaciones
                .GroupBy(o => o.id_estado)
                .Select(g => new
                {
                    IdEstado = g.Key,
                    Total = g.Count()
                })
                .ToListAsync();

            var estadosProyecto = await _context.Estados
                .AsNoTracking()
                .Where(e => e.id_proyecto == idProyecto && e.activo)
                .OrderBy(e => e.orden)
                .Select(e => new
                {
                    e.id_estado,
                    e.nombre,
                    e.orden,
                    e.control_vencimiento,
                    e.control_seguimiento,
                    e.bloquea
                })
                .ToListAsync();

            var resumenPorEstados = estadosProyecto
                .Select(e =>
                {
                    var estadoVm = new EstadoResumenDashboardVm
                    {
                        IdEstado = e.id_estado,
                        NombreEstado = e.nombre,
                        Orden = e.orden,
                        ControlVencimiento = e.control_vencimiento,
                        ControlSeguimiento = e.control_seguimiento,
                        Bloquea = e.bloquea,
                        Total = conteoPorEstado
                            .FirstOrDefault(c => c.IdEstado == e.id_estado)?.Total ?? 0
                    };

                    estadoVm.ColorHex = ObtenerColorEstadoHex(estadoVm);
                    estadoVm.CssClass = ObtenerCssEstado(estadoVm);
                    estadoVm.Icono = ObtenerIconoEstado(estadoVm);
                    estadoVm.ColorHex20 = estadoVm.ColorHex + "22";

                    return estadoVm;
                })
                .ToList();

            var aprobadasCumplidas = await obligaciones
                .CountAsync(o =>
                    estadosControlVencimiento.Contains(o.id_estado) &&
                    o.aprobado == true);

            filtros.Responsables = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, 1);
            filtros.Elaboradores = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, 2);
            filtros.Autorizadores = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, 3);
            filtros.Aprobadores = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, 4);
            filtros.UsuariosVencimiento = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, 5);

            var vm = new DashboardOperativoProyectoVm
            {
                IdProyecto = idProyecto,
                NombreProyecto = proyecto.nombre,

                TotalObligaciones = total,
                ObligacionesVencidas = vencidas,
                ProximasAVencer = proximasAVencer,

                ResumenPorEstados = resumenPorEstados,
                Filtros = filtros,

                ObligacionesPorMes = obligacionesPorMes,
                TendenciaCumplimientoMensual = tendenciaCumplimientoMensual,

                PorcentajeCumplimiento = total == 0
                ? 0
                : Math.Round((decimal)cumplidas * 100 / total, 2),

                PorcentajeAprobadas = cumplidas == 0
                ? 0
                : Math.Round((decimal)aprobadasCumplidas * 100 / cumplidas, 2),

                ObligacionesPorEstado = await obligaciones
                    .GroupBy(o => o.Estado.nombre)
                    .Select(g => new SerieDashboardVm
                    {
                        Label = g.Key,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .ToListAsync(),

                TopTiposObligacion = await obligaciones
                    .GroupBy(o => new
                    {
                        o.id_tipo_obligacion,
                        o.TipoObligacion.nombre
                    })
                    .Select(g => new SerieDashboardVm
                    {
                        Id = g.Key.id_tipo_obligacion,
                        Label = g.Key.nombre,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .Take(10)
                    .ToListAsync(),

                ObligacionesPorCiudad = await obligaciones
                    .Where(o => o.id_ciudad != null)
                    .GroupBy(o => new
                    {
                        IdCiudad = o.id_ciudad!.Value,
                        o.Ciudad!.nombre
                    })
                    .Select(g => new SerieDashboardVm
                    {
                        Id = g.Key.IdCiudad,
                        Label = g.Key.nombre,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .Take(10)
                    .ToListAsync(),

                ObligacionesPorEmpresaCliente = await obligaciones
                    .GroupBy(o => new
                    {
                        o.id_empresa,
                        o.Empresa.nombre
                    })
                    .Select(g => new SerieDashboardVm
                    {
                        Id = g.Key.id_empresa,
                        Label = g.Key.nombre,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .Take(10)
                    .ToListAsync()
            };

            return vm;
        }

        public async Task<DetalleDashboardOperativoVm> ObtenerDetalleOperativoAsync(
            int idProyecto,
            string tipo,
            FiltrosDashboardOperativoVm filtros,
            int? idEstadoDetalle)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var fechaLimiteProximas = hoy.AddDays(30);

            var proyecto = await _context.Proyectos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.id_proyecto == idProyecto);

            if (proyecto == null)
                throw new Exception("Proyecto no encontrado.");

            var obligaciones = _context.RegObls
                .AsNoTracking()
                .Where(o => o.id_proyecto == idProyecto);

            if (filtros.IdCliente.HasValue)
                obligaciones = obligaciones.Where(o => o.id_cliente == filtros.IdCliente.Value);

            if (filtros.IdEmpresa.HasValue)
                obligaciones = obligaciones.Where(o => o.id_empresa == filtros.IdEmpresa.Value);

            if (filtros.IdCiudad.HasValue)
                obligaciones = obligaciones.Where(o => o.id_ciudad == filtros.IdCiudad.Value);

            if (filtros.IdEstado.HasValue)
                obligaciones = obligaciones.Where(o => o.id_estado == filtros.IdEstado.Value);

            if (filtros.IdTipoObligacion.HasValue)
                obligaciones = obligaciones.Where(o => o.id_tipo_obligacion == filtros.IdTipoObligacion.Value);

            if (filtros.Anio.HasValue)
                obligaciones = obligaciones.Where(o => o.anio == filtros.Anio.Value);

            if (filtros.Mes.HasValue)
                obligaciones = obligaciones.Where(o => o.mes == filtros.Mes.Value);

            if (filtros.IdResponsable.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdResponsable.Value &&
                        uo.id_rol == 1 &&
                        uo.activo));

            if (filtros.IdElaborador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdElaborador.Value &&
                        uo.id_rol == 2 &&
                        uo.activo));

            if (filtros.IdAutorizador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAutorizador.Value &&
                        uo.id_rol == 3 &&
                        uo.activo));

            if (filtros.IdAprobador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAprobador.Value &&
                        uo.id_rol == 4 &&
                        uo.activo));

            if (filtros.IdUsuarioVencimiento.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdUsuarioVencimiento.Value &&
                        uo.id_rol == 5 &&
                        uo.activo));

            tipo = (tipo ?? string.Empty).Trim().ToLower();

            string titulo = tipo switch
            {
                "total" => "Total de obligaciones",
                "vencidas" => "Obligaciones vencidas",
                "proximas" => "Obligaciones próximas a vencer",
                "estado" => "Obligaciones por estado",
                _ => "Detalle de obligaciones"
            };

            obligaciones = tipo switch
            {
                "vencidas" => obligaciones.Where(o =>
                    o.fecha_venc_obl < hoy &&
                    (o.aprobado == null || o.aprobado == false)),

                "proximas" => obligaciones.Where(o =>
                    o.fecha_venc_obl >= hoy &&
                    o.fecha_venc_obl <= fechaLimiteProximas &&
                    (o.aprobado == null || o.aprobado == false)),

                "estado" when idEstadoDetalle.HasValue => obligaciones.Where(o =>
                    o.id_estado == idEstadoDetalle.Value),

                "total" => obligaciones,

                _ => obligaciones
            };

            var items = await obligaciones
                .OrderBy(o => o.fecha_venc_obl)
                .Select(o => new ItemDetalleDashboardOperativoVm
                {
                    IdRegObl = o.id_reg_obl,
                    Nombre = o.nombre,
                    CodigoObligacion = o.cod_obligacion,
                    Cliente = o.Cliente.nombre,
                    Empresa = o.Empresa.nombre,
                    TipoObligacion = o.TipoObligacion.nombre,
                    Estado = o.Estado.nombre,
                    FechaVencimiento = o.fecha_venc_obl.ToString("dd/MM/yyyy"),
                    FechaSeguimiento = o.fecha_venc_seguimiento.ToString("dd/MM/yyyy"),
                    DiasAtrasoVencimiento = o.dias_atraso_vencimiento,
                    DiasAtrasoSeguimiento = o.dias_atraso_seguimiento,
                    Aprobado = o.aprobado == true
                })
                .ToListAsync();

            return new DetalleDashboardOperativoVm
            {
                Tipo = tipo,
                Titulo = titulo,
                NombreProyecto = proyecto.nombre,
                Obligaciones = items
            };
        }

        private async Task<List<SelectListItem>> ObtenerUsuariosPorRolProyectoAsync(
            int idProyecto,
            int idRol)
        {
            return await _context.UsuariosObligaciones
                .AsNoTracking()
                .Where(uo =>
                    uo.activo &&
                    uo.id_rol == idRol &&
                    uo.RegObl.id_proyecto == idProyecto)
                .Select(uo => new SelectListItem
                {
                    Value = uo.id_usuario.ToString(),
                    Text = uo.Usuario.nombre
                })
                .Distinct()
                .OrderBy(x => x.Text)
                .ToListAsync();
        }

        private static string ObtenerColorEstadoHex(EstadoResumenDashboardVm estado)
        {
            if (estado.Bloquea)
                return "#dc3545"; // rojo

            if (estado.ControlVencimiento)
                return "#198754"; // verde

            if (estado.ControlSeguimiento)
                return "#0d6efd"; // azul

            return estado.Orden switch
            {
                1 => "#6c757d", // gris
                2 => "#ffc107", // amarillo
                3 => "#6610f2", // morado
                4 => "#0dcaf0", // cyan
                _ => "#6c757d"
            };
        }

        private static string ObtenerCssEstado(EstadoResumenDashboardVm estado)
        {
            if (estado.Bloquea)
                return "danger";

            if (estado.ControlVencimiento)
                return "success";

            if (estado.ControlSeguimiento)
                return "primary";

            return estado.Orden switch
            {
                1 => "secondary",
                2 => "warning",
                3 => "info",
                _ => "secondary"
            };
        }

        private static string ObtenerIconoEstado(EstadoResumenDashboardVm estado)
        {
            if (estado.Bloquea)
                return "bi-lock";

            if (estado.ControlVencimiento)
                return "bi-calendar2-check";

            if (estado.ControlSeguimiento)
                return "bi-clipboard-check";

            return estado.Orden switch
            {
                1 => "bi-circle",
                2 => "bi-pencil-square",
                3 => "bi-arrow-repeat",
                _ => "bi-circle"
            };
        }
    }
}