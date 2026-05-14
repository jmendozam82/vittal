using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Parámetros asociados a un reporte generado (filtros aplicados).
/// Tabla: public.reporte_parametros
/// Historia de Usuario: HU22 — Reportes
/// </summary>
public class ReporteParametro
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ReporteId { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Campos del parámetro ──────────────────────────────────────
    /// <summary>Clave del parámetro (ej: "doctor_id", "sala_id", "estado").</summary>
    public string Clave { get; set; } = string.Empty;

    /// <summary>Valor del parámetro serializado como string.</summary>
    public string Valor { get; set; } = string.Empty;

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
