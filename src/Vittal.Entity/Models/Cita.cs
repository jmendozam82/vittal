using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Citas médicas programadas por clínica.
/// Tabla: public.citas
/// Historia de Usuario: HU21 — Agenda (HU-E01 — hora_fin)
/// </summary>
public class Cita
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? SalaId { get; set; }

    // ── Campos de fecha y hora ────────────────────────────────────
    /// <summary>Fecha de la cita (DATE en BD).</summary>
    public DateTime FechaCita { get; set; }

    /// <summary>Hora de inicio de la cita (TIME en BD).</summary>
    public TimeSpan HoraCita { get; set; }

    /// <summary>Hora de fin estimada de la cita (TIME nullable). HU-E01.</summary>
    public TimeSpan? HoraFin { get; set; }

    /// <summary>Hora en que el paciente llegó a la clínica.</summary>
    public TimeSpan? HoraLlegada { get; set; }

    // ── Campos de negocio ─────────────────────────────────────────
    /// <summary>Lugar o sala física donde se atiende la cita.</summary>
    public string? Lugar { get; set; }

    /// <summary>Motivo de la consulta.</summary>
    public string? Motivo { get; set; }

    /// <summary>
    /// Estado de la cita: agendada | cancelada | atendida | en_espera | en_atencion.
    /// Valor por defecto: "agendada".
    /// </summary>
    public string Estado { get; set; } = "agendada";

    /// <summary>Notas internas de la cita.</summary>
    public string? Notas { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }

    // ── Propiedades calculadas / JOIN (no se persisten directamente) ──
    /// <summary>Nombre completo del paciente (JOIN con pacientes).</summary>
    public string PacienteNombre { get; set; } = string.Empty;

    /// <summary>Nombre completo del doctor (JOIN con usuarios).</summary>
    public string DoctorNombre { get; set; } = string.Empty;

    /// <summary>Nombre de la sala (JOIN con salas).</summary>
    public string? SalaNombre { get; set; }
}
