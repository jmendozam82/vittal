namespace Vittal.Entity;

/// <summary>
/// Tratamiento y/o medicamento recetado en una hoja de cita médica.
/// Tabla: public.hoja_tratamientos
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaTratamiento
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid? MedicamentoId { get; set; }
    public Guid? TratamientoId { get; set; }

    // ── Campos de prescripción ────────────────────────────────────
    /// <summary>Dosis del medicamento recetado.</summary>
    public string? Dosis { get; set; }

    /// <summary>Frecuencia de administración (ej. "cada 8 horas").</summary>
    public string? Frecuencia { get; set; }

    /// <summary>Duración del tratamiento (ej. "7 días", "1 mes").</summary>
    public string? Duracion { get; set; }

    /// <summary>Instrucciones adicionales para el tratamiento.</summary>
    public string? Instrucciones { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // ── Propiedades JOIN (no se persisten directamente) ───────────
    /// <summary>Nombre del medicamento (JOIN con medicamentos).</summary>
    public string? MedicamentoNombre { get; set; }

    /// <summary>Nombre del tratamiento (JOIN con tratamientos).</summary>
    public string? TratamientoNombre { get; set; }
}
