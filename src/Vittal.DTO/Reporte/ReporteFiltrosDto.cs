using System;
using System.Collections.Generic;
using Vittal.DTO;

namespace Vittal.DTO.Reporte;

/// <summary>
/// DTO con opciones de filtro disponibles para generación de reportes.
/// Historia de Usuario: HU22 — Reportes
/// </summary>
public class ReporteFiltrosDto
{
    /// <summary>Lista de doctores disponibles para filtrar.</summary>
    public List<SelectOption> Doctores { get; set; } = new();

    /// <summary>Lista de salas disponibles para filtrar.</summary>
    public List<SelectOption> Salas { get; set; } = new();

    /// <summary>Fecha mínima permitida para el rango del reporte.</summary>
    public DateTime FechaMin { get; set; }

    /// <summary>Fecha máxima permitida para el rango del reporte.</summary>
    public DateTime FechaMax { get; set; }
}
