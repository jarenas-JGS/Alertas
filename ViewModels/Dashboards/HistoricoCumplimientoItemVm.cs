namespace Alertas.ViewModels.Dashboards
{
    public class HistoricoCumplimientoItemVm
    {
        public int IdRegObl { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string? CodigoObligacion { get; set; }

        public int? IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;

        public int IdEmpresa { get; set; }
        public string Empresa { get; set; } = string.Empty;

        public int IdTipoObligacion { get; set; }
        public string TipoObligacion { get; set; } = string.Empty;

        public int? IdCiudad { get; set; }
        public string? Ciudad { get; set; }

        public int IdEstado { get; set; }
        public string EstadoActual { get; set; } = string.Empty;

        public DateOnly FechaVencimiento { get; set; }
        public DateOnly? FechaCumplimiento { get; set; }

        public int DiasVencida { get; set; }

        public string Clasificacion { get; set; } = string.Empty;

        public List<ParticipanteHistoricoCumplimientoVm>
            Autorizadores
        { get; set; } = new();

        public string AutorizadoresTexto =>
            Autorizadores.Count == 0
                ? "Sin autorizador"
                : string.Join(
                    ", ",
                    Autorizadores
                        .Select(x => x.Nombre)
                        .Distinct()
                        .OrderBy(x => x));
    }
}