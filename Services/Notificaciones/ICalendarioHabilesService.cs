namespace Alertas.Services.Notificaciones
{
    public interface ICalendarioHabilesService
    {
        Task<int> CalcularDiasHabilesAsync(DateOnly fechaInicio, DateOnly fechaFin);

        Task<bool> EsDiaHabilAsync(DateOnly fecha);

        Task<DateOnly> SumarDiasHabilesAsync(DateOnly fechaInicio, int diasHabiles);
    }
}