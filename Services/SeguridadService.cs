using System.Security.Claims;
using Alertas.Data;
using Microsoft.EntityFrameworkCore;
using Alertas.ViewModels;

namespace Alertas.Services
{
    public class SeguridadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SeguridadService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public int? ObtenerIdUsuario()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                return null;

            return int.TryParse(claim.Value, out int idUsuario)
                ? idUsuario
                : null;
        }

        public string? ObtenerLogin()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }

        public string? ObtenerNombreCompleto()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("NombreCompleto")?.Value;
        }

        public string? ObtenerEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
        }

        public int? ObtenerIdProyectoActivo()
        {
            var valor = _httpContextAccessor.HttpContext?.Session.GetString("id_proyecto_activo");

            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return int.TryParse(valor, out int idProyecto) ? idProyecto : null;
        }

        public string? ObtenerNombreProyectoActivo()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("nombre_proyecto_activo");
        }

        public async Task<List<ProyectoAccesoViewModel>> ObtenerAccesosProyectoUsuarioAsync(int idUsuario)
        {
            var accesosPorProyecto = await _context.UsuariosProyectos
                .Where(up => up.id_usuario == idUsuario && up.activo)
                .Join(_context.Proyectos,
                    up => up.id_proyecto,
                    p => p.id_proyecto,
                    (up, p) => new { up, p })
                .Where(x => x.p.activo && x.p.configuracion_completa)
                .Join(_context.Roles,
                    x => x.up.id_rol,
                    r => r.id_rol,
                    (x, r) => new
                    {
                        x.p.id_proyecto,
                        nombre_proyecto = x.p.nombre,
                        proyecto_activo = x.p.activo,
                        nombre_rol = r.nombre
                    })
                .Select(x => new ProyectoAccesoViewModel
                {
                    id_proyecto = x.id_proyecto,
                    nombre_proyecto = x.nombre_proyecto,
                    tipo_acceso = x.nombre_rol,
                    descripcion_acceso = $"{x.nombre_proyecto} - {x.nombre_rol}"
                })
                .Distinct()
                .OrderBy(x => x.nombre_proyecto)
                .ToListAsync();

            var accesosPorObligacion = await _context.UsuariosObligaciones
                .Where(uo => uo.id_usuario == idUsuario && uo.activo)
                .Join(_context.RegObls,
                    uo => uo.id_reg_obl,
                    ro => ro.id_reg_obl,
                    (uo, ro) => new { ro.id_proyecto })
                .Join(_context.Proyectos,
                    x => x.id_proyecto,
                    p => p.id_proyecto,
                    (x, p) => new { p.id_proyecto, p.nombre, p.activo, p.configuracion_completa })
                .Where(p => p.activo && p.configuracion_completa == true)

                .Distinct()
                .Select(x => new ProyectoAccesoViewModel
                {
                    id_proyecto = x.id_proyecto,
                    nombre_proyecto = x.nombre,
                    tipo_acceso = "OBLIGACION",
                    descripcion_acceso = $"{x.nombre} - acceso por obligación"
                })
                .OrderBy(x => x.nombre_proyecto)
                .ToListAsync();

            return accesosPorProyecto
                .Concat(accesosPorObligacion)
                .OrderBy(x => x.nombre_proyecto)
                .ThenBy(x => x.tipo_acceso)
                .ToList();
        }


        public async Task<List<string>> ObtenerRolesEnProyectoActivoAsync()
        {
            var idUsuario = ObtenerIdUsuario();
            var idProyecto = ObtenerIdProyectoActivo();

            if (idUsuario == null || idProyecto == null)
                return new List<string>();

            return await _context.UsuariosProyectos
                .Where(up => up.id_usuario == idUsuario
                          && up.id_proyecto == idProyecto
                          && up.activo)
                .Join(_context.Roles,
                    up => up.id_rol,
                    r => r.id_rol,
                    (up, r) => r.nombre)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> UsuarioParticipaEnObligacionAsync(int idRegObl)
        {
            var idUsuario = ObtenerIdUsuario();

            if (idUsuario == null)
                return false;

            return await _context.UsuariosObligaciones
                .AnyAsync(uo => uo.id_usuario == idUsuario
                             && uo.id_reg_obl == idRegObl
                             && uo.activo);
        }

        public async Task<List<string>> ObtenerRolesEnObligacionAsync(int idRegObl)
        {
            var idUsuario = ObtenerIdUsuario();

            if (idUsuario == null)
                return new List<string>();

            return await _context.UsuariosObligaciones
                .Where(uo => uo.id_usuario == idUsuario
                          && uo.id_reg_obl == idRegObl
                          && uo.activo)
                .Join(_context.Roles,
                    uo => uo.id_rol,
                    r => r.id_rol,
                    (uo, r) => r.nombre)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> UsuarioTieneRolEnObligacionAsync(int idRegObl, string nombreRol)
        {
            var idUsuario = ObtenerIdUsuario();

            if (idUsuario == null)
                return false;

            return await _context.UsuariosObligaciones
                .Where(uo => uo.id_usuario == idUsuario
                          && uo.id_reg_obl == idRegObl
                          && uo.activo)
                .Join(_context.Roles,
                    uo => uo.id_rol,
                    r => r.id_rol,
                    (uo, r) => r.nombre)
                .AnyAsync(r => r == nombreRol);
        }


        public bool EsSuperAdmin()
        {
            var valor = _httpContextAccessor.HttpContext?.User?.HasClaim("EsSuperAdmin", "true") ?? false;
            return valor;
        }

        public async Task<List<string>> ObtenerRolesObligacionEnProyectoActivoAsync()
        {
            var idUsuario = ObtenerIdUsuario();
            var idProyecto = ObtenerIdProyectoActivo();

            if (idUsuario == null || idProyecto == null)
                return new List<string>();

            return await _context.UsuariosObligaciones
                .Where(uo => uo.id_usuario == idUsuario && uo.activo)
                .Join(_context.RegObls,
                    uo => uo.id_reg_obl,
                    ro => ro.id_reg_obl,
                    (uo, ro) => new { uo, ro })
                .Where(x => x.ro.id_proyecto == idProyecto)
                .Join(_context.Roles,
                    x => x.uo.id_rol,
                    r => r.id_rol,
                    (x, r) => r.nombre)
                .Distinct()
                .ToListAsync();
        }

        public string? ObtenerTipoAccesoProyectoActivo()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("tipo_acceso_proyecto_activo");
        }

        public bool EsAccesoProyectoActivoPorProyecto()
        {
            return string.Equals(
                ObtenerTipoAccesoProyectoActivo(),
                "PROYECTO",
                StringComparison.OrdinalIgnoreCase);
        }

        public bool EsAccesoProyectoActivoPorObligacion()
        {
            return string.Equals(
                ObtenerTipoAccesoProyectoActivo(),
                "OBLIGACION",
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> UsuarioTieneAccesoProyectoAsync(int idProyecto, string tipoAcceso)
        {
            var idUsuario = ObtenerIdUsuario();

            if (idUsuario == null)
                return false;

            if (string.Equals(tipoAcceso, "PROYECTO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tipoAcceso, "Consulta Proyecto", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tipoAcceso, "Administrador Proyecto", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.UsuariosProyectos
                    .AnyAsync(up => up.id_usuario == idUsuario
                                 && up.id_proyecto == idProyecto
                                 && up.activo);
            }

            if (string.Equals(tipoAcceso, "OBLIGACION", StringComparison.OrdinalIgnoreCase))
            {
                return await _context.UsuariosObligaciones
                    .Where(uo => uo.id_usuario == idUsuario && uo.activo)
                    .Join(_context.RegObls,
                        uo => uo.id_reg_obl,
                        ro => ro.id_reg_obl,
                        (uo, ro) => ro)
                    .AnyAsync(ro => ro.id_proyecto == idProyecto);
            }

            return false;
        }

        public bool EsAdministradorProyectoActivo()
        {
            return string.Equals(
                ObtenerRolProyectoActivo(),
                "Administrador Proyecto",
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> UsuarioPuedeCrearObligacionesEnProyectoAsync(int idProyecto)
        {
            if (EsSuperAdmin())
                return true;

            var idUsuario = ObtenerIdUsuario();

            if (idUsuario == null)
                return false;

            return await _context.UsuariosProyectos
                .Where(up => up.id_usuario == idUsuario
                          && up.id_proyecto == idProyecto
                          && up.activo)
                .Join(_context.Roles,
                    up => up.id_rol,
                    r => r.id_rol,
                    (up, r) => r.nombre)
                .AnyAsync(nombreRol =>
                    nombreRol == "Administrador Proyecto");
        }

        public string? ObtenerRolProyectoActivo()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("rol_proyecto_activo");
        }

        public async Task<bool> UsuarioPuedeVerRutinasActualizacionAsync()
        {
            if (EsSuperAdmin())
                return true;

            var idUsuario = ObtenerIdUsuario();

            if (idUsuario == null)
                return false;

            return await _context.UsuariosProyectos
                .Where(up => up.id_usuario == idUsuario && up.activo)
                .Join(_context.Roles,
                    up => up.id_rol,
                    r => r.id_rol,
                    (up, r) => r.nombre)
                .AnyAsync(nombreRol => nombreRol == "Administrador Proyecto");
        }

        public async Task<List<ProyectoAccesoViewModel>> ObtenerProyectosAdministrablesAsync()
        {
            var idUsuario = ObtenerIdUsuario();

            if (EsSuperAdmin())
            {
                return await _context.Proyectos
                    .Where(p => p.activo && p.configuracion_completa)
                    .OrderBy(p => p.nombre)
                    .Select(p => new ProyectoAccesoViewModel
                    {
                        id_proyecto = p.id_proyecto,
                        nombre_proyecto = p.nombre,
                        tipo_acceso = "SUPERADMIN",
                        descripcion_acceso = p.nombre
                    })
                    .ToListAsync();
            }

            if (idUsuario == null)
                return new List<ProyectoAccesoViewModel>();

            return await _context.UsuariosProyectos
                .Where(up => up.id_usuario == idUsuario && up.activo)
                .Join(_context.Roles,
                    up => up.id_rol,
                    r => r.id_rol,
                    (up, r) => new { up, r })
                .Where(x => x.r.nombre == "Administrador Proyecto")
                .Join(_context.Proyectos,
                    x => x.up.id_proyecto,
                    p => p.id_proyecto,
                    (x, p) => p)
                .Where(p => p.activo && p.configuracion_completa)
                .OrderBy(p => p.nombre)
                .Select(p => new ProyectoAccesoViewModel
                {
                    id_proyecto = p.id_proyecto,
                    nombre_proyecto = p.nombre,
                    tipo_acceso = "Administrador Proyecto",
                    descripcion_acceso = p.nombre
                })
                .Distinct()
                .ToListAsync();
        }


        public async Task<bool> UsuarioPuedeAdministrarProyectoAsync(int idProyecto)
        {
            if (EsSuperAdmin())
                return true;

            var idUsuario = ObtenerIdUsuario();

            if (idUsuario == null)
                return false;

            return await _context.UsuariosProyectos
                .Where(up => up.id_usuario == idUsuario
                          && up.id_proyecto == idProyecto
                          && up.activo)
                .Join(_context.Roles,
                    up => up.id_rol,
                    r => r.id_rol,
                    (up, r) => r.nombre)
                .AnyAsync(nombreRol => nombreRol == "Administrador Proyecto");
        }


    }
}