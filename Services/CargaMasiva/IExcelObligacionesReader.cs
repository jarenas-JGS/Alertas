using Alertas.ViewModels.CargaMasiva;

namespace Alertas.Services.CargaMasiva
{
    public interface IExcelObligacionesReader
    {
        Task<List<CargaObligacionesFilaViewModel>> LeerArchivoAsync(IFormFile archivo);
    }
}