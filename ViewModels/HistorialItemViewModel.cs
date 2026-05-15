namespace Alertas.ViewModels
{
    public class HistorialItemViewModel
    {
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // Flujo, Campo, Soporte
        public string Titulo { get; set; } = string.Empty;
        public string? Detalle { get; set; }
    }
}