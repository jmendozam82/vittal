using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.TipoCirugia;

/// <summary>
/// Request DTO para crear o editar un tipo de cirugía.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU11 — Gestión de Tipos de Cirugías
/// </summary>
public class TipoCirugiaRequestDto
{
    [Required(ErrorMessage = "El nombre del tipo de cirugía es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
