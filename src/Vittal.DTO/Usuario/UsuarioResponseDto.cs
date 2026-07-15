using System;

namespace Vittal.DTO.Usuario;

/// <summary>
/// Response DTO para datos del usuario. Incluye campos de CRUD y JOIN con perfiles.
/// </summary>
public class UsuarioResponseDto
{
    public Guid UsuarioId { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid? AuthUserId { get; set; }
    public Guid PerfilId { get; set; }
    
    public string Username { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Sexo { get; set; }
    public string? Celular { get; set; }
    public string? Direccion { get; set; }
    public string? FotoUrl { get; set; }
    public string? TipoDocumentoIdentificacion { get; set; }
    public string? NumeroDocumentoIdentificacion { get; set; }
    public bool EsDoctor { get; set; }
    public string ClinicaNombre { get; set; } = string.Empty;
    public string PerfilNombre { get; set; } = string.Empty;
    public bool EsAdmin { get; set; }
    public bool EsSuperAdmin { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    
    public string NombreCompleto => $"{Nombres} {Apellidos}";
}
