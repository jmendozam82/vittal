using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Vittal.API.Models;
using Vittal.BLL.Services;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Auth;
using Vittal.Utility;

namespace Vittal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IUsuarioService _usuarioService;
    private readonly IPermisoService _permisoService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IUsuarioService usuarioService, IPermisoService permisoService, ILogger<AuthController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("SupabaseAuth");
        _configuration = configuration;
        _usuarioService = usuarioService;
        _permisoService = permisoService;
        _logger = logger;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            var supabaseKey = _configuration["Supabase:AnonKey"];

            _logger.LogInformation("Login attempt for {Email}. SupabaseUrl configured: {HasUrl}, AnonKey configured: {HasKey}",
                request.Email, supabaseUrl != null, supabaseKey != null);

            var authRequest = new
            {
                email = request.Email,
                password = request.Password
            };

            var req = new HttpRequestMessage(HttpMethod.Post, $"{supabaseUrl}/auth/v1/token?grant_type=password")
            {
                Content = JsonContent.Create(authRequest)
            };
            req.Headers.Add("apikey", supabaseKey);

            var response = await _httpClient.SendAsync(req);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Supabase Auth failed for {Email}: {StatusCode} - {Error}",
                    request.Email, response.StatusCode, errorBody);
                return BadRequest(new ApiResponse { Success = false, Message = "Credenciales inválidas" });
            }

            var authResponse = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>();
            if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
                return BadRequest(new ApiResponse { Success = false, Message = "Error al obtener el token" });

            _logger.LogInformation("Supabase Auth success for {Email}. User.Id: {UserId}", request.Email, authResponse.User?.Id);

            if (!Guid.TryParse(authResponse.User?.Id, out var authUserId))
                return BadRequest(new ApiResponse { Success = false, Message = "ID de usuario inválido en respuesta de autenticación" });

            var userResult = await _usuarioService.GetByAuthUserIdAsync(authUserId);

            if (!userResult.IsSuccess || userResult.Data == null)
            {
                _logger.LogWarning("Usuario no encontrado en BD para AuthUserId: {AuthUserId}. Message: {Msg}", authUserId, userResult.Message);
                return Unauthorized(new ApiResponse { Success = false, Message = userResult.Message });
            }

            if (!userResult.Data.EsAdmin && !userResult.Data.EsSuperAdmin)
            {
                var permisoResult = await _permisoService.HasPermissionAsync(
                    userResult.Data.ClinicaId,
                    userResult.Data.PerfilId,
                    "login",
                    PermissionType.Read);

                if (!permisoResult.IsSuccess || !permisoResult.Data)
                {
                    _logger.LogWarning("Acceso denegado para {Email}: perfil {Perfil} no tiene permiso 'Acceso al sistema'",
                        request.Email, userResult.Data.PerfilNombre);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Su perfil no tiene acceso al sistema. Consulte con un administrador."
                    });
                }
            }

            var loginResponse = new LoginResponseDto
            {
                AccessToken = authResponse.AccessToken,
                RefreshToken = authResponse.RefreshToken,
                ExpiresIn = authResponse.ExpiresIn,
                UsuarioId = userResult.Data.UsuarioId,
                ClinicaId = userResult.Data.ClinicaId,
                Nombres = userResult.Data.Nombres,
                Apellidos = userResult.Data.Apellidos,
                Email = userResult.Data.Email,
                ClinicaNombre = userResult.Data.ClinicaNombre,
                Perfil = userResult.Data.PerfilNombre,
                PerfilId = userResult.Data.PerfilId,
                EsAdmin = userResult.Data.EsAdmin,
                EsSuperAdmin = userResult.Data.EsSuperAdmin
            };

            return Ok(new ApiResponse<LoginResponseDto>
            {
                Success = true,
                Message = "Login exitoso",
                Data = loginResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en Login para {Email}", request.Email);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor. Intente de nuevo o contacte al administrador."
            });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:AnonKey"];

        var authRequest = new
        {
            refresh_token = request.RefreshToken
        };

        var req = new HttpRequestMessage(HttpMethod.Post, $"{supabaseUrl}/auth/v1/token?grant_type=refresh_token")
        {
            Content = JsonContent.Create(authRequest)
        };
        req.Headers.Add("apikey", supabaseKey);

        var response = await _httpClient.SendAsync(req);

        if (!response.IsSuccessStatusCode)
        {
            return BadRequest(new ApiResponse { Success = false, Message = "Token de refresco inválido o expirado" });
        }

        var authResponse = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>();
        if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
            return BadRequest(new ApiResponse { Success = false, Message = "Error al refrescar el token" });

        return Ok(new ApiResponse<SupabaseAuthResponse>
        {
            Success = true,
            Message = "Token refrescado exitosamente",
            Data = authResponse
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // El logout real se hace en cliente eliminando los tokens, o invalidando el refresh token en Supabase
        return Ok(new ApiResponse { Success = true, Message = "Sesión cerrada correctamente" });
    }
}
