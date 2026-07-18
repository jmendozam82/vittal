using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Vittal.API.Services;

/// <summary>
/// Genera JWT de corta vida (60 segundos) firmados con HMAC-SHA256,
/// exclusivamente para la conexión SignalR WebSocket.
///
/// Seguridad: El token de Supabase (ES256) se usa para REST API.
/// El token de corta vida (HMAC-SHA256) se usa solo para WebSocket/SignalR,
/// minimizando la ventana de exposición del token en la URL del query string.
/// </summary>
public class SignalrTokenService
{
    private readonly SymmetricSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly ILogger<SignalrTokenService> _logger;

    /// <summary>Duración del token SignalR en segundos.</summary>
    private const int TokenLifetimeSeconds = 60;

    public SignalrTokenService(IConfiguration configuration, ILogger<SignalrTokenService> logger)
    {
        _logger = logger;

        var jwtSecret = configuration["Supabase:JwtSecret"]
            ?? throw new InvalidOperationException("Supabase:JwtSecret no está configurado.");

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url no está configurado.");

        _issuer = $"{supabaseUrl}/auth/v1";
    }

    /// <summary>
    /// Genera un JWT de corta vida para SignalR.
    /// Copia los claims esenciales del token Supabase original.
    /// </summary>
    /// <param name="originalToken">Token Supabase original (ES256) del usuario autenticado.</param>
    /// <returns>JWT de corta vida (HMAC-SHA256) para SignalR.</returns>
    public string GenerateSignalrToken(string originalToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var originalJwt = handler.ReadJwtToken(originalToken);

        // Extraer claims esenciales del token original
        var claims = new List<Claim>();

        // Claim principal: sub (user ID)
        var sub = originalJwt.Claims.FirstOrDefault(c => c.Type == "sub");
        if (sub != null) claims.Add(sub);

        // Email
        var email = originalJwt.Claims.FirstOrDefault(c => c.Type == "email");
        if (email != null) claims.Add(email);

        // Roles de Supabase
        var role = originalJwt.Claims.FirstOrDefault(c => c.Type == "role");
        if (role != null) claims.Add(role);

        // AAL y AMR (autenticación)
        var aal = originalJwt.Claims.FirstOrDefault(c => c.Type == "aal");
        if (aal != null) claims.Add(aal);

        var amr = originalJwt.Claims.FirstOrDefault(c => c.Type == "amr");
        if (amr != null) claims.Add(amr);

        // session_id
        var sessionId = originalJwt.Claims.FirstOrDefault(c => c.Type == "session_id");
        if (sessionId != null) claims.Add(sessionId);

        // Marcar como token de corta vida para distinguirlo
        claims.Add(new Claim("token_type", "signalr_short_lived"));

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(TokenLifetimeSeconds),
            Issuer = _issuer,
            Audience = "authenticated",
            SigningCredentials = credentials
        };

        var securityToken = handler.CreateToken(tokenDescriptor);
        var tokenString = handler.WriteToken(securityToken);

        _logger.LogDebug("Token SignalR de corta vida generado. Expira en {Seconds}s", TokenLifetimeSeconds);

        return tokenString;
    }

    /// <summary>
    /// Obtiene la clave de firma HMAC para ser registrada en la validación JWT.
    /// Permite que el middleware de autenticación valide tokens HMAC de corta vida.
    /// </summary>
    public SecurityKey GetSigningKey() => _signingKey;
}
