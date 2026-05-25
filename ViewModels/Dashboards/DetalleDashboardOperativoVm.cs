namespace Alertas.ViewModels.Dashboards
{
    public class DetalleDashboardOperativoVm
    {
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string NombreProyecto { get; set; } = string.Empty;

        public List<ItemDetalleDashboardOperativoVm> Obligaciones { get; set; } = new();
    }

    public class ItemDetalleDashboardOperativoVm
    {
        public int IdRegObl { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? CodigoObligacion { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string TipoObligacion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string FechaVencimiento { get; set; } = string.Empty;
        public string FechaSeguimiento { get; set; } = string.Empty;
        public int? DiasAtrasoVencimiento { get; set; }
        public int? DiasAtrasoSeguimiento { get; set; }
        public bool Aprobado { get; set; }
        public string EstadoCssClass { get; set; } = "secondary";
    }
}