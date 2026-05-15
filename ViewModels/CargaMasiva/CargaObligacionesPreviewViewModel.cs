namespace Alertas.ViewModels.CargaMasiva
{
    public class CargaObligacionesPreviewViewModel
    {
        public int id_proyecto { get; set; }
        public string nombre_proyecto { get; set; } = string.Empty;

        public List<CargaObligacionesFilaViewModel> filas { get; set; } = new();

        public int total_filas => filas.Count;
        public List<CargaObligacionesErrorViewModel> errores { get; set; } = new();
        public Guid id_carga { get; set; }
        public bool tiene_errores => errores.Any();
        public int total_errores => errores.Count;
    }
}