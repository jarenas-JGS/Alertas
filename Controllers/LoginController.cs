using Alertas.Data;
using Alertas.Helpers;
using Alertas.Models;
using Alertas.Services;
using Alertas.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace Alertas.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SeguridadService _seguridadService;

        public LoginController(ApplicationDbContext context, SeguridadService seguridadService)
        {
            _context = context;
            _seguridadService = seguridadService;
        }

        [HttpGet]
        public IActionResult Index(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string usuarioIngresado = model.usuario.Trim();

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.usuario == usuarioIngresado);

            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "Usuario o contraseña inválidos.");
                return View(model);
            }

            if (!usuario.activo)
            {
                ModelState.AddModelError(string.Empty, "El usuario se encuentra inactivo.");
                return View(model);
            }

            var passwordValido = SeguridadHelper.VerificarPassword(model.password, usuario.clave_hash);

            if (!passwordValido)
            {
                ModelState.AddModelError("", "Usuario o contraseña inválidos.");
                return View(model);
            }

            bool esSuperAdmin = usuario.es_super_admin;

            var accesosUsuario = await _seguridadService.ObtenerAccesosProyectoUsuarioAsync(usuario.id_usuario);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.id_usuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.usuario),
                new Claim("NombreCompleto", usuario.nombre ?? string.Empty),
                new Claim(ClaimTypes.Email, usuario.email ?? string.Empty),
                new Claim("EsSuperAdmin", esSuperAdmin.ToString().ToLower())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.recordarme,
                AllowRefresh = true,
                ExpiresUtc = model.recordarme
                    ? DateTimeOffset.UtcNow.AddDays(15)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties
            );

            HttpContext.Session.SetString("id_usuario", usuario.id_usuario.ToString());
            HttpContext.Session.SetString("usuario", usuario.usuario ?? string.Empty);
            HttpContext.Session.SetString("nombre", usuario.nombre ?? string.Empty);
            HttpContext.Session.SetString("email", usuario.email ?? string.Empty);
            HttpContext.Session.SetString("es_super_admin", esSuperAdmin.ToString().ToLower());

            // Limpiar contexto de proyecto activo antes de volver a definirlo
            HttpContext.Session.Remove("id_proyecto_activo");
            HttpContext.Session.Remove("nombre_proyecto_activo");
            HttpContext.Session.Remove("tipo_acceso_proyecto_activo");

            // Si es super admin, no depende de accesos para entrar al sistema
            if (!esSuperAdmin)
            {
                if (accesosUsuario.Count == 1)
                {
                    HttpContext.Session.SetString("id_proyecto_activo", accesosUsuario[0].id_proyecto.ToString());
                    HttpContext.Session.SetString("nombre_proyecto_activo", accesosUsuario[0].nombre_proyecto);
                    string tipoAccesoSesion = string.Equals(accesosUsuario[0].tipo_acceso, "OBLIGACION", StringComparison.OrdinalIgnoreCase)
                        ? "OBLIGACION"
                        : "PROYECTO";

                    HttpContext.Session.SetString("tipo_acceso_proyecto_activo", tipoAccesoSesion);
                    HttpContext.Session.SetString("rol_proyecto_activo", accesosUsuario[0].tipo_acceso);
                }
            }

            usuario.ultimo_login = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (usuario.must_change_password)
            {
                return RedirectToAction("CambiarPassword", "Login");
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SeleccionarProyecto()
        {
            var idUsuario = _seguridadService.ObtenerIdUsuario();

            if (idUsuario == null)
                return RedirectToAction("Index", "Login");

            bool esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");

            List<AccesoProyectoViewModel> accesos;

            if (esSuperAdmin)
            {
                accesos = await _context.Proyectos
                    .Where(p => p.activo && p.configuracion_completa)
                                    .OrderBy(p => p.nombre)
                    .Select(p => new AccesoProyectoViewModel
                    {
                        id_proyecto = p.id_proyecto,
                        nombre_proyecto = p.nombre,
                        tipo_acceso = "SUPER_ADMIN"
                    })
                    .ToListAsync();

                if (accesos.Count == 0)
                    return RedirectToAction("AccessDenied", "Login");

                return View(accesos);
            }

            var accesosUsuario = await _seguridadService.ObtenerAccesosProyectoUsuarioAsync(idUsuario.Value);

            if (accesosUsuario.Count == 0)
                return RedirectToAction("Index", "Home");

            if (accesosUsuario.Count == 1)
            {
                HttpContext.Session.SetString("id_proyecto_activo", accesosUsuario[0].id_proyecto.ToString());
                HttpContext.Session.SetString("nombre_proyecto_activo", accesosUsuario[0].nombre_proyecto);
                string tipoAccesoSesion = string.Equals(accesosUsuario[0].tipo_acceso, "OBLIGACION", StringComparison.OrdinalIgnoreCase)
                    ? "OBLIGACION"
                    : "PROYECTO";

                HttpContext.Session.SetString("tipo_acceso_proyecto_activo", tipoAccesoSesion);
                HttpContext.Session.SetString("rol_proyecto_activo", accesosUsuario[0].tipo_acceso);

                return RedirectToAction("Index", "RegObl");
            }

            accesos = accesosUsuario
                .Select(a => new AccesoProyectoViewModel
                {
                    id_proyecto = a.id_proyecto,
                    nombre_proyecto = a.nombre_proyecto,
                    tipo_acceso = a.tipo_acceso
                })
                .ToList();

            return View(accesos);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeleccionarProyecto(int idProyecto, string tipoAcceso)
        {
            bool esSuperAdmin = User.HasClaim("EsSuperAdmin", "true");

            if (!esSuperAdmin)
            {
                if (!await _seguridadService.UsuarioTieneAccesoProyectoAsync(idProyecto, tipoAcceso))
                    return RedirectToAction("AccessDenied", "Login");
            }

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p =>
                    p.id_proyecto == idProyecto &&
                    p.activo);

            if (proyecto == null)
                return RedirectToAction("AccessDenied", "Login");

            if (!proyecto.configuracion_completa)
            {
                TempData["Error"] = "Este proyecto aún no está completamente configurado.";
                return RedirectToAction("SeleccionarProyecto", "Login");
            }

            if (proyecto == null)
                return RedirectToAction("AccessDenied", "Login");

            HttpContext.Session.SetString("id_proyecto_activo", proyecto.id_proyecto.ToString());
            HttpContext.Session.SetString("nombre_proyecto_activo", proyecto.nombre);
            string tipoAccesoSesion = esSuperAdmin
                ? "SUPER_ADMIN"
                : string.Equals(tipoAcceso, "OBLIGACION", StringComparison.OrdinalIgnoreCase)
                    ? "OBLIGACION"
                    : "PROYECTO";

            HttpContext.Session.SetString("tipo_acceso_proyecto_activo", tipoAccesoSesion);
            HttpContext.Session.SetString("rol_proyecto_activo", tipoAcceso);

            return RedirectToAction("Index", "RegObl");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult CambiarPassword()
        {
            return View(new CambiarPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return RedirectToAction("Index");

            int idUsuario = int.Parse(userIdClaim.Value);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.id_usuario == idUsuario);

            if (usuario == null)
                return RedirectToAction("Index");

            // Validar clave actual
            var claveValida = SeguridadHelper.VerificarPassword(model.ClaveActual, usuario.clave_hash);

            if (!claveValida)
            {
                ModelState.AddModelError(nameof(model.ClaveActual), "La clave actual no es válida.");
                return View(model);
            }

            // Validar que la nueva clave sea diferente
            var mismaClave = SeguridadHelper.VerificarPassword(model.NuevaClave, usuario.clave_hash);

            if (mismaClave)
            {
                ModelState.AddModelError(nameof(model.NuevaClave),
                    "La nueva clave debe ser diferente a la actual.");

                return View(model);
            }

            // Actualizar password
            usuario.clave_hash = SeguridadHelper.HashPassword(model.NuevaClave);

            usuario.must_change_password = false;

            usuario.last_password_change_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "La contraseña fue actualizada correctamente.";

            return RedirectToAction("Index");
        }
    }
}