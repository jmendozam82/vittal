using System.ComponentModel.DataAnnotations;
namespace Vittal.DTO.Examen;
/// <summary>
/// Request DTO para crear o editar un examen.
/// No expone campos de auditor�a ni tenant � el servidor los maneja autom�ticamente.
/// Historia de Usuario: HU17 � Gesti�n de Ex�menes
/// </summary>
public class ExamenRequestDto
{
    [Required(ErrorMessage = "El nombre del examen es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
