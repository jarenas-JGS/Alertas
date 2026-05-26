namespace Alertas.ViewModels.Dashboards
{
    public class ConsultaObligacionDashboardVm
    {
        public int IdRegObl { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? CodigoObligacion { get; set; }

        public string Proyecto { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string TipoObligacion { get; set; } = string.Empty;
        public string? Ciudad { get; set; }
        public string Dominio { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        public string FechaCreacion { get; set; } = string.Empty;
        public string FechaVencimiento { get; set; } = string.Empty;
        public string FechaSeguimiento { get; set; } = string.Empty;
        public string? FechaSeguimientoEjecutada { get; set; }
        public string? FechaVencimientoEjecutada { get; set; }
        public string? FechaAprobadoFinal { get; set; }

        public int? DiasAtrasoSeguimiento { get; set; }
        public int? DiasAtrasoVencimiento { get; set; }

        public int? ValorAprox { get; set; }
        public int? ValorReal { get; set; }
        public int? Diferencia { get; set; }
        public decimal? Variacion { get; set; }
        public int? SaldoFavor { get; set; }

        public string? Justificacion { get; set; }
        public string? Observaciones { get; set; }

        public bool? Aprobado { get; set; }
        public string? AprobadoPor { get; set; }

        public List<string> Responsables { get; set; } = new();
        public List<string> Elaboradores { get; set; } = new();
        public List<string> Autorizadores { get; set; } = new();
        public List<string> Aprobadores { get; set; } = new();
        public List<string> UsuariosVencimiento { get; set; } = new();

        public string? ReturnUrl { get; set; }
    }
}