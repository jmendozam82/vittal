using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.HojaRecomendacion;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de recomendaciones en hojas de cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaRecomendacionService
{
    /// <summary>Obtiene detalle de una recomendación por ID.</summary>
    Task<ServiceResult<HojaRecomendacionResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todas las recomendaciones activas de una hoja de cita.</summary>
    Task<ServiceResult<IEnumerable<HojaRecomendacionResponseDto>>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea una nueva recomendación en la hoja de cita.</summary>
    Task<ServiceResult<HojaRecomendacionResponseDto>> CreateAsync(HojaRecomendacionRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>Actualiza una recomendación existente.</summary>
    Task<ServiceResult<HojaRecomendacionResponseDto>> UpdateAsync(Guid id, HojaRecomendacionRequestDto dto, Guid clinicaId);

    /// <summary>Desactiva una recomendación (activo = false). Nunca elimina.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
