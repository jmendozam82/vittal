using System;
namespace Vittal.DTO.Cirugia;
/// <summary>
/// Response DTO para datos de la cirug�a.
/// Incluye nombre del tipo de cirug�a mediante JOIN.
/// Historia de Usuario: HU12 � Gesti�n de Cirug�as
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
    /// <summary>Nombre + tipo de cirug�a concatenados.</summary>
    public string NombreCompleto { get; set; } = string.Empty;
}
