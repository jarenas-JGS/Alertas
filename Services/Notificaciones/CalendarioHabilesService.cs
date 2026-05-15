using Alertas.Data;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Services.Notificaciones
{
    public class CalendarioHabilesService : ICalendarioHabilesService
    {
        private readonly ApplicationDbContext _context;

        public CalendarioHabilesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> EsDiaHabilAsync(DateOnly fecha)
        {
            if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday)
                return false;

            bool esFestivo = await _context.Festivos
                .AnyAsync(f => f.fecha == fecha && f.activo);

            return !esFestivo;
        }

        public async Task<int> CalcularDiasHabilesAsync(DateOnly fechaInicio, DateOnly fechaFin)
        {
            if (fechaInicio == fechaFin)
                return 0;

            bool negativo = fechaFin < fechaInicio;

            DateOnly desde = negativo ? fechaFin : fechaInicio;
            DateOnly hasta = negativo ? fechaInicio : fechaFin;

            var festivos = await _context.Festivos
                .Where(f => f.activo && f.fecha > desde && f.fecha <= hasta)
                .Select(f => f.fecha)
                .ToListAsync();

            var festivosSet = festivos.ToHashSet();

            int diasHabiles = 0;

            for (var fecha = desde.AddDays(1); fecha <= hasta; fecha = fecha.AddDays(1))
            {
                if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                if (festivosSet.Contains(fecha))
                    continue;

                diasHabiles++;
            }

            return negativo ? diasHabiles * -1 : diasHabiles;
        }

        public async Task<DateOnly> SumarDiasHabilesAsync(DateOnly fechaInicio, int diasHabiles)
        {
            if (diasHabiles == 0)
                return fechaInicio;

            int direccion = diasHabiles > 0 ? 1 : -1;
            int pendientes = Math.Abs(diasHabiles);

            DateOnly fecha = fechaInicio;

            while (pendientes > 0)
            {
                fecha = fecha.AddDays(direccion);

                if (await EsDiaHabilAsync(fecha))
                    pendientes--;
            }

            return fecha;
        }
    }
}