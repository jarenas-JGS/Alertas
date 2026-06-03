namespace Alertas.ViewModels.RutinasActualizacion
{
    public class RutinaParticipantesPreviewVm
    {
        public RutinaParticipantesVm Parametros { get; set; } = new();

        public int TotalObligaciones { get; set; }

        public int TotalAfectadas { get; set; }

        public int TotalOmitidas { get; set; }

        public List<string> Advertencias { get; set; } = new();

        public List<ObligacionPreviewVm> Obligaciones { get; set; } = new();
    }
}