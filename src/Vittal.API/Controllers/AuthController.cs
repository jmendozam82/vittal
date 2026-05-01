using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Vittal.API.Models;
using Vittal.BLL.Services;
using Vittal.DTO.Auth;

namespace Vittal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IUsuarioService _usuarioService;

    public AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IUsuarioService usuarioService)
    {
        _httpClient = httpClientFactory.CreateClient("SupabaseAuth");
        _configuration = configuration;
        _usuarioService = usuarioService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:AnonKey"];

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
            var errorContent = await response.Content.ReadAsStringAsync();
            return BadRequest(new ApiResponse { IsSuccess = false, Message = "Credenciales inválidas" });
        }

        var authResponse = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>();
        if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
            return BadRequest(new ApiResponse { IsSuccess = false, Message = "Error al obtener el token" });

        // Fetch internal user details
        var authUserId = Guid.Parse(authResponse.User.Id);
        var userResult = await _usuarioService.GetByAuthUserIdAsync(authUserId);

        if (!userResult.IsSuccess || userResult.Data == null)
            return Unauthorized(new ApiResponse { IsSuccess = false, Message = "Usuario no registrado en el sistema o inactivo" });

        var loginResponse = new LoginResponseDto
        {
            AccessToken = authResponse.AccessToken,
            RefreshToken = authResponse.RefreshToken,
            ExpiresIn = authResponse.ExpiresIn,
            UsuarioId = userResult.Data.Id,
            ClinicaId = userResult.Data.ClinicaId,
            Nombres = userResult.Data.Nombres,
            Apellidos = userResult.Data.Apellidos,
            Email = userResult.Data.Email,
            Perfil = userResult.Data.PerfilNombre,
            EsAdmin = userResult.Data.EsAdmin
        };

        return Ok(new ApiResponse<LoginResponseDto>
        {
            IsSuccess = true,
            Message = "Login exitoso",
            Data = loginResponse
        });
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
            return BadRequest(new ApiResponse { IsSuccess = false, Message = "Token de refresco inválido o expirado" });
        }

        var authResponse = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>();
        if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
            return BadRequest(new ApiResponse { IsSuccess = false, Message = "Error al refrescar el token" });

        return Ok(new ApiResponse<SupabaseAuthResponse>
        {
            IsSuccess = true,
            Message = "Token refrescado exitosamente",
            Data = authResponse
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // El logout real se hace en cliente eliminando los tokens, o invalidando el refresh token en Supabase
        return Ok(new ApiResponse { IsSuccess = true, Message = "Sesión cerrada correctamente" });
    }
}
