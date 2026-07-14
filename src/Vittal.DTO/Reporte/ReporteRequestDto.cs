using System;

namespace Vittal.DTO.Reporte;

/// <summary>
/// Request DTO para generar un nuevo reporte.
/// Historia de Usuario: HU22 — Reportes
/// </summary>
public class ReporteRequestDto
{
    /// <summary>
    /// Tipo de reporte: pacientes_por_dia | citas_por_estado | doctores_mas_activos | tiempo_promedio_espera |
    ///   tiempos_espera | citas_atendidas | pacientes_atendidos | ingresos | historial_citas | cirugias | examenes.
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Fecha inicio del rango del reporte.</summary>
    public DateTime FechaInicio { get; set; }

    /// <summary>Fecha fin del rango del reporte.</summary>
    public DateTime FechaFin { get; set; }

    /// <summary>Filtro opcional por doctor.</summary>
    public Guid? DoctorId { get; set; }

    /// <summary>Filtro opcional por sala.</summary>
    public Guid? SalaId { get; set; }

    /// <summary>
    /// Formato de exportación: pdf | excel | csv | json.
    /// Por defecto: pdf.
    /// </summary>
    public string Formato { get; set; } = "pdf";
}
