namespace Alertas.Services.Notificaciones.DTOs
{
    public class AlertaObligacionDto
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; } = string.Empty;

        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string EmailUsuario { get; set; } = string.Empty;

        public int IdRegObl { get; set; }
        public string NombreObligacion { get; set; } = string.Empty;

        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; } = string.Empty;

        public int IdGrupoAlertaDia { get; set; }
        public string NombreAlerta { get; set; } = string.Empty;

        public int IdMensaje { get; set; }
        public string NombreMensaje { get; set; } = string.Empty;
        public string TextoMensaje { get; set; } = string.Empty;
        public int Prioridad { get; set; }

        public DateOnly FechaVencimientoObligacion { get; set; }
        public int DiasVencimientoObligacion { get; set; }

        public DateOnly FechaVencimientoSeguimiento { get; set; }
        public int DiasVencimientoSeguimiento { get; set; }

        public string Autorizadores { get; set; } = string.Empty;


    }
}