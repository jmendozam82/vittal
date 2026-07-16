using System;
namespace Vittal.DTO.Perfil;
/// <summary>
/// Response DTO para lectura de perfiles.
/// Incluye conteos asociados pero no expone datos sensibles de otros tenants.
/// </summary>
public class PerfilResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsAdmin { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public int CantidadPermisos { get; set; }
    public int CantidadUsuarios { get; set; }
}
