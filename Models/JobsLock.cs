using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("jobs_locks")]
    public class JobsLock
    {
        [Key]
        public string nombre_job { get; set; } = string.Empty;

        public DateTime locked_until { get; set; }
        public string locked_by { get; set; } = string.Empty;

        public DateTime? fecha_ult_ejecucion { get; set; }
        public DateTime fecha_creacion { get; set; }
        public DateTime fecha_actualizacion { get; set; }
    }
}