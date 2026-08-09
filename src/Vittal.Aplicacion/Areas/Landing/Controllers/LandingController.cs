using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Vittal.DTO.ContactoLanding;

namespace Vittal.Aplicacion.Areas.Landing.Controllers;

/// <summary>
/// Controller de Landing Page — público, sin autenticación.
/// Consume la API de ContactoLanding para el formulario de contacto.
/// Historia de Usuario: HU-L01 — Landing Page Informativa
/// </summary>
[Area("Landing")]
public class LandingController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LandingController> _logger;

    public LandingController(
        IHttpClientFactory httpClientFactory,
        ILogger<LandingController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Página principal de la landing.
    /// </summary>
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/home");
        }

        ViewData["Title"] = "Software de Gestión Clínica Inteligente";
        ViewData["MetaDescription"] = "Vittal es el software de gestión clínica que centraliza citas, expedientes, diagnósticos y más. Diseñado para clínicas médicas modernas.";
        ViewData["MetaKeywords"] = "gestión clínica, software médico, expedientes electrónicos, agenda médica, citas online";
        return View();
    }

    /// <summary>
    /// Sección de funcionalidades del sistema.
    /// </summary>
    public IActionResult Funcionalidades()
    {
        ViewData["Title"] = "Funcionalidades - Software Vittal";
        ViewData["MetaDescription"] = "Conoce todas las funcionalidades de Vittal: expedientes, agenda, cola de espera, diagnósticos, cirugías, reportes y más.";
        return View();
    }

    /// <summary>
    /// Sección de beneficios por rol.
    /// </summary>
    public IActionResult Beneficios()
    {
        ViewData["Title"] = "Beneficios - Software Vittal";
        ViewData["MetaDescription"] = "Descubre cómo Vittal beneficia a cada rol de tu clínica: directores, gerentes, doctores y recepcionistas.";
        return View();
    }

    /// <summary>
    /// Formulario de contacto — GET.
    /// </summary>
    [HttpGet]
    public IActionResult Contacto()
    {
        ViewData["Title"] = "Contacto - Software Vittal";
        ViewData["MetaDescription"] = "Contáctanos para conocer más sobre Vittal. Solicita una demo o información personalizada para tu clínica.";
        return View();
    }

    /// <summary>
    /// Formulario de contacto — POST.
    /// Consume la API ContactoLanding para registrar el contacto.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contacto(ContactoLandingRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("VittalApi");
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Reintento ante cold start de la API en Render free tier
            // (HttpClient.Timeout puede vencer mientras la API "despierta")
            HttpResponseMessage? response = null;
            var maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    response = await client.PostAsync("/api/ContactoLanding", content);
                    break;
                }
                catch (TaskCanceledException) when (attempt < maxAttempts)
                {
                    _logger.LogWarning(
                        "Timeout al contactar API (intento {Attempt}/{Max}): cold start en Render",
                        attempt, maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(3 * attempt));
                }
            }

            if (response is null)
            {
                throw new HttpRequestException("No se obtuvo respuesta de la API de contacto.");
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "API retornó error {StatusCode} al procesar contacto desde {Email}: {Response}",
                    response.StatusCode, dto.Email, responseBody);

                ModelState.AddModelError(string.Empty, "Error al enviar el formulario. Intente nuevamente.");
                return View(dto);
            }

            _logger.LogInformation(
                "Formulario de contacto enviado exitosamente desde {Email} (Rol: {Rol})",
                dto.Email, dto.Rol);

            TempData["Success"] = "¡Mensaje enviado con éxito! Nos pondremos en contacto pronto.";
            return RedirectToAction(nameof(ContactoEnviado));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Excepción al consumir API de contacto para {Email}",
                dto.Email);

            ModelState.AddModelError(string.Empty, "Error de conexión. Intente nuevamente.");
            return View(dto);
        }
    }

    /// <summary>
    /// Página de confirmación después del envío exitoso del formulario.
    /// </summary>
    public IActionResult ContactoEnviado()
    {
        ViewData["Title"] = "Mensaje Enviado - Software Vittal";
        ViewData["MetaDescription"] = "Gracias por contactarnos. Hemos recibido tu mensaje y te responderemos pronto.";
        return View();
    }
}
