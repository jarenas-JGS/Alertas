using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alertas.Models
{
    [Table("jobs_ejecuciones")]
    public class JobsEjecucion
    {
        [Key]
        public int id_job_ejecucion { get; set; }

        public string nombre_job { get; set; } = string.Empty;
        public string ambiente { get; set; } = string.Empty;
        public DateTime fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string estado { get; set; } = string.Empty;

        public int total_generadas { get; set; }
        public int total_enviadas { get; set; }
        public int total_error { get; set; }

        public string? mensaje_error { get; set; }
    }
}