using System;

namespace Vittal.DTO.ExpedienteArchivo;

/// <summary>
/// Response DTO para un archivo adjunto al expediente médico.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class ExpedienteArchivoResponseDto
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid ExpedienteId { get; set; }
    public Guid? HojaCitaId { get; set; }

    // ── Metadatos del archivo ─────────────────────────────────────
    public string NombreArchivo { get; set; } = string.Empty;
    public string TipoMime { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? UrlPublica { get; set; }
    public long? TamanoBytes { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
