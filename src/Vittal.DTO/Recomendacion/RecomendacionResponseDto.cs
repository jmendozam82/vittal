using System;
namespace Vittal.DTO.Recomendacion;
/// <summary>
/// Response DTO para datos de la recomendaci�n.
/// Historia de Usuario: HU16 � Gesti�n de Recomendaciones
/// </summary>
public class RecomendacionResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
