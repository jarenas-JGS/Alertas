using Alertas.Data;
using Alertas.Models;
using Alertas.ViewModels.CargaMasiva;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Alertas.Services.CargaMasiva
{
    public class ConfirmadorCargaObligacionesService : IConfirmadorCargaObligacionesService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConfirmadorCargaObligacionesService> _logger;

        public ConfirmadorCargaObligacionesService(
            ApplicationDbContext context,
            ILogger<ConfirmadorCargaObligacionesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoCargaObligacionesViewModel> ConfirmarAsync(
            CargaObligacionesTemporalViewModel carga,
            int idUsuarioActual)
        {
            var stopwatch = Stopwatch.StartNew();

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

                // ===============================
                // IDs de los roles del sistema
                // ===============================

                int idRolResponsable = ObtenerRol(roles, "Responsable");
                int idRolElaborador = ObtenerRol(roles, "Elaborador");
                int idRolAutorizador = ObtenerRol(roles, "Autorizador");
                int idRolAprobador = ObtenerRol(roles, "Aprobador");
                int idRolVencimiento = ObtenerRol(roles, "Vencimiento");

                // ======================================================
                // Precarga de catálogos para evitar consultas por fila
                // ======================================================

                var clientesLista = await _context.Clientes
                    .AsNoTracking()
                    .Where(c => c.activo)
                    .ToListAsync();

                var clientesPorNit = clientesLista
                    .Where(c => !string.IsNullOrWhiteSpace(c.nit))
                    .GroupBy(c => NormalizarTexto(c.nit))
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.First());


                var empresasLista = await _context.Empresas
                    .AsNoTracking()
                    .Where(e => e.activo)
                    .ToListAsync();

                var empresasPorNit = empresasLista
                    .Where(e => !string.IsNullOrWhiteSpace(e.nit))
                    .GroupBy(e => NormalizarTexto(e.nit))
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.First());


                var ciudadesLista = await _context.Ciudades
                    .AsNoTracking()
                    .ToListAsync();

                var ciudadesPorNombre = ciudadesLista
                    .Where(c => !string.IsNullOrWhiteSpace(c.nombre))
                    .GroupBy(c => NormalizarTexto(c.nombre))
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.First());


                var periodosLista = await _context.Periodos
                    .AsNoTracking()
                    .ToListAsync();

                var periodosPorNombre = periodosLista
                    .Where(p => !string.IsNullOrWhiteSpace(p.nombre))
                    .GroupBy(p => NormalizarTexto(p.nombre))
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.First());


                var dominiosLista = await _context.Dominios
                    .AsNoTracking()
                    .ToListAsync();

                var dominiosPorNombre = dominiosLista
                    .Where(d => !string.IsNullOrWhiteSpace(d.nombre))
                    .GroupBy(d => NormalizarTexto(d.nombre))
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.First());


                var tiposObligacionLista = await _context.TipoObligaciones
                    .AsNoTracking()
                    .Where(t =>
                        t.id_area == proyecto.id_area &&
                        t.activo)
                    .ToListAsync();

                var tiposObligacionPorNombre = tiposObligacionLista
                    .Where(t => !string.IsNullOrWhiteSpace(t.nombre))
                    .GroupBy(t => NormalizarTexto(t.nombre))
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.First());

                // ======================================================
                // Precarga de usuarios activos
                // ======================================================

                var usuariosLista = await _context.Usuarios
                    .AsNoTracking()
                    .Where(u =>
                        u.activo &&
                        !string.IsNullOrWhiteSpace(u.email))
                    .ToListAsync();

                var usuariosPorEmail = usuariosLista
                    .GroupBy(u => NormalizarTexto(u.email))
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.First());


                int totalInsertadas = 0;

                const int TAMANO_LOTE = 50;

                foreach (var lote in carga.filas.Chunk(TAMANO_LOTE))
                {
                    /*
                     * En esta lista conservamos la relación entre:
                     *
                     * - La fila original del Excel.
                     * - La entidad RegObl creada para esa fila.
                     *
                     * Después del primer SaveChangesAsync, cada RegObl
                     * tendrá asignado su id_reg_obl.
                     */
                    var obligacionesDelLote =
                        new List<(CargaObligacionesFilaViewModel Fila, RegObl Obligacion)>();

                    // ======================================================
                    // PASO 1: preparar las obligaciones del lote
                    // ======================================================

                    foreach (var fila in lote)
                    {
                        var nitCliente = NormalizarTexto(
                            ExtraerCodigo(fila.cliente));

                        if (!clientesPorNit.TryGetValue(
                            nitCliente,
                            out var cliente))
                        {
                            throw new InvalidOperationException(
                                $"Cliente no válido: {fila.cliente}");
                        }

                        var nitEmpresa = NormalizarTexto(
                            ExtraerCodigo(fila.empresa));

                        if (!empresasPorNit.TryGetValue(
                            nitEmpresa,
                            out var empresa))
                        {
                            throw new InvalidOperationException(
                                $"Empresa no válida: {fila.empresa}");
                        }

                        Ciudad? ciudad = null;

                        if (!string.IsNullOrWhiteSpace(fila.ciudad))
                        {
                            var ciudadNormalizada =
                                NormalizarTexto(fila.ciudad);

                            ciudadesPorNombre.TryGetValue(
                                ciudadNormalizada,
                                out ciudad);
                        }

                        var periodoNormalizado =
                            NormalizarTexto(fila.periodo);

                        if (!periodosPorNombre.TryGetValue(
                            periodoNormalizado,
                            out var periodo))
                        {
                            throw new InvalidOperationException(
                                $"Periodo no válido: {fila.periodo}");
                        }

                        var dominioNormalizado =
                            NormalizarTexto(fila.dominio);

                        if (!dominiosPorNombre.TryGetValue(
                            dominioNormalizado,
                            out var dominio))
                        {
                            throw new InvalidOperationException(
                                $"Dominio no válido: {fila.dominio}");
                        }

                        var tipoObligacionNormalizado =
                            NormalizarTexto(fila.tipo_obligacion);

                        if (!tiposObligacionPorNombre.TryGetValue(
                            tipoObligacionNormalizado,
                            out var tipoObligacion))
                        {
                            throw new InvalidOperationException(
                                $"Tipo de obligación no válido: " +
                                $"{fila.tipo_obligacion}");
                        }

                        var fechaVencObl = DateOnly.FromDateTime(
                            fila.fecha_vencimiento_obligacion!.Value);

                        var fechaVencSeg = DateOnly.FromDateTime(
                            fila.fecha_vencimiento_seguimiento!.Value);

                        var valorAprox =
                            ConvertirDecimalAInt(fila.valor_aproximado);

                        var saldoFavor =
                            ConvertirDecimalAInt(fila.saldo_favor);

                        var regObl = new RegObl
                        {
                            nombre = fila.nombre ?? string.Empty,
                            cod_obligacion = fila.codigo_obligacion,

                            id_cliente = cliente.id_cliente,
                            id_empresa = empresa.id_empresa,
                            id_ciudad = ciudad?.id_ciudad,
                            id_periodo = periodo.id_periodo,
                            id_dominio = dominio.id_dominio,
                            id_tipo_obligacion =
                                tipoObligacion.id_tipo_obligacion,
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

                            fecha_creac =
                                DateOnly.FromDateTime(DateTime.Today),

                            fecha_ult_modif = DateTime.UtcNow,
                            id_usuario_ult_modif = idUsuarioActual,

                            soporte_post_cierre_cumplido = false,
                            fecha_soporte_post_cierre = null,
                            id_usuario_soporte_post_cierre = null
                        };

                        obligacionesDelLote.Add(
                            (fila, regObl));
                    }

                    // ======================================================
                    // PASO 2: guardar las obligaciones del lote
                    //
                    // Después de este SaveChangesAsync, PostgreSQL habrá
                    // generado los id_reg_obl.
                    // ======================================================

                    _context.RegObls.AddRange(
                        obligacionesDelLote.Select(x => x.Obligacion));

                    await _context.SaveChangesAsync();

                    // ======================================================
                    // PASO 3: crear participantes e historial
                    // ======================================================

                    foreach (var registro in obligacionesDelLote)
                    {
                        var fila = registro.Fila;
                        var regObl = registro.Obligacion;

                        CrearUsuariosObligacion(
                            regObl.id_reg_obl,
                            fila.responsable,
                            idRolResponsable,
                            idUsuarioActual,
                            usuariosPorEmail);

                        CrearUsuariosObligacion(
                            regObl.id_reg_obl,
                            fila.elaborador,
                            idRolElaborador,
                            idUsuarioActual,
                            usuariosPorEmail);

                        CrearUsuariosObligacion(
                            regObl.id_reg_obl,
                            fila.autorizador,
                            idRolAutorizador,
                            idUsuarioActual,
                            usuariosPorEmail);

                        CrearUsuariosObligacion(
                            regObl.id_reg_obl,
                            fila.aprobador,
                            idRolAprobador,
                            idUsuarioActual,
                            usuariosPorEmail);

                        CrearUsuariosObligacion(
                            regObl.id_reg_obl,
                            fila.usuario_vencimiento,
                            idRolVencimiento,
                            idUsuarioActual,
                            usuariosPorEmail);

                        _context.HistOblFlujos.Add(
                            new HistOblFlujo
                            {
                                id_reg_obl = regObl.id_reg_obl,
                                id_estado_origen = null,
                                id_estado_destino =
                                    estadoInicial.id_estado,

                                accion =
                                    "Creación por cargue masivo",

                                observacion =
                                    "Obligación creada desde plantilla Excel.",

                                id_usuario = idUsuarioActual,
                                fecha = DateTime.UtcNow,

                                rol_ejecutor = "Cargue masivo",
                                es_automatico = true
                            });
                    }

                    // ======================================================
                    // PASO 4: guardar participantes e historial
                    // ======================================================

                    await _context.SaveChangesAsync();

                    totalInsertadas += obligacionesDelLote.Count;

                    _logger.LogInformation(
                        "Cargue masivo en progreso. " +
                        "Proyecto: {IdProyecto}. " +
                        "Procesadas: {Procesadas}/{Total}. " +
                        "Tiempo: {TiempoSegundos:N2} segundos.",
                        carga.id_proyecto,
                        totalInsertadas,
                        carga.filas.Count,
                        stopwatch.Elapsed.TotalSeconds);

                    /*
                     * En este punto todo el lote ya fue guardado.
                     *
                     * Clear evita que EF Core conserve en memoria todas
                     * las obligaciones, participantes e historiales de
                     * los lotes anteriores.
                     *
                     * No afecta la transacción de PostgreSQL.
                     */
                    _context.ChangeTracker.Clear();
                }


                await transaction.CommitAsync();

                stopwatch.Stop();

                _logger.LogInformation(
                    "Cargue masivo finalizado correctamente. Proyecto: {IdProyecto}. " +
                    "Obligaciones insertadas: {Cantidad}. Tiempo total: {TiempoSegundos:N2} segundos.",
                    carga.id_proyecto,
                    totalInsertadas,
                    stopwatch.Elapsed.TotalSeconds);

                string tiempoTranscurrido =
                    stopwatch.Elapsed.TotalMinutes >= 1
                        ? $"{stopwatch.Elapsed.TotalMinutes:N2} minutos"
                        : $"{stopwatch.Elapsed.TotalSeconds:N2} segundos";

                return new ResultadoCargaObligacionesViewModel
                {
                    exitoso = true,
                    total_insertadas = totalInsertadas,
                    mensaje =
                        $"Se cargaron correctamente {totalInsertadas} obligaciones " +
                        $"en {tiempoTranscurrido}."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Error en cargue masivo. Proyecto: {IdProyecto}. " +
                    "Tiempo transcurrido: {TiempoSegundos:N2} segundos.",
                    carga.id_proyecto,
                    stopwatch.Elapsed.TotalSeconds);

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


        private void CrearUsuariosObligacion(
            int idRegObl,
            string? usuariosTexto,
            int idRol,
            int idUsuarioAsignacion,
            IReadOnlyDictionary<string, Usuario> usuariosPorEmail)
        {
            var usuarios = SepararUsuarios(usuariosTexto);

            foreach (var usuarioTexto in usuarios)
            {
                var email = ExtraerCodigo(usuarioTexto);
                var emailNormalizado = NormalizarTexto(email);

                if (!usuariosPorEmail.TryGetValue(
                    emailNormalizado,
                    out var usuario))
                {
                    throw new InvalidOperationException(
                        $"Usuario no válido: {usuarioTexto}");
                }

                _context.UsuariosObligaciones.Add(
                    new UsuarioObligacion
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