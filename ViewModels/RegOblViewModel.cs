using System.ComponentModel.DataAnnotations;

namespace Alertas.ViewModels
{
    public class RegOblViewModel
    {
        public int? id_reg_obl { get; set; }

        public int? id_proyecto { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el cliente.")]
        [Display(Name = "Cliente")]
        public int? id_cliente { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la empresa.")]
        [Display(Name = "Empresa")]
        public int? id_empresa { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el tipo de obligación.")]
        [Display(Name = "Tipo de obligación")]
        public int? id_tipo_obligacion { get; set; }

        [Display(Name = "Código obligación")]
        [MaxLength(30)]
        public string? cod_obligacion { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la ciudad.")]
        [Display(Name = "Ciudad")]
        public int? id_ciudad { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el dominio.")]
        [Display(Name = "Dominio")]
        public int? id_dominio { get; set; }

        [Required(ErrorMessage = "La fecha vencimiento de seguimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha venc. seguimiento")]
        public DateOnly fecha_venc_seguimiento { get; set; }

        [Required(ErrorMessage = "La fecha vencimiento de la obligación es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha venc. obligación")]
        public DateOnly fecha_venc_obl { get; set; }

        [Required(ErrorMessage = "La vigencia es obligatoria.")]
        [Display(Name = "Vigencia")]
        public int vigencia { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el período.")]
        [Display(Name = "Período")]
        public int? id_periodo { get; set; }

        [Display(Name = "Año")]
        public int anio { get; set; }

        [Display(Name = "Mes")]
        public int mes { get; set; }

        [Display(Name = "Día")]
        public int dia { get; set; }

        [Display(Name = "Valor aproximado")]
        public int? vlr_aprox { get; set; }

        [Display(Name = "Valor real")]
        public int? vlr_real { get; set; }

        [Display(Name = "Diferencia")]
        public int? diferencia { get; set; }

        [Display(Name = "Variación")]
        public decimal? variacion { get; set; }

        [Display(Name = "Saldo a favor")]
        public int? saldo_favor { get; set; }

        [Display(Name = "Justificación variación")]
        public int? id_justif_var { get; set; }

        [Display(Name = "CC Empleador")]
        [MaxLength(30)]
        public string? cc_empleador { get; set; }

        [Display(Name = "Empleador")]
        [MaxLength(150)]
        public string? nombre_empleador { get; set; }

        [Display(Name = "CC Empleado")]
        [MaxLength(30)]
        public string? cc_empleado { get; set; }

        [Display(Name = "Empleado")]
        [MaxLength(150)]
        public string? nombre_empleado { get; set; }

        public int? id_estado { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha creación")]

        public DateOnly? fecha_creac { get; set; }

        [Display(Name = "Observaciones")]
        [MaxLength(255)]
        public string? observaciones { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        [Display(Name = "Nombre")]
        public string nombre { get; set; } = string.Empty;

        [Display(Name = "Responsables")]
        public List<int> ids_responsables { get; set; } = new();

        [Display(Name = "Elaboradores")]
        public List<int> ids_elaboradores { get; set; } = new();

        [Display(Name = "Autorizadores")]
        public List<int> ids_autorizadores { get; set; } = new();

        [Display(Name = "Aprobadores")]
        public List<int> ids_aprobadores { get; set; } = new();

        [Display(Name = "Usuarios vencimiento")]
        public List<int> ids_vencimiento { get; set; } = new();


    }
}