using Alertas.Data;
using Alertas.Services.Notificaciones.DTOs;
using Microsoft.EntityFrameworkCore;
using Alertas.Models;
using Alertas.Services.Notificaciones.Enums;

namespace Alertas.Services.Notificaciones
{
    public class NotificacionesAlertasService : INotificacionesAlertasService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICalendarioHabilesService _calendarioHabilesService;
        private readonly IEmailService _emailService;
        private readonly IPlantillaCorreoAlertasService _plantillaCorreoService;

        public NotificacionesAlertasService(
            ApplicationDbContext context,
            ICalendarioHabilesService calendarioHabilesService,
            IEmailService emailService,
            IPlantillaCorreoAlertasService plantillaCorreoService)
        {
            _context = context;
            _calendarioHabilesService = calendarioHabilesService;
            _emailService = emailService;
            _plantillaCorreoService = plantillaCorreoService;
        }

        public async Task<ResultadoNotificacionesDto> EnviarAlertasAsync(
            string tipoEjecucion,
            int? idUsuarioEjecucion = null,
            int? idProyecto = null)
        {
            var resultado = await PrepararAlertasAsync(
                tipoEjecucion,
                idUsuarioEjecucion,
                idProyecto);

            var log = new NotificacionLog
            {
                tipo_ejecucion = tipoEjecucion,
                estado = EstadoLogNotificacion.Iniciado,
                fecha_inicio = DateTime.UtcNow,
                id_usuario_ejecucion = idUsuarioEjecucion,
                proyectos_procesados = resultado.ProyectosProcesados,
                alertas_generadas = resultado.AlertasGeneradas
            };

            _context.NotificacionesLog.Add(log);
            await _context.SaveChangesAsync();

            if (!resultado.Correos.Any())
            {
                log.estado = EstadoLogNotificacion.Finalizado;
                log.fecha_fin = DateTime.UtcNow;
                log.correos_enviados = 0;
                log.correos_error = 0;

                await _context.SaveChangesAsync();
                return resultado;
            }

            foreach (var correo in resultado.Correos)
            {
                string asunto = $"Alertas del Proyecto {correo.NombreProyecto}";
                string htmlBody = _plantillaCorreoService.GenerarHtml(correo);

                var envio = new NotificacionEnvio
                {
                    id_proyecto = correo.IdProyecto,
                    id_usuario = correo.IdUsuario,
                    fecha_envio = DateTime.UtcNow,
                    tipo_ejecucion = tipoEjecucion,
                    estado_envio = EstadoEnvioNotificacion.Enviado,
                    asunto = asunto,
                    destinatario_email = correo.EmailUsuario,
                    id_usuario_ejecucion = idUsuarioEjecucion
                };

                foreach (var alerta in correo.Alertas)
                {
                    envio.Detalles.Add(new NotificacionEnvioDetalle
                    {
                        id_grupo_alerta_dia = alerta.IdGrupoAlertaDia,
                        id_reg_obl = alerta.IdRegObl,
                        id_mensaje = alerta.IdMensaje,

                        nombre_alerta = alerta.NombreAlerta,
                        nombre_mensaje = alerta.NombreMensaje,
                        prioridad = alerta.Prioridad,

                        fecha_venc_obl = alerta.FechaVencimientoObligacion,
                        dias_vencimiento_obl = alerta.DiasVencimientoObligacion,

                        fecha_venc_seguimiento = alerta.FechaVencimientoSeguimiento,
                        dias_vencimiento_seguimiento = alerta.DiasVencimientoSeguimiento
                    });
                }

                try
                {
                    await _emailService.EnviarEmailAsync(
                        correo.EmailUsuario,
                        asunto,
                        htmlBody);

                    envio.estado_envio = EstadoEnvioNotificacion.Enviado;
                    resultado.CorreosEnviados++;
                }
                catch (Exception ex)
                {
                    envio.estado_envio = EstadoEnvioNotificacion.Error;
                    envio.error_mensaje = ex.Message;
                    resultado.CorreosError++;
                    resultado.Errores.Add($"{correo.EmailUsuario}: {ex.Message}");
                }
                finally
                {
                    // Resend permite 5 correos por segundo.
                    // Con 250 ms entre envíos quedamos aprox. en 4 por segundo.
                    await Task.Delay(250);
                }

                _context.NotificacionesEnvios.Add(envio);
                await _context.SaveChangesAsync();
            }

            log.estado = resultado.CorreosError > 0
                ? EstadoLogNotificacion.Error
                : EstadoLogNotificacion.Finalizado;

            log.fecha_fin = DateTime.UtcNow;
            log.correos_enviados = resultado.CorreosEnviados;
            log.correos_error = resultado.CorreosError;
            log.alertas_generadas = resultado.AlertasGeneradas;

            if (resultado.Errores.Any())
            {
                log.mensaje_error = string.Join(" | ", resultado.Errores);
            }

            await _context.SaveChangesAsync();

            return resultado;
        }

        public async Task<ResultadoNotificacionesDto> PrepararAlertasAsync(
            string tipoEjecucion,
            int? idUsuarioEjecucion = null,
            int? idProyecto = null)
        {
            var resultado = new ResultadoNotificacionesDto();

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            var proyectosQuery = _context.Proyectos
                .Where(p => p.activo && p.configuracion_completa);

            if (idProyecto.HasValue)
            {
                proyectosQuery = proyectosQuery
                    .Where(p => p.id_proyecto == idProyecto.Value);
            }

            var proyectos = await proyectosQuery
                .Include(p => p.GruposAlertas)
                    .ThenInclude(g => g.GruposAlertasDias)
                        .ThenInclude(d => d.Mensaje)
                .Include(p => p.GruposAlertas)
                    .ThenInclude(g => g.GruposAlertasDias)
                        .ThenInclude(d => d.EstadosOff)
                .AsNoTracking()
                .ToListAsync();

            resultado.ProyectosProcesados = proyectos.Count;

            var alertasGeneradas = new List<AlertaObligacionDto>();

            foreach (var proyecto in proyectos)
            {
                var reglasAlertas = proyecto.GruposAlertas
                    .Where(g => g.activo)
                    .SelectMany(g => g.GruposAlertasDias)
                    .Where(a => a.activo && a.Mensaje.activo)
                    .ToList();

                if (!reglasAlertas.Any())
                    continue;

                var obligaciones = await _context.RegObls
                    .Where(o => o.id_proyecto == proyecto.id_proyecto)
                    .Include(o => o.Empresa)
                    .Include(o => o.UsuariosObligaciones)
                        .ThenInclude(uo => uo.Usuario)
                    .Include(o => o.UsuariosObligaciones)
                        .ThenInclude(uo => uo.Rol)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var obligacion in obligaciones)
                {
                    int diasEvaluacionObligacion = await CalcularDiasEvaluacionAsync(
                        obligacion.fecha_venc_obl,
                        hoy);

                    int diasEvaluacionSeguimiento = await CalcularDiasEvaluacionAsync(
                        obligacion.fecha_venc_seguimiento,
                        hoy);

                    int diasCorridosObligacion = hoy.DayNumber - obligacion.fecha_venc_obl.DayNumber;

                    int diasCorridosSeguimiento = hoy.DayNumber - obligacion.fecha_venc_seguimiento.DayNumber;

                    var alertasCumplidasObligacion = new List<AlertaObligacionDto>();

                    var autorizadores = obligacion.UsuariosObligaciones
                        .Where(uo =>
                            uo.activo &&
                            uo.Usuario.activo &&
                            uo.Rol.nombre.Trim().ToUpper() == "AUTORIZADOR")
                        .Select(uo => uo.Usuario.nombre)
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList();

                    string nombresAutorizadores = autorizadores.Any()
                        ? string.Join(", ", autorizadores)
                        : "";

                    foreach (var regla in reglasAlertas)
                    {

                        if (regla.EstadosOff.Any(e => e.id_estado == obligacion.id_estado))
                            continue;

                        int diasEvaluar = ObtenerDiasEvaluar(
                            regla.tipo_control,
                            diasEvaluacionObligacion,
                            diasEvaluacionSeguimiento);

                        if (!CumpleCondicion(diasEvaluar, regla.operador, regla.valor_dias))
                            continue;

                        var usuariosDestino = obligacion.UsuariosObligaciones
                            .Where(uo =>
                                uo.activo &&
                                uo.id_rol == regla.id_rol &&
                                uo.Usuario.activo &&
                                !string.IsNullOrWhiteSpace(uo.Usuario.email))
                            .ToList();


                        foreach (var usuarioObligacion in usuariosDestino)
                        {
                            alertasCumplidasObligacion.Add(new AlertaObligacionDto
                            {
                                IdProyecto = proyecto.id_proyecto,
                                NombreProyecto = proyecto.nombre,

                                IdUsuario = usuarioObligacion.Usuario.id_usuario,
                                NombreUsuario = usuarioObligacion.Usuario.nombre,
                                EmailUsuario = usuarioObligacion.Usuario.email,

                                Autorizadores = nombresAutorizadores,

                                IdRegObl = obligacion.id_reg_obl,
                                NombreObligacion = obligacion.nombre,

                                IdEmpresa = obligacion.id_empresa,
                                NombreEmpresa = obligacion.Empresa.nombre,

                                IdGrupoAlertaDia = regla.id_grupo_alerta_dia,
                                NombreAlerta = regla.nombre,

                                IdMensaje = regla.id_mensaje,
                                NombreMensaje = regla.Mensaje.nombre,
                                TextoMensaje = regla.Mensaje.texto,
                                Prioridad = regla.Mensaje.prioridad,

                                FechaVencimientoObligacion = obligacion.fecha_venc_obl,
                                DiasVencimientoObligacion = diasEvaluacionObligacion,

                                FechaVencimientoSeguimiento = obligacion.fecha_venc_seguimiento,
                                DiasVencimientoSeguimiento = diasEvaluacionSeguimiento
                            });
                        }
                    }

                    alertasCumplidasObligacion = AplicarDependencias(
                        alertasCumplidasObligacion,
                        reglasAlertas);

                    alertasCumplidasObligacion = alertasCumplidasObligacion
                        .GroupBy(a => new
                        {
                            a.IdProyecto,
                            a.IdUsuario,
                            a.IdRegObl,
                            NombreAlerta = a.NombreAlerta.Trim().ToUpper(),
                            a.IdMensaje
                        })
                        .Select(g => g.First())
                        .ToList();

                    alertasGeneradas.AddRange(alertasCumplidasObligacion);
                }
            }

            alertasGeneradas = alertasGeneradas
                .GroupBy(a => new
                {
                    a.IdProyecto,
                    a.IdUsuario,
                    a.IdRegObl,
                    NombreAlerta = a.NombreAlerta.Trim().ToUpper(),
                    a.IdMensaje
                })
                .Select(g => g.First())
                .ToList();

            resultado.AlertasGeneradas = alertasGeneradas.Count;

            resultado.Correos = alertasGeneradas
                .GroupBy(a => new
                {
                    a.IdProyecto,
                    a.NombreProyecto,
                    a.IdUsuario,
                    a.NombreUsuario,
                    a.EmailUsuario
                })
                .Select(g => new GrupoAlertasUsuarioDto
                {
                    IdProyecto = g.Key.IdProyecto,
                    NombreProyecto = g.Key.NombreProyecto,
                    IdUsuario = g.Key.IdUsuario,
                    NombreUsuario = g.Key.NombreUsuario,
                    EmailUsuario = g.Key.EmailUsuario,
                    Alertas = g
                        .OrderBy(a => a.Prioridad)
                        .ThenBy(a => a.NombreAlerta)
                        .ThenBy(a => a.FechaVencimientoObligacion)
                        .ToList()
                })
                .ToList();

            resultado.CorreosPreparados = resultado.Correos.Count;

            return resultado;
        }

        private int ObtenerDiasEvaluar(
            string tipoControl,
            int diasObligacion,
            int diasSeguimiento)
        {
            var tipo = tipoControl.ToUpper().Trim();

            return tipo switch
            {
                "VENCIMIENTO" => diasObligacion,
                "SEGUIMIENTO" => diasSeguimiento,
                _ => diasObligacion
            };
        }

        private bool CumpleCondicion(int dias, string operador, int valorDias)
        {
            return operador.Trim() switch
            {
                "=" => dias == valorDias,
                "==" => dias == valorDias,
                ">" => dias > valorDias,
                ">=" => dias >= valorDias,
                "<" => dias < valorDias,
                "<=" => dias <= valorDias,
                "!=" => dias != valorDias,
                "<>" => dias != valorDias,
                _ => false
            };
        }

        private List<AlertaObligacionDto> AplicarDependencias(
            List<AlertaObligacionDto> alertas,
            List<Models.GrupoAlertaDia> reglas)
        {
            if (!alertas.Any())
                return alertas;

            var idsAlertasCumplidas = alertas
                .Select(a => a.IdGrupoAlertaDia)
                .Distinct()
                .ToHashSet();

            var idsDependientesAOmitir = reglas
                .Where(r =>
                    r.id_dependencia.HasValue &&
                    idsAlertasCumplidas.Contains(r.id_grupo_alerta_dia) &&
                    idsAlertasCumplidas.Contains(r.id_dependencia.Value))
                .Select(r => r.id_dependencia!.Value)
                .ToHashSet();

            return alertas
                .Where(a => !idsDependientesAOmitir.Contains(a.IdGrupoAlertaDia))
                .ToList();
        }

        private async Task<int> CalcularDiasEvaluacionAsync(DateOnly fechaBase, DateOnly hoy)
        {
            if (hoy <= fechaBase)
            {
                // Antes del vencimiento o el mismo día:
                // devuelve negativo si falta tiempo, 0 si vence hoy.
                return await _calendarioHabilesService
                    .CalcularDiasHabilesAsync(fechaBase, hoy);
            }

            // Después del vencimiento:
            // devuelve positivo en días corridos.
            return hoy.DayNumber - fechaBase.DayNumber;
        }
    }
}