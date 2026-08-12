using System;
namespace Vittal.DTO.TipoDiagnostico;
/// <summary>
/// Response DTO para datos del tipo de diagnóstico.
/// Historia de Usuario: HU13 — Gestión de Tipos de Diagnóstico
/// </summary>
public class TipoDiagnosticoResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
