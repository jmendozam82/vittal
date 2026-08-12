using System.ComponentModel.DataAnnotations;
namespace Vittal.DTO.Tratamiento;
/// <summary>
/// Request DTO para crear o editar un tratamiento.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU15 — Gestión de Tratamientos
/// </summary>
public class TratamientoRequestDto
{
    [Required(ErrorMessage = "El nombre del tratamiento es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
