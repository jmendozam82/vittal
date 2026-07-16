using System.ComponentModel.DataAnnotations;
namespace Vittal.DTO.TipoCirugia;
/// <summary>
/// Request DTO para crear o editar un tipo de cirug�a.
/// No expone campos de auditor�a ni tenant � el servidor los maneja autom�ticamente.
/// Historia de Usuario: HU11 � Gesti�n de Tipos de Cirug�as
/// </summary>
public class TipoCirugiaRequestDto
{
    [Required(ErrorMessage = "El nombre del tipo de cirug�a es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
