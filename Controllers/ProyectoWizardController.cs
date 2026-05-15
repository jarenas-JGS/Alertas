using Alertas.Data;
using Alertas.Models;
using Alertas.Services;
using Alertas.ViewModels;
using Alertas.ViewModels.ProyectoWizard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace Alertas.Controllers
{
    public class ProyectoWizardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SeguridadService _seguridadService;

        public ProyectoWizardController(ApplicationDbContext context, SeguridadService seguridadService)
        {
            _context = context;
            _seguridadService = seguridadService;
        }


        public async Task<IActionResult> Index()
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyectos = await _context.Proyectos
                .Include(p => p.Area)
                .Include(p => p.UsuarioCreacion)
                .Where(p => !p.configuracion_completa)
                .OrderByDescending(p => p.fecha_creacion)
                .ToListAsync();

            var vm = new List<ProyectoWizardIndexItemVm>();

            foreach (var p in proyectos)
            {
                var paso = await ObtenerSiguientePasoProyecto(p.id_proyecto);

                vm.Add(new ProyectoWizardIndexItemVm
                {
                    id_proyecto = p.id_proyecto,
                    nombre = p.nombre,
                    area = p.Area?.nombre ?? "",
                    fecha_creacion = p.fecha_creacion,
                    creado_por = p.UsuarioCreacion?.nombre ?? "",
                    paso_actual = paso.NombrePaso
                });
            }

            return View(vm);
        }

        public async Task<IActionResult> Continuar(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var paso = await ObtenerSiguientePasoProyecto(id);

            return RedirectToAction(paso.Accion, new { id });
        }

        private class PasoWizardProyectoDto
        {
            public string Accion { get; set; } = "Index";
            public string NombrePaso { get; set; } = "";
        }

        // GET: ProyectoWizard/DatosGenerales
        public async Task<IActionResult> DatosGenerales(int? id)
        {
            var vm = new ProyectoWizardDatosGeneralesVm();

            if (!EsSuperAdmin())
            {
                return RedirectToAction("AccesoDenegado", "Auth");
            }

            if (id.HasValue)
            {
                var proyecto = await _context.Proyectos
                    .FirstOrDefaultAsync(p => p.id_proyecto == id.Value);

                if (proyecto == null)
                    return NotFound();

                vm.id_proyecto = proyecto.id_proyecto;
                vm.nombre = proyecto.nombre;
                vm.id_area = proyecto.id_area;
                vm.nombre_seguimiento = proyecto.nombre_seguimiento;
            }

            vm.Areas = await CargarAreasAsync(vm.id_area);

            return View(vm);
        }

        // POST: ProyectoWizard/DatosGenerales
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatosGenerales(ProyectoWizardDatosGeneralesVm vm)
        {
            if (!EsSuperAdmin())
            {
                return RedirectToAction("AccesoDenegado", "Auth");
            }

            if (!ModelState.IsValid)
            {
                vm.Areas = await CargarAreasAsync(vm.id_area);
                return View(vm);
            }

            bool existeNombreArea = await _context.Proyectos.AnyAsync(p =>
                p.nombre.ToLower() == vm.nombre.Trim().ToLower()
                && p.id_area == vm.id_area
                && (!vm.id_proyecto.HasValue || p.id_proyecto != vm.id_proyecto.Value));

            if (existeNombreArea)
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe un proyecto con este nombre para el área seleccionada.");
                vm.Areas = await CargarAreasAsync(vm.id_area);
                return View(vm);
            }

            Proyecto proyecto;

            if (vm.id_proyecto.HasValue)
            {
                proyecto = await _context.Proyectos
                    .FirstOrDefaultAsync(p => p.id_proyecto == vm.id_proyecto.Value);

                if (proyecto == null)
                    return NotFound();

                proyecto.nombre = vm.nombre.Trim();
                proyecto.id_area = vm.id_area!.Value;
                proyecto.nombre_seguimiento = vm.nombre_seguimiento.Trim();
            }
            else
            {
                int? idUsuario = _seguridadService.ObtenerIdUsuario();

                proyecto = new Proyecto
                {
                    nombre = vm.nombre.Trim(),
                    id_area = vm.id_area!.Value,
                    nombre_seguimiento = vm.nombre_seguimiento.Trim(),
                    activo = true,
                    fecha_creacion = DateTime.UtcNow,
                    id_usuario_creacion = idUsuario,
                    configuracion_completa = false
                };

                _context.Proyectos.Add(proyecto);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Estados", new { id = proyecto.id_proyecto });
        }

        private async Task<List<SelectListItem>> CargarAreasAsync(int? idAreaSeleccionada = null)
        {
            return await _context.Areas
                .OrderBy(a => a.nombre)
                .Select(a => new SelectListItem
                {
                    Value = a.id_area.ToString(),
                    Text = a.nombre,
                    Selected = idAreaSeleccionada.HasValue && a.id_area == idAreaSeleccionada.Value
                })
                .ToListAsync();
        }

        public async Task<IActionResult> Estados(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .Include(p => p.Estados)
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            var estados = await _context.Estados
                .Where(e => e.id_proyecto == id && e.activo)
                .OrderBy(e => e.orden)
                .ToListAsync();

            if (!estados.Any())
            {
                estados = CrearPlantillaEstados(id);
            }

            var vm = new ProyectoWizardEstadosVm
            {
                id_proyecto = proyecto.id_proyecto,
                nombre_proyecto = proyecto.nombre,
                Estados = estados.Select(e => new ProyectoWizardEstadoItemVm
                {
                    id_estado = e.id_estado == 0 ? null : e.id_estado,
                    nombre = e.nombre,
                    orden = e.orden,
                    bloquea = e.bloquea,
                    control_vencimiento = e.control_vencimiento,
                    control_seguimiento = e.control_seguimiento,
                    activo = e.activo
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Estados(ProyectoWizardEstadosVm vm)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .Include(p => p.Estados)
                .FirstOrDefaultAsync(p => p.id_proyecto == vm.id_proyecto);

            if (proyecto == null)
                return NotFound();

            vm.nombre_proyecto = proyecto.nombre;

            var estadosValidar = vm.Estados
                .Where(e => !e.eliminar && e.activo)
                .ToList();

            if (!estadosValidar.Any())
                ModelState.AddModelError("", "Debe existir al menos un estado activo.");

            if (estadosValidar.Count(e => e.control_seguimiento) != 1)
                ModelState.AddModelError("", "Debe existir un único estado activo con control de seguimiento.");

            if (estadosValidar.Count(e => e.control_vencimiento) != 1)
                ModelState.AddModelError("", "Debe existir un único estado activo con control de vencimiento.");

            if (estadosValidar.GroupBy(e => e.orden).Any(g => g.Count() > 1))
                ModelState.AddModelError("", "No se puede repetir el orden entre estados activos.");

            var estadoCerradaValidacion = estadosValidar
                .FirstOrDefault(e => e.nombre.Trim().ToLower() == "cerrada");

            if (estadoCerradaValidacion != null)
            {
                var estadosDespuesDeCerrada = estadosValidar
                    .Where(e =>
                        e.orden > estadoCerradaValidacion.orden &&
                        e.nombre.Trim().ToLower() != "anulada")
                    .ToList();

                if (estadosDespuesDeCerrada.Any())
                {
                    ModelState.AddModelError(
                        "",
                        "No puede existir ningún estado activo con orden superior a Cerrada, excepto Anulada."
                    );
                }
            }

            if (estadosValidar.GroupBy(e => e.nombre.Trim().ToLower()).Any(g => g.Count() > 1))
                ModelState.AddModelError("", "No se puede repetir el nombre entre estados activos.");

            if (!estadosValidar.Any(e => e.nombre.Trim().ToLower() == "cerrada"))
                ModelState.AddModelError("", "Debe existir el estado final Cerrada.");

            if (!estadosValidar.Any(e => e.nombre.Trim().ToLower() == "anulada"))
                ModelState.AddModelError("", "Debe existir el estado final Anulada.");


            var estadosBaseNoEliminables = new[] { "creada", "cerrada", "anulada" };

            foreach (var item in vm.Estados.Where(e => e.eliminar))
            {
                var nombreNormalizado = item.nombre.Trim().ToLower();

                if (estadosBaseNoEliminables.Contains(nombreNormalizado))
                {
                    ModelState.AddModelError("", $"El estado '{item.nombre}' no se puede eliminar porque hace parte del flujo base.");
                }
            }

            foreach (var item in vm.Estados.Where(e => e.eliminar && e.id_estado.HasValue))
            {
                bool tieneObligaciones = await _context.RegObls
                    .AnyAsync(o => o.id_estado == item.id_estado.Value);

                if (tieneObligaciones)
                {
                    ModelState.AddModelError("", $"No se puede eliminar/inactivar el estado '{item.nombre}' porque ya tiene obligaciones asociadas.");
                }
            }

            bool estadosCambiaron = false;

            foreach (var item in vm.Estados)
            {
                if (item.id_estado.HasValue)
                {
                    var estadoActual = proyecto.Estados
                        .FirstOrDefault(e => e.id_estado == item.id_estado.Value);

                    if (estadoActual == null)
                    {
                        estadosCambiaron = true;
                        continue;
                    }

                    if (
                        estadoActual.nombre.Trim() != item.nombre.Trim() ||
                        estadoActual.orden != item.orden ||
                        estadoActual.bloquea != item.bloquea ||
                        estadoActual.control_vencimiento != item.control_vencimiento ||
                        estadoActual.control_seguimiento != item.control_seguimiento ||
                        estadoActual.activo != item.activo ||
                        item.eliminar
                    )
                    {
                        estadosCambiaron = true;
                    }
                }
                else
                {
                    if (!item.eliminar)
                        estadosCambiaron = true;
                }
            }


            if (!ModelState.IsValid)
                return View(vm);

            var estadoControlVencimientoVm = vm.Estados
                .Where(e => !e.eliminar && e.activo)
                .FirstOrDefault(e => e.control_vencimiento);

            var estadoCerradaVm = vm.Estados
                .Where(e => !e.eliminar && e.activo)
                .FirstOrDefault(e => e.nombre.Trim().ToLower() == "cerrada");

            if (estadoControlVencimientoVm != null && estadoCerradaVm != null)
            {
                foreach (var item in vm.Estados.Where(e =>
                    !e.eliminar &&
                    e.activo &&
                    e.orden > estadoControlVencimientoVm.orden &&
                    e.orden < estadoCerradaVm.orden))
                {
                    item.bloquea = true;
                }
            }

            foreach (var item in vm.Estados)
            {
                if (item.eliminar)
                {
                    if (item.id_estado.HasValue)
                    {
                        var estadoEliminar = proyecto.Estados
                            .FirstOrDefault(e => e.id_estado == item.id_estado.Value);

                        if (estadoEliminar != null)
                        {
                            estadoEliminar.activo = false;
                            estadoEliminar.control_vencimiento = false;
                            estadoEliminar.control_seguimiento = false;
                        }
                    }

                    continue;
                }

                Estado estado;

                if (item.id_estado.HasValue)
                {
                    estado = proyecto.Estados
                        .First(e => e.id_estado == item.id_estado.Value);

                    estado.nombre = item.nombre.Trim();
                    estado.orden = item.orden;
                    estado.bloquea = item.bloquea;
                    estado.control_vencimiento = item.control_vencimiento;
                    estado.control_seguimiento = item.control_seguimiento;
                    estado.activo = item.activo;
                }
                else
                {
                    var nombreNuevo = item.nombre.Trim().ToLower();

                    var estadoInactivoExistente = proyecto.Estados
                        .FirstOrDefault(e =>
                            e.nombre.Trim().ToLower() == nombreNuevo &&
                            !e.activo);

                    if (estadoInactivoExistente != null)
                    {
                        estadoInactivoExistente.nombre = item.nombre.Trim();
                        estadoInactivoExistente.orden = item.orden;
                        estadoInactivoExistente.bloquea = item.bloquea;
                        estadoInactivoExistente.control_vencimiento = item.control_vencimiento;
                        estadoInactivoExistente.control_seguimiento = item.control_seguimiento;
                        estadoInactivoExistente.activo = item.activo;
                    }
                    else
                    {
                        estado = new Estado
                        {
                            id_proyecto = proyecto.id_proyecto,
                            nombre = item.nombre.Trim(),
                            orden = item.orden,
                            bloquea = item.bloquea,
                            control_vencimiento = item.control_vencimiento,
                            control_seguimiento = item.control_seguimiento,
                            activo = item.activo
                        };

                        _context.Estados.Add(estado);
                    }
                }
            }

            if (estadosCambiaron)
            {
                proyecto.configuracion_completa = false;

                var gruposAlertas = await _context.GruposAlertas
                    .Where(g => g.id_proyecto == proyecto.id_proyecto)
                    .Select(g => g.id_grupo_alerta)
                    .ToListAsync();

                var alertasDiasIds = await _context.GruposAlertasDias
                    .Where(a => gruposAlertas.Contains(a.id_grupo_alerta))
                    .Select(a => a.id_grupo_alerta_dia)
                    .ToListAsync();

                var estadosOff = await _context.GruposAlertasDiasEstadosOff
                    .Where(eo => alertasDiasIds.Contains(eo.id_grupo_alerta_dia))
                    .ToListAsync();

                _context.GruposAlertasDiasEstadosOff.RemoveRange(estadosOff);

                var transicionesProyecto = await _context.EstadosTransicion
                    .Where(t => t.id_proyecto == proyecto.id_proyecto)
                    .ToListAsync();

                var idsTransiciones = transicionesProyecto
                    .Select(t => t.id_estado_transicion)
                    .ToList();

                var rolesTransiciones = await _context.RolesEstadosTransicion
                    .Where(r => idsTransiciones.Contains(r.id_estado_transicion))
                    .ToListAsync();

                _context.RolesEstadosTransicion.RemoveRange(rolesTransiciones);
                _context.EstadosTransicion.RemoveRange(transicionesProyecto);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Transiciones", new { id = proyecto.id_proyecto });
        }

        private List<Estado> CrearPlantillaEstados(int idProyecto)
        {
            return new List<Estado>
            {
                new Estado
                {
                    id_proyecto = idProyecto,
                    nombre = "Creada",
                    orden = 1,
                    bloquea = false,
                    control_vencimiento = false,
                    control_seguimiento = false,
                    activo = true
                },
                new Estado
                {
                    id_proyecto = idProyecto,
                    nombre = "En elaboración",
                    orden = 2,
                    bloquea = false,
                    control_vencimiento = false,
                    control_seguimiento = false,
                    activo = true
                },
                new Estado
                {
                    id_proyecto = idProyecto,
                    nombre = "En seguimiento",
                    orden = 3,
                    bloquea = false,
                    control_vencimiento = false,
                    control_seguimiento = true,
                    activo = true
                },
                new Estado
                {
                    id_proyecto = idProyecto,
                    nombre = "Presentada",
                    orden = 4,
                    bloquea = true,
                    control_vencimiento = true,
                    control_seguimiento = false,
                    activo = true
                },
                new Estado
                {
                    id_proyecto = idProyecto,
                    nombre = "Cerrada",
                    orden = 5,
                    bloquea = true,
                    control_vencimiento = false,
                    control_seguimiento = false,
                    activo = true
                },
                new Estado
                {
                    id_proyecto = idProyecto,
                    nombre = "Anulada",
                    orden = 6,
                    bloquea = true,
                    control_vencimiento = false,
                    control_seguimiento = false,
                    activo = true
                }
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarProyecto(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            if (proyecto.configuracion_completa)
            {
                TempData["Error"] = "No se puede eliminar un proyecto que ya tiene la configuración completa.";
                return RedirectToAction(nameof(Index));
            }

            bool tieneObligaciones = await _context.RegObls
                .AnyAsync(o => o.id_proyecto == id);

            if (tieneObligaciones)
            {
                TempData["Error"] = "No se puede eliminar el proyecto porque ya tiene obligaciones asociadas.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var gruposAlertas = await _context.GruposAlertas
                    .Where(g => g.id_proyecto == id)
                    .ToListAsync();

                var idsGruposAlertas = gruposAlertas
                    .Select(g => g.id_grupo_alerta)
                    .ToList();

                var alertasDias = await _context.GruposAlertasDias
                    .Where(a => idsGruposAlertas.Contains(a.id_grupo_alerta))
                    .ToListAsync();

                var idsAlertasDias = alertasDias
                    .Select(a => a.id_grupo_alerta_dia)
                    .ToList();

                var estadosOff = await _context.GruposAlertasDiasEstadosOff
                    .Where(eo => idsAlertasDias.Contains(eo.id_grupo_alerta_dia))
                    .ToListAsync();

                _context.GruposAlertasDiasEstadosOff.RemoveRange(estadosOff);

                foreach (var alerta in alertasDias)
                {
                    alerta.id_dependencia = null;
                }

                await _context.SaveChangesAsync();

                _context.GruposAlertasDias.RemoveRange(alertasDias);
                _context.GruposAlertas.RemoveRange(gruposAlertas);

                var transiciones = await _context.EstadosTransicion
                    .Where(t => t.id_proyecto == id)
                    .ToListAsync();

                var idsTransiciones = transiciones
                    .Select(t => t.id_estado_transicion)
                    .ToList();

                var rolesTransicion = await _context.RolesEstadosTransicion
                    .Where(r => idsTransiciones.Contains(r.id_estado_transicion))
                    .ToListAsync();

                _context.RolesEstadosTransicion.RemoveRange(rolesTransicion);
                _context.EstadosTransicion.RemoveRange(transiciones);

                var estados = await _context.Estados
                    .Where(e => e.id_proyecto == id)
                    .ToListAsync();

                _context.Estados.RemoveRange(estados);

                _context.Proyectos.Remove(proyecto);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Proyecto pendiente eliminado correctamente.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "No fue posible eliminar el proyecto pendiente. Revise si tiene registros asociados.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Transiciones(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .Include(p => p.Estados)
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            var estadosActivos = proyecto.Estados
                .Where(e => e.activo)
                .OrderBy(e => e.orden)
                .ToList();

            if (!estadosActivos.Any())
            {
                TempData["Error"] = "Debe configurar primero los estados del proyecto.";
                return RedirectToAction("Estados", new { id = proyecto.id_proyecto });
            }

            var transiciones = await _context.EstadosTransicion
                .Where(t => t.id_proyecto == id)
                .OrderBy(t => t.orden)
                .ThenBy(t => t.id_estado_transicion)
                .ToListAsync();

            var vm = new ProyectoWizardTransicionesVm
            {
                id_proyecto = proyecto.id_proyecto,
                nombre_proyecto = proyecto.nombre,
                Estados = estadosActivos.Select(e => new SelectListItem
                {
                    Value = e.id_estado.ToString(),
                    Text = $"{e.orden}. {e.nombre}"
                }).ToList(),
                Transiciones = transiciones.Select(t => new ProyectoWizardTransicionItemVm
                {
                    id_estado_transicion = t.id_estado_transicion,
                    id_estado_origen = t.id_estado_origen,
                    id_estado_destino = t.id_estado_destino,
                    nombre_accion = t.nombre_accion,
                    requiere_observacion = t.requiere_observacion,
                    es_aprobacion = t.es_aprobacion,
                    es_rechazo = t.es_rechazo,
                    es_anulacion = t.es_anulacion,
                    activo = t.activo,
                    orden = t.orden
                }).ToList()
            };

            if (!vm.Transiciones.Any())
            {
                vm.Transiciones = CrearPlantillaTransiciones(estadosActivos);
            }

            return View(vm);
        }

        private List<ProyectoWizardTransicionItemVm> CrearPlantillaTransiciones(List<Estado> estados)
        {
            var lista = new List<ProyectoWizardTransicionItemVm>();

            var estadosOrdenados = estados
                .Where(e => e.activo)
                .OrderBy(e => e.orden)
                .ToList();

            var estadoCerrada = estadosOrdenados
                .FirstOrDefault(e => e.nombre.Trim().ToLower() == "cerrada");

            var estadoAnulada = estadosOrdenados
                .FirstOrDefault(e => e.nombre.Trim().ToLower() == "anulada");

            var estadoControlSeguimiento = estadosOrdenados
                .FirstOrDefault(e => e.control_seguimiento);

            var estadoControlVencimiento = estadosOrdenados
                .FirstOrDefault(e => e.control_vencimiento);

            int orden = 1;

            // Secuencia normal hasta Cerrada, respetando estados intermedios.
            for (int i = 0; i < estadosOrdenados.Count - 1; i++)
            {
                var origen = estadosOrdenados[i];
                var destino = estadosOrdenados[i + 1];

                // Anulada no participa en la secuencia normal.
                if (origen.id_estado == estadoAnulada?.id_estado ||
                    destino.id_estado == estadoAnulada?.id_estado)
                    continue;

                // Cerrada es final. No debe tener salidas.
                if (origen.id_estado == estadoCerrada?.id_estado)
                    continue;

                bool esAprobacionFinal = destino.id_estado == estadoCerrada?.id_estado;

                lista.Add(new ProyectoWizardTransicionItemVm
                {
                    id_estado_origen = origen.id_estado,
                    id_estado_destino = destino.id_estado,
                    nombre_accion = esAprobacionFinal
                        ? "Aprobar / cerrar"
                        : $"Enviar a {destino.nombre}",
                    orden = orden++,
                    es_aprobacion = esAprobacionFinal,
                    activo = true
                });

                // Si ya llegamos a Cerrada, no seguimos construyendo secuencia.
                if (destino.id_estado == estadoCerrada?.id_estado)
                    break;
            }

            // Devolver: control_seguimiento -> estado anterior
            if (estadoControlSeguimiento != null)
            {
                var indexSeguimiento = estadosOrdenados
                    .FindIndex(e => e.id_estado == estadoControlSeguimiento.id_estado);

                if (indexSeguimiento > 0)
                {
                    var estadoAnterior = estadosOrdenados[indexSeguimiento - 1];

                    if (estadoAnterior.id_estado != estadoAnulada?.id_estado &&
                        estadoAnterior.id_estado != estadoCerrada?.id_estado)
                    {
                        lista.Add(new ProyectoWizardTransicionItemVm
                        {
                            id_estado_origen = estadoControlSeguimiento.id_estado,
                            id_estado_destino = estadoAnterior.id_estado,
                            nombre_accion = "Devolver",
                            requiere_observacion = true,
                            orden = orden++,
                            activo = true
                        });
                    }
                }
            }

            // Rechazar: control_vencimiento -> estado anterior
            if (estadoControlVencimiento != null)
            {
                var indexVencimiento = estadosOrdenados
                    .FindIndex(e => e.id_estado == estadoControlVencimiento.id_estado);

                if (indexVencimiento > 0)
                {
                    var estadoAnterior = estadosOrdenados[indexVencimiento - 1];

                    if (estadoAnterior.id_estado != estadoAnulada?.id_estado &&
                        estadoAnterior.id_estado != estadoCerrada?.id_estado)
                    {
                        lista.Add(new ProyectoWizardTransicionItemVm
                        {
                            id_estado_origen = estadoControlVencimiento.id_estado,
                            id_estado_destino = estadoAnterior.id_estado,
                            nombre_accion = "Rechazar",
                            requiere_observacion = true,
                            es_rechazo = true,
                            orden = orden++,
                            activo = true
                        });
                    }
                }
            }

            // Anular desde estados anteriores a control_vencimiento
            if (estadoControlVencimiento != null && estadoAnulada != null)
            {
                int ordenAnulacion = 90;

                var estadosPreviosAVencimiento = estadosOrdenados
                    .Where(e =>
                        e.orden < estadoControlVencimiento.orden &&
                        e.id_estado != estadoCerrada?.id_estado &&
                        e.id_estado != estadoAnulada.id_estado)
                    .OrderBy(e => e.orden)
                    .ToList();

                foreach (var estado in estadosPreviosAVencimiento)
                {
                    lista.Add(new ProyectoWizardTransicionItemVm
                    {
                        id_estado_origen = estado.id_estado,
                        id_estado_destino = estadoAnulada.id_estado,
                        nombre_accion = "Anular",
                        requiere_observacion = true,
                        es_anulacion = true,
                        orden = ordenAnulacion++,
                        activo = true
                    });
                }
            }

            return lista
                .GroupBy(t => new { t.id_estado_origen, t.id_estado_destino })
                .Select(g => g.First())
                .OrderBy(t => t.orden)
                .ToList();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transiciones(ProyectoWizardTransicionesVm vm)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .Include(p => p.Estados)
                .FirstOrDefaultAsync(p => p.id_proyecto == vm.id_proyecto);

            if (proyecto == null)
                return NotFound();

            vm.nombre_proyecto = proyecto.nombre;

            var estadosActivosIds = proyecto.Estados
                .Where(e => e.activo)
                .Select(e => e.id_estado)
                .ToHashSet();

            vm.Estados = proyecto.Estados
                .Where(e => e.activo)
                .OrderBy(e => e.orden)
                .Select(e => new SelectListItem
                {
                    Value = e.id_estado.ToString(),
                    Text = $"{e.orden}. {e.nombre}"
                })
                .ToList();

            var transicionesValidar = vm.Transiciones
                .Where(t => !t.eliminar && t.activo)
                .ToList();

            if (!transicionesValidar.Any())
                ModelState.AddModelError("", "Debe existir al menos una transición activa.");

            foreach (var t in transicionesValidar)
            {
                if (!t.id_estado_origen.HasValue || !t.id_estado_destino.HasValue)
                    continue;

                if (t.id_estado_origen.Value == t.id_estado_destino.Value)
                    ModelState.AddModelError("", "El estado origen y destino no pueden ser iguales.");

                if (!estadosActivosIds.Contains(t.id_estado_origen.Value))
                    ModelState.AddModelError("", "Una transición tiene un estado origen que no pertenece a los estados activos del proyecto.");

                if (!estadosActivosIds.Contains(t.id_estado_destino.Value))
                    ModelState.AddModelError("", "Una transición tiene un estado destino que no pertenece a los estados activos del proyecto.");
            }

            if (transicionesValidar
                .Where(t => t.id_estado_origen.HasValue && t.id_estado_destino.HasValue)
                .GroupBy(t => new { t.id_estado_origen, t.id_estado_destino })
                .Any(g => g.Count() > 1))
            {
                ModelState.AddModelError("", "No se puede repetir la misma transición origen-destino activa.");
            }

            if (!transicionesValidar.Any(t => t.es_aprobacion))
                ModelState.AddModelError("", "Debe existir al menos una transición activa marcada como aprobación.");

            if (transicionesValidar.Count(t => t.es_rechazo) > 1)
                ModelState.AddModelError("", "Solo debería existir una transición activa marcada como rechazo.");

            if (transicionesValidar.Any(t => t.es_aprobacion && t.es_rechazo))
                ModelState.AddModelError("", "Una transición no puede ser aprobación y rechazo al mismo tiempo.");

            if (transicionesValidar.Any(t => t.es_aprobacion && t.es_anulacion))
                ModelState.AddModelError("", "Una transición no puede ser aprobación y anulación al mismo tiempo.");

            if (transicionesValidar.Any(t => t.es_rechazo && t.es_anulacion))
                ModelState.AddModelError("", "Una transición no puede ser rechazo y anulación al mismo tiempo.");

            if (!ModelState.IsValid)
                return View(vm);

            var transicionesActuales = await _context.EstadosTransicion
                .Where(t => t.id_proyecto == proyecto.id_proyecto)
                .ToListAsync();

            bool transicionesCambiaron = false;

            foreach (var item in vm.Transiciones)
            {
                if (item.id_estado_transicion.HasValue)
                {
                    var actual = transicionesActuales
                        .FirstOrDefault(t => t.id_estado_transicion == item.id_estado_transicion.Value);

                    if (actual == null)
                    {
                        transicionesCambiaron = true;
                        continue;
                    }

                    if (
                        actual.id_estado_origen != item.id_estado_origen ||
                        actual.id_estado_destino != item.id_estado_destino ||
                        actual.nombre_accion.Trim() != item.nombre_accion.Trim() ||
                        actual.requiere_observacion != item.requiere_observacion ||
                        actual.es_aprobacion != item.es_aprobacion ||
                        actual.es_rechazo != item.es_rechazo ||
                        actual.es_anulacion != item.es_anulacion ||
                        actual.activo != item.activo ||
                        actual.orden != item.orden ||
                        item.eliminar
                    )
                    {
                        transicionesCambiaron = true;
                    }
                }
                else
                {
                    if (!item.eliminar)
                        transicionesCambiaron = true;
                }
            }

            foreach (var item in vm.Transiciones)
            {
                if (item.eliminar)
                {
                    if (item.id_estado_transicion.HasValue)
                    {
                        var transicionEliminar = transicionesActuales
                            .FirstOrDefault(t => t.id_estado_transicion == item.id_estado_transicion.Value);

                        if (transicionEliminar != null)
                            transicionEliminar.activo = false;
                    }

                    continue;
                }

                if (!item.id_estado_origen.HasValue || !item.id_estado_destino.HasValue)
                    continue;

                EstadoTransicion transicion;

                if (item.id_estado_transicion.HasValue)
                {
                    transicion = transicionesActuales
                        .First(t => t.id_estado_transicion == item.id_estado_transicion.Value);

                    transicion.id_estado_origen = item.id_estado_origen.Value;
                    transicion.id_estado_destino = item.id_estado_destino.Value;
                    transicion.nombre_accion = item.nombre_accion.Trim();
                    transicion.requiere_observacion = item.requiere_observacion;
                    transicion.es_aprobacion = item.es_aprobacion;
                    transicion.es_rechazo = item.es_rechazo;
                    transicion.es_anulacion = item.es_anulacion;
                    transicion.activo = item.activo;
                    transicion.orden = item.orden;
                }
                else
                {
                    transicion = new EstadoTransicion
                    {
                        id_proyecto = proyecto.id_proyecto,
                        id_estado_origen = item.id_estado_origen.Value,
                        id_estado_destino = item.id_estado_destino.Value,
                        nombre_accion = item.nombre_accion.Trim(),
                        requiere_observacion = item.requiere_observacion,
                        es_aprobacion = item.es_aprobacion,
                        es_rechazo = item.es_rechazo,
                        es_anulacion = item.es_anulacion,
                        activo = item.activo,
                        orden = item.orden
                    };

                    _context.EstadosTransicion.Add(transicion);
                }
            }

            if (transicionesCambiaron)
            {
                proyecto.configuracion_completa = false;

                var idsTransiciones = transicionesActuales
                    .Select(t => t.id_estado_transicion)
                    .ToList();

                var rolesTransicion = await _context.RolesEstadosTransicion
                    .Where(r => idsTransiciones.Contains(r.id_estado_transicion))
                    .ToListAsync();

                _context.RolesEstadosTransicion.RemoveRange(rolesTransicion);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("RolesTransicion", new { id = proyecto.id_proyecto });
        }

        public async Task<IActionResult> RolesTransicion(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            var transiciones = await _context.EstadosTransicion
                .Include(t => t.EstadoOrigen)
                .Include(t => t.EstadoDestino)
                .Include(t => t.RolesEstadosTransicion)
                .Where(t => t.id_proyecto == id
                    && t.activo
                    && !t.es_anulacion)
                .OrderBy(t => t.orden)
                .ToListAsync();

            if (!transiciones.Any())
            {
                TempData["Error"] = "Debe configurar primero las transiciones del proyecto.";
                return RedirectToAction("Transiciones", new { id });
            }

            var roles = await _context.Roles
                .Where(r => r.Activo)
                .Where(r => !r.nombre.ToLower().EndsWith("proyecto"))
                .Where(r => r.nombre.ToLower() != "vencimiento")
                .OrderBy(r => r.nombre)
                .Select(r => new ProyectoWizardRolVm
                {
                    id_rol = r.id_rol,
                    nombre = r.nombre
                })
                .ToListAsync();

            var vm = new ProyectoWizardRolesTransicionVm
            {
                id_proyecto = proyecto.id_proyecto,
                nombre_proyecto = proyecto.nombre,
                Roles = roles,
                Transiciones = transiciones.Select(t => new ProyectoWizardRolTransicionItemVm
                {
                    id_estado_transicion = t.id_estado_transicion,
                    estado_origen = t.EstadoOrigen.nombre,
                    estado_destino = t.EstadoDestino.nombre,
                    nombre_accion = t.nombre_accion,
                    es_aprobacion = t.es_aprobacion,
                    es_rechazo = t.es_rechazo,
                    es_anulacion = t.es_anulacion,
                    roles_seleccionados = t.RolesEstadosTransicion
                        .Where(r => r.activo)
                        .Select(r => r.id_rol)
                        .ToList()
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RolesTransicion(ProyectoWizardRolesTransicionVm vm)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == vm.id_proyecto);

            if (proyecto == null)
                return NotFound();

            var transicionesActivas = await _context.EstadosTransicion
                .Where(t => t.id_proyecto == vm.id_proyecto
                    && t.activo
                    && !t.es_anulacion)
                .Select(t => t.id_estado_transicion)
                .ToListAsync();

            foreach (var item in vm.Transiciones)
            {
                if (transicionesActivas.Contains(item.id_estado_transicion)
                    && (item.roles_seleccionados == null || !item.roles_seleccionados.Any()))
                {
                    ModelState.AddModelError("", "Cada transición activa debe tener al menos un rol asignado.");
                }
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction("RolesTransicion", new { id = vm.id_proyecto });
            }

            var idsTransiciones = transicionesActivas;

            var rolesActuales = await _context.RolesEstadosTransicion
                .Where(r => idsTransiciones.Contains(r.id_estado_transicion))
                .ToListAsync();

            _context.RolesEstadosTransicion.RemoveRange(rolesActuales);

            foreach (var item in vm.Transiciones)
            {
                if (!transicionesActivas.Contains(item.id_estado_transicion))
                    continue;

                foreach (var idRol in item.roles_seleccionados.Distinct())
                {
                    _context.RolesEstadosTransicion.Add(new RolEstadoTransicion
                    {
                        id_estado_transicion = item.id_estado_transicion,
                        id_rol = idRol,
                        activo = true
                    });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("SoportePostCierre", new { id = vm.id_proyecto });
        }

        public async Task<IActionResult> GrupoAlertas(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            var grupo = await _context.GruposAlertas
                .Where(g => g.id_proyecto == id)
                .OrderByDescending(g => g.activo)
                .ThenBy(g => g.id_grupo_alerta)
                .FirstOrDefaultAsync();

            var vm = new ProyectoWizardGrupoAlertasVm
            {
                id_proyecto = proyecto.id_proyecto,
                nombre_proyecto = proyecto.nombre,
                id_grupo_alerta = grupo?.id_grupo_alerta,
                nombre = grupo?.nombre ?? $"Alertas {proyecto.nombre}",
                activo = grupo?.activo ?? true
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrupoAlertas(ProyectoWizardGrupoAlertasVm vm)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == vm.id_proyecto);

            if (proyecto == null)
                return NotFound();

            vm.nombre_proyecto = proyecto.nombre;

            if (!ModelState.IsValid)
                return View(vm);

            bool nombreRepetido = await _context.GruposAlertas.AnyAsync(g =>
                g.id_proyecto == vm.id_proyecto &&
                g.nombre.ToLower() == vm.nombre.Trim().ToLower() &&
                (!vm.id_grupo_alerta.HasValue || g.id_grupo_alerta != vm.id_grupo_alerta.Value));

            if (nombreRepetido)
            {
                ModelState.AddModelError(nameof(vm.nombre), "Ya existe un grupo de alertas con este nombre para el proyecto.");
                return View(vm);
            }

            var gruposActivos = await _context.GruposAlertas
                .Where(g => g.id_proyecto == vm.id_proyecto && g.activo)
                .ToListAsync();

            GrupoAlerta grupo;

            if (vm.id_grupo_alerta.HasValue)
            {
                grupo = await _context.GruposAlertas
                    .FirstOrDefaultAsync(g => g.id_grupo_alerta == vm.id_grupo_alerta.Value);

                if (grupo == null)
                    return NotFound();

                grupo.nombre = vm.nombre.Trim();
                grupo.activo = vm.activo;
            }
            else
            {
                grupo = new GrupoAlerta
                {
                    id_proyecto = vm.id_proyecto,
                    nombre = vm.nombre.Trim(),
                    activo = true
                };

                _context.GruposAlertas.Add(grupo);
            }

            // Regla: un único grupo activo principal por proyecto
            foreach (var g in gruposActivos.Where(g => g.id_grupo_alerta != grupo.id_grupo_alerta))
            {
                g.activo = false;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("AlertasDias", new { id = vm.id_proyecto });
        }

        public async Task<IActionResult> AlertasDias(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            var grupo = await _context.GruposAlertas
                .FirstOrDefaultAsync(g => g.id_proyecto == id && g.activo);

            if (grupo == null)
            {
                TempData["Error"] = "Debe configurar primero el grupo de alertas.";
                return RedirectToAction("GrupoAlertas", new { id });
            }

            var alertas = await _context.GruposAlertasDias
                .Where(a => a.id_grupo_alerta == grupo.id_grupo_alerta)
                .OrderBy(a => a.id_grupo_alerta_dia)
                .ToListAsync();

            if (!alertas.Any())
            {
                alertas = await CrearPlantillaAlertasDiasAsync(grupo.id_grupo_alerta);
            }

            var vm = new ProyectoWizardAlertasDiasVm
            {
                id_proyecto = proyecto.id_proyecto,
                id_grupo_alerta = grupo.id_grupo_alerta,
                nombre_proyecto = proyecto.nombre,
                nombre_grupo_alerta = grupo.nombre,
                Roles = await CargarRolesAlertasAsync(),
                Mensajes = await CargarMensajesAsync(),
                Dependencias = alertas
                    .Where(a => a.id_grupo_alerta_dia > 0)
                    .Select(a => new SelectListItem
                    {
                        Value = a.id_grupo_alerta_dia.ToString(),
                        Text = a.nombre
                    })
                    .ToList(),
                AlertasDias = alertas.Select(a => new ProyectoWizardAlertaDiaItemVm
                {
                    id_grupo_alerta_dia = a.id_grupo_alerta_dia == 0 ? null : a.id_grupo_alerta_dia,
                    nombre = a.nombre,
                    tipo_control = a.tipo_control,
                    operador = a.operador,
                    valor_dias = a.valor_dias,
                    id_rol = a.id_rol,
                    id_mensaje = a.id_mensaje,
                    id_dependencia = a.id_dependencia,
                    activo = a.activo
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlertasDias(ProyectoWizardAlertasDiasVm vm)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var grupo = await _context.GruposAlertas
                .FirstOrDefaultAsync(g => g.id_grupo_alerta == vm.id_grupo_alerta);

            if (grupo == null)
                return NotFound();

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == vm.id_proyecto);

            if (proyecto == null)
                return NotFound();

            vm.nombre_proyecto = proyecto.nombre;
            vm.nombre_grupo_alerta = grupo.nombre;
            vm.Roles = await CargarRolesAlertasAsync();
            vm.Mensajes = await CargarMensajesAsync();

            var alertasValidar = vm.AlertasDias
                .Where(a => !a.eliminar && a.activo)
                .ToList();

            if (!alertasValidar.Any())
                ModelState.AddModelError("", "Debe existir al menos una regla de alerta activa.");

            if (alertasValidar.Any(a => a.tipo_control != "VENCIMIENTO" && a.tipo_control != "SEGUIMIENTO"))
                ModelState.AddModelError("", "El tipo de control debe ser VENCIMIENTO o SEGUIMIENTO.");

            var operadoresValidos = new[] { "<", "<=", "=", ">=", ">" };

            if (alertasValidar.Any(a => !operadoresValidos.Contains(a.operador)))
                ModelState.AddModelError("", "Hay operadores no válidos.");

            if (alertasValidar
                .Where(a => a.id_rol.HasValue)
                .GroupBy(a => new { a.tipo_control, a.operador, a.valor_dias, a.id_rol })
                .Any(g => g.Count() > 1))
            {
                ModelState.AddModelError("", "No se puede repetir la misma combinación de tipo control, operador, días y rol.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var existentes = await _context.GruposAlertasDias
                .Where(a => a.id_grupo_alerta == vm.id_grupo_alerta)
                .ToListAsync();

            bool alertasCambiaron = false;

            foreach (var item in vm.AlertasDias)
            {
                if (item.id_grupo_alerta_dia.HasValue)
                {
                    var actual = existentes
                        .FirstOrDefault(a => a.id_grupo_alerta_dia == item.id_grupo_alerta_dia.Value);

                    if (actual == null)
                    {
                        alertasCambiaron = true;
                        continue;
                    }

                    if (
                        actual.nombre.Trim() != item.nombre.Trim() ||
                        actual.tipo_control != item.tipo_control ||
                        actual.operador != item.operador ||
                        actual.valor_dias != item.valor_dias ||
                        actual.id_rol != item.id_rol ||
                        actual.id_mensaje != item.id_mensaje ||
                        actual.activo != item.activo ||
                        item.eliminar
                    )
                    {
                        alertasCambiaron = true;
                    }
                }
                else
                {
                    if (!item.eliminar)
                        alertasCambiaron = true;
                }
            }

            bool eraPrimeraConfiguracion = !existentes.Any(a => a.activo);

            foreach (var item in vm.AlertasDias)
            {
                if (item.eliminar)
                {
                    if (item.id_grupo_alerta_dia.HasValue)
                    {
                        var alertaEliminar = existentes
                            .FirstOrDefault(a => a.id_grupo_alerta_dia == item.id_grupo_alerta_dia.Value);

                        if (alertaEliminar != null)
                            alertaEliminar.activo = false;
                    }

                    continue;
                }

                GrupoAlertaDia alerta;

                if (item.id_grupo_alerta_dia.HasValue)
                {
                    alerta = existentes.First(a => a.id_grupo_alerta_dia == item.id_grupo_alerta_dia.Value);

                    alerta.nombre = item.nombre.Trim();
                    alerta.tipo_control = item.tipo_control;
                    alerta.operador = item.operador;
                    alerta.valor_dias = item.valor_dias;
                    alerta.id_rol = item.id_rol!.Value;
                    alerta.id_mensaje = item.id_mensaje!.Value;
                    alerta.activo = item.activo;
                }
                else
                {
                    alerta = new GrupoAlertaDia
                    {
                        id_grupo_alerta = vm.id_grupo_alerta,
                        nombre = item.nombre.Trim(),
                        tipo_control = item.tipo_control,
                        operador = item.operador,
                        valor_dias = item.valor_dias,
                        id_dependencia = null,
                        id_rol = item.id_rol!.Value,
                        id_mensaje = item.id_mensaje!.Value,
                        activo = item.activo
                    };

                    _context.GruposAlertasDias.Add(alerta);
                }
            }

            if (alertasCambiaron)
            {
                proyecto.configuracion_completa = false;

                var idsAlertas = existentes
                    .Select(a => a.id_grupo_alerta_dia)
                    .ToList();

                var estadosOff = await _context.GruposAlertasDiasEstadosOff
                    .Where(eo => idsAlertas.Contains(eo.id_grupo_alerta_dia))
                    .ToListAsync();

                _context.GruposAlertasDiasEstadosOff.RemoveRange(estadosOff);

                foreach (var alerta in existentes)
                {
                    alerta.id_dependencia = null;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("DependenciasAlertas", new
            {
                id = vm.id_proyecto,
                sugerir = eraPrimeraConfiguracion
            });
        }

        private async Task<List<GrupoAlertaDia>> CrearPlantillaAlertasDiasAsync(int idGrupoAlerta)
        {
            var roles = await _context.Roles
                .Where(r => r.Activo)
                .ToListAsync();

            var mensajes = await _context.Mensajes
                .Where(m => m.activo)
                .ToListAsync();

            int? RolId(string nombre) =>
                roles.FirstOrDefault(r => r.nombre.Trim().ToLower() == nombre.Trim().ToLower())?.id_rol;

            int? MensajeId(string nombre) =>
                mensajes.FirstOrDefault(m => m.nombre.Trim().ToLower() == nombre.Trim().ToLower())?.id_mensaje;

            var responsable = RolId("Responsable");
            var elaborador = RolId("Elaborador");
            var autorizador = RolId("Autorizador");
            var aprobador = RolId("Aprobador");
            var vencimiento = RolId("Vencimiento");

            var defInicioObl = MensajeId("Def_Inicio_Obl");
            var defInicioSeg = MensajeId("Def_Inicio_Seg");
            var defPresentacionObl = MensajeId("Def_Presentacion_Obl");
            var defEnvioSeg = MensajeId("Def_Envio_Seg");
            var defVencimientoSeg = MensajeId("Def_Vencimiento_Seg");
            var defVencimientoObl = MensajeId("Def_Vencimiento_Obl");

            var lista = new List<GrupoAlertaDia>();

            void Add(string nombre, string tipoControl, string operador, int dias, int? idMensaje, params int?[] rolesDestino)
            {
                foreach (var idRol in rolesDestino.Where(r => r.HasValue).Select(r => r!.Value).Distinct())
                {
                    if (!idMensaje.HasValue)
                        continue;

                    lista.Add(new GrupoAlertaDia
                    {
                        id_grupo_alerta = idGrupoAlerta,
                        nombre = nombre,
                        tipo_control = tipoControl,
                        operador = operador,
                        valor_dias = dias,
                        id_rol = idRol,
                        id_mensaje = idMensaje.Value,
                        activo = true
                    });
                }
            }

            Add("Inicio elaboración de la obligación", "VENCIMIENTO", "=", -20, defInicioObl, responsable, elaborador);
            Add("Inicio seguimiento de la obligación", "SEGUIMIENTO", "=", -3, defInicioSeg, responsable, elaborador);
            Add("Presentación de la obligación", "VENCIMIENTO", "=", 0, defPresentacionObl, responsable, elaborador, autorizador);
            Add("Envío seguimiento de la obligación", "SEGUIMIENTO", "=", 0, defEnvioSeg, responsable, elaborador, autorizador);
            Add("Vencimiento envío del seguimiento", "SEGUIMIENTO", ">", 0, defVencimientoSeg, responsable, elaborador, autorizador, aprobador);
            Add("Vencimiento cumplimiento de la obligación", "VENCIMIENTO", ">", 0, defVencimientoObl, responsable, elaborador, autorizador, aprobador, vencimiento);

            return lista;
        }

        private async Task<List<SelectListItem>> CargarRolesAlertasAsync()
        {
            return await _context.Roles
                .Where(r => r.Activo)
                .OrderBy(r => r.nombre)
                .Select(r => new SelectListItem
                {
                    Value = r.id_rol.ToString(),
                    Text = r.nombre
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> CargarMensajesAsync()
        {
            return await _context.Mensajes
                .Where(m => m.activo)
                .OrderBy(m => m.nombre)
                .Select(m => new SelectListItem
                {
                    Value = m.id_mensaje.ToString(),
                    Text = m.nombre
                })
                .ToListAsync();
        }

        public async Task<IActionResult> DependenciasAlertas(int id, bool sugerir = false)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            var grupo = await _context.GruposAlertas
                .FirstOrDefaultAsync(g => g.id_proyecto == id && g.activo);

            if (grupo == null)
            {
                TempData["Error"] = "Debe configurar primero el grupo de alertas.";
                return RedirectToAction("GrupoAlertas", new { id });
            }

            var alertas = await _context.GruposAlertasDias
                .Include(a => a.Rol)
                .Where(a => a.id_grupo_alerta == grupo.id_grupo_alerta && a.activo)
                .OrderBy(a => a.id_grupo_alerta_dia)
                .ToListAsync();

            if (!alertas.Any())
            {
                TempData["Error"] = "Debe configurar primero las reglas de alertas por días.";
                return RedirectToAction("AlertasDias", new { id });
            }


            var vm = new ProyectoWizardDependenciasAlertasVm
            {
                id_proyecto = proyecto.id_proyecto,
                id_grupo_alerta = grupo.id_grupo_alerta,
                nombre_proyecto = proyecto.nombre,
                nombre_grupo_alerta = grupo.nombre,


                Alertas = alertas.Select(a =>
                {
                    var dependencias = alertas
                        .Where(d => d.id_grupo_alerta_dia != a.id_grupo_alerta_dia)
                        .OrderBy(d => d.tipo_control)
                        .ThenBy(d => d.valor_dias)
                        .ThenBy(d => d.nombre)
                        .ThenBy(d => d.Rol.nombre)
                        .Select(d => new SelectListItem
                        {
                            Value = d.id_grupo_alerta_dia.ToString(),
                            Text = $"{d.nombre} ({d.tipo_control} {d.operador} {d.valor_dias}) - {d.Rol.nombre}",
                            Group = new SelectListGroup
                            {
                                Name = d.tipo_control
                            }
                        })
                        .ToList();

                    dependencias.Insert(0, new SelectListItem
                    {
                        Value = "",
                        Text = "Sin dependencia"
                    });

                    int? dependenciaMostrar = a.id_dependencia;

                    if (sugerir && dependenciaMostrar == null &&
                        (a.nombre == "Presentación de la obligación" ||
                         a.nombre == "Vencimiento cumplimiento de la obligación"))
                    {
                        var dependenciaSugerida = alertas.FirstOrDefault(d =>
                            d.nombre == "Vencimiento envío del seguimiento" &&
                            d.id_rol == a.id_rol);

                        if (dependenciaSugerida != null)
                        {
                            dependenciaMostrar = dependenciaSugerida.id_grupo_alerta_dia;
                        }
                    }

                    return new ProyectoWizardDependenciaAlertaItemVm
                    {
                        id_grupo_alerta_dia = a.id_grupo_alerta_dia,
                        nombre = a.nombre,
                        rol = a.Rol.nombre,
                        tipo_control = a.tipo_control,
                        id_dependencia = dependenciaMostrar,
                        condicion = $"{a.tipo_control} {a.operador} {a.valor_dias}",
                        DependenciasDisponibles = dependencias
                    };
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DependenciasAlertas(ProyectoWizardDependenciasAlertasVm vm)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var grupo = await _context.GruposAlertas
                .FirstOrDefaultAsync(g => g.id_grupo_alerta == vm.id_grupo_alerta);

            if (grupo == null)
                return NotFound();

            var alertas = await _context.GruposAlertasDias
                .Where(a => a.id_grupo_alerta == vm.id_grupo_alerta && a.activo)
                .ToListAsync();

            var idsAlertas = alertas
                .Select(a => a.id_grupo_alerta_dia)
                .ToHashSet();

            foreach (var item in vm.Alertas)
            {
                if (!idsAlertas.Contains(item.id_grupo_alerta_dia))
                    continue;

                if (item.id_dependencia.HasValue &&
                    !idsAlertas.Contains(item.id_dependencia.Value))
                {
                    ModelState.AddModelError("", "Una dependencia seleccionada no pertenece al grupo de alertas del proyecto.");
                }

                if (item.id_dependencia == item.id_grupo_alerta_dia)
                {
                    ModelState.AddModelError("", "Una alerta no puede depender de sí misma.");
                }
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Hay dependencias inválidas. Revise la configuración.";
                return RedirectToAction("DependenciasAlertas", new { id = vm.id_proyecto });
            }

            foreach (var item in vm.Alertas)
            {
                var alerta = alertas
                    .FirstOrDefault(a => a.id_grupo_alerta_dia == item.id_grupo_alerta_dia);

                if (alerta == null)
                    continue;

                alerta.id_dependencia = item.id_dependencia;
            }

            bool TieneCiclo(int origenId, int? dependenciaId, Dictionary<int, int?> mapa)
            {
                var visitados = new HashSet<int>();

                while (dependenciaId.HasValue)
                {
                    if (!visitados.Add(dependenciaId.Value))
                        return true;

                    if (dependenciaId.Value == origenId)
                        return true;

                    mapa.TryGetValue(dependenciaId.Value, out dependenciaId);
                }

                return false;
            }

            var mapa = vm.Alertas.ToDictionary(
                a => a.id_grupo_alerta_dia,
                a => a.id_dependencia
);

            var errores = new List<string>();

            foreach (var item in vm.Alertas)
            {
                if (item.id_dependencia.HasValue &&
                    TieneCiclo(item.id_grupo_alerta_dia, item.id_dependencia, mapa))
                {
                    errores.Add($"La alerta '{item.nombre}' genera un ciclo de dependencias.");
                }
            }

            if (errores.Any())
            {
                TempData["Error"] = string.Join("<br>", errores);
                return RedirectToAction("DependenciasAlertas", new { id = vm.id_proyecto });
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction("DependenciasAlertas", new { id = vm.id_proyecto });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("EstadosOff", new { id = vm.id_proyecto });
        }
        public async Task<IActionResult> EstadosOff(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            var grupo = await _context.GruposAlertas
                .FirstOrDefaultAsync(g => g.id_proyecto == id && g.activo);

            if (grupo == null)
            {
                TempData["Error"] = "Debe configurar primero el grupo de alertas.";
                return RedirectToAction("GrupoAlertas", new { id });
            }

            var estados = await _context.Estados
                .Where(e => e.id_proyecto == id && e.activo)
                .OrderBy(e => e.orden)
                .ToListAsync();

            var alertas = await _context.GruposAlertasDias
                .Include(a => a.Rol)
                .Include(a => a.EstadosOff)
                .Where(a => a.id_grupo_alerta == grupo.id_grupo_alerta && a.activo)
                .OrderBy(a => a.id_grupo_alerta_dia)
                .ToListAsync();

            if (!alertas.Any())
            {
                TempData["Error"] = "Debe configurar primero las reglas de alertas.";
                return RedirectToAction("AlertasDias", new { id });
            }

            bool yaTieneEstadosOffConfigurados = alertas
                .Any(a => a.EstadosOff != null && a.EstadosOff.Any());

            var estadoCerrada = estados.FirstOrDefault(e => e.nombre.Trim().ToLower() == "cerrada");
            var estadoAnulada = estados.FirstOrDefault(e => e.nombre.Trim().ToLower() == "anulada");
            var estadoControlVencimiento = estados.FirstOrDefault(e => e.control_vencimiento);
            var estadoControlSeguimiento = estados.FirstOrDefault(e => e.control_seguimiento);

            var vm = new ProyectoWizardEstadosOffVm
            {
                id_proyecto = proyecto.id_proyecto,
                id_grupo_alerta = grupo.id_grupo_alerta,
                nombre_proyecto = proyecto.nombre,
                nombre_grupo_alerta = grupo.nombre,

                Estados = estados.Select(e => new ProyectoWizardEstadoOffEstadoVm
                {
                    id_estado = e.id_estado,
                    nombre = e.nombre,
                    orden = e.orden
                }).ToList(),

                Alertas = alertas.Select(a =>
                {
                    List<int> estadosSeleccionados;

                    if (yaTieneEstadosOffConfigurados)
                    {
                        estadosSeleccionados = a.EstadosOff
                            .Select(eo => eo.id_estado)
                            .ToList();
                    }
                    else
                    {
                        estadosSeleccionados = new List<int>();

                        if (estadoCerrada != null)
                            estadosSeleccionados.Add(estadoCerrada.id_estado);

                        if (estadoAnulada != null)
                            estadosSeleccionados.Add(estadoAnulada.id_estado);

                        if (a.tipo_control == "VENCIMIENTO" && estadoControlVencimiento != null)
                        {
                            estadosSeleccionados.AddRange(
                                estados
                                    .Where(e => e.orden >= estadoControlVencimiento.orden)
                                    .Select(e => e.id_estado)
                            );
                        }

                        if (a.tipo_control == "SEGUIMIENTO" && estadoControlSeguimiento != null)
                        {
                            estadosSeleccionados.AddRange(
                                estados
                                    .Where(e => e.orden >= estadoControlSeguimiento.orden)
                                    .Select(e => e.id_estado)
                            );
                        }

                        estadosSeleccionados = estadosSeleccionados
                            .Distinct()
                            .ToList();
                    }

                    return new ProyectoWizardEstadoOffAlertaVm
                    {
                        id_grupo_alerta_dia = a.id_grupo_alerta_dia,
                        nombre = a.nombre,
                        rol = a.Rol.nombre,
                        condicion = $"{a.tipo_control} {a.operador} {a.valor_dias}",
                        estados_off_seleccionados = estadosSeleccionados
                    };
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EstadosOff(ProyectoWizardEstadosOffVm vm)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var grupo = await _context.GruposAlertas
                .FirstOrDefaultAsync(g => g.id_grupo_alerta == vm.id_grupo_alerta);

            if (grupo == null)
                return NotFound();

            var alertasIds = await _context.GruposAlertasDias
                .Where(a => a.id_grupo_alerta == vm.id_grupo_alerta && a.activo)
                .Select(a => a.id_grupo_alerta_dia)
                .ToListAsync();

            var estadosIds = await _context.Estados
                .Where(e => e.id_proyecto == vm.id_proyecto && e.activo)
                .Select(e => e.id_estado)
                .ToListAsync();

            foreach (var alerta in vm.Alertas)
            {
                if (!alertasIds.Contains(alerta.id_grupo_alerta_dia))
                    continue;

                foreach (var idEstado in alerta.estados_off_seleccionados.Distinct())
                {
                    if (!estadosIds.Contains(idEstado))
                        ModelState.AddModelError("", "Uno de los estados seleccionados no pertenece al proyecto.");
                }
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Hay estados off inválidos. Revise la configuración.";
                return RedirectToAction("EstadosOff", new { id = vm.id_proyecto });
            }

            var existentes = await _context.GruposAlertasDiasEstadosOff
                .Where(eo => alertasIds.Contains(eo.id_grupo_alerta_dia))
                .ToListAsync();

            _context.GruposAlertasDiasEstadosOff.RemoveRange(existentes);

            foreach (var alerta in vm.Alertas)
            {
                if (!alertasIds.Contains(alerta.id_grupo_alerta_dia))
                    continue;

                foreach (var idEstado in alerta.estados_off_seleccionados.Distinct())
                {
                    _context.GruposAlertasDiasEstadosOff.Add(new GrupoAlertaDiaEstadoOff
                    {
                        id_grupo_alerta_dia = alerta.id_grupo_alerta_dia,
                        id_estado = idEstado
                    });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Resumen", new { id = vm.id_proyecto });
        }

        public async Task<IActionResult> Resumen(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .Include(p => p.Area)
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            var estados = await _context.Estados
                .Where(e => e.id_proyecto == id && e.activo)
                .ToListAsync();

            var transiciones = await _context.EstadosTransicion
                .Where(t => t.id_proyecto == id && t.activo)
                .ToListAsync();

            var transicionesOperativas = transiciones
                .Where(t => !t.es_anulacion)
                .Select(t => t.id_estado_transicion)
                .ToList();

            var rolesTransicion = await _context.RolesEstadosTransicion
                .Where(r => transicionesOperativas.Contains(r.id_estado_transicion) && r.activo)
                .ToListAsync();

            var grupo = await _context.GruposAlertas
                .FirstOrDefaultAsync(g => g.id_proyecto == id && g.activo);

            var alertasDias = new List<GrupoAlertaDia>();
            var estadosOff = 0;
            var dependencias = 0;

            if (grupo != null)
            {
                alertasDias = await _context.GruposAlertasDias
                    .Where(a => a.id_grupo_alerta == grupo.id_grupo_alerta && a.activo)
                    .ToListAsync();

                var idsAlertas = alertasDias.Select(a => a.id_grupo_alerta_dia).ToList();

                estadosOff = await _context.GruposAlertasDiasEstadosOff
                    .CountAsync(eo => idsAlertas.Contains(eo.id_grupo_alerta_dia));

                dependencias = alertasDias.Count(a => a.id_dependencia.HasValue);
            }

            var vm = new ProyectoWizardResumenVm
            {
                id_proyecto = proyecto.id_proyecto,
                nombre_proyecto = proyecto.nombre,
                area = proyecto.Area?.nombre ?? "",
                nombre_seguimiento = proyecto.nombre_seguimiento,

                total_estados = estados.Count,
                total_transiciones = transiciones.Count,
                total_roles_transicion = rolesTransicion.Count,
                total_grupos_alertas = grupo == null ? 0 : 1,
                total_alertas_dias = alertasDias.Count,
                total_dependencias = dependencias,
                total_estados_off = estadosOff,

                tiene_estado_seguimiento = estados.Count(e => e.control_seguimiento) == 1,
                tiene_estado_vencimiento = estados.Count(e => e.control_vencimiento) == 1,
                tiene_cerrada = estados.Any(e => e.nombre.Trim().ToLower() == "cerrada"),
                tiene_anulada = estados.Any(e => e.nombre.Trim().ToLower() == "anulada")
            };

            if (!estados.Any())
                vm.Errores.Add("El proyecto no tiene estados activos.");

            if (!vm.tiene_estado_seguimiento)
                vm.Errores.Add("Debe existir un único estado con control de seguimiento.");

            if (!vm.tiene_estado_vencimiento)
                vm.Errores.Add("Debe existir un único estado con control de vencimiento.");

            if (!vm.tiene_cerrada)
                vm.Errores.Add("Debe existir el estado Cerrada.");

            if (!vm.tiene_anulada)
                vm.Errores.Add("Debe existir el estado Anulada.");

            if (!transiciones.Any())
                vm.Errores.Add("El proyecto no tiene transiciones activas.");

            var transicionesSinRol = transicionesOperativas
                .Where(idTransicion => !rolesTransicion.Any(r => r.id_estado_transicion == idTransicion))
                .ToList();

            if (transicionesSinRol.Any())
                vm.Errores.Add("Hay transiciones operativas sin roles asignados.");

            if (grupo == null)
                vm.Errores.Add("El proyecto no tiene grupo de alertas activo.");

            if (!alertasDias.Any())
                vm.Errores.Add("El proyecto no tiene reglas de alertas por días activas.");

            if (estadosOff == 0)
                vm.Errores.Add("El proyecto no tiene estados off configurados para alertas.");

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalizar(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            var estados = await _context.Estados
                .Where(e => e.id_proyecto == id && e.activo)
                .ToListAsync();

            var grupo = await _context.GruposAlertas
                .FirstOrDefaultAsync(g => g.id_proyecto == id && g.activo);

            var transiciones = await _context.EstadosTransicion
                .Where(t => t.id_proyecto == id && t.activo)
                .ToListAsync();

            if (!estados.Any() ||
                estados.Count(e => e.control_seguimiento) != 1 ||
                estados.Count(e => e.control_vencimiento) != 1 ||
                !estados.Any(e => e.nombre.Trim().ToLower() == "cerrada") ||
                !estados.Any(e => e.nombre.Trim().ToLower() == "anulada") ||
                grupo == null ||
                !transiciones.Any())
            {
                TempData["Error"] = "La configuración del proyecto aún no está completa.";
                return RedirectToAction("Resumen", new { id });
            }

            proyecto.configuracion_completa = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Proyecto configurado correctamente.";
            return RedirectToAction("Index");
        }

        private async Task<PasoWizardProyectoDto> ObtenerSiguientePasoProyecto(int idProyecto)
        {
            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == idProyecto);

            if (proyecto == null)
                return new PasoWizardProyectoDto { Accion = "Index", NombrePaso = "No encontrado" };

            if (string.IsNullOrWhiteSpace(proyecto.nombre) || proyecto.id_area == 0)
                return new PasoWizardProyectoDto { Accion = "DatosGenerales", NombrePaso = "Paso 1 - Datos generales" };

            var estados = await _context.Estados
                .Where(e => e.id_proyecto == idProyecto && e.activo)
                .ToListAsync();

            if (!estados.Any())
                return new PasoWizardProyectoDto { Accion = "Estados", NombrePaso = "Paso 2 - Estados" };

            var transiciones = await _context.EstadosTransicion
                .Where(t => t.id_proyecto == idProyecto && t.activo)
                .ToListAsync();

            if (!transiciones.Any())
                return new PasoWizardProyectoDto { Accion = "Transiciones", NombrePaso = "Paso 3 - Transiciones" };

            var transicionesOperativas = transiciones
                .Where(t => !t.es_anulacion)
                .Select(t => t.id_estado_transicion)
                .ToList();

            var roles = await _context.RolesEstadosTransicion
                .Where(r => transicionesOperativas.Contains(r.id_estado_transicion) && r.activo)
                .ToListAsync();

            if (transicionesOperativas.Any(t => !roles.Any(r => r.id_estado_transicion == t)))
                return new PasoWizardProyectoDto { Accion = "RolesTransicion", NombrePaso = "Paso 4 - Roles por transición" };

            /* (!proyecto.usa_soporte_post_cierre &&
                proyecto.nombre_soporte_post_cierre != null)
            {
                return new PasoWizardProyectoDto
                {
                    Accion = "SoportePostCierre",
                    NombrePaso = "Paso 5 - Soporte posterior al cierre"
                };
            }*/

            var grupo = await _context.GruposAlertas
                .FirstOrDefaultAsync(g => g.id_proyecto == idProyecto && g.activo);

            if (grupo == null)
                return new PasoWizardProyectoDto { Accion = "GrupoAlertas", NombrePaso = "Paso 6 - Grupo de alertas" };

            var alertas = await _context.GruposAlertasDias
                .Where(a => a.id_grupo_alerta == grupo.id_grupo_alerta && a.activo)
                .ToListAsync();

            if (!alertas.Any())
                return new PasoWizardProyectoDto { Accion = "AlertasDias", NombrePaso = "Paso 7 - Reglas de alertas" };

            if (!alertas.Any(a => a.id_dependencia.HasValue))
                return new PasoWizardProyectoDto { Accion = "DependenciasAlertas", NombrePaso = "Paso 8 - Dependencias" };

            var idsAlertas = alertas.Select(a => a.id_grupo_alerta_dia).ToList();

            var tieneEstadosOff = await _context.GruposAlertasDiasEstadosOff
                .AnyAsync(eo => idsAlertas.Contains(eo.id_grupo_alerta_dia));

            if (!tieneEstadosOff)
                return new PasoWizardProyectoDto { Accion = "EstadosOff", NombrePaso = "Paso 9 - Estados off" };

            return new PasoWizardProyectoDto { Accion = "Resumen", NombrePaso = "Paso 10 - Resumen final" };
        }
        private bool EsSuperAdmin()
        {
            return User.HasClaim("EsSuperAdmin", "true");
        }

        public async Task<IActionResult> SoportePostCierre(int id)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == id);

            if (proyecto == null)
                return NotFound();

            var vm = new ProyectoWizardSoportePostCierreVm
            {
                id_proyecto = proyecto.id_proyecto,
                nombre_proyecto = proyecto.nombre,
                usa_soporte_post_cierre = proyecto.usa_soporte_post_cierre,
                nombre_soporte_post_cierre = proyecto.nombre_soporte_post_cierre
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoportePostCierre(ProyectoWizardSoportePostCierreVm vm)
        {
            if (!EsSuperAdmin())
                return RedirectToAction("AccesoDenegado", "Auth");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.id_proyecto == vm.id_proyecto);

            if (proyecto == null)
                return NotFound();

            vm.nombre_proyecto = proyecto.nombre;

            if (vm.usa_soporte_post_cierre &&
                string.IsNullOrWhiteSpace(vm.nombre_soporte_post_cierre))
            {
                ModelState.AddModelError(nameof(vm.nombre_soporte_post_cierre),
                    "Debe indicar el nombre de la opción posterior al cierre.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            proyecto.usa_soporte_post_cierre = vm.usa_soporte_post_cierre;
            proyecto.nombre_soporte_post_cierre = vm.usa_soporte_post_cierre
                ? vm.nombre_soporte_post_cierre?.Trim()
                : null;

            await _context.SaveChangesAsync();

            return RedirectToAction("GrupoAlertas", new { id = vm.id_proyecto });
        }


    }
}