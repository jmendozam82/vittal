using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.HojaExamen;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de exámenes en hojas de cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaExamenService
{
    /// <summary>Obtiene detalle de un examen por ID.</summary>
    Task<ServiceResult<HojaExamenResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todos los exámenes activos de una hoja de cita.</summary>
    Task<ServiceResult<IEnumerable<HojaExamenResponseDto>>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea un nuevo examen en la hoja de cita.</summary>
    Task<ServiceResult<HojaExamenResponseDto>> CreateAsync(HojaExamenRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>Actualiza un examen existente.</summary>
    Task<ServiceResult<HojaExamenResponseDto>> UpdateAsync(Guid id, HojaExamenRequestDto dto, Guid clinicaId);

    /// <summary>Desactiva un examen (activo = false). Nunca elimina.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
