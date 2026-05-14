using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Reportes generados por clínica.
/// Tabla: public.reportes
/// Historia de Usuario: HU22 — Reportes
/// </summary>
public class Reporte
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Campos de identidad ───────────────────────────────────────
    /// <summary>Nombre descriptivo del reporte.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de reporte: citas_atendidas | pacientes_atendidos | ingresos | cirugias | examenes | personalizado.
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Descripción opcional del reporte.</summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// Formato de exportación: pdf | excel | csv | json.
    /// </summary>
    public string Formato { get; set; } = "pdf";

    /// <summary>Contenido del reporte en formato JSON (datos serializados).</summary>
    public string ContenidoJson { get; set; } = string.Empty;

    // ── Campos de filtro de fecha ─────────────────────────────────
    /// <summary>Fecha inicio del rango del reporte.</summary>
    public DateTime FechaInicio { get; set; }

    /// <summary>Fecha fin del rango del reporte.</summary>
    public DateTime FechaFin { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
}
