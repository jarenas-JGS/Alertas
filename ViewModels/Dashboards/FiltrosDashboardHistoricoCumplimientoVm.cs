using Microsoft.AspNetCore.Mvc.Rendering;

namespace Alertas.ViewModels.Dashboards
{
    public class FiltrosDashboardHistoricoCumplimientoVm
    {
        // El período siempre se aplica sobre fecha_venc_obl.
        public DateOnly? FechaDesde { get; set; }
        public DateOnly? FechaHasta { get; set; }

        public int? IdCliente { get; set; }
        public int? IdEmpresa { get; set; }
        public int? IdCiudad { get; set; }
        public int? IdEstado { get; set; }
        public int? IdTipoObligacion { get; set; }

        public int? IdResponsable { get; set; }
        public int? IdElaborador { get; set; }
        public int? IdAutorizador { get; set; }
        public int? IdAprobador { get; set; }
        public int? IdUsuarioVencimiento { get; set; }

        public string? Clasificacion { get; set; }

        public List<SelectListItem> Clientes { get; set; } = new();
        public List<SelectListItem> Empresas { get; set; } = new();
        public List<SelectListItem> Ciudades { get; set; } = new();
        public List<SelectListItem> Estados { get; set; } = new();
        public List<SelectListItem> TiposObligacion { get; set; } = new();

        public List<SelectListItem> Responsables { get; set; } = new();
        public List<SelectListItem> Elaboradores { get; set; } = new();
        public List<SelectListItem> Autorizadores { get; set; } = new();
        public List<SelectListItem> Aprobadores { get; set; } = new();
        public List<SelectListItem> UsuariosVencimiento { get; set; } = new();

        public List<SelectListItem> Clasificaciones { get; set; } = new();
    }
}