using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vittal.Aplicacion.Helpers;
using Vittal.Aplicacion.Models;
using Vittal.Aplicacion.Models.ViewModels;
using Vittal.DTO.Auth;

namespace Vittal.Aplicacion.Areas.Login.Controllers
{
    [Area("Login")]
    public class AuthController : Controller
    {
        private readonly ApiClientHelper _apiClient;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApiClientHelper apiClient, ILogger<AuthController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            try
            {
                var formKeys = string.Join(", ", Request.Form.Keys);
                _logger.LogInformation("POST Login recibido. Form keys: [{Keys}], ContentType: {ContentType}, Method: {Method}, ContentLength: {Length}, Model.Email: {Email}, Model.Password null? {IsNull}",
                    formKeys, Request.ContentType, Request.Method, Request.ContentLength, model.Email, model.Password == null);
                _logger.LogInformation("POST Login recibido. ModelState.IsValid: {IsValid}, Model.Email: {Email}, Model.Password: {Password}",
                    ModelState.IsValid, model.Email, model.Password != null ? $"***{model.Password.Length} chars" : "NULL");

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    _logger.LogWarning("ModelState inválido: {Errors}", errors);
                    return View(model);
                }

                var loginRequest = new LoginRequestDto
                {
                    Email = model.Email ?? string.Empty,
                    Password = model.Password ?? string.Empty
                };

                _logger.LogDebug("Enviando request a API: api/Auth/login");
                var (success, response, errorMessage) = await _apiClient.PostAnonymousAsync<ApiResponse<LoginResponseDto>>("api/Auth/login", loginRequest);

                _logger.LogInformation("Respuesta API - Success: {Success}, Response: {ResponseSuccess}, Error: {Error}",
                    success, response?.Success, errorMessage);

                if (success && response != null && response.Success && response.Data != null)
                {
                    var user = response.Data;

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),
                        new Claim(ClaimTypes.Name, $"{user.Nombres} {user.Apellidos}"),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("clinica_id", user.ClinicaId.ToString()),
                        new Claim(ClaimTypes.Role, user.Perfil),
                        new Claim("access_token", user.AccessToken),
                        new Claim("refresh_token", user.RefreshToken)
                    };

                    if (user.EsAdmin)
                    {
                        claims.Add(new Claim("IsAdmin", "true"));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("Usuario {Email} inició sesión correctamente.", user.Email);

                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                var errorMsg = errorMessage ?? response?.Message ?? "Credenciales inválidas.";
                _logger.LogWarning("Login fallido para {Email}: {Error}", model.Email, errorMsg);
                ModelState.AddModelError(string.Empty, errorMsg);
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante el login para {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intente nuevamente.");
                return View(model);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
