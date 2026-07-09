using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Diagnostico;

/// <summary>
/// Request DTO para crear o editar un diagnóstico en el catálogo.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU14 — Gestión de Diagnósticos
/// </summary>
public class DiagnosticoRequestDto
{
    /// <summary>Nombre del diagnóstico (obligatorio).</summary>
    [Required(ErrorMessage = "El nombre del diagnóstico es obligatorio.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Código CIE-10 del diagnóstico (opcional).</summary>
    [StringLength(20, ErrorMessage = "El código CIE-10 no puede exceder 20 caracteres.")]
    public string? CodigoCie10 { get; set; }

    /// <summary>Tipo de diagnóstico (obligatorio).</summary>
    [Required(ErrorMessage = "El tipo de diagnóstico es obligatorio.")]
    public Guid TipoDiagnosticoId { get; set; }
}
