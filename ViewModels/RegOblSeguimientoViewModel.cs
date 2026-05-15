using Alertas.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Alertas.ViewModels
{
    public class RegOblSeguimientoViewModel
    {
        // Identificación
        public int id_reg_obl { get; set; }
        public int id_proyecto { get; set; }
        public string nombre_proyecto { get; set; } = string.Empty;

        // Datos generales
        public string nombre { get; set; } = string.Empty;
        public string? cod_obligacion { get; set; }

        public int? id_cliente { get; set; }
        public string? nombre_cliente { get; set; }

        public int? id_empresa { get; set; }
        public string? nombre_empresa { get; set; }

        public int? id_tipo_obligacion { get; set; }
        public string? nombre_tipo_obligacion { get; set; }

        public int? id_dominio { get; set; }
        public string? nombre_dominio { get; set; }

        public int? id_ciudad { get; set; }
        public string? nombre_ciudad { get; set; }

        public int? id_periodo { get; set; }
        public string? nombre_periodo { get; set; }

        public int vigencia { get; set; }
        public int anio { get; set; }
        public int mes { get; set; }
        public int dia { get; set; }

        // Fechas
        public DateOnly? fecha_creac { get; set; }
        public DateOnly fecha_venc_obl { get; set; }
        public DateOnly fecha_venc_seguimiento { get; set; }
        public DateOnly? fecha_seguimiento_ejecutada { get; set; }
        public DateOnly? fecha_vencimiento_ejecutada { get; set; }
        public DateOnly? fecha_aprobado_final { get; set; }

        // Valores
        public int? vlr_aprox { get; set; }
        public int? vlr_real { get; set; }
        public int? diferencia { get; set; }
        public decimal? variacion { get; set; }
        public int? saldo_favor { get; set; }

        public int? id_justif_var { get; set; }
        public string? nombre_justif_var { get; set; }
        public int? dias_atraso_seguimiento { get; set; }
        public int? dias_atraso_vencimiento { get; set; }

        // Datos complementarios
        public string? cc_empleador { get; set; }
        public string? nombre_empleador { get; set; }
        public string? cc_empleado { get; set; }
        public string? nombre_empleado { get; set; }
        public string? observaciones { get; set; }

        // Estado
        public int id_estado { get; set; }
        public string nombre_estado { get; set; } = string.Empty;
        public bool? aprobado { get; set; }
        public int? id_aprobado_por { get; set; }
        public string? nombre_aprobado_por { get; set; }

        // Participantes seleccionados
        public List<int> ids_responsables { get; set; } = new();
        public List<int> ids_elaboradores { get; set; } = new();
        public List<int> ids_autorizadores { get; set; } = new();
        public List<int> ids_aprobadores { get; set; } = new();
        public List<int> ids_vencimiento { get; set; } = new();

        // Catálogos para edición futura
        public List<SelectListItem> responsablesDisponibles { get; set; } = new();
        public List<SelectListItem> elaboradoresDisponibles { get; set; } = new();
        public List<SelectListItem> autorizadoresDisponibles { get; set; } = new();
        public List<SelectListItem> aprobadoresDisponibles { get; set; } = new();
        public List<SelectListItem> vencimientoDisponibles { get; set; } = new();

        public List<SelectListItem> justificacionesDisponibles { get; set; } = new();

        // Permisos de edición
        public bool PuedeEditarDatosGenerales { get; set; }
        public bool PuedeEditarParticipantes { get; set; }
        public bool PuedeEditarFechasBase { get; set; }
        public bool PuedeEditarValores { get; set; }
        public bool PuedeEditarJustificacion { get; set; }
        public bool PuedeEditarObservaciones { get; set; }
        public bool PuedeCargarSoporte { get; set; }

        // Acciones de flujo
        public bool PuedePasarAEnElaboracion { get; set; }
        public bool PuedePasarAEnSeguimiento { get; set; }
        public bool PuedePasarAPresentada { get; set; }
        public bool PuedeAprobar { get; set; }
        public bool PuedeRechazar { get; set; }
        public bool PuedeAnular { get; set; }
        public bool PuedeDevolver { get; set; }

        // Adjuntos e historial
        public List<OblAdjunto> Adjuntos { get; set; } = new();
        public List<HistorialItemViewModel> Historial { get; set; } = new();

        // Indicadores
        public bool EstaVencida { get; set; }
        public bool EstaPorVencer { get; set; }
        public bool EsCerrada { get; set; }
        public bool EsAnulada { get; set; }

        //Soportes check post-cierre

        public bool UsaSoportePostCierre { get; set; }

        public string? NombreSoportePostCierre { get; set; }

        public bool MostrarSoportePostCierre { get; set; }

        public bool SoportePostCierreCumplido { get; set; }

        public DateOnly? FechaSoportePostCierre { get; set; }

        public string? NombreUsuarioSoportePostCierre { get; set; }

        public string? UltimoSoportePostCierre { get; set; }

        public int? IdUltimoSoportePostCierre { get; set; }

        public bool PuedeCargarSoportePostCierre { get; set; }
        public string? FechaHoraSoportePostCierreLocal { get; set; }

        public List<TransicionDisponibleViewModel> TransicionesDisponibles { get; set; } = new();
    }
}