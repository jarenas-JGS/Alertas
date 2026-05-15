namespace Alertas.ViewModels.CargaMasiva
{
    public class CargaObligacionesFilaViewModel
    {
        public int numero_fila { get; set; }

        public string? nombre { get; set; }
        public string? codigo_obligacion { get; set; }
        public string? cliente { get; set; }
        public string? empresa { get; set; }
        public string? ciudad { get; set; }
        public string? periodo { get; set; }
        public string? dominio { get; set; }
        public string? tipo_obligacion { get; set; }

        public DateTime? fecha_vencimiento_obligacion { get; set; }
        public DateTime? fecha_vencimiento_seguimiento { get; set; }

        public decimal? valor_aproximado { get; set; }
        public decimal? saldo_favor { get; set; }
        public string? cc_empleador { get; set; }
        public string? empleador { get; set; }
        public string? cc_empleado { get; set; }
        public string? empleado { get; set; }

        public string? responsable { get; set; }
        public string? elaborador { get; set; }
        public string? autorizador { get; set; }
        public string? aprobador { get; set; }
        public string? usuario_vencimiento { get; set; }
        public int? vigencia { get; set; }

        public string? observaciones { get; set; }
        public List<CargaObligacionesErrorViewModel> errores { get; set; } = new();
        public bool es_valida => !errores.Any();
    }
}