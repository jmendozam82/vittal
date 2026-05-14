using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Reporte;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de reportes del sistema.
/// Historia de Usuario: HU22 — Reportes
/// </summary>
public interface IReporteService
{
    /// <summary>Obtiene todos los reportes generados de la clínica.</summary>
    Task<ServiceResult<List<ReporteResponseDto>>> GetAllAsync(Guid clinicaId);

    /// <summary>Obtiene un reporte por ID.</summary>
    Task<ServiceResult<ReporteResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Genera un nuevo reporte con los filtros especificados.</summary>
    Task<ServiceResult<ReporteResponseDto>> GenerarReporteAsync(ReporteRequestDto dto, Guid clinicaId, Guid usuarioId);

    /// <summary>Exporta un reporte existente en el formato especificado.</summary>
    Task<ServiceResult<byte[]>> ExportarAsync(Guid clinicaId, Guid id, string formato);
}
