using Alertas.Data;
using Alertas.ViewModels.Dashboards;
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

        public async Task<DashboardOperativoProyectoVm> ObtenerDashboardProyectoAsync(int idProyecto)
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

            var aprobadas = await obligaciones
                .CountAsync(o => o.aprobado == true);

            var cerradas = await obligaciones
                .CountAsync(o => o.Estado.nombre.ToLower() == "cerrada");

            var presentadas = await obligaciones
                .CountAsync(o => o.Estado.nombre.ToLower() == "presentada");

            var enElaboracion = await obligaciones
                .CountAsync(o => o.Estado.nombre.ToLower() == "en elaboración");

            var enSeguimiento = await obligaciones
                .CountAsync(o => o.Estado.nombre.ToLower() == "en seguimiento");

            var diasPromedioCierre = await obligaciones
                .Where(o => o.fecha_creac.HasValue && o.fecha_aprobado_final.HasValue)
                .Select(o => o.fecha_aprobado_final.Value.DayNumber - o.fecha_creac.Value.DayNumber)
                .DefaultIfEmpty()
                .AverageAsync();

            var vm = new DashboardOperativoProyectoVm
            {
                IdProyecto = idProyecto,
                NombreProyecto = proyecto.nombre,

                TotalObligaciones = total,
                ObligacionesVencidas = vencidas,
                ProximasAVencer = proximasAVencer,

                EnElaboracion = enElaboracion,
                EnSeguimiento = enSeguimiento,
                Presentadas = presentadas,
                Cerradas = cerradas,

                PorcentajeAprobadas = total == 0 ? 0 : Math.Round((decimal)aprobadas * 100 / total, 2),
                PorcentajeCumplimiento = total == 0 ? 0 : Math.Round((decimal)cerradas * 100 / total, 2),
                DiasPromedioCierre = Math.Round((decimal)diasPromedioCierre, 2),

                ObligacionesPorEstado = await obligaciones
                    .GroupBy(o => o.Estado.nombre)
                    .Select(g => new SerieDashboardVm
                    {
                        Label = g.Key,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .ToListAsync(),

                ObligacionesPorMes = await obligaciones
                    .GroupBy(o => new { o.anio, o.mes })
                    .Select(g => new SerieDashboardVm
                    {
                        Label = g.Key.anio + "-" + g.Key.mes.ToString("00"),
                        Valor = g.Count()
                    })
                    .OrderBy(x => x.Label)
                    .ToListAsync(),

                TendenciaCumplimientoMensual = await obligaciones
                    .Where(o => o.fecha_aprobado_final.HasValue)
                    .GroupBy(o => new
                    {
                        Anio = o.fecha_aprobado_final.Value.Year,
                        Mes = o.fecha_aprobado_final.Value.Month
                    })
                    .Select(g => new SerieDashboardVm
                    {
                        Label = g.Key.Anio + "-" + g.Key.Mes.ToString("00"),
                        Valor = g.Count()
                    })
                    .OrderBy(x => x.Label)
                    .ToListAsync(),

                TopTiposObligacion = await obligaciones
                    .GroupBy(o => o.TipoObligacion.nombre)
                    .Select(g => new SerieDashboardVm
                    {
                        Label = g.Key,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .Take(10)
                    .ToListAsync(),

                ObligacionesPorCiudad = await obligaciones
                    .GroupBy(o => o.Ciudad != null ? o.Ciudad.nombre : "Sin ciudad")
                    .Select(g => new SerieDashboardVm
                    {
                        Label = g.Key,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .Take(10)
                    .ToListAsync(),

                ObligacionesPorEmpresaCliente = await obligaciones
                    .GroupBy(o => o.Empresa.nombre)
                    .Select(g => new SerieDashboardVm
                    {
                        Label = g.Key,
                        Valor = g.Count()
                    })
                    .OrderByDescending(x => x.Valor)
                    .Take(10)
                    .ToListAsync()
            };

            return vm;
        }
    }
}