using System;
namespace Vittal.DTO.Cirugia;
/// <summary>
/// Response DTO para datos de la cirugía.
/// Incluye nombre del tipo de cirugía mediante JOIN.
/// Historia de Usuario: HU12 — Gestión de Cirugías
/// </summary>
public class CirugiaResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid TipoCirugiaId { get; set; }
    public string TipoCirugiaNombre { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    /// <summary>Nombre + tipo de cirugía concatenados.</summary>
    public string NombreCompleto { get; set; } = string.Empty;
}
