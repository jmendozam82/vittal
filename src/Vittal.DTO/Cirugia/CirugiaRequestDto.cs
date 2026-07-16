using System;
using System.ComponentModel.DataAnnotations;
namespace Vittal.DTO.Cirugia;
/// <summary>
/// Request DTO para crear o editar una cirug�a.
/// No expone campos de auditor�a ni tenant � el servidor los maneja autom�ticamente.
/// Historia de Usuario: HU12 � Gesti�n de Cirug�as
/// </summary>
public class CirugiaRequestDto
{
    [Required(ErrorMessage = "El tipo de cirug�a es obligatorio.")]
    public Guid TipoCirugiaId { get; set; }
    [Required(ErrorMessage = "El nombre de la cirug�a es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
