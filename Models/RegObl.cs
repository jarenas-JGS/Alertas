using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("reg_obl")]
    public class RegObl
    {
        [Key]
        public int id_reg_obl { get; set; }

        [Required]
        [MaxLength(200)]
        public string nombre { get; set; } = string.Empty;

        [Required]
        public int? id_cliente { get; set; }

        [Required]
        public int id_empresa { get; set; }

        [Required]
        public int id_tipo_obligacion { get; set; }

        [Required]
        public int id_proyecto { get; set; }

        [MaxLength(30)]
        public string? cod_obligacion { get; set; }

        public int? id_ciudad { get; set; }

        [Required]
        public int id_dominio { get; set; }

        [Required]
        public DateOnly fecha_venc_seguimiento { get; set; }

        [Required]
        public DateOnly fecha_venc_obl { get; set; }

        [Required]
        public int vigencia { get; set; }

        [Required]
        public int id_periodo { get; set; }

        [Required]
        public int anio { get; set; }

        [Required]
        public int mes { get; set; }

        [Required]
        public int dia { get; set; }

        public int? vlr_aprox { get; set; }
        public int? vlr_real { get; set; }
        public int? diferencia { get; set; }
        public decimal? variacion { get; set; }
        public int? saldo_favor { get; set; }

        public int? id_justif_var { get; set; }

        public int? id_autorizado_por { get; set; }

        public int? id_aprobado_por { get; set; }

        [Required]
        public int id_estado { get; set; }

        public DateOnly? fecha_creac { get; set; }
        public DateOnly? fecha_seguimiento_ejecutada { get; set; }
        public DateOnly? fecha_vencimiento_ejecutada { get; set; }

        public DateOnly? fecha_aprobado_final { get; set; }

        public int? dias_atraso_seguimiento { get; set; }

        public int? dias_atraso_vencimiento { get; set; }

        public bool? aprobado { get; set; }

        [MaxLength(150)]
        public string? nombre_empleador { get; set; }

        [MaxLength(150)]
        public string? nombre_empleado { get; set; }

        [MaxLength(255)]
        public string? observaciones { get; set; }

        [MaxLength(30)]
        public string? cc_empleador { get; set; }

        [MaxLength(30)]
        public string? cc_empleado { get; set; }

        public DateTime? fecha_ult_modif { get; set; }
        public int? id_usuario_ult_modif { get; set; }
        public bool soporte_post_cierre_cumplido { get; set; }

        public DateOnly? fecha_soporte_post_cierre { get; set; }

        public int? id_usuario_soporte_post_cierre { get; set; }

        [ForeignKey(nameof(id_usuario_soporte_post_cierre))]
        public Usuario? UsuarioSoportePostCierre { get; set; }

        [ForeignKey(nameof(id_usuario_ult_modif))]
        public Usuario? UsuarioUltModif { get; set; }

        [ForeignKey(nameof(id_cliente))]
        public Cliente Cliente { get; set; } = null!;

        [ForeignKey(nameof(id_empresa))]
        public Empresa Empresa { get; set; } = null!;

        [ForeignKey(nameof(id_tipo_obligacion))]
        public TipoObligacion TipoObligacion { get; set; } = null!;

        [ForeignKey(nameof(id_proyecto))]
        public Proyecto Proyecto { get; set; } = null!;

        [ForeignKey(nameof(id_ciudad))]
        public Ciudad? Ciudad { get; set; }

        [ForeignKey(nameof(id_dominio))]
        public Dominio Dominio { get; set; } = null!;

        [ForeignKey(nameof(id_periodo))]
        public Periodo Periodo { get; set; } = null!;

        [ForeignKey(nameof(id_justif_var))]
        public JustifVar? JustifVar { get; set; }

        [ForeignKey(nameof(id_autorizado_por))]
        public Usuario? AutorizadoPor { get; set; }

        [ForeignKey(nameof(id_aprobado_por))]
        public Usuario? AprobadoPor { get; set; }

        [ForeignKey(nameof(id_estado))]
        public Estado Estado { get; set; } = null!;

        [InverseProperty(nameof(UsuarioObligacion.RegObl))]
        public ICollection<UsuarioObligacion> UsuariosObligaciones { get; set; } = new List<UsuarioObligacion>();

        public ICollection<OblAdjunto> Adjuntos { get; set; } = new List<OblAdjunto>();

    }
}