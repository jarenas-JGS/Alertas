namespace Alertas.ViewModels.ConfiguracionOperativa
{
    public class ConfiguracionOperativaViewModel
    {
        // Estado operativo almacenado en la base de datos
        public bool AlertasAutomaticasHabilitadas { get; set; }

        public DateTime? FechaActualizacion { get; set; }
        public string? UsuarioActualizacion { get; set; }
        public string? Descripcion { get; set; }

        // Estado técnico leído desde la configuración del ambiente
        public string AmbienteActual { get; set; } = "";

        public bool JobHabilitadoConfiguracion { get; set; }
        public bool PermitirEnvioReal { get; set; }
        public bool SimularEjecucion { get; set; }
        public bool BloquearEnviosFueraDeProduccion { get; set; }

        public bool UsarHorarioDiario { get; set; }
        public int HoraEjecucion { get; set; }
        public int MinutoEjecucion { get; set; }
        public string ZonaHoraria { get; set; } = "";

        public bool OmitirFestivos { get; set; }
        public int IntervaloMinutos { get; set; }

        public string? EmailDestinoPruebas { get; set; }

        public bool AmbientePermitido { get; set; }
        public bool EstadoEfectivo { get; set; }

        public string EstadoEfectivoMensaje { get; set; } = "";
    }
}