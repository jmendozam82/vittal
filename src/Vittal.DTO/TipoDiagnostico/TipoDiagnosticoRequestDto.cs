using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.TipoDiagnostico;

/// <summary>
/// Request DTO para crear o editar un tipo de diagnóstico.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU13 — Gestión de Tipos de Diagnóstico
/// </summary>
public class TipoDiagnosticoRequestDto
{
    [Required(ErrorMessage = "El nombre del tipo de diagnóstico es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
