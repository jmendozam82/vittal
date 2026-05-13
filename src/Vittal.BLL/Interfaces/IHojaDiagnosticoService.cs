using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.HojaDiagnostico;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de diagnósticos en hojas de cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaDiagnosticoService
{
    /// <summary>Obtiene detalle de un diagnóstico por ID.</summary>
    Task<ServiceResult<HojaDiagnosticoResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todos los diagnósticos activos de una hoja de cita.</summary>
    Task<ServiceResult<IEnumerable<HojaDiagnosticoResponseDto>>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea un nuevo diagnóstico en la hoja de cita.</summary>
    Task<ServiceResult<HojaDiagnosticoResponseDto>> CreateAsync(HojaDiagnosticoRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>Actualiza un diagnóstico existente.</summary>
    Task<ServiceResult<HojaDiagnosticoResponseDto>> UpdateAsync(Guid id, HojaDiagnosticoRequestDto dto, Guid clinicaId);

    /// <summary>Desactiva un diagnóstico (activo = false). Nunca elimina.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
