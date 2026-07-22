using Alertas.Data;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Services.ConfiguracionOperativa
{
    public class ConfiguracionOperativaService : IConfiguracionOperativaService
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracionOperativaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> EstaHabilitadoAsync(
            string clave,
            CancellationToken cancellationToken = default)
        {
            var valor = await _context.ConfiguracionesOperativas
                .AsNoTracking()
                .Where(x => x.clave == clave)
                .Select(x => x.valor)
                .FirstOrDefaultAsync(cancellationToken);

            // Si la configuración no existe o no es válida,
            // se considera deshabilitada por seguridad.
            return bool.TryParse(valor, out var habilitado) && habilitado;
        }

        public async Task CambiarEstadoAsync(
            string clave,
            bool habilitado,
            int idUsuario,
            CancellationToken cancellationToken = default)
        {
            var configuracion = await _context.ConfiguracionesOperativas
                .FirstOrDefaultAsync(x => x.clave == clave, cancellationToken);

            if (configuracion == null)
            {
                throw new InvalidOperationException(
                    $"No existe la configuración operativa '{clave}'.");
            }

            configuracion.valor = habilitado.ToString().ToLowerInvariant();
            configuracion.fecha_actualizacion = DateTime.UtcNow;
            configuracion.id_usuario_actualizacion = idUsuario;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}