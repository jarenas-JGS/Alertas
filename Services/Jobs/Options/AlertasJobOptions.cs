namespace Alertas.Services.Jobs.Options
{
    public class AlertasJobOptions
    {
        public bool Habilitado { get; set; } = false;
        public bool PermitirEnvioReal { get; set; } = false;
        public bool EjecutarAlIniciar { get; set; } = false;

        public int IntervaloMinutos { get; set; } = 60;
        public int MaxCorreosPorEjecucion { get; set; } = 100;
        public int DelayEntreCorreosMs { get; set; } = 250;

        public int? SoloProyectoId { get; set; }

        public string AmbientePermitido { get; set; } = "Production";
    }
}