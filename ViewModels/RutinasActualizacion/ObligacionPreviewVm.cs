namespace Alertas.ViewModels.RutinasActualizacion
{
    public class ObligacionPreviewVm
    {
        public int IdRegObl { get; set; }

        public string NombreObligacion { get; set; } = string.Empty;

        public string Empresa { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public string ResultadoEsperado { get; set; } = string.Empty;

        public string Observacion { get; set; } = string.Empty;
        public bool MostrarAunqueSeaOmitida { get; set; }
    }
}