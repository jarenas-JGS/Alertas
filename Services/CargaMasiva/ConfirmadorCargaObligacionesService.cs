using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels.CargaMasiva;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Services.CargaMasiva
{
    public class ConfirmadorCargaObligacionesService : IConfirmadorCargaObligacionesService
    {
        private readonly ApplicationDbContext _context;

        public ConfirmadorCargaObligacionesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ResultadoCargaObligacionesViewModel> ConfirmarAsync(
            CargaObligacionesTemporalViewModel carga,
            int idUsuarioActual)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var proyecto = await _context.Proyectos
                    .FirstOrDefaultAsync(p =>
                        p.id_proyecto == carga.id_proyecto &&
                        p.activo &&
                        p.configuracion_completa);

                if (proyecto == null)
                    throw new InvalidOperationException("El proyecto no existe, no está activo o no está completamente configurado.");

                var estadoInicial = await _context.Estados
                    .Where(e =>
                        e.id_proyecto == proyecto.id_proyecto &&
                        e.activo)
                    .OrderBy(e => e.orden)
                    .FirstOrDefaultAsync();

                if (estadoInicial == null)
                    throw new InvalidOperationException("El proyecto no tiene estado inicial configurado.");

                var roles = await _context.Roles
                    .Where(r => r.Activo)
                    .ToDictionaryAsync(r => r.nombre, r => r.id_rol);

                int idRolResponsable = ObtenerRol(roles, "Responsable");
                int idRolElaborador = ObtenerRol(roles, "Elaborador");
                int idRolAutorizador = ObtenerRol(roles, "Autorizador");
                int idRolAprobador = ObtenerRol(roles, "Aprobador");
                int idRolVencimiento = ObtenerRol(roles, "Vencimiento");

                int totalInsertadas = 0;

                foreach (var fila in carga.filas)
                {
                    var cliente = await ObtenerClienteAsync(fila.cliente);
                    var empresa = await ObtenerEmpresaAsync(fila.empresa);
                    var ciudad = await ObtenerCiudadAsync(fila.ciudad);
                    var periodo = await ObtenerPeriodoAsync(fila.periodo);
                    var dominio = await ObtenerDominioAsync(fila.dominio);
                    var tipoObligacion = await ObtenerTipoObligacionAsync(fila.tipo_obligacion, proyecto.id_area);

                    var fechaVencObl = DateOnly.FromDateTime(fila.fecha_vencimiento_obligacion!.Value);
                    var fechaVencSeg = DateOnly.FromDateTime(fila.fecha_vencimiento_seguimiento!.Value);

                    var valorAprox = ConvertirDecimalAInt(fila.valor_aproximado);
                    var saldoFavor = ConvertirDecimalAInt(fila.saldo_favor);

                    var regObl = new RegObl
                    {
                        nombre = fila.nombre ?? string.Empty,
                        cod_obligacion = fila.codigo_obligacion,

                        id_cliente = cliente.id_cliente,
                        id_empresa = empresa.id_empresa,
                        id_ciudad = ciudad?.id_ciudad,
                        id_periodo = periodo.id_periodo,
                        id_dominio = dominio.id_dominio,
                        id_tipo_obligacion = tipoObligacion.id_tipo_obligacion,
                        id_proyecto = proyecto.id_proyecto,

                        fecha_venc_obl = fechaVencObl,
                        fecha_venc_seguimiento = fechaVencSeg,

                        vigencia = fila.vigencia!.Value,
                        anio = fechaVencObl.Year,
                        mes = fechaVencObl.Month,
                        dia = fechaVencObl.Day,

                        vlr_aprox = valorAprox,
                        saldo_favor = saldoFavor,
                        vlr_real = null,
                        diferencia = null,
                        variacion = null,

                        cc_empleador = fila.cc_empleador,
                        nombre_empleador = fila.empleador,
                        cc_empleado = fila.cc_empleado,
                        nombre_empleado = fila.empleado,

                        observaciones = fila.observaciones,

                        id_estado = estadoInicial.id_estado,
                        fecha_creac = DateOnly.FromDateTime(DateTime.Today),

                        fecha_ult_modif = DateTime.UtcNow,
                        id_usuario_ult_modif = idUsuarioActual,

                        soporte_post_cierre_cumplido = false,
                        fecha_soporte_post_cierre = null,
                        id_usuario_soporte_post_cierre = null,
                    };

                    _context.RegObls.Add(regObl);
                    await _context.SaveChangesAsync();

                    await CrearUsuariosObligacionAsync(regObl.id_reg_obl, fila.responsable, idRolResponsable, idUsuarioActual);
                    await CrearUsuariosObligacionAsync(regObl.id_reg_obl, fila.elaborador, idRolElaborador, idUsuarioActual);
                    await CrearUsuariosObligacionAsync(regObl.id_reg_obl, fila.autorizador, idRolAutorizador, idUsuarioActual);
                    await CrearUsuariosObligacionAsync(regObl.id_reg_obl, fila.aprobador, idRolAprobador, idUsuarioActual);
                    await CrearUsuariosObligacionAsync(regObl.id_reg_obl, fila.usuario_vencimiento, idRolVencimiento, idUsuarioActual);

                    _context.HistOblFlujos.Add(new HistOblFlujo
                    {
                        id_reg_obl = regObl.id_reg_obl,
                        id_estado_origen = null,
                        id_estado_destino = estadoInicial.id_estado,
                        accion = "Creación por cargue masivo",
                        observacion = "Obligación creada desde plantilla Excel.",
                        id_usuario = idUsuarioActual,
                        fecha = DateTime.UtcNow,
                        rol_ejecutor = "Cargue masivo",
                        es_automatico = true
                    });

                    totalInsertadas++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ResultadoCargaObligacionesViewModel
                {
                    exitoso = true,
                    total_insertadas = totalInsertadas,
                    mensaje = $"Se cargaron correctamente {totalInsertadas} obligaciones."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                var mensajes = new List<string>();
                var excepcionActual = ex;

                while (excepcionActual != null)
                {
                    mensajes.Add(excepcionActual.Message);
                    excepcionActual = excepcionActual.InnerException;
                }

                var detalle = string.Join(" | ", mensajes.Distinct());

                throw new Exception($"ERROR DETALLADO: {detalle}", ex);
            }
        }

        private static int ObtenerRol(Dictionary<string, int> roles, string nombreRol)
        {
            var rol = roles
                .FirstOrDefault(r => string.Equals(r.Key, nombreRol, StringComparison.OrdinalIgnoreCase));

            if (rol.Value == 0)
                throw new InvalidOperationException($"No existe el rol requerido: {nombreRol}");

            return rol.Value;
        }

        private async Task<Cliente> ObtenerClienteAsync(string? valor)
        {
            var nit = ExtraerCodigo(valor);

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.activo && c.nit == nit);

            return cliente ?? throw new InvalidOperationException($"Cliente no válido: {valor}");
        }

        private async Task<Empresa> ObtenerEmpresaAsync(string? valor)
        {
            var nit = ExtraerCodigo(valor);

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.activo && e.nit == nit);

            return empresa ?? throw new InvalidOperationException($"Empresa no válida: {valor}");
        }

        private async Task<Ciudad?> ObtenerCiudadAsync(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            var valorNormalizado = NormalizarTexto(valor);

            return await _context.Ciudades
                .FirstOrDefaultAsync(p =>
                    p.nombre.Trim().ToUpper() == valorNormalizado);
        }

        private async Task<Periodo> ObtenerPeriodoAsync(string? valor)
        {
            var valorNormalizado = NormalizarTexto(valor);

            var periodo = await _context.Periodos
                .FirstOrDefaultAsync(p =>
                    p.nombre.Trim().ToUpper() == valorNormalizado);

            return periodo ?? throw new InvalidOperationException($"Periodo no válido: {valor}");
        }

        private async Task<Dominio> ObtenerDominioAsync(string? valor)
        {
            var valorNormalizado = NormalizarTexto(valor);

            var dominio = await _context.Dominios
                .FirstOrDefaultAsync(p =>
                    p.nombre.Trim().ToUpper() == valorNormalizado);

            return dominio ?? throw new InvalidOperationException($"Dominio no válido: {valor}");
        }

        private async Task<TipoObligacion> ObtenerTipoObligacionAsync(string? valor, int idArea)
        {
            var valorNormalizado = NormalizarTexto(valor);

            var tipo = await _context.TipoObligaciones
                .FirstOrDefaultAsync(t =>
                    t.nombre.Trim().ToUpper() == valorNormalizado &&
                    t.id_area == idArea &&
                    t.activo);

            return tipo ?? throw new InvalidOperationException($"Tipo de obligación no válido: {valor}");
        }

        private async Task CrearUsuariosObligacionAsync(
            int idRegObl,
            string? usuariosTexto,
            int idRol,
            int idUsuarioAsignacion)
        {
            var usuarios = SepararUsuarios(usuariosTexto);

            foreach (var usuarioTexto in usuarios)
            {
                var email = ExtraerCodigo(usuarioTexto);

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.activo &&
                        u.email == email);

                if (usuario == null)
                    throw new InvalidOperationException($"Usuario no válido: {usuarioTexto}");

                _context.UsuariosObligaciones.Add(new UsuarioObligacion
                {
                    id_reg_obl = idRegObl,
                    id_usuario = usuario.id_usuario,
                    id_rol = idRol,
                    activo = true,
                    fecha_asignacion = DateTime.UtcNow,
                    id_usuario_asignacion = idUsuarioAsignacion
                });
            }
        }

        private static List<string> SepararUsuarios(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return new List<string>();

            return valor
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string ExtraerCodigo(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            var partes = valor.Split(" - ", 2, StringSplitOptions.None);
            return partes[0].Trim();
        }

        private static int? ConvertirDecimalAInt(decimal? valor)
        {
            if (!valor.HasValue)
                return null;

            return Convert.ToInt32(Math.Round(valor.Value, 0));
        }

        private static string NormalizarTexto(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : valor.Trim().ToUpper();
        }
    }
}