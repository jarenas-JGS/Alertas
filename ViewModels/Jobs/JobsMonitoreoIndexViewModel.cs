namespace Alertas.ViewModels.Jobs
{
    public class JobsMonitoreoIndexViewModel
    {
        public List<JobEjecucionItemViewModel> Ejecuciones { get; set; } = new();
        public List<JobLockItemViewModel> Locks { get; set; } = new();
    }

    public class JobEjecucionItemViewModel
    {
        public int IdJobEjecucion { get; set; }
        public string NombreJob { get; set; } = "";
        public string Ambiente { get; set; } = "";
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Estado { get; set; } = "";
        public int TotalGeneradas { get; set; }
        public int TotalEnviadas { get; set; }
        public int TotalError { get; set; }
        public string? MensajeError { get; set; }

        public int? DuracionSegundos =>
            FechaFin.HasValue
                ? (int)(FechaFin.Value - FechaInicio).TotalSeconds
                : null;
    }

    public class JobLockItemViewModel
    {
        public string NombreJob { get; set; } = "";
        public DateTime LockedUntil { get; set; }
        public string LockedBy { get; set; } = "";
        public DateTime? FechaUltEjecucion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}