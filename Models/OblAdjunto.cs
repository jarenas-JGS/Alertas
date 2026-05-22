using Alertas.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Alertas.Models
{
    [Table("obl_adjuntos")]
    public class OblAdjunto
    {
        [Key]
        [Display(Name = "Id")]
        public int id_obl_adjunto { get; set; }

        [Required(ErrorMessage = "Debe indicar el registro del impuesto del adjunto")]
        [Display(Name = "Reg_Imp")]
        public int id_reg_obl { get; set; }

        [Required(ErrorMessage = "El nombre del adjunto obligatorio.")]
        [MaxLength(255, ErrorMessage = "El nombre del adjunto no puede tener más de 255 caracteres.")]
        [Display(Name = "Nombre_Orig")]
        public string nombre_orig { get; set; }

        [Required(ErrorMessage = "La ruta del adjunto es obligatorio.")]
        [MaxLength(500, ErrorMessage = "La ruta del adjunto no puede tener más de 500 caracteres.")]
        [Display(Name = "Object_Key")]
        public string object_key { get; set; }

        [Required(ErrorMessage = "El nombre del bucket es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre del bucket no puede tener más de 100 caracteres.")]
        [Display(Name = "Nombre_Bucket")]
        public string bucket_name { get; set; }

        [Required(ErrorMessage = "El tipo de mime es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El tipo de mime no puede tener más de 100 caracteres.")]
        [Display(Name = "Tipo_Mime")]
        public string mime_type { get; set; }

        [Required(ErrorMessage = "La extensión del archivo es obligatoria.")]
        [MaxLength(20, ErrorMessage = "La extensión del archivo no puede tener más de 20 caracteres.")]
        [Display(Name = "Extension")]
        public string extension { get; set; }

        [Required(ErrorMessage = "El tamaño del archivo es obligatorio")]
        [Display(Name = "Tamaño_Bytes")]
        public long tamano_bytes { get; set; }

        [Display(Name = "Fecha_Carga")]
        public DateTime fecha_carga { get; set; }

        [Required(ErrorMessage = "Debe indicar el usuario que hace el cambio")]
        [Display(Name = "Usuario")]
        public int id_usuario { get; set; }

        [NotMapped]
        public string? fecha_carga_local { get; set; }

        [Display(Name = "Activo")]
        public bool activo { get; set; } = true;

        [Display(Name = "Eliminado")]
        public bool eliminado { get; set; } = false;

        [Display(Name = "Fecha eliminación")]
        public DateTime? fecha_eliminacion { get; set; }

        [Display(Name = "Usuario eliminación")]
        public int? id_usuario_eliminacion { get; set; }

        [MaxLength(500)]
        [Display(Name = "Motivo eliminación")]
        public string? motivo_eliminacion { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Tipo soporte")]
        public string tipo_soporte { get; set; } = "NORMAL";

        [ForeignKey("id_usuario_eliminacion")]
        public Usuario? UsuarioEliminacion { get; set; }

        [ForeignKey("id_reg_obl")]
        public RegObl? RegObl { get; set; }

        [ForeignKey("id_usuario")]
        public Usuario? Usuario { get; set; }

        [Display(Name = "Eliminado físicamente")]
        public bool eliminado_fisicamente { get; set; } = false;

        [Display(Name = "Fecha eliminación física")]
        public DateTime? fecha_eliminacion_fisica { get; set; }

    }
}