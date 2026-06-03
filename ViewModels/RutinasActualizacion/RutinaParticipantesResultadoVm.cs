namespace Alertas.ViewModels.RutinasActualizacion
{
    public class RutinaParticipantesResultadoVm
    {
        public int ObligacionesProcesadas { get; set; }

        public int RegistrosInsertados { get; set; }

        public int RegistrosReactivados { get; set; }

        public int RegistrosDesactivados { get; set; }

        public int RegistrosOmitidos { get; set; }

        public List<string> Errores { get; set; } = new();
    }
}