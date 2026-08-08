using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Auth;
using Vittal.Utility;

namespace Vittal.API.Controllers;

/// <summary>
/// Reusable HTML builder for password-reset notification emails sent to clinic admins.
/// </summary>
internal static class PasswordResetEmailBuilder
{
    public static string BuildHtml(string usuarioNombre, string usuarioEmail, string adminNombre)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f4f6f9; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .header {{ background: linear-gradient(135deg, #0F1A2E, #1A6FA8); padding: 24px 30px; color: #fff; }}
        .header h1 {{ margin: 0; font-size: 20px; font-weight: 700; }}
        .header p {{ margin: 6px 0 0; font-size: 13px; opacity: 0.8; }}
        .body {{ padding: 30px; }}
        .field {{ margin-bottom: 18px; }}
        .field-label {{ font-size: 12px; font-weight: 600; color: #6c757d; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px; }}
        .field-value {{ font-size: 15px; color: #2C3E50; line-height: 1.5; }}
        .badge {{ display: inline-block; background: #FFF3CD; color: #856404; padding: 3px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }}
        .info-box {{ background: #f8fafc; padding: 16px; border-radius: 8px; border-left: 3px solid #1A6FA8; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; font-size: 14px; color: #495057; }}
        .footer {{ padding: 20px 30px; background: #f8fafc; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #eee; }}
        .footer a {{ color: #1A6FA8; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Restablecimiento de Contraseña</h1>
            <p>Un usuario ha solicitado restablecer su contraseña.</p>
        </div>
        <div class='body'>
            <p>Hola <strong>{System.Net.WebUtility.HtmlEncode(adminNombre)}</strong>,</p>
            <p>El siguiente usuario ha solicitado restablecer su contraseña a través del sistema:</p>
            <div class='field'>
                <div class='field-label'>Nombre del Usuario</div>
                <div class='field-value'><strong>{System.Net.WebUtility.HtmlEncode(usuarioNombre)}</strong></div>
            </div>
            <div class='field'>
                <div class='field-label'>Correo Electrónico</div>
                <div class='field-value'><a href='mailto:{System.Net.WebUtility.HtmlEncode(usuarioEmail)}'>{System.Net.WebUtility.HtmlEncode(usuarioEmail)}</a></div>
            </div>
            <div class='info-box'>
                <p><strong>Acción requerida:</strong> Contacte al usuario para verificar su identidad y gestionar el cambio de contraseña desde el panel de administración.</p>
            </div>
            <p style='font-size: 13px; color: #6c757d;'>Si usted no solicitó este cambio, ignore este mensaje.</p>
        </div>
        <div class='footer'>
            <p>Este correo fue enviado automáticamente por <a href='#'>Vittal Software</a>.</p>
            <p>Fecha: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC</p>
        </div>
    </div>
</body>
</html>";
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IUsuarioService _usuarioService;
    private readonly IPermisoService _permisoService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IUsuarioService usuarioService, IPermisoService permisoService, IEmailService emailService, ILogger<AuthController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("SupabaseAuth");
        _configuration = configuration;
        _usuarioService = usuarioService;
        _permisoService = permisoService;
        _emailService = emailService;
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
                EsSuperAdmin = userResult.Data.EsSuperAdmin,
                EsDoctor = userResult.Data.EsDoctor
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

    // ────────────────────────────────────────────────────────────────────
    // Forgot Password — Notifica al admin de la clínica del usuario
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Busca al usuario por email, localiza al admin de su clínica y envía
    /// un correo de notificación. Siempre retorna 200 OK por seguridad
    /// (no revela si el email existe o no en el sistema).
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        try
        {
            _logger.LogInformation("Solicitud de recuperación de contraseña para email: {Email}", request.Email);

            // 1. Buscar usuario por email (sin filtro de clínica)
            var usuarioResult = await _usuarioService.GetByEmailAsync(request.Email);

            if (usuarioResult.IsSuccess && usuarioResult.Data != null)
            {
                var usuario = usuarioResult.Data;

                // 2. Buscar admin de la clínica del usuario
                var adminResult = await _usuarioService.GetAdminByClinicaAsync(usuario.ClinicaId);

                if (adminResult.IsSuccess && adminResult.Data != null)
                {
                    var admin = adminResult.Data;

                    // 3. Enviar email de notificación al admin
                    var subject = "Restablecimiento de Contraseña — Solicitud de Usuario";
                    var htmlBody = PasswordResetEmailBuilder.BuildHtml(
                        $"{usuario.Nombres} {usuario.Apellidos}",
                        usuario.Email!,
                        admin.Nombres);

                    var emailSent = await _emailService.SendEmailAsync(admin.Email!, subject, htmlBody);

                    if (emailSent)
                    {
                        _logger.LogInformation(
                            "Notificación de restablecimiento enviada al admin {AdminEmail} para usuario {UsuarioEmail}",
                            admin.Email, usuario.Email);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "No se pudo enviar email al admin {AdminEmail} para usuario {UsuarioEmail}",
                            admin.Email, usuario.Email);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "No se encontró admin para la clínica {ClinicaId} del usuario {UsuarioEmail}",
                        usuario.ClinicaId, usuario.Email);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Email no encontrado en el sistema: {Email} — se muestra confirmación por seguridad",
                    request.Email);
            }

            // 4. Siempre retornar éxito (por seguridad, no revelar si el email existe)
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Si el correo está registrado, se ha enviado una notificación al administrador."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en ForgotPassword para {Email}", request.Email);
            // Retornar éxito igualmente (no revelar errores internos)
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Si el correo está registrado, se ha enviado una notificación al administrador."
            });
        }
    }
}
