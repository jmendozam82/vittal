using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Registro de línea de tiempo de los pasos de atención de una cita.
/// Tabla: public.linea_tiempo
/// Historia de Usuario: HU19 — Línea de Tiempo
/// </summary>
public class LineaTiempo
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid CitaId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid? SalaId { get; set; }

    // ── Campos del paso ───────────────────────────────────────────
    /// <summary>Nombre o descripción del paso (ej: "Recepción", "Toma de signos", "Consulta").</summary>
    public string NombrePaso { get; set; } = string.Empty;

    /// <summary>Orden del paso dentro de la secuencia de la cita.</summary>
    public int Orden { get; set; }

    /// <summary>
    /// Estado del paso: pendiente | en_sala | completado | saltado
    /// </summary>
    public string Estado { get; set; } = "pendiente";

    // ── Campos de tiempo ──────────────────────────────────────────
    /// <summary>Hora en que el paciente ingresó al paso (TIME nullable).</summary>
    public TimeSpan? HoraLlegada { get; set; }

    /// <summary>Hora en que el paciente salió del paso (TIME nullable).</summary>
    public TimeSpan? HoraSalida { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
}
