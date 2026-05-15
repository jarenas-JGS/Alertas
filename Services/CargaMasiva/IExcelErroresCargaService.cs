using Alertas.ViewModels.CargaMasiva;

namespace Alertas.Services.CargaMasiva
{
    public interface IExcelErroresCargaService
    {
        ArchivoGeneradoViewModel GenerarExcelErrores(
            CargaObligacionesPreviewViewModel preview);
    }
}