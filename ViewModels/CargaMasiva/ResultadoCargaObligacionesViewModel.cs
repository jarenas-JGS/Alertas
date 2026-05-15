namespace Alertas.ViewModels.CargaMasiva
{
    public class ResultadoCargaObligacionesViewModel
    {
        public bool exitoso { get; set; }
        public int total_insertadas { get; set; }
        public string mensaje { get; set; } = string.Empty;
    }
}