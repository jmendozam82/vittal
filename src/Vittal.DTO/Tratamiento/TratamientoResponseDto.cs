using System;

namespace Vittal.DTO.Tratamiento;

/// <summary>
/// Response DTO para datos del tratamiento.
/// Historia de Usuario: HU15 — Gestión de Tratamientos
/// </summary>
public class TratamientoResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
