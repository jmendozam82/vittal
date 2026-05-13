using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Cirugia;

/// <summary>
/// Request DTO para crear o editar una cirugía.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU12 — Gestión de Cirugías
/// </summary>
public class CirugiaRequestDto
{
    [Required(ErrorMessage = "El tipo de cirugía es obligatorio.")]
    public Guid TipoCirugiaId { get; set; }

    [Required(ErrorMessage = "El nombre de la cirugía es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
