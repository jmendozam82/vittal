using System;

namespace Vittal.Entity.Models;

/// <summary>
/// Catálogo de medicamentos disponibles por clínica para prescripción en expedientes.
/// Tabla: public.medicamentos
/// Historia de Usuario: HU08 — Gestión de Medicamentos
/// </summary>
public class Medicamento
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }

    // ── Datos del medicamento ─────────────────────────────────────
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Concentracion { get; set; }
    public string? UnidadMedida { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }

    // ── Propiedad calculada ───────────────────────────────────────
    /// <summary>Nombre + concentración (ej: Paracetamol 500mg).</summary>
    public string NombreCompleto =>
        string.IsNullOrEmpty(Concentracion)
            ? Nombre
            : $"{Nombre} {Concentracion}";
}
