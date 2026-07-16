namespace Vittal.Entity;

/// <summary>
/// Pacientes registrados en el sistema por clínica.
/// Tabla: public.pacientes
/// Historia de Usuario: HU07 — Gestión de Pacientes
/// </summary>
public class Paciente
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid DoctorId { get; set; }

    // ── Campos de identidad ───────────────────────────────────────
    public string PrimerNombre { get; set; } = string.Empty;
    public string? SegundoNombre { get; set; }
    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }
    public string? Email { get; set; }
    public string? Celular { get; set; }
    public string? Direccion { get; set; }

    /// <summary>'M' = Masculino, 'F' = Femenino</summary>
    public string? Sexo { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? FotoUrl { get; set; }
    public string? TipoDocumentoIdentificacion { get; set; }
    public string? NumeroDocumentoIdentificacion { get; set; }
    public string? Observaciones { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }

    // ── Propiedades calculadas / JOIN (no se persisten directamente) ──
    /// <summary>Nombre completo del doctor asignado (JOIN con usuarios).</summary>
    public string DoctorNombre { get; set; } = string.Empty;

    /// <summary>Nombre completo del paciente (calculado).</summary>
    public string NombreCompleto =>
        $"{PrimerNombre} {SegundoNombre} {PrimerApellido} {SegundoApellido}"
            .Replace("  ", " ").Trim();
}
