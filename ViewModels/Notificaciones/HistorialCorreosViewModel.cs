namespace Alertas.ViewModels.Notificaciones
{
    public class HistorialCorreosViewModel
    {
        public List<HistorialCorreoItemViewModel> Correos { get; set; } = new();
    }

    public class HistorialCorreoItemViewModel
    {
        public int IdNotificacionEnvio { get; set; }
        public DateTime FechaEnvio { get; set; }
        public string TipoEjecucion { get; set; } = "";
        public string EstadoEnvio { get; set; } = "";
        public string? NombreProyecto { get; set; }
        public string? NombreUsuario { get; set; }
        public string DestinatarioEmail { get; set; } = "";
        public string Asunto { get; set; } = "";
        public int CantidadAlertas { get; set; }
        public string? ErrorMensaje { get; set; }
    }

    public class HistorialCorreoDetalleViewModel
    {
        public int IdNotificacionEnvio { get; set; }
        public DateTime FechaEnvio { get; set; }
        public string TipoEjecucion { get; set; } = "";
        public string EstadoEnvio { get; set; } = "";
        public string? NombreProyecto { get; set; }
        public string? NombreUsuario { get; set; }
        public string DestinatarioEmail { get; set; } = "";
        public string Asunto { get; set; } = "";
        public string? ErrorMensaje { get; set; }

        public List<HistorialCorreoAlertaDetalleViewModel> Alertas { get; set; } = new();
    }

    public class HistorialCorreoAlertaDetalleViewModel
    {
        public string NombreAlerta { get; set; } = "";
        public string NombreMensaje { get; set; } = "";
        public int Prioridad { get; set; }

        public int IdRegObl { get; set; }
        public DateOnly? FechaVencObl { get; set; }
        public int? DiasVencimientoObl { get; set; }

        public DateOnly? FechaVencSeguimiento { get; set; }
        public int? DiasVencimientoSeguimiento { get; set; }
    }
}