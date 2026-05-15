namespace Alertas.ViewModels
{
    public class ProyectoAccesoViewModel
    {
        public int id_proyecto { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;

        // "PROYECTO" o "OBLIGACION"
        public string tipo_acceso { get; set; } = string.Empty;

        // Texto para mostrar en pantalla
        public string descripcion_acceso { get; set; } = string.Empty;
    }
}