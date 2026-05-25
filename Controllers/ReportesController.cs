using Alertas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alertas.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly SeguridadService _seguridadService;

        public ReportesController(SeguridadService seguridadService)
        {
            _seguridadService = seguridadService;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}