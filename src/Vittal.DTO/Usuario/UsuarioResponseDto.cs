using System;

namespace Vittal.DTO.Usuario;

/// <summary>
/// Response DTO para datos del usuario autenticado.
/// Mapea desde la Entity Usuario (sin campos sensibles internos).
/// </summary>
public class UsuarioResponseDto
{
    public Guid UsuarioId { get; set; }
    public Guid ClinicaId { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Celular { get; set; }
    public string? Sexo { get; set; }
    public bool EsDoctor { get; set; }
    public string PerfilNombre { get; set; } = string.Empty;
    public bool EsAdmin { get; set; }
}
