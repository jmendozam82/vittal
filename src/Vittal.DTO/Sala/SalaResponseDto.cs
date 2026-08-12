using System;
namespace Vittal.DTO.Sala;
/// <summary>
/// Response DTO para lectura de salas/áreas.
/// Incluye todos los campos de la entidad para visualización en listados y formularios.
/// </summary>
public class SalaResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
