namespace Alertas.ViewModels
{
    public class HomeViewModel
    {
        public string Usuario { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public bool EsSuperAdmin { get; set; }

        public bool PuedeVerSeguimientoProyecto { get; set; }
        public bool PuedeVerReportes { get; set; }
        public bool PuedeVerCreacionProyecto { get; set; }
        public bool PuedeVerCarguePlantilla { get; set; }

        public bool PuedeVerNotificaciones { get; set; }

        public bool TieneProyectosDisponibles { get; set; }
        public bool TieneProyectosComoAdministrador { get; set; }

        public string? MensajeInformativo { get; set; }
    }
}