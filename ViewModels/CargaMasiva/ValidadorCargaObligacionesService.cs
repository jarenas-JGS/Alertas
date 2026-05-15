using Alertas.Data;
using Alertas.ViewModels.CargaMasiva;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Services.CargaMasiva
{
    public class ValidadorCargaObligacionesService : IValidadorCargaObligacionesService
    {
        private readonly ApplicationDbContext _context;

        public ValidadorCargaObligacionesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CargaObligacionesErrorViewModel>> ValidarAsync(
            int idProyecto,
            List<CargaObligacionesFilaViewModel> filas)
        {
            var errores = new List<CargaObligacionesErrorViewModel>();

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p =>
                    p.id_proyecto == idProyecto &&
                    p.activo &&
                    p.configuracion_completa);

            if (proyecto == null)
            {
                errores.Add(new CargaObligacionesErrorViewModel
                {
                    numero_fila = 0,
                    columna = "Proyecto",
                    mensaje = "El proyecto no existe, no está activo o no está completamente configurado."
                });

                return errores;
            }

            var idArea = proyecto.id_area;

            var clientesValidos = await _context.AreasEmpresas
                .Where(ae => ae.id_area == idArea && ae.activo)
                .Join(_context.Empresas,
                    ae => ae.id_empresa,
                    e => e.id_empresa,
                    (ae, e) => e)
                .Where(e => e.activo)
                .Join(_context.Clientes,
                    e => e.id_cliente,
                    c => c.id_cliente,
                    (e, c) => c)
                .Where(c => c.activo)
                .Select(c => c.nit + " - " + c.nombre)
                .Distinct()
                .ToListAsync();

            var empresasValidas = await _context.AreasEmpresas
                .Where(ae => ae.id_area == idArea && ae.activo)
                .Join(_context.Empresas,
                    ae => ae.id_empresa,
                    e => e.id_empresa,
                    (ae, e) => e)
                .Where(e => e.activo)
                .Select(e => e.nit + " - " + e.nombre)
                .Distinct()
                .ToListAsync();

            var tiposValidos = await _context.TipoObligaciones
                .Where(t => t.id_area == idArea && t.activo)
                .Select(t => t.nombre)
                .ToListAsync();

            var ciudadesValidas = await _context.Ciudades
                .Select(c => c.nombre)
                .ToListAsync();

            var periodosValidos = await _context.Periodos
                .Select(p => p.nombre)
                .ToListAsync();

            var dominiosValidos = await _context.Dominios
                .Select(d => d.nombre)
                .ToListAsync();

            var usuariosValidos = await _context.UsuarioArea
                .Where(ua => ua.id_area == idArea && ua.activo)
                .Join(_context.Usuarios,
                    ua => ua.id_usuario,
                    u => u.id_usuario,
                    (ua, u) => u)
                .Where(u => u.activo)
                .Select(u => u.email + " - " + u.nombre)
                .Distinct()
                .ToListAsync();

            var relacionesClienteEmpresa = await _context.AreasEmpresas
                .Where(ae => ae.id_area == idArea && ae.activo)
                .Join(_context.Empresas,
                    ae => ae.id_empresa,
                    e => e.id_empresa,
                    (ae, e) => e)
                .Where(e => e.activo)
                .Join(_context.Clientes,
                    e => e.id_cliente,
                    c => c.id_cliente,
                    (e, c) => new
                    {
                        ClienteTexto = c.nit + " - " + c.nombre,
                        EmpresaTexto = e.nit + " - " + e.nombre
                    })
                .ToListAsync();

            var empresasPorTexto = await _context.AreasEmpresas
                .Where(ae => ae.id_area == idArea && ae.activo)
                .Join(_context.Empresas,
                    ae => ae.id_empresa,
                    e => e.id_empresa,
                    (ae, e) => e)
                .Where(e => e.activo)
                .Select(e => new
                {
                    Texto = e.nit + " - " + e.nombre,
                    e.id_empresa
                })
                .Distinct()
                .ToDictionaryAsync(
                    x => x.Texto,
                    x => x.id_empresa,
                    StringComparer.OrdinalIgnoreCase);

            foreach (var fila in filas)
            {
                ValidarObligatorio(fila, errores, fila.nombre, "Nombre");
                ValidarObligatorio(fila, errores, fila.cliente, "Cliente");
                ValidarObligatorio(fila, errores, fila.empresa, "Empresa");
                ValidarObligatorio(fila, errores, fila.ciudad, "Ciudad");
                ValidarObligatorio(fila, errores, fila.periodo, "Periodo");
                ValidarObligatorio(fila, errores, fila.dominio, "Dominio");
                ValidarObligatorio(fila, errores, fila.tipo_obligacion, "TipoObligacion");

                ValidarObligatorio(fila, errores, fila.responsable, "Responsable");
                ValidarObligatorio(fila, errores, fila.elaborador, "Elaborador");
                ValidarObligatorio(fila, errores, fila.autorizador, "Autorizador");
                ValidarObligatorio(fila, errores, fila.aprobador, "Aprobador");
                ValidarObligatorio(fila, errores, fila.usuario_vencimiento, "UsuarioVencimiento");

                if (fila.fecha_vencimiento_obligacion == null)
                    AgregarError(fila, errores, "FechaVencimientoObligacion", "La fecha de vencimiento de la obligación es obligatoria o no tiene un formato válido.");

                if (fila.fecha_vencimiento_seguimiento == null)
                    AgregarError(fila, errores, "FechaVencimientoSeguimiento", "La fecha de vencimiento de seguimiento es obligatoria o no tiene un formato válido.");

                if (fila.fecha_vencimiento_obligacion != null &&
                    fila.fecha_vencimiento_seguimiento != null &&
                    fila.fecha_vencimiento_seguimiento > fila.fecha_vencimiento_obligacion)
                {
                    AgregarError(fila, errores, "FechaVencimientoSeguimiento", "La fecha de seguimiento no puede ser mayor que la fecha de vencimiento de la obligación.");
                }

                if (!string.IsNullOrWhiteSpace(fila.cliente) &&
                    !string.IsNullOrWhiteSpace(fila.empresa))
                {
                    var relacionValida = relacionesClienteEmpresa.Any(x =>
                        string.Equals(x.ClienteTexto, fila.cliente, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.EmpresaTexto, fila.empresa, StringComparison.OrdinalIgnoreCase));

                    if (!relacionValida)
                    {
                        AgregarError(
                            fila,
                            errores,
                            "Empresa",
                            "La empresa seleccionada no pertenece al cliente indicado.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(fila.nombre) &&
                    !string.IsNullOrWhiteSpace(fila.empresa) &&
                    fila.fecha_vencimiento_obligacion != null &&
                    empresasPorTexto.TryGetValue(fila.empresa, out var idEmpresaDuplicado))
                {
                    var fechaVencObl = DateOnly.FromDateTime(fila.fecha_vencimiento_obligacion.Value);

                    var existeDuplicado = await _context.RegObls.AnyAsync(ro =>
                        ro.id_proyecto == idProyecto &&
                        ro.id_empresa == idEmpresaDuplicado &&
                        ro.nombre.ToUpper() == fila.nombre.Trim().ToUpper() &&
                        ro.fecha_venc_obl == fechaVencObl);

                    if (existeDuplicado)
                    {
                        AgregarError(
                            fila,
                            errores,
                            "Duplicado",
                            "Ya existe una obligación registrada con el mismo Proyecto, Nombre, Empresa y FechaVencimientoObligacion.");
                    }
                }

                if (fila.vigencia != null && (fila.vigencia < 2000 || fila.vigencia > 2100))
                {
                    AgregarError(fila, errores, "Vigencia", "La vigencia debe estar entre 2000 y 2100.");
                }

                if (!string.IsNullOrWhiteSpace(fila.nombre) &&
                     fila.nombre.Length > 200)
                {
                    AgregarError(
                        fila,
                        errores,
                        "Nombre",
                        "El nombre no puede superar los 200 caracteres.");
                }

                ValidarEnLista(fila, errores, fila.cliente, "Cliente", clientesValidos);
                ValidarEnLista(fila, errores, fila.empresa, "Empresa", empresasValidas);
                ValidarEnLista(fila, errores, fila.ciudad, "Ciudad", ciudadesValidas);
                ValidarEnLista(fila, errores, fila.periodo, "Periodo", periodosValidos);
                ValidarEnLista(fila, errores, fila.dominio, "Dominio", dominiosValidos);
                ValidarEnLista(fila, errores, fila.tipo_obligacion, "TipoObligacion", tiposValidos);

                ValidarUsuarios(fila, errores, fila.responsable, "Responsable", usuariosValidos);
                ValidarUsuarios(fila, errores, fila.elaborador, "Elaborador", usuariosValidos);
                ValidarUsuarios(fila, errores, fila.autorizador, "Autorizador", usuariosValidos);
                ValidarUsuarios(fila, errores, fila.aprobador, "Aprobador", usuariosValidos);
                ValidarUsuarios(fila, errores, fila.usuario_vencimiento, "UsuarioVencimiento", usuariosValidos);
            }

            var duplicadosExcel = filas
                .Where(f =>
                    !string.IsNullOrWhiteSpace(f.nombre) &&
                    !string.IsNullOrWhiteSpace(f.empresa) &&
                    f.fecha_vencimiento_obligacion != null)
                .GroupBy(f => new
                {
                    Nombre = f.nombre!.Trim().ToUpper(),
                    Empresa = f.empresa!.Trim().ToUpper(),
                    Fecha = f.fecha_vencimiento_obligacion!.Value.Date
                })
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var grupo in duplicadosExcel)
            {
                foreach (var filaDuplicada in grupo)
                {
                    AgregarError(
                        filaDuplicada,
                        errores,
                        "Duplicado",
                        "Existe otra fila en el archivo con el mismo Proyecto, Nombre, Empresa y FechaVencimientoObligacion.");
                }
            }

            return errores;
        }

        private static void ValidarObligatorio(
            CargaObligacionesFilaViewModel fila,
            List<CargaObligacionesErrorViewModel> errores,
            string? valor,
            string columna)
        {
            if (string.IsNullOrWhiteSpace(valor))
                AgregarError(fila, errores, columna, "El campo es obligatorio.");
        }

        private static void ValidarEnLista(
            CargaObligacionesFilaViewModel fila,
            List<CargaObligacionesErrorViewModel> errores,
            string? valor,
            string columna,
            List<string> valoresValidos)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            if (!valoresValidos.Contains(valor, StringComparer.OrdinalIgnoreCase))
                AgregarError(fila, errores, columna, "El valor no existe o no está permitido para el proyecto.");
        }

        private static void ValidarUsuarios(
            CargaObligacionesFilaViewModel fila,
            List<CargaObligacionesErrorViewModel> errores,
            string? valor,
            string columna,
            List<string> usuariosValidos)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            var usuarios = SepararUsuarios(valor);

            if (!usuarios.Any())
            {
                AgregarError(fila, errores, columna, "Debe indicar al menos un usuario.");
                return;
            }

            foreach (var usuario in usuarios)
            {
                if (!usuariosValidos.Contains(usuario, StringComparer.OrdinalIgnoreCase))
                {
                    AgregarError(fila, errores, columna, $"El usuario '{usuario}' no existe, está inactivo o no pertenece al área del proyecto.");
                }
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

        private static void AgregarError(
            CargaObligacionesFilaViewModel fila,
            List<CargaObligacionesErrorViewModel> errores,
            string columna,
            string mensaje)
        {
            var error = new CargaObligacionesErrorViewModel
            {
                numero_fila = fila.numero_fila,
                columna = columna,
                mensaje = mensaje
            };

            fila.errores.Add(error);
            errores.Add(error);
        }
    }
}