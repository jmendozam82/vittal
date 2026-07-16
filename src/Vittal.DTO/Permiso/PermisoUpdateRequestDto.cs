using System;
using System.Collections.Generic;

namespace Vittal.DTO.Permiso;

/// <summary>
/// Request DTO para actualizar los permisos de un perfil sobre un módulo específico.
/// </summary>
public class PermisoItemUpdateDto
{
    public Guid ModuloId { get; set; }
    public bool PuedeLeer { get; set; }
    public bool PuedeCrear { get; set; }
    public bool PuedeActualizar { get; set; }
}

/// <summary>
/// Request DTO para actualizar todos los permisos de un perfil (batch).
/// </summary>
public class PermisoUpdateRequestDto
{
    public List<PermisoItemUpdateDto> Permisos { get; set; } = new();
}
