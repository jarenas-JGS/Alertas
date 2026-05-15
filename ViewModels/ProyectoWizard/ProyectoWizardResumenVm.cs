namespace Alertas.ViewModels.ProyectoWizard
{
    public class ProyectoWizardResumenVm
    {
        public int id_proyecto { get; set; }

        public string nombre_proyecto { get; set; } = string.Empty;
        public string area { get; set; } = string.Empty;
        public string nombre_seguimiento { get; set; } = string.Empty;

        public int total_estados { get; set; }
        public int total_transiciones { get; set; }
        public int total_roles_transicion { get; set; }
        public int total_grupos_alertas { get; set; }
        public int total_alertas_dias { get; set; }
        public int total_dependencias { get; set; }
        public int total_estados_off { get; set; }

        public bool tiene_estado_seguimiento { get; set; }
        public bool tiene_estado_vencimiento { get; set; }
        public bool tiene_cerrada { get; set; }
        public bool tiene_anulada { get; set; }

        public List<string> Errores { get; set; } = new();

        public bool configuracion_valida => !Errores.Any();
    }
}