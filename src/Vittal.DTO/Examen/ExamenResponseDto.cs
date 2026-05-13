using System;

namespace Vittal.DTO.Examen;

/// <summary>
/// Response DTO para datos del examen.
/// Historia de Usuario: HU17 — Gestión de Exámenes
/// </summary>
public class ExamenResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
