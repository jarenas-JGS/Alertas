using Alertas.Data;
using Alertas.Services.Constantes;
using Alertas.ViewModels.Dashboards;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Services.Dashboards
{
    public class DashboardHistoricoCumplimientoService
        : IDashboardHistoricoCumplimientoService
    {
        private readonly ApplicationDbContext _context;

        public DashboardHistoricoCumplimientoService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardHistoricoCumplimientoVm>
            ObtenerDashboardAsync(
                int idProyecto,
                FiltrosDashboardHistoricoCumplimientoVm filtros)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            NormalizarPeriodo(filtros, hoy);

            var proyecto = await _context.Proyectos
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.id_proyecto == idProyecto);

            if (proyecto == null)
                throw new Exception("Proyecto no encontrado.");

            var items = await ObtenerItemsAsync(
                idProyecto,
                filtros,
                hoy);

            await CargarCombosFiltrosAsync(
                idProyecto,
                filtros);

            var oportunas = items
                .Where(x =>
                    x.Clasificacion ==
                    ClasificacionesCumplimiento.CumplidaOportunamente)
                .ToList();

            var conAtraso = items
                .Where(x =>
                    x.Clasificacion ==
                    ClasificacionesCumplimiento.CumplidaConAtraso)
                .ToList();

            var siguenVencidas = items
                .Where(x =>
                    x.Clasificacion ==
                    ClasificacionesCumplimiento.SigueVencida)
                .ToList();

            /*
             * Para promedio y mayor atraso incluimos tanto:
             * - obligaciones cumplidas con atraso;
             * - obligaciones que continúan vencidas.
             *
             * Las oportunas no se incluyen porque tienen cero días.
             */
            var obligacionesConAtraso = items
                .Where(x => x.DiasVencida > 0)
                .ToList();

            var vm = new DashboardHistoricoCumplimientoVm
            {
                IdProyecto = idProyecto,
                NombreProyecto = proyecto.nombre,

                FechaDesde = filtros.FechaDesde,
                FechaHasta = filtros.FechaHasta,

                Filtros = filtros,
                Obligaciones = items,

                TotalObligacionesPeriodo = items.Count,
                CumplidasOportunamente = oportunas.Count,
                CumplidasConAtraso = conAtraso.Count,
                SiguenVencidas = siguenVencidas.Count,

                PorcentajeCumplimientoOportuno =
                    items.Count == 0
                        ? 0
                        : Math.Round(
                            (decimal)oportunas.Count * 100 / items.Count,
                            2),

                PromedioDiasAtraso =
                    obligacionesConAtraso.Count == 0
                        ? 0
                        : Math.Round(
                            (decimal)obligacionesConAtraso
                                .Average(x => x.DiasVencida),
                            2),

                MayorAtrasoDias =
                    obligacionesConAtraso.Count == 0
                        ? 0
                        : obligacionesConAtraso
                            .Max(x => x.DiasVencida),

                DistribucionCumplimiento =
                    ConstruirDistribucionCumplimiento(
                        oportunas.Count,
                        conAtraso.Count,
                        siguenVencidas.Count),

                VencidasPorEmpresa =
                    ConstruirVencidasPorEmpresa(items),

                VencidasPorAutorizador =
                    ConstruirVencidasPorAutorizador(items),

                VencidasPorTipoObligacion =
                    ConstruirVencidasPorTipoObligacion(items),

                RangosAtraso =
                    ConstruirRangosAtraso(items)
            };

            return vm;
        }

        public async Task<DetalleHistoricoCumplimientoVm>
            ObtenerDetalleAsync(
                int idProyecto,
                string tipo,
                FiltrosDashboardHistoricoCumplimientoVm filtros,
                int? idEmpresaDetalle = null,
                int? idAutorizadorDetalle = null,
                int? idTipoObligacionDetalle = null)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            NormalizarPeriodo(filtros, hoy);

            var proyecto = await _context.Proyectos
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.id_proyecto == idProyecto);

            if (proyecto == null)
                throw new Exception("Proyecto no encontrado.");

            var items = await ObtenerItemsAsync(
                idProyecto,
                filtros,
                hoy);

            tipo = (tipo ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            var titulo = "Detalle histórico de cumplimiento";

            switch (tipo)
            {
                case "total":
                    titulo = "Obligaciones del período";
                    break;

                case "oportunas":
                    items = items
                        .Where(x =>
                            x.Clasificacion ==
                            ClasificacionesCumplimiento
                                .CumplidaOportunamente)
                        .ToList();

                    titulo = "Obligaciones cumplidas oportunamente";
                    break;

                case "atraso":
                    items = items
                        .Where(x =>
                            x.Clasificacion ==
                            ClasificacionesCumplimiento
                                .CumplidaConAtraso)
                        .ToList();

                    titulo = "Obligaciones cumplidas con atraso";
                    break;

                case "pendientes":
                    items = items
                        .Where(x =>
                            x.Clasificacion ==
                            ClasificacionesCumplimiento
                                .SigueVencida)
                        .ToList();

                    titulo = "Obligaciones que siguen vencidas";
                    break;

                case "empresa" when idEmpresaDetalle.HasValue:
                    items = items
                        .Where(x =>
                            x.IdEmpresa ==
                            idEmpresaDetalle.Value &&
                            x.DiasVencida > 0)
                        .ToList();

                    titulo = "Obligaciones vencidas por empresa";
                    break;

                case "autorizador"
                    when idAutorizadorDetalle.HasValue:

                    if (idAutorizadorDetalle.Value == 0)
                    {
                        items = items
                            .Where(x =>
                                x.DiasVencida > 0 &&
                                x.Autorizadores.Count == 0)
                            .ToList();

                        titulo =
                            "Obligaciones vencidas sin autorizador";
                    }
                    else
                    {
                        items = items
                            .Where(x =>
                                x.DiasVencida > 0 &&
                                x.Autorizadores.Any(a =>
                                    a.IdUsuario ==
                                    idAutorizadorDetalle.Value))
                            .ToList();

                        titulo =
                            "Obligaciones vencidas por autorizador";
                    }

                    break;

                case "tipo"
                    when idTipoObligacionDetalle.HasValue:

                    items = items
                        .Where(x =>
                            x.IdTipoObligacion ==
                            idTipoObligacionDetalle.Value &&
                            x.DiasVencida > 0)
                        .ToList();

                    titulo = "Obligaciones vencidas por tipo";
                    break;
            }

            items = items
                .OrderByDescending(x => x.DiasVencida)
                .ThenBy(x => x.FechaVencimiento)
                .ThenBy(x => x.Nombre)
                .ToList();

            return new DetalleHistoricoCumplimientoVm
            {
                Titulo = titulo,
                TipoDetalle = tipo,
                NombreProyecto = proyecto.nombre,
                FechaDesde = filtros.FechaDesde,
                FechaHasta = filtros.FechaHasta,
                Filtros = filtros,
                Obligaciones = items
            };
        }

        private async Task<List<HistoricoCumplimientoItemVm>>
            ObtenerItemsAsync(
                int idProyecto,
                FiltrosDashboardHistoricoCumplimientoVm filtros,
                DateOnly hoy)
        {
            var obligaciones = _context.RegObls
                .AsNoTracking()
                .Where(o =>
                    o.id_proyecto == idProyecto);

            /*
             * El período del histórico siempre se filtra
             * directamente por fecha_venc_obl.
             */
            if (filtros.FechaDesde.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.fecha_venc_obl >= filtros.FechaDesde.Value);
            }

            if (filtros.FechaHasta.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.fecha_venc_obl <= filtros.FechaHasta.Value);
            }

            /*
             * Una obligación sin cumplimiento solo se considera
             * histórica cuando su vencimiento ya pasó.
             *
             * Las obligaciones que vencen hoy se incluyen únicamente
             * si ya tienen fecha de cumplimiento.
                */
            obligaciones = obligaciones.Where(o =>
                o.fecha_venc_obl < hoy ||
                o.fecha_vencimiento_ejecutada.HasValue);

            if (filtros.IdCliente.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.id_cliente == filtros.IdCliente.Value);
            }

            if (filtros.IdEmpresa.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.id_empresa == filtros.IdEmpresa.Value);
            }

            if (filtros.IdCiudad.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.id_ciudad == filtros.IdCiudad.Value);
            }

            if (filtros.IdEstado.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.id_estado == filtros.IdEstado.Value);
            }

            if (filtros.IdTipoObligacion.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.id_tipo_obligacion ==
                    filtros.IdTipoObligacion.Value);
            }

            if (filtros.IdResponsable.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.activo &&
                        uo.id_usuario ==
                            filtros.IdResponsable.Value &&
                        uo.Rol.nombre ==
                            RolesSistema.Responsable));
            }

            if (filtros.IdElaborador.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.activo &&
                        uo.id_usuario ==
                            filtros.IdElaborador.Value &&
                        uo.Rol.nombre ==
                            RolesSistema.Elaborador));
            }

            if (filtros.IdAutorizador.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.activo &&
                        uo.id_usuario ==
                            filtros.IdAutorizador.Value &&
                        uo.Rol.nombre ==
                            RolesSistema.Autorizador));
            }

            if (filtros.IdAprobador.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.activo &&
                        uo.id_usuario ==
                            filtros.IdAprobador.Value &&
                        uo.Rol.nombre ==
                            RolesSistema.Aprobador));
            }

            if (filtros.IdUsuarioVencimiento.HasValue)
            {
                obligaciones = obligaciones.Where(o =>
                    o.UsuariosObligaciones.Any(uo =>
                        uo.activo &&
                        uo.id_usuario ==
                            filtros.IdUsuarioVencimiento.Value &&
                        uo.Rol.nombre ==
                            RolesSistema.Vencimiento));
            }

            /*
             * Primero cargamos la información principal.
             * No proyectamos listas de participantes dentro
             * de esta consulta para evitar problemas de traducción
             * de EF Core.
             */
            var datos = await obligaciones
                .OrderBy(o => o.fecha_venc_obl)
                .Select(o => new
                {
                    IdRegObl = o.id_reg_obl,
                    Nombre = o.nombre,
                    CodigoObligacion = o.cod_obligacion,

                    IdCliente = o.id_cliente,
                    Cliente = o.Cliente != null
                        ? o.Cliente.nombre
                        : "Sin cliente",

                    IdEmpresa = o.id_empresa,
                    Empresa = o.Empresa.nombre,

                    IdTipoObligacion = o.id_tipo_obligacion,
                    TipoObligacion = o.TipoObligacion.nombre,

                    IdCiudad = o.id_ciudad,
                    Ciudad = o.Ciudad != null
                        ? o.Ciudad.nombre
                        : null,

                    IdEstado = o.id_estado,
                    EstadoActual = o.Estado.nombre,

                    FechaVencimiento = o.fecha_venc_obl,
                    FechaCumplimiento = o.fecha_vencimiento_ejecutada
                })
                .ToListAsync();

            var idsObligaciones = datos
                .Select(x => x.IdRegObl)
                .ToList();

            var autorizadoresRaw = await _context
                .UsuariosObligaciones
                .AsNoTracking()
                .Where(uo =>
                    idsObligaciones.Contains(uo.id_reg_obl) &&
                    uo.activo &&
                    uo.Rol.nombre ==
                        RolesSistema.Autorizador)
                .Select(uo => new
                {
                    uo.id_reg_obl,
                    IdUsuario = uo.id_usuario,
                    Nombre = uo.Usuario.nombre
                })
                .Distinct()
                .ToListAsync();

            var autorizadoresPorObligacion =
                autorizadoresRaw
                    .GroupBy(x => x.id_reg_obl)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Select(x =>
                                new ParticipanteHistoricoCumplimientoVm
                                {
                                    IdUsuario = x.IdUsuario,
                                    Nombre = x.Nombre
                                })
                            .OrderBy(x => x.Nombre)
                            .ToList());

            var items = datos
                .Select(x =>
                {
                    var item =
                        new HistoricoCumplimientoItemVm
                        {
                            IdRegObl = x.IdRegObl,
                            Nombre = x.Nombre,
                            CodigoObligacion =
                                x.CodigoObligacion,

                            IdCliente = x.IdCliente,
                            Cliente = x.Cliente,

                            IdEmpresa = x.IdEmpresa,
                            Empresa = x.Empresa,

                            IdTipoObligacion =
                                x.IdTipoObligacion,

                            TipoObligacion =
                                x.TipoObligacion,

                            IdCiudad = x.IdCiudad,
                            Ciudad = x.Ciudad,

                            IdEstado = x.IdEstado,
                            EstadoActual = x.EstadoActual,

                            FechaVencimiento =
                                x.FechaVencimiento,

                            FechaCumplimiento =
                                x.FechaCumplimiento,

                            Autorizadores =
                                autorizadoresPorObligacion
                                    .GetValueOrDefault(
                                        x.IdRegObl,
                                        new List<
                                            ParticipanteHistoricoCumplimientoVm>())
                        };

                    ClasificarItem(item, hoy);

                    return item;
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(
                filtros.Clasificacion))
            {
                items = items
                    .Where(x =>
                        x.Clasificacion ==
                        filtros.Clasificacion)
                    .ToList();
            }

            return items;
        }

        private static void ClasificarItem(
            HistoricoCumplimientoItemVm item,
            DateOnly hoy)
        {
            if (item.FechaCumplimiento.HasValue)
            {
                if (item.FechaCumplimiento.Value <=
                    item.FechaVencimiento)
                {
                    item.Clasificacion =
                        ClasificacionesCumplimiento
                            .CumplidaOportunamente;

                    item.DiasVencida = 0;
                    return;
                }

                if (item.FechaVencimiento < hoy)
                {
                    item.Clasificacion =
                        ClasificacionesCumplimiento.SigueVencida;

                    item.DiasVencida =
                        hoy.DayNumber -
                        item.FechaVencimiento.DayNumber;
                }
                else
                {
                    /*
                     * Esta obligación todavía no está vencida.
                     * Normalmente no llegará aquí porque se excluye
                     * desde la consulta base.
                     */
                    item.Clasificacion = string.Empty;
                    item.DiasVencida = 0;
                }

                return;
            }

            item.Clasificacion =
                ClasificacionesCumplimiento.SigueVencida;

            item.DiasVencida = Math.Max(
                0,
                hoy.DayNumber -
                item.FechaVencimiento.DayNumber);
        }

        private static List<SerieDashboardVm>
            ConstruirDistribucionCumplimiento(
                int oportunas,
                int conAtraso,
                int siguenVencidas)
        {
            return new List<SerieDashboardVm>
            {
                new()
                {
                    Label = ClasificacionesCumplimiento
                        .CumplidaOportunamente,
                    Valor = oportunas
                },
                new()
                {
                    Label = ClasificacionesCumplimiento
                        .CumplidaConAtraso,
                    Valor = conAtraso
                },
                new()
                {
                    Label = ClasificacionesCumplimiento
                        .SigueVencida,
                    Valor = siguenVencidas
                }
            };
        }

        private static List<SerieDashboardVm>
            ConstruirVencidasPorEmpresa(
                IEnumerable<HistoricoCumplimientoItemVm> items)
        {
            return items
                .Where(x => x.DiasVencida > 0)
                .GroupBy(x => new
                {
                    x.IdEmpresa,
                    x.Empresa
                })
                .Select(g => new SerieDashboardVm
                {
                    Id = g.Key.IdEmpresa,
                    Label = g.Key.Empresa,
                    Valor = g.Count()
                })
                .OrderByDescending(x => x.Valor)
                .ThenBy(x => x.Label)
                .Take(10)
                .ToList();
        }

        private static List<SerieDashboardVm>
            ConstruirVencidasPorAutorizador(
                IEnumerable<HistoricoCumplimientoItemVm> items)
        {
            return items
                .Where(x => x.DiasVencida > 0)
                .SelectMany(x =>
                    x.Autorizadores.Count > 0
                        ? x.Autorizadores
                            .Select(a => new
                            {
                                a.IdUsuario,
                                a.Nombre,
                                x.IdRegObl
                            })
                        : new[]
                        {
                            new
                            {
                                IdUsuario = 0,
                                Nombre = "Sin autorizador",
                                x.IdRegObl
                            }
                        })
                .GroupBy(x => new
                {
                    x.IdUsuario,
                    x.Nombre
                })
                .Select(g => new SerieDashboardVm
                {
                    Id = g.Key.IdUsuario,
                    Label = g.Key.Nombre,

                    /*
                     * Distinct evita contar dos veces una obligación
                     * si existiera una asociación duplicada.
                     */
                    Valor = g
                        .Select(x => x.IdRegObl)
                        .Distinct()
                        .Count()
                })
                .OrderByDescending(x => x.Valor)
                .ThenBy(x => x.Label)
                .Take(10)
                .ToList();
        }

        private static List<SerieDashboardVm>
            ConstruirVencidasPorTipoObligacion(
                IEnumerable<HistoricoCumplimientoItemVm> items)
        {
            return items
                .Where(x => x.DiasVencida > 0)
                .GroupBy(x => new
                {
                    x.IdTipoObligacion,
                    x.TipoObligacion
                })
                .Select(g => new SerieDashboardVm
                {
                    Id = g.Key.IdTipoObligacion,
                    Label = g.Key.TipoObligacion,
                    Valor = g.Count()
                })
                .OrderByDescending(x => x.Valor)
                .ThenBy(x => x.Label)
                .Take(10)
                .ToList();
        }

        private static List<SerieDashboardVm>
            ConstruirRangosAtraso(
                IEnumerable<HistoricoCumplimientoItemVm> items)
        {
            var lista = items.ToList();

            return new List<SerieDashboardVm>
            {
                new()
                {
                    Label = "Cumplidas oportunamente",
                    Valor = lista.Count(x =>
                        x.Clasificacion ==
                        ClasificacionesCumplimiento
                            .CumplidaOportunamente)
                },
                new()
                {
                    Label = "1 a 5 días",
                    Valor = lista.Count(x =>
                        x.Clasificacion ==
                        ClasificacionesCumplimiento
                            .CumplidaConAtraso &&
                        x.DiasVencida >= 1 &&
                        x.DiasVencida <= 5)
                },
                new()
                {
                    Label = "6 a 15 días",
                    Valor = lista.Count(x =>
                        x.Clasificacion ==
                        ClasificacionesCumplimiento
                            .CumplidaConAtraso &&
                        x.DiasVencida >= 6 &&
                        x.DiasVencida <= 15)
                },
                new()
                {
                    Label = "16 a 30 días",
                    Valor = lista.Count(x =>
                        x.Clasificacion ==
                        ClasificacionesCumplimiento
                            .CumplidaConAtraso &&
                        x.DiasVencida >= 16 &&
                        x.DiasVencida <= 30)
                },
                new()
                {
                    Label = "Más de 30 días",
                    Valor = lista.Count(x =>
                        x.Clasificacion ==
                        ClasificacionesCumplimiento
                            .CumplidaConAtraso &&
                        x.DiasVencida > 30)
                },
                new()
                {
                    Label = "Siguen vencidas",
                    Valor = lista.Count(x =>
                        x.Clasificacion ==
                        ClasificacionesCumplimiento
                            .SigueVencida)
                }
            };
        }

        private static void NormalizarPeriodo(
            FiltrosDashboardHistoricoCumplimientoVm filtros,
            DateOnly hoy)
        {
            /*
             * Si no se indica período, mostramos el mes actual
             * desde su primer día hasta hoy.
             */
            filtros.FechaDesde ??=
                new DateOnly(
                    hoy.Year,
                    hoy.Month,
                    1);

            filtros.FechaHasta ??= hoy;

            /*
             * El histórico no puede analizar fechas futuras.
             */
            if (filtros.FechaHasta.Value > hoy)
            {
                filtros.FechaHasta = hoy;
            }

            if (filtros.FechaDesde.Value >
                filtros.FechaHasta.Value)
            {
                throw new ArgumentException(
                    "La fecha inicial no puede ser posterior " +
                    "a la fecha final.");
            }
        }

        private async Task CargarCombosFiltrosAsync(
            int idProyecto,
            FiltrosDashboardHistoricoCumplimientoVm filtros)
        {
            filtros.Clientes = await _context.RegObls
                .AsNoTracking()
                .Where(o =>
                    o.id_proyecto == idProyecto &&
                    o.Cliente != null)
                .Select(o => new SelectListItem
                {
                    Value = o.id_cliente.ToString(),
                    Text = o.Cliente.nombre
                })
                .Distinct()
                .OrderBy(x => x.Text)
                .ToListAsync();

            filtros.Empresas = await _context.RegObls
                .AsNoTracking()
                .Where(o =>
                    o.id_proyecto == idProyecto)
                .Select(o => new SelectListItem
                {
                    Value = o.id_empresa.ToString(),
                    Text = o.Empresa.nombre
                })
                .Distinct()
                .OrderBy(x => x.Text)
                .ToListAsync();

            filtros.Ciudades = await _context.RegObls
                .AsNoTracking()
                .Where(o =>
                    o.id_proyecto == idProyecto &&
                    o.id_ciudad != null)
                .Select(o => new SelectListItem
                {
                    Value = o.id_ciudad!.Value.ToString(),
                    Text = o.Ciudad!.nombre
                })
                .Distinct()
                .OrderBy(x => x.Text)
                .ToListAsync();

            filtros.Estados = await _context.Estados
                .AsNoTracking()
                .Where(e =>
                    e.id_proyecto == idProyecto &&
                    e.activo)
                .OrderBy(e => e.orden)
                .Select(e => new SelectListItem
                {
                    Value = e.id_estado.ToString(),
                    Text = e.nombre
                })
                .ToListAsync();

            filtros.TiposObligacion = await _context.RegObls
                .AsNoTracking()
                .Where(o =>
                    o.id_proyecto == idProyecto)
                .Select(o => new SelectListItem
                {
                    Value = o.id_tipo_obligacion.ToString(),
                    Text = o.TipoObligacion.nombre
                })
                .Distinct()
                .OrderBy(x => x.Text)
                .ToListAsync();

            filtros.Responsables =
                await ObtenerUsuariosPorRolProyectoAsync(
                    idProyecto,
                    RolesSistema.Responsable);

            filtros.Elaboradores =
                await ObtenerUsuariosPorRolProyectoAsync(
                    idProyecto,
                    RolesSistema.Elaborador);

            filtros.Autorizadores =
                await ObtenerUsuariosPorRolProyectoAsync(
                    idProyecto,
                    RolesSistema.Autorizador);

            filtros.Aprobadores =
                await ObtenerUsuariosPorRolProyectoAsync(
                    idProyecto,
                    RolesSistema.Aprobador);

            filtros.UsuariosVencimiento =
                await ObtenerUsuariosPorRolProyectoAsync(
                    idProyecto,
                    RolesSistema.Vencimiento);

            filtros.Clasificaciones =
                new List<SelectListItem>
                {
                    new()
                    {
                        Value =
                            ClasificacionesCumplimiento
                                .CumplidaOportunamente,

                        Text =
                            ClasificacionesCumplimiento
                                .CumplidaOportunamente
                    },
                    new()
                    {
                        Value =
                            ClasificacionesCumplimiento
                                .CumplidaConAtraso,

                        Text =
                            ClasificacionesCumplimiento
                                .CumplidaConAtraso
                    },
                    new()
                    {
                        Value =
                            ClasificacionesCumplimiento
                                .SigueVencida,

                        Text =
                            ClasificacionesCumplimiento
                                .SigueVencida
                    }
                };
        }

        private async Task<List<SelectListItem>>
            ObtenerUsuariosPorRolProyectoAsync(
                int idProyecto,
                string nombreRol)
        {
            return await _context.UsuariosObligaciones
                .AsNoTracking()
                .Where(uo =>
                    uo.activo &&
                    uo.Rol.nombre == nombreRol &&
                    uo.RegObl.id_proyecto == idProyecto)
                .Select(uo => new SelectListItem
                {
                    Value = uo.id_usuario.ToString(),
                    Text = uo.Usuario.nombre
                })
                .Distinct()
                .OrderBy(x => x.Text)
                .ToListAsync();
        }
    }
}