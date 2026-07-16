using System.ComponentModel.DataAnnotations;
namespace Vittal.DTO.Recomendacion;
/// <summary>
/// Request DTO para crear o editar una recomendaci�n.
/// No expone campos de auditor�a ni tenant � el servidor los maneja autom�ticamente.
/// Historia de Usuario: HU16 � Gesti�n de Recomendaciones
/// </summary>
public class RecomendacionRequestDto
{
    [Required(ErrorMessage = "El nombre de la recomendaci�n es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
