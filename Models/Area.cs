using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("areas")]
    public class Area
    {
        [Key]
        public int id_area { get; set; }

        [Required(ErrorMessage = "El nombre del área es obligatorio.")]
        [MaxLength(100)]
        public string nombre { get; set; } = string.Empty;

        [InverseProperty(nameof(Proyecto.Area))]
        public ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();

        [InverseProperty(nameof(AreaEmpresa.Area))]
        public ICollection<AreaEmpresa> AreasEmpresas { get; set; } = new List<AreaEmpresa>();

        [InverseProperty(nameof(TipoObligacion.Area))]
        public ICollection<TipoObligacion> TiposObligacion { get; set; } = new List<TipoObligacion>();
    }
}