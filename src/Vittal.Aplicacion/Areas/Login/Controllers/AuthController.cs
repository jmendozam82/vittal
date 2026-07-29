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

        public AuthController(
            ApiClientHelper apiClient,
            ILogger<AuthController> logger)
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
                        new Claim("app_perfil_id", user.PerfilId.ToString()),
                        new Claim("app_clinica_id", user.ClinicaId.ToString()),
                        new Claim("app_clinica_nombre", user.ClinicaNombre),
                        new Claim("app_es_admin", user.EsAdmin.ToString().ToLower()),
                        new Claim("app_es_super_admin", user.EsSuperAdmin.ToString().ToLower())
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

                    // Guardar JWT en cookie HttpOnly separada (evita limite de 4KB de la cookie de auth)
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false, // Development; en Production usar CookieSecurePolicy.Always
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddHours(8),
                        Path = "/"
                    };
                    Response.Cookies.Append("vittal_jwt", user.AccessToken, cookieOptions);

                    // Guardar en Session para que _Layout.cshtml pueda acceder (token + clinica_id)
                    HttpContext.Session.SetString("AccessToken", user.AccessToken);
                    HttpContext.Session.SetString("ClinicaId", user.ClinicaId.ToString());

                    _logger.LogInformation("Usuario {Email} inició sesión correctamente. JWT guardado en cookie y session.", user.Email);

                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                var errorMsg = errorMessage ?? response?.Message ?? "Credenciales inválidas.";
                _logger.LogWarning("Login fallido para {Email}: {Error}", model.Email, errorMsg);
                ModelState.AddModelError(string.Empty, errorMsg);
                ViewData["LoginError"] = errorMsg;
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante el login para {Email}", model.Email);
                var errMsg = "Ocurrió un error inesperado. Intente nuevamente.";
                ModelState.AddModelError(string.Empty, errMsg);
                ViewData["LoginError"] = errMsg;
                return View(model);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Limpiar cookie del JWT
            Response.Cookies.Delete("vittal_jwt");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ────────────────────────────────────────────────────────────────────
        // Forgot Password — Notifica al admin de la clínica
        // ────────────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                _logger.LogInformation("Solicitud de recuperación de contraseña para email: {Email}", model.Email);

                // Llamar a la API (no al BLL directamente) para mantener la arquitectura N-capas
                var (success, response, errorMessage) = await _apiClient.PostAnonymousAsync<ApiResponse>(
                    "api/Auth/forgot-password",
                    new { email = model.Email });

                if (success && response != null)
                {
                    _logger.LogInformation("API ForgotPassword respondió exitosamente para {Email}: {Message}",
                        model.Email, response.Message);
                }
                else
                {
                    _logger.LogWarning("API ForgotPassword responded with {Error} for {Email}",
                        errorMessage ?? response?.Message ?? "unknown error", model.Email);
                }

                // Siempre mostrar confirmación (por seguridad, no revelar si el email existe)
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en ForgotPassword para {Email}", model.Email);
                // Redirigir a confirmación igualmente (no revelar errores)
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

    }
}
