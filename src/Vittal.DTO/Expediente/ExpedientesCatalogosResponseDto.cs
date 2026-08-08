using System;
using System.Collections.Generic;
using Vittal.DTO.Catalogos;
using Vittal.DTO.Cirugia;
using Vittal.DTO.Diagnostico;
using Vittal.DTO.Examen;
using Vittal.DTO.Medicamento;
using Vittal.DTO.Recomendacion;
using Vittal.DTO.Tratamiento;

namespace Vittal.DTO.Expediente;

/// <summary>
/// Response DTO con los catálogos necesarios para la pantalla de expedientes.
/// Agrupa los datos maestros (diagnósticos, medicamentos, tratamientos, etc.)
/// que se cargan junto con la hoja de cita para alimentar los combos y
/// selecciones del expediente clínico.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class ExpedientesCatalogosResponseDto
{
    /// <summary>Catálogo de diagnósticos de la clínica.</summary>
    public List<DiagnosticoResponseDto> Diagnosticos { get; set; } = new();

    /// <summary>Catálogo de medicamentos de la clínica.</summary>
    public List<MedicamentoResponseDto> Medicamentos { get; set; } = new();

    /// <summary>Catálogo de tratamientos de la clínica.</summary>
    public List<TratamientoResponseDto> Tratamientos { get; set; } = new();

    /// <summary>Catálogo de recomendaciones de la clínica.</summary>
    public List<RecomendacionResponseDto> Recomendaciones { get; set; } = new();

    /// <summary>Catálogo de cirugías de la clínica.</summary>
    public List<CirugiaResponseDto> Cirugias { get; set; } = new();

    /// <summary>Catálogo de exámenes de la clínica.</summary>
    public List<ExamenResponseDto> Examenes { get; set; } = new();

    /// <summary>Catálogo de tipos de signo vital de la clínica.</summary>
    public List<TipoSignoVitalDTOs.Response> TiposSignoVital { get; set; } = new();

    /// <summary>Catálogo de tipos de antecedente de la clínica.</summary>
    public List<TipoAntecedenteDTOs.Response> TiposAntecedente { get; set; } = new();
}
