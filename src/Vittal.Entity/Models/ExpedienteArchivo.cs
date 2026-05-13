namespace Vittal.Entity.Models;

/// <summary>
/// Archivo adjunto a un expediente o a una hoja de cita específica.
/// Almacena metadatos del archivo subido a Supabase Storage.
/// Tabla: public.expedientes_archivos
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class ExpedienteArchivo
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid ExpedienteId { get; set; }

    /// <summary>Hoja de cita asociada (opcional — el archivo puede ser general del expediente).</summary>
    public Guid? HojaCitaId { get; set; }

    // ── Metadatos del archivo ─────────────────────────────────────
    /// <summary>Nombre original del archivo subido.</summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>Tipo MIME del archivo (ej. "application/pdf", "image/jpeg").</summary>
    public string TipoMime { get; set; } = string.Empty;

    /// <summary>Ruta de almacenamiento en Supabase Storage.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>URL pública firmada del archivo (generada temporalmente).</summary>
    public string? UrlPublica { get; set; }

    /// <summary>Tamaño del archivo en bytes.</summary>
    public long? TamanoBytes { get; set; }

    // ── Campos de estado y auditoría ──────────────────────────────
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Usuario que subió el archivo.</summary>
    public Guid? CreadoPor { get; set; }
}
