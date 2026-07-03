using System;

namespace Vittal.DTO.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    
    // User info
    public Guid UsuarioId { get; set; }
    public Guid ClinicaId { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public Guid PerfilId { get; set; }
    public bool EsAdmin { get; set; }
    public bool EsSuperAdmin { get; set; }
    public string ClinicaNombre { get; set; } = string.Empty;
}
