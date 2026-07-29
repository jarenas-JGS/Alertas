namespace Alertas.ViewModels.Dashboards
{
    public class DetalleHistoricoCumplimientoVm
    {
        public string Titulo { get; set; } = "Detalle histórico de cumplimiento";
        public string NombreProyecto { get; set; } = string.Empty;

        public string TipoDetalle { get; set; } = string.Empty;

        public DateOnly? FechaDesde { get; set; }
        public DateOnly? FechaHasta { get; set; }

        public FiltrosDashboardHistoricoCumplimientoVm Filtros { get; set; }
            = new();

        public List<HistoricoCumplimientoItemVm> Obligaciones { get; set; }
            = new();
    }
}