using System;

namespace Vittal.DTO.Reporte;

/// <summary>
/// Response DTO para un reporte generado.
/// Historia de Usuario: HU22 — Reportes
/// </summary>
public class ReporteResponseDto
{
    public Guid Id { get; set; }

    /// <summary>Nombre descriptivo del reporte.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Tipo de reporte.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Fecha de creación del reporte.</summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>Contenido del reporte serializado en JSON.</summary>
    public string ContenidoJson { get; set; } = string.Empty;

    /// <summary>Formato de exportación del reporte.</summary>
    public string Formato { get; set; } = string.Empty;
}
