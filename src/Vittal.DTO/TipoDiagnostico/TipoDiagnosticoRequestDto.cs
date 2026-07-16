using System.ComponentModel.DataAnnotations;
namespace Vittal.DTO.TipoDiagnostico;
/// <summary>
/// Request DTO para crear o editar un tipo de diagn�stico.
/// No expone campos de auditor�a ni tenant � el servidor los maneja autom�ticamente.
/// Historia de Usuario: HU13 � Gesti�n de Tipos de Diagn�stico
/// </summary>
public class TipoDiagnosticoRequestDto
{
    [Required(ErrorMessage = "El nombre del tipo de diagn�stico es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
