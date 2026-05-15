namespace Alertas.ViewModels.CargaMasiva
{
    public class CargaObligacionesTemporalViewModel
    {
        public Guid id_carga { get; set; } = Guid.NewGuid();

        public int id_proyecto { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;

        public DateTime fecha_carga { get; set; } = DateTime.Now;

        public List<CargaObligacionesFilaViewModel> filas { get; set; } = new();
    }
}