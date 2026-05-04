using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.Aplicacion.Helpers;

namespace Vittal.Aplicacion.Areas.Administracion.Controllers
{
    [Area("Administracion")]
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly ApiClientHelper _apiClient;
        private readonly ILogger<PerfilController> _logger;

        public PerfilController(ApiClientHelper apiClient, ILogger<PerfilController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (success, response, _) = await _apiClient.GetAsync<dynamic>("api/Perfiles");

            if (!success)
            {
                _logger.LogWarning("No se pudieron cargar los perfiles desde el API");
                ViewBag.Perfiles = new List<object>();
            }
            else
            {
                ViewBag.Perfiles = response?.Data ?? new List<object>();
            }

            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var (success, response, _) = await _apiClient.GetAsync<dynamic>($"api/Perfiles/{id}");

            if (!success || response?.Data == null)
            {
                TempData["Error"] = "Perfil no encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.Perfil = response!.Data;
            return View();
        }
    }
}
