# Controller — Auth (Login)

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para el controlador de autenticación.
> **Prerequisito:** skills/controller/SKILL.md

---

## AuthController

```csharp
// src/Vittal.API/Controllers/AuthController.cs
namespace Vittal.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags("Autenticación")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUsuarioService usuarioService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _usuarioService = usuarioService;
        _configuration  = configuration;
        _logger         = logger;
    }

    /// <summary>Inicia sesión con usuario y contraseña.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Usuario) || string.IsNullOrWhiteSpace(dto.Contrasena))
            return BadRequest(ApiResponse<object>.Fail(
                "El usuario y la contraseña son obligatorios."));

        var supabaseUrl    = _configuration["Supabase:Url"]!;
        var supabaseAnonKey = _configuration["Supabase:AnonKey"]!;

        try
        {
            // 1. Autenticar contra Supabase Auth
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);

            var response = await httpClient.PostAsJsonAsync(
                $"{supabaseUrl}/auth/v1/token?grant_type=password",
                new { email = dto.Usuario, password = dto.Contrasena });

            if (!response.IsSuccessStatusCode)
                return Unauthorized(ApiResponse<object>.Fail(
                    "Usuario o contraseña incorrectos."));

            var authData = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>();

            // 2. Obtener datos del tenant desde la BD
            var usuarioResult = await _usuarioService
                .GetByAuthUserIdAsync(Guid.Parse(authData!.User.Id));

            if (!usuarioResult.Success || usuarioResult.Data is null)
                return Unauthorized(ApiResponse<object>.Fail(
                    "El usuario no está registrado en el sistema."));

            var usuario = usuarioResult.Data;

            // 3. Retornar JWT + datos del tenant
            var loginResponse = new LoginResponseDto
            {
                AccessToken  = authData.AccessToken,
                RefreshToken = authData.RefreshToken,
                ExpiresIn    = authData.ExpiresIn,
                UsuarioId    = usuario.Id,
                ClinicaId    = usuario.ClinicaId,
                NombreCompleto = usuario.NombreCompleto,
                EsAdmin      = usuario.EsAdmin,
                PerfilNombre = usuario.PerfilNombre
            };

            return Ok(ApiResponse<LoginResponseDto>.Ok(loginResponse,
                $"Bienvenido, {usuario.NombreCompleto}."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en login para usuario {Usuario}", dto.Usuario);
            return StatusCode(500, ApiResponse<object>.Fail(
                "Error inesperado al iniciar sesión."));
        }
    }

    /// <summary>Renueva el access token usando el refresh token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RefreshResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
    {
        // Similar a Login pero con grant_type=refresh_token
        var supabaseUrl = _configuration["Supabase:Url"]!;
        var supabaseAnonKey = _configuration["Supabase:AnonKey"]!;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);

        var response = await httpClient.PostAsJsonAsync(
            $"{supabaseUrl}/auth/v1/token?grant_type=refresh_token",
            new { refresh_token = dto.RefreshToken });

        if (!response.IsSuccessStatusCode)
            return Unauthorized(ApiResponse<object>.Fail("Token de renovación inválido."));

        var authData = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>();

        return Ok(ApiResponse<RefreshResponseDto>.Ok(new RefreshResponseDto
        {
            AccessToken  = authData!.AccessToken,
            RefreshToken = authData.RefreshToken,
            ExpiresIn    = authData.ExpiresIn
        }));
    }

    /// <summary>Cierra la sesión del usuario actual.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var supabaseUrl = _configuration["Supabase:Url"]!;
        var supabaseAnonKey = _configuration["Supabase:AnonKey"]!;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await httpClient.PostAsync($"{supabaseUrl}/auth/v1/logout", null);

        return Ok(ApiResponse<bool>.Ok(true, "Sesión cerrada exitosamente."));
    }
}
```

---

## Checklist de Calidad — Auth Controller

- [ ] `[AllowAnonymous]` en login y refresh — NO en logout
- [ ] Valida usuario y contraseña no vacíos antes de llamar a Supabase
- [ ] Usa Supabase Auth endpoint `/auth/v1/token?grant_type=password`
- [ ] Obtiene datos del tenant via `IUsuarioService.GetByAuthUserIdAsync`
- [ ] Retorna 401 si credenciales incorrectas
- [ ] Retorna 401 si usuario no existe en la BD
- [ ] Retorna 200 con `LoginResponseDto` completo
- [ ] Logout llama a `/auth/v1/logout` de Supabase
- [ ] Logging de errores con usuario (sin contraseña)

---

*skills/controller/auth-controller.md — Vittal v1.0.0*
