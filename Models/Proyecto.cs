using Alertas.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("proyectos")]
    public class Proyecto
    {
        [Key]
        public int id_proyecto { get; set; }

        [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
        [MaxLength(200, ErrorMessage = "El nombre del proyecto no puede tener más de 200 caracteres.")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar el área")]
        public int id_area { get; set; }

        public bool activo { get; set; }

        [Required(ErrorMessage = "El nombre de seguimiento es obligatorio.")]
        [MaxLength(150, ErrorMessage = "El nombre de seguimiento no puede tener más de 150 caracteres.")]
        public string nombre_seguimiento { get; set; } = string.Empty;

        public DateTime fecha_creacion { get; set; }

        public int? id_usuario_creacion { get; set; }

        public Area Area { get; set; } = null!;

        public Usuario? UsuarioCreacion { get; set; }

        public bool configuracion_completa { get; set; }
        public bool usa_soporte_post_cierre { get; set; } = false;

        [MaxLength(100, ErrorMessage = "El nombre del soporte posterior al cierre no puede tener más de 100 caracteres.")]
        public string? nombre_soporte_post_cierre { get; set; }

        public ICollection<RegObl> Obligaciones { get; set; } = new List<RegObl>();

        public ICollection<Estado> Estados { get; set; } = new List<Estado>();

        public ICollection<GrupoAlerta> GruposAlertas { get; set; } = new List<GrupoAlerta>();
        
        public ICollection<UsuarioProyecto> UsuariosProyectos { get; set; } = new List<UsuarioProyecto>();

        public ICollection<EstadoTransicion> EstadosTransicion { get; set; } = new List<EstadoTransicion>();
    }
}