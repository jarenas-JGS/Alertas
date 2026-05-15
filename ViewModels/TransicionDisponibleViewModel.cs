namespace Alertas.ViewModels
{
    public class TransicionDisponibleViewModel
    {
        public int id_estado_transicion { get; set; }

        public string nombre_accion { get; set; } = string.Empty;

        public bool requiere_observacion { get; set; }

        public bool es_aprobacion { get; set; }

        public bool es_rechazo { get; set; }

        public bool es_anulacion { get; set; }

        public int id_estado_destino { get; set; }

        public string nombre_estado_destino { get; set; } = string.Empty;
    }
}