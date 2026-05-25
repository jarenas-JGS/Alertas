public class EstadoResumenDashboardVm
{
    public int IdEstado { get; set; }
    public string NombreEstado { get; set; } = string.Empty;
    public int Orden { get; set; }
    public int Total { get; set; }
    public bool ControlVencimiento { get; set; }
    public bool ControlSeguimiento { get; set; }
    public bool Bloquea { get; set; }
    public string ColorHex { get; set; } = "#6c757d";
    public string CssClass { get; set; } = "secondary";
    public string Icono { get; set; } = "bi-circle";
    public string ColorHex20 { get; set; } = "#f8f9fa";


}