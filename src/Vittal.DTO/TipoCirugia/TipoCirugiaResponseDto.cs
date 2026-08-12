using System;
namespace Vittal.DTO.TipoCirugia;
/// <summary>
/// Response DTO para datos del tipo de cirugía.
/// Historia de Usuario: HU11 — Gestión de Tipos de Cirugías
/// </summary>
public class TipoCirugiaResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
