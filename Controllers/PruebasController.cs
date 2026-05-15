using Alertas.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Alertas.Controllers
{
    public class PruebasController : Controller
    {
        [HttpGet]
        public IActionResult GenerarHash()
        {
            var hasher = new PasswordHasher<Usuario>();

            var usuario = new Usuario
            {
                usuario = "jarenas",
                nombre = "John Jairo Arenas",
                email = "jarenas@jgs.com.co"
            };

            string hash = hasher.HashPassword(usuario, "Omraam2018");

            return Content(hash);
        }
    }
}