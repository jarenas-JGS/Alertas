using Alertas.Data;
using Alertas.ViewModels.RutinasActualizacion;
using Microsoft.EntityFrameworkCore;
using Alertas.Models;

namespace Alertas.Services.RutinasActualizacion
{
    public class RutinasActualizacionService
        : IRutinasActualizacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly SeguridadService _seguridad;

        public RutinasActualizacionService(
            ApplicationDbContext context,
            SeguridadService seguridad)
        {
            _context = context;
            _seguridad = seguridad;
        }

        public async Task<RutinaParticipantesPreviewVm> GenerarPreviewAsync(
            RutinaParticipantesVm model)
        {
            var preview = new RutinaParticipantesPreviewVm
            {
                Parametros = model
            };

            if (model.IdProyecto == null ||
                model.IdEmpresa == null ||
                model.IdRol == null)
            {
                preview.Advertencias.Add("Faltan parámetros obligatorios para generar el preview.");
                return preview;
            }

            var puedeAdministrar = await _seguridad.UsuarioPuedeAdministrarProyectoAsync(
                model.IdProyecto.Value);

            if (!puedeAdministrar)
            {
                preview.Advertencias.Add("No tiene permisos para administrar este proyecto.");
                return preview;
            }

            var obligaciones = await _context.RegObls
                .Where(ro =>
                    ro.id_proyecto == model.IdProyecto.Value &&
                    ro.id_empresa == model.IdEmpresa.Value)
                .Join(_context.Empresas,
                    ro => ro.id_empresa,
                    e => e.id_empresa,
                    (ro, e) => new { ro, e })
                .Join(_context.Estados,
                    x => x.ro.id_estado,
                    es => es.id_estado,
                    (x, es) => new
                    {
                        x.ro.id_reg_obl,
                        x.ro.nombre,
                        Empresa = x.e.nombre,
                        Estado = es.nombre
                    })
                .OrderBy(x => x.nombre)
                .ToListAsync();

            preview.TotalObligaciones = obligaciones.Count;

            if (!obligaciones.Any())
            {
                preview.Advertencias.Add("No existen obligaciones para el proyecto y empresa seleccionados.");
                return preview;
            }

            var idsObligaciones = obligaciones
                .Select(x => x.id_reg_obl)
                .ToList();

            var usuariosObligaciones = await _context.UsuariosObligaciones
                .Where(uo =>
                    idsObligaciones.Contains(uo.id_reg_obl) &&
                    uo.id_rol == model.IdRol.Value)
                .ToListAsync();

            var nombreRol = await _context.Roles
                .Where(r => r.id_rol == model.IdRol.Value)
                .Select(r => r.nombre)
                .FirstOrDefaultAsync();

            var nombreUsuarioOrigen = model.IdUsuarioOrigen.HasValue
                ? await _context.Usuarios
                    .Where(u => u.id_usuario == model.IdUsuarioOrigen.Value)
                    .Select(u => u.nombre)
                    .FirstOrDefaultAsync()
                : null;

            var nombreUsuarioDestino = model.IdUsuarioDestino.HasValue
                ? await _context.Usuarios
                    .Where(u => u.id_usuario == model.IdUsuarioDestino.Value)
                    .Select(u => u.nombre)
                    .FirstOrDefaultAsync()
                : null;

            foreach (var obl in obligaciones)
            {
                var item = new ObligacionPreviewVm
                {
                    IdRegObl = obl.id_reg_obl,
                    NombreObligacion = obl.nombre,
                    Empresa = obl.Empresa,
                    Estado = obl.Estado
                };

                var cantidadActivosRol = usuariosObligaciones
                    .Count(uo =>
                        uo.id_reg_obl == obl.id_reg_obl &&
                        uo.activo);

                if (model.Accion == "INCLUIR")
                {
                    var existente = usuariosObligaciones
                        .FirstOrDefault(uo =>
                            uo.id_reg_obl == obl.id_reg_obl &&
                            uo.id_usuario == model.IdUsuarioDestino);

                    if (existente == null)
                    {
                        item.ResultadoEsperado = $"Incluir {nombreUsuarioDestino} como {nombreRol}.";
                        item.Observacion = "El usuario no está asignado en este rol. Se insertará un nuevo registro.";
                    }
                    else if (!existente.activo)
                    {
                        item.ResultadoEsperado = $"Reactivar {nombreUsuarioDestino} como {nombreRol}.";
                        item.Observacion = "El usuario ya existía en este rol, pero estaba inactivo. Se reactivará.";
                    }
                    else
                    {
                        item.ResultadoEsperado = $"Omitir {nombreUsuarioDestino}.";
                        item.Observacion = "El usuario ya está asignado y activo en este rol para esta obligación.";
                    }
                }

                if (model.Accion == "ELIMINAR")
                {
                    var existente = usuariosObligaciones
                        .FirstOrDefault(uo =>
                            uo.id_reg_obl == obl.id_reg_obl &&
                            uo.id_usuario == model.IdUsuarioOrigen &&
                            uo.activo);

                    if (existente != null)
                    {
                        if (cantidadActivosRol <= 1)
                        {
                            item.ResultadoEsperado = $"No se inactivará {nombreUsuarioOrigen}.";
                            item.Observacion = "No se puede inactivar este usuario porque la obligación quedaría sin usuarios activos en este rol. Este registro se muestra como advertencia y no será ejecutado.";
                            item.MostrarAunqueSeaOmitida = true;
                        }
                        else
                        {
                            item.ResultadoEsperado = $"Inactivar {nombreUsuarioOrigen} como {nombreRol}.";
                            item.Observacion = "Se inactivará la relación existente.";
                        }
                    }
                    else
                    {
                        item.ResultadoEsperado = $"Omitir {nombreUsuarioOrigen}.";
                        item.Observacion = "El usuario no está activo en este rol para esta obligación.";
                    }
                }

                if (model.Accion == "CAMBIAR")
                {
                    var origenActivo = usuariosObligaciones
                        .FirstOrDefault(uo =>
                            uo.id_reg_obl == obl.id_reg_obl &&
                            uo.id_usuario == model.IdUsuarioOrigen &&
                            uo.activo);

                    var destinoExistente = usuariosObligaciones
                        .FirstOrDefault(uo =>
                            uo.id_reg_obl == obl.id_reg_obl &&
                            uo.id_usuario == model.IdUsuarioDestino);

                    if (origenActivo == null)
                    {
                        item.ResultadoEsperado = "Omitir cambio.";
                        item.Observacion = $"{nombreUsuarioOrigen} no está activo en este rol para esta obligación.";
                    }
                    else if (destinoExistente != null && destinoExistente.activo)
                    {
                        item.ResultadoEsperado = $"Inactivar {nombreUsuarioOrigen}.";
                        item.Observacion = $"{nombreUsuarioDestino} ya está asignado y activo como {nombreRol}. Solo se inactivará el usuario actual.";
                    }
                    else if (destinoExistente != null && !destinoExistente.activo)
                    {
                        item.ResultadoEsperado = $"Cambiar {nombreUsuarioOrigen} por {nombreUsuarioDestino}.";
                        item.Observacion = $"{nombreUsuarioDestino} ya existía como {nombreRol}, pero estaba inactivo. Se inactivará el usuario actual y se reactivará el usuario nuevo.";
                    }
                    else
                    {
                        item.ResultadoEsperado = $"Cambiar {nombreUsuarioOrigen} por {nombreUsuarioDestino}.";
                        item.Observacion = $"{nombreUsuarioDestino} no está asignado en este rol. Se inactivará el usuario actual y se insertará el usuario nuevo.";
                    }
                }

                bool esOmitida =
                    item.ResultadoEsperado.StartsWith("Omitir", StringComparison.OrdinalIgnoreCase);

                if (esOmitida)
                {
                    preview.TotalOmitidas++;

                    if (item.MostrarAunqueSeaOmitida)
                        preview.Obligaciones.Add(item);
                }
                else
                {
                    preview.TotalAfectadas++;
                    preview.Obligaciones.Add(item);
                }
            }

            if (preview.TotalAfectadas == 0)
            {
                preview.Advertencias.Add("No hay obligaciones con cambios efectivos para ejecutar.");
            }

            return preview;
        }

        public async Task<RutinaParticipantesResultadoVm> EjecutarAsync(
            RutinaParticipantesVm model)
        {
            var resultado = new RutinaParticipantesResultadoVm();

            if (model.IdProyecto == null ||
                model.IdEmpresa == null ||
                model.IdRol == null)
            {
                resultado.Errores.Add("Faltan parámetros obligatorios.");
                return resultado;
            }

            var idUsuarioEjecuta = _seguridad.ObtenerIdUsuario();

            if (idUsuarioEjecuta == null)
            {
                resultado.Errores.Add("No se pudo identificar el usuario logueado.");
                return resultado;
            }

            if (!await _seguridad.UsuarioPuedeAdministrarProyectoAsync(model.IdProyecto.Value))
            {
                resultado.Errores.Add("No tiene permisos para administrar este proyecto.");
                return resultado;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var obligaciones = await _context.RegObls
                    .Where(ro =>
                        ro.id_proyecto == model.IdProyecto.Value &&
                        ro.id_empresa == model.IdEmpresa.Value)
                    .ToListAsync();

                var idsObligaciones = obligaciones
                    .Select(x => x.id_reg_obl)
                    .ToList();

                var relaciones = await _context.UsuariosObligaciones
                    .Where(uo =>
                        idsObligaciones.Contains(uo.id_reg_obl) &&
                        uo.id_rol == model.IdRol.Value)
                    .ToListAsync();

                var nombreRol = await _context.Roles
                    .Where(r => r.id_rol == model.IdRol.Value)
                    .Select(r => r.nombre)
                    .FirstOrDefaultAsync() ?? "Rol";

                var nombreUsuarioOrigen = model.IdUsuarioOrigen.HasValue
                    ? await _context.Usuarios
                        .Where(u => u.id_usuario == model.IdUsuarioOrigen.Value)
                        .Select(u => u.nombre)
                        .FirstOrDefaultAsync()
                    : null;

                var nombreUsuarioDestino = model.IdUsuarioDestino.HasValue
                    ? await _context.Usuarios
                        .Where(u => u.id_usuario == model.IdUsuarioDestino.Value)
                        .Select(u => u.nombre)
                        .FirstOrDefaultAsync()
                    : null;

                foreach (var obl in obligaciones)
                {
                    if (model.Accion == "INCLUIR")
                    {
                        var existente = relaciones.FirstOrDefault(uo =>
                            uo.id_reg_obl == obl.id_reg_obl &&
                            uo.id_usuario == model.IdUsuarioDestino);

                        if (existente == null)
                        {
                            var nuevo = new UsuarioObligacion
                            {
                                id_reg_obl = obl.id_reg_obl,
                                id_usuario = model.IdUsuarioDestino!.Value,
                                id_rol = model.IdRol.Value,
                                activo = true,
                                fecha_asignacion = DateTime.UtcNow,
                                id_usuario_asignacion = idUsuarioEjecuta.Value
                            };

                            _context.UsuariosObligaciones.Add(nuevo);
                            resultado.RegistrosInsertados++;
                            resultado.ObligacionesProcesadas++;

                            AgregarHistorial(
                                obl,
                                "Participantes",
                                "-",
                                $"{nombreRol}: {nombreUsuarioDestino}",
                                idUsuarioEjecuta.Value,
                                "RUTINA_MASIVA_INCLUSION");
                        }
                        else if (!existente.activo)
                        {
                            existente.activo = true;
                            existente.fecha_asignacion = DateTime.UtcNow;
                            existente.id_usuario_asignacion = idUsuarioEjecuta.Value;

                            resultado.RegistrosReactivados++;
                            resultado.ObligacionesProcesadas++;

                            AgregarHistorial(
                                obl,
                                "Participantes",
                                "-",
                                $"{nombreRol}: {nombreUsuarioDestino}",
                                idUsuarioEjecuta.Value,
                                "RUTINA_MASIVA_REACTIVACION");
                        }
                        else
                        {
                            resultado.RegistrosOmitidos++;
                        }
                    }

                    if (model.Accion == "ELIMINAR")
                    {
                        var existente = relaciones.FirstOrDefault(uo =>
                            uo.id_reg_obl == obl.id_reg_obl &&
                            uo.id_usuario == model.IdUsuarioOrigen &&
                            uo.activo);

                        if (existente == null)
                        {
                            resultado.RegistrosOmitidos++;
                            continue;
                        }

                        var cantidadActivosRol = relaciones.Count(uo =>
                            uo.id_reg_obl == obl.id_reg_obl &&
                            uo.activo);

                        if (cantidadActivosRol <= 1)
                        {
                            resultado.RegistrosOmitidos++;
                            continue;
                        }

                        existente.activo = false;

                        existente.activo = false;
                        existente.fecha_asignacion = DateTime.UtcNow;
                        existente.id_usuario_asignacion = idUsuarioEjecuta.Value;

                        resultado.RegistrosDesactivados++;
                        resultado.ObligacionesProcesadas++;

                        AgregarHistorial(
                            obl,
                            "Participantes",
                            $"{nombreRol}: {nombreUsuarioOrigen}",
                            "-",
                            idUsuarioEjecuta.Value,
                            "RUTINA_MASIVA_ELIMINACION");
                    }

                    if (model.Accion == "CAMBIAR")
                    {
                        var origenActivo = relaciones.FirstOrDefault(uo =>
                            uo.id_reg_obl == obl.id_reg_obl &&
                            uo.id_usuario == model.IdUsuarioOrigen &&
                            uo.activo);

                        if (origenActivo == null)
                        {
                            resultado.RegistrosOmitidos++;
                            continue;
                        }

                        origenActivo.activo = false;
                        origenActivo.fecha_asignacion = DateTime.UtcNow;
                        origenActivo.id_usuario_asignacion = idUsuarioEjecuta.Value;

                        resultado.RegistrosDesactivados++;

                        var destinoExistente = relaciones.FirstOrDefault(uo =>
                            uo.id_reg_obl == obl.id_reg_obl &&
                            uo.id_usuario == model.IdUsuarioDestino);

                        if (destinoExistente == null)
                        {
                            var nuevo = new UsuarioObligacion
                            {
                                id_reg_obl = obl.id_reg_obl,
                                id_usuario = model.IdUsuarioDestino!.Value,
                                id_rol = model.IdRol.Value,
                                activo = true,
                                fecha_asignacion = DateTime.UtcNow,
                                id_usuario_asignacion = idUsuarioEjecuta.Value
                            };

                            _context.UsuariosObligaciones.Add(nuevo);
                            resultado.RegistrosInsertados++;
                        }
                        else if (!destinoExistente.activo)
                        {
                            destinoExistente.activo = true;
                            destinoExistente.fecha_asignacion = DateTime.UtcNow;
                            destinoExistente.id_usuario_asignacion = idUsuarioEjecuta.Value;

                            resultado.RegistrosReactivados++;
                        }
                        else
                        {
                            resultado.RegistrosOmitidos++;
                        }

                        resultado.ObligacionesProcesadas++;

                        AgregarHistorial(
                            obl,
                            "Participantes",
                            $"{nombreRol}: {nombreUsuarioOrigen}",
                            $"{nombreRol}: {nombreUsuarioDestino}",
                            idUsuarioEjecuta.Value,
                            "RUTINA_MASIVA_CAMBIO");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return resultado;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                resultado = new RutinaParticipantesResultadoVm();

                var mensaje = ex.InnerException?.Message ?? ex.Message;

                resultado.Errores.Add(mensaje);

                return resultado;
            }
        }

        private void AgregarHistorial(
            RegObl obligacion,
            string campo,
            string? valorAnterior,
            string? valorNuevo,
            int idUsuario,
            string tipoCambio)
        {
            var hist = new HistOblCampo
            {
                id_reg_obl = obligacion.id_reg_obl,
                campo = campo,
                valor_anterior = valorAnterior,
                valor_nuevo = valorNuevo,
                id_usuario = idUsuario,
                fecha = DateTime.UtcNow,
                id_estado_en_momento = obligacion.id_estado,
                tipo_cambio = tipoCambio
            };

            _context.HistOblCampos.Add(hist);
        }
    }
}