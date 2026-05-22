namespace Alertas.Services.Jobs.Options
{
    public class AlertasJobOptions
    {
        public bool Habilitado { get; set; } = false;
        public bool PermitirEnvioReal { get; set; } = false;
        public bool EjecutarAlIniciar { get; set; } = false;

        public int IntervaloMinutos { get; set; } = 5;
        public int MaxCorreosPorEjecucion { get; set; } = 100;
        public int DelayEntreCorreosMs { get; set; } = 250;

        public int? SoloProyectoId { get; set; }

        public string AmbientePermitido { get; set; } = "Production";
        public bool BloquearEnviosFueraDeProduccion { get; set; } = true;
        public string? EmailDestinoPruebas { get; set; }
        public bool SimularEjecucion { get; set; } = true;

        public bool UsarHorarioDiario { get; set; } = true;
        public int HoraEjecucion { get; set; } = 6;
        public int MinutoEjecucion { get; set; } = 0;
        public string ZonaHoraria { get; set; } = "America/Bogota";
        public bool OmitirFestivos { get; set; } = true;
    }
}