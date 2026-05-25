namespace Alertas.ViewModels.Monitoreo
{
    public class MonitoreoDashboardViewModel
    {
        public DateTime? UltimaEjecucionInicio { get; set; }
        public DateTime? UltimaEjecucionFin { get; set; }
        public string? UltimaEjecucionEstado { get; set; }
        public string? UltimaEjecucionAmbiente { get; set; }

        public int AlertasGeneradasHoy { get; set; }
        public int CorreosEnviadosHoy { get; set; }
        public int CorreosErrorHoy { get; set; }

        public int JobsFinalizadosHoy { get; set; }
        public int JobsErrorHoy { get; set; }

        public string? LockNombreJob { get; set; }
        public DateTime? LockHasta { get; set; }
        public DateTime? LockUltimaEjecucion { get; set; }

        public List<string> Advertencias { get; set; } = new();
    }
}