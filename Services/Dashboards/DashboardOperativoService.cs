using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels.Dashboards;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Alertas.Services.Constantes;

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
                        uo.Rol.nombre == RolesSistema.Responsable &&
                        uo.activo));

            if (filtros.IdElaborador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdElaborador.Value &&
                        uo.Rol.nombre == RolesSistema.Elaborador &&
                        uo.activo));

            if (filtros.IdAutorizador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAutorizador.Value &&
                        uo.Rol.nombre == RolesSistema.Autorizador &&
                        uo.activo));

            if (filtros.IdAprobador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAprobador.Value &&
                        uo.Rol.nombre == RolesSistema.Aprobador &&
                        uo.activo));

            if (filtros.IdUsuarioVencimiento.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdUsuarioVencimiento.Value &&
                        uo.Rol.nombre == RolesSistema.Vencimiento &&
                        uo.activo));

            var total = await obligaciones.CountAsync();

            var vencidas = await obligaciones
                .CountAsync(o =>
                    o.fecha_venc_obl < hoy &&
                    o.fecha_vencimiento_ejecutada == null &&
                    !o.Estado.control_vencimiento);

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

            filtros.Responsables = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, RolesSistema.Responsable);
            filtros.Elaboradores = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, RolesSistema.Elaborador);
            filtros.Autorizadores = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, RolesSistema.Autorizador);
            filtros.Aprobadores = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, RolesSistema.Aprobador);
            filtros.UsuariosVencimiento = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, RolesSistema.Vencimiento);

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
                        uo.Rol.nombre == RolesSistema.Responsable &&
                        uo.activo));

            if (filtros.IdElaborador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdElaborador.Value &&
                        uo.Rol.nombre == RolesSistema.Elaborador &&
                        uo.activo));

            if (filtros.IdAutorizador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAutorizador.Value &&
                        uo.Rol.nombre == RolesSistema.Autorizador &&
                        uo.activo));

            if (filtros.IdAprobador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAprobador.Value &&
                        uo.Rol.nombre == RolesSistema.Aprobador &&
                        uo.activo));

            if (filtros.IdUsuarioVencimiento.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdUsuarioVencimiento.Value &&
                        uo.Rol.nombre == RolesSistema.Vencimiento &&
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
                    o.fecha_vencimiento_ejecutada == null &&
                    !o.Estado.control_vencimiento),

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
                    DiasAtrasoVencimiento =
                        o.fecha_venc_obl < hoy &&
                        o.fecha_vencimiento_ejecutada == null &&
                        !o.Estado.control_vencimiento
                            ? hoy.DayNumber - o.fecha_venc_obl.DayNumber
                            : null,

                    DiasAtrasoSeguimiento =
                        o.fecha_venc_seguimiento < hoy &&
                        o.fecha_seguimiento_ejecutada == null &&
                        !o.Estado.control_seguimiento
                            ? hoy.DayNumber - o.fecha_venc_seguimiento.DayNumber
                            : null,
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
            string nombreRol)
        {
            return await _context.UsuariosObligaciones
                .AsNoTracking()
                .Where(uo =>
                    uo.activo &&
                    uo.Rol.nombre == nombreRol &&
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

        public async Task<DashboardWorkflowProyectoVm> ObtenerDashboardWorkflowAsync(
            int idProyecto,
            FiltrosDashboardOperativoVm filtros)
        {
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
                        uo.Rol.nombre == RolesSistema.Responsable &&
                        uo.activo));

            if (filtros.IdElaborador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdElaborador.Value &&
                        uo.Rol.nombre == RolesSistema.Elaborador &&
                        uo.activo));

            if (filtros.IdAutorizador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAutorizador.Value &&
                        uo.Rol.nombre == RolesSistema.Autorizador &&
                        uo.activo));

            if (filtros.IdAprobador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAprobador.Value &&
                        uo.Rol.nombre == RolesSistema.Aprobador &&
                        uo.activo));

            if (filtros.IdUsuarioVencimiento.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdUsuarioVencimiento.Value &&
                        uo.Rol.nombre == RolesSistema.Vencimiento &&
                        uo.activo));

            var idsObligaciones = obligaciones.Select(o => o.id_reg_obl);

            var flujo = _context.HistOblFlujos
                .AsNoTracking()
                .Where(h => idsObligaciones.Contains(h.id_reg_obl));

            var totalTransiciones = await flujo.CountAsync();

            var obligacionesConMovimiento = await flujo
                .Select(h => h.id_reg_obl)
                .Distinct()
                .CountAsync();

            var automaticas = await flujo.CountAsync(h => h.es_automatico);
            var manuales = await flujo.CountAsync(h => !h.es_automatico);

            var transicionesPorMesRaw = await flujo
                .GroupBy(h => new
                {
                    h.fecha.Year,
                    h.fecha.Month
                })
                .Select(g => new
                {
                    Anio = g.Key.Year,
                    Mes = g.Key.Month,
                    Total = g.Count()
                })
                .OrderBy(x => x.Anio)
                .ThenBy(x => x.Mes)
                .ToListAsync();

            var transicionesPorMes = transicionesPorMesRaw
                .Select(x => new SerieDashboardVm
                {
                    Anio = x.Anio,
                    Mes = x.Mes,
                    Label = $"{x.Anio}-{x.Mes:00}",
                    Valor = x.Total
                })
                .ToList();

            await CargarCombosFiltrosWorkflowAsync(idProyecto, filtros);

            var hoyUtc = DateTime.UtcNow;

            var historialOrdenado = await flujo
                .Where(h => h.id_estado_destino != null)
                .OrderBy(h => h.id_reg_obl)
                .ThenBy(h => h.fecha)
                .Select(h => new
                {
                    h.id_reg_obl,
                    h.id_estado_destino,
                    h.fecha,
                    EstadoDestino = h.EstadoDestino!.nombre
                })
                .ToListAsync();

            var tiemposPorEstado = new List<(int IdEstado, string Estado, double Dias)>();

            var gruposPorObligacion = historialOrdenado
                .GroupBy(h => h.id_reg_obl);

            foreach (var grupo in gruposPorObligacion)
            {
                var movimientos = grupo
                    .OrderBy(h => h.fecha)
                    .ToList();

                for (int i = 0; i < movimientos.Count; i++)
                {
                    var actual = movimientos[i];

                    if (actual.id_estado_destino == null)
                        continue;

                    DateTime fechaFin;

                    if (i < movimientos.Count - 1)
                    {
                        fechaFin = movimientos[i + 1].fecha;
                    }
                    else
                    {
                        fechaFin = hoyUtc;
                    }

                    var dias = (fechaFin - actual.fecha).TotalDays;

                    if (dias < 0)
                        continue;

                    tiemposPorEstado.Add((
                        actual.id_estado_destino.Value,
                        actual.EstadoDestino,
                        dias
                    ));
                }
            }

            var estadosProyecto = await _context.Estados
            .AsNoTracking()
            .Where(e => e.id_proyecto == idProyecto && e.activo)
            .OrderBy(e => e.orden)
            .ToListAsync();

            var estadoInicial = estadosProyecto
                .OrderBy(e => e.orden)
                .FirstOrDefault();

            var idsEstadosFinales = estadosProyecto
                .Where(e =>
                    e.nombre.ToLower().Contains("cerrada") ||
                    e.nombre.ToLower().Contains("anulada"))
                .Select(e => e.id_estado)
                .ToHashSet();

            var tiempoPromedioPorEstado = tiemposPorEstado
                .Join(
                    estadosProyecto,
                    t => t.IdEstado,
                    e => e.id_estado,
                    (t, e) => new
                    {
                        Tiempo = t,
                        Estado = e
                    })
                .Where(x =>
                    estadoInicial == null || x.Estado.id_estado != estadoInicial.id_estado)
                .Where(x =>
                    !idsEstadosFinales.Contains(x.Estado.id_estado))
                .GroupBy(x => new
                {
                    x.Tiempo.IdEstado,
                    x.Tiempo.Estado,
                    x.Estado.orden
                })
                .Select(g => new TiempoEstadoWorkflowVm
                {
                    IdEstado = g.Key.IdEstado,
                    Estado = g.Key.Estado,
                    Orden = g.Key.orden,
                    DiasPromedio = Math.Round(
                        (decimal)g.Average(x => x.Tiempo.Dias), 2),
                    TotalMediciones = g.Count()
                })
                .OrderBy(x => x.Orden)
                .ToList();

            var mayorCuelloBotella = tiempoPromedioPorEstado
                .OrderByDescending(x => x.DiasPromedio)
                .FirstOrDefault();

            return new DashboardWorkflowProyectoVm
            {
                IdProyecto = idProyecto,
                NombreProyecto = proyecto.nombre,

                TotalTransiciones = totalTransiciones,
                ObligacionesConMovimiento = obligacionesConMovimiento,
                TransicionesAutomaticas = automaticas,
                TransicionesManuales = manuales,
                TiempoPromedioPorEstado = tiempoPromedioPorEstado,
                MayorCuelloBotella = mayorCuelloBotella,

                Filtros = filtros,

                TransicionesPorEstadoDestino = await flujo
                    .Where(h => h.EstadoDestino != null)
                    .GroupBy(h => new
                    {
                        h.id_estado_destino,
                        h.EstadoDestino!.nombre
                    })
                    .Select(g => new SerieDashboardVm
                    {
                        Id = g.Key.id_estado_destino,
                        Label = g.Key.nombre,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .ToListAsync(),

                TransicionesPorAccion = await flujo
                    .GroupBy(h => h.accion ?? "Sin acción")
                    .Select(g => new SerieDashboardVm
                    {
                        Label = g.Key,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .Take(10)
                    .ToListAsync(),

                TransicionesPorUsuario = await flujo
                    .GroupBy(h => new
                    {
                        h.id_usuario,
                        h.Usuario.nombre
                    })
                    .Select(g => new SerieDashboardVm
                    {
                        Id = g.Key.id_usuario,
                        Label = g.Key.nombre,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .Take(10)
                    .ToListAsync(),

                TransicionesPorMes = transicionesPorMes
            };
        }

        private async Task CargarCombosFiltrosWorkflowAsync(
            int idProyecto,
            FiltrosDashboardOperativoVm filtros)
        {
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

            filtros.Responsables = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, RolesSistema.Responsable);
            filtros.Elaboradores = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, RolesSistema.Elaborador);
            filtros.Autorizadores = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, RolesSistema.Autorizador);
            filtros.Aprobadores = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, RolesSistema.Aprobador);
            filtros.UsuariosVencimiento = await ObtenerUsuariosPorRolProyectoAsync(idProyecto, RolesSistema.Vencimiento);
        }

        public async Task<DashboardVencimientosProyectoVm> ObtenerDashboardVencimientosAsync(
            int idProyecto,
            FiltrosDashboardOperativoVm filtros)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var limite7 = hoy.AddDays(7);
            var limite30 = hoy.AddDays(30);

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
                        uo.Rol.nombre == RolesSistema.Responsable &&
                        uo.activo));

            if (filtros.IdElaborador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdElaborador.Value &&
                        uo.Rol.nombre == RolesSistema.Elaborador &&
                        uo.activo));

            if (filtros.IdAutorizador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAutorizador.Value &&
                        uo.Rol.nombre == RolesSistema.Autorizador &&
                        uo.activo));

            if (filtros.IdAprobador.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdAprobador.Value &&
                        uo.Rol.nombre == RolesSistema.Aprobador &&
                        uo.activo));

            if (filtros.IdUsuarioVencimiento.HasValue)
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.id_usuario == filtros.IdUsuarioVencimiento.Value &&
                        uo.Rol.nombre == RolesSistema.Vencimiento &&
                        uo.activo));

            var idsEstadosFinales = await _context.Estados
                .AsNoTracking()
                .Where(e =>
                    e.id_proyecto == idProyecto &&
                    e.activo &&
                    (
                        e.nombre.ToLower().Contains("cerrad") ||
                        e.nombre.ToLower().Contains("anulad") ||
                        e.nombre.ToLower().Contains("anulac")
                    ))
                .Select(e => e.id_estado)
                .ToListAsync();

            var pendientes = obligaciones
                .Where(o =>
                    o.fecha_vencimiento_ejecutada == null &&
                    !o.Estado.control_vencimiento &&
                    !idsEstadosFinales.Contains(o.id_estado));

            var vencidasQuery = pendientes
                .Where(o => o.fecha_venc_obl < hoy);

            var vencidasRaw = await vencidasQuery
                .Select(o => new
                {
                    o.fecha_venc_obl
                })
                .ToListAsync();

            var atrasos = vencidasRaw
                .Select(x => hoy.DayNumber - x.fecha_venc_obl.DayNumber)
                .Where(x => x > 0)
                .ToList();

            var vencimientosPorMesRaw = await pendientes
                .GroupBy(o => new { o.anio, o.mes })
                .Select(g => new
                {
                    Anio = g.Key.anio,
                    Mes = g.Key.mes,
                    Total = g.Count()
                })
                .OrderBy(x => x.Anio)
                .ThenBy(x => x.Mes)
                .ToListAsync();

            var vencimientosPorMes = vencimientosPorMesRaw
                .Select(x => new SerieDashboardVm
                {
                    Anio = x.Anio,
                    Mes = x.Mes,
                    Label = $"{x.Anio}-{x.Mes:00}",
                    Valor = x.Total
                })
                .ToList();

            await CargarCombosFiltrosWorkflowAsync(idProyecto, filtros);

            var totalVencidas = await vencidasQuery.CountAsync();
            var totalVencen7 = await pendientes.CountAsync(o =>
                o.fecha_venc_obl >= hoy &&
                o.fecha_venc_obl <= limite7);

            string nivelRiesgo;
            string riesgoCssClass;
            string riesgoIcono;

            if (totalVencidas > 0)
            {
                nivelRiesgo = "Alto";
                riesgoCssClass = "danger";
                riesgoIcono = "bi-exclamation-triangle";
            }
            else if (totalVencen7 > 0)
            {
                nivelRiesgo = "Medio";
                riesgoCssClass = "warning";
                riesgoIcono = "bi-exclamation-circle";
            }
            else
            {
                nivelRiesgo = "Bajo";
                riesgoCssClass = "success";
                riesgoIcono = "bi-check-circle";
            }

            return new DashboardVencimientosProyectoVm
            {
                IdProyecto = idProyecto,
                NombreProyecto = proyecto.nombre,
                Filtros = filtros,

                NivelRiesgo = nivelRiesgo,
                RiesgoCssClass = riesgoCssClass,
                RiesgoIcono = riesgoIcono,

                TotalPendientes = await pendientes.CountAsync(),
                Vencidas = await vencidasQuery.CountAsync(),
                Vencen7Dias = await pendientes.CountAsync(o =>
                    o.fecha_venc_obl >= hoy &&
                    o.fecha_venc_obl <= limite7),
                Vencen30Dias = await pendientes.CountAsync(o =>
                    o.fecha_venc_obl >= hoy &&
                    o.fecha_venc_obl <= limite30),

                AtrasoPromedioDias = atrasos.Any()
                    ? Math.Round((decimal)atrasos.Average(), 2)
                    : 0,

                MayorAtrasoDias = atrasos.Any()
                    ? atrasos.Max()
                    : 0,

                VencimientosPorMes = vencimientosPorMes,

                VencidasPorEstado = await vencidasQuery
                    .GroupBy(o => new
                    {
                        o.id_estado,
                        o.Estado.nombre
                    })
                    .Select(g => new SerieDashboardVm
                    {
                        Id = g.Key.id_estado,
                        Label = g.Key.nombre,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .ToListAsync(),

                TopAutorizadoresVencidas = await vencidasQuery
                    .SelectMany(o => o.UsuariosObligaciones
                        .Where(uo =>
                            uo.activo &&
                            uo.Rol != null &&
                            uo.Rol.nombre == "Autorizador")
                        .Select(uo => new
                        {
                            uo.id_usuario,
                            uo.Usuario.nombre
                        }))
                    .GroupBy(x => new
                    {
                        x.id_usuario,
                        x.nombre
                    })
                    .Select(g => new SerieDashboardVm
                    {
                        Id = g.Key.id_usuario,
                        Label = g.Key.nombre,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .Take(10)
                    .ToListAsync(),

                VencidasPorEmpresa = await vencidasQuery
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
                    .ToListAsync(),

                VencidasPorTipoObligacion = await vencidasQuery
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
                    .ToListAsync()
            };
        }
    }
}