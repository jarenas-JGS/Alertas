using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("hist_obl_campos")]
    public class HistOblCampo
    {
        [Key]
        [Display(Name = "Id")]
        public int id_hist_obl_campo { get; set; }

        [Required(ErrorMessage = "Debe indicar el Id de la obligación")]
        [Display(Name = "RegObl")]
        public int id_reg_obl { get; set; }

        [Required(ErrorMessage = "El nombre del campo es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El campo no puede tener más de 100 caracteres.")]
        [Display(Name = "Campo")]
        public string campo { get; set; }

        [Display(Name = "valorAnterior")]
        public string? valor_anterior { get; set; }

        [Display(Name = "valorNuevo")]
        public string? valor_nuevo { get; set; }

        [Required(ErrorMessage = "Debe indicar el Id del usuario")]
        [Display(Name = "Usuario")]
        public int id_usuario { get; set; }

        [Display(Name = "Fecha")]
        public DateTime fecha { get; set; }

        [Required(ErrorMessage = "Debe indicar el Id del estado")]
        [Display(Name = "EstadoEnMomento")]
        public int id_estado_en_momento { get; set; }

        [Required(ErrorMessage = "El tipo de cambio es obligatorio.")]
        [MaxLength(50, ErrorMessage = "El tipo de cambio no puede tener más de 50 caracteres.")]
        [Display(Name = "TipoCambio")]
        public string tipo_cambio { get; set; }


        [ForeignKey("id_reg_obl")]
        public RegObl? RegObl { get; set; }

        [ForeignKey("id_usuario")]
        public Usuario? Usuario { get; set; }

        [ForeignKey("id_estado_en_momento")]
        public Estado? Estado { get; set; }

    }
}
