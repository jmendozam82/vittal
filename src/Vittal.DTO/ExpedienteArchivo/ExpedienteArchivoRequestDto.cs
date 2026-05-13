using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.ExpedienteArchivo;

/// <summary>
/// Request DTO para subir un archivo al expediente médico.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class ExpedienteArchivoRequestDto
{
    /// <summary>Expediente al que pertenece el archivo (obligatorio).</summary>
    [Required(ErrorMessage = "Debe seleccionar un expediente.")]
    public Guid ExpedienteId { get; set; }

    /// <summary>Hoja de cita asociada al archivo (opcional).</summary>
    public Guid? HojaCitaId { get; set; }

    /// <summary>Nombre original del archivo (obligatorio).</summary>
    [Required(ErrorMessage = "El nombre del archivo es obligatorio.")]
    [StringLength(255, ErrorMessage = "El nombre del archivo no puede exceder 255 caracteres.")]
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>Tipo MIME del archivo (obligatorio).</summary>
    [Required(ErrorMessage = "El tipo MIME es obligatorio.")]
    [StringLength(100, ErrorMessage = "El tipo MIME no puede exceder 100 caracteres.")]
    public string TipoMime { get; set; } = string.Empty;

    /// <summary>Ruta de almacenamiento en Supabase Storage (obligatorio).</summary>
    [Required(ErrorMessage = "La ruta de almacenamiento es obligatoria.")]
    [StringLength(500, ErrorMessage = "La ruta de almacenamiento no puede exceder 500 caracteres.")]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>URL pública firmada del archivo (opcional).</summary>
    [StringLength(1000, ErrorMessage = "La URL pública no puede exceder 1000 caracteres.")]
    public string? UrlPublica { get; set; }

    /// <summary>Tamaño del archivo en bytes.</summary>
    public long? TamanoBytes { get; set; }
}
