namespace Alertas.Services.ConfiguracionOperativa
{
    public interface IConfiguracionOperativaService
    {
        Task<bool> EstaHabilitadoAsync(
            string clave,
            CancellationToken cancellationToken = default);

        Task CambiarEstadoAsync(
            string clave,
            bool habilitado,
            int idUsuario,
            CancellationToken cancellationToken = default);
    }
}