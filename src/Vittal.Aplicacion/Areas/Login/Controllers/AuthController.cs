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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var loginRequest = new LoginRequestDto
            {
                Email = model.Email,
                Password = model.Password
            };

            var (success, response, errorMessage) = await _apiClient.PostAnonymousAsync<ApiResponse<LoginResponseDto>>("api/Auth/login", loginRequest);

            if (success && response != null && response.IsSuccess && response.Data != null)
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

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home", new { area = "" });
            }

            ModelState.AddModelError(string.Empty, errorMessage ?? response?.Message ?? "Credenciales inválidas.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
