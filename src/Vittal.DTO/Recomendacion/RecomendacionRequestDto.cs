using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Recomendacion;

/// <summary>
/// Request DTO para crear o editar una recomendación.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU16 — Gestión de Recomendaciones
/// </summary>
public class RecomendacionRequestDto
{
    [Required(ErrorMessage = "El nombre de la recomendación es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
