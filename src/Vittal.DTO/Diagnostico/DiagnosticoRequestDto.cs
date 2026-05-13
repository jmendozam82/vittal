using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.Diagnostico;

/// <summary>
/// Request DTO para crear o editar un diagnóstico en una cita.
/// La cita y el tipo de diagnóstico ya existen, solo se asigna la relación con descripción.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU14 — Gestión de Diagnósticos
/// </summary>
public class DiagnosticoRequestDto
{
    [Required(ErrorMessage = "La cita es obligatoria.")]
    public Guid CitaId { get; set; }

    [Required(ErrorMessage = "El tipo de diagnóstico es obligatorio.")]
    public Guid TipoDiagnosticoId { get; set; }

    /// <summary>Descripción detallada del diagnóstico en el contexto de esta cita.</summary>
    public string? Descripcion { get; set; }
}
