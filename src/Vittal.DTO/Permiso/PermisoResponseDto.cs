using System;

namespace Vittal.DTO.Permiso;

/// <summary>
/// Response DTO que representa un permiso individual de un perfil sobre un módulo.
/// </summary>
public class PermisoResponseDto
{
    public Guid Id { get; set; }
    public Guid ModuloId { get; set; }
    public string ModuloClave { get; set; } = string.Empty;
    public string ModuloNombre { get; set; } = string.Empty;
    public string? ModuloDescripcion { get; set; }
    public bool PuedeLeer { get; set; }
    public bool PuedeCrear { get; set; }
    public bool PuedeActualizar { get; set; }
}
