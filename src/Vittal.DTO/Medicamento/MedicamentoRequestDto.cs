using System.ComponentModel.DataAnnotations;
namespace Vittal.DTO.Medicamento;
/// <summary>
/// Request DTO para crear o editar un medicamento.
/// No expone campos de auditor�a ni tenant � el servidor los maneja autom�ticamente.
/// Historia de Usuario: HU08 � Gesti�n de Medicamentos
/// </summary>
public class MedicamentoRequestDto
{
    [Required(ErrorMessage = "El nombre del medicamento es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    [StringLength(100, ErrorMessage = "La concentraci�n no puede exceder 100 caracteres.")]
    public string? Concentracion { get; set; }
    [StringLength(50, ErrorMessage = "La unidad de medida no puede exceder 50 caracteres.")]
    public string? UnidadMedida { get; set; }
}
