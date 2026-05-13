using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.HojaCirugia;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de cirugías en hojas de cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaCirugiaService
{
    /// <summary>Obtiene detalle de una cirugía por ID.</summary>
    Task<ServiceResult<HojaCirugiaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todas las cirugías activas de una hoja de cita.</summary>
    Task<ServiceResult<IEnumerable<HojaCirugiaResponseDto>>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea una nueva cirugía en la hoja de cita.</summary>
    Task<ServiceResult<HojaCirugiaResponseDto>> CreateAsync(HojaCirugiaRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>Actualiza una cirugía existente.</summary>
    Task<ServiceResult<HojaCirugiaResponseDto>> UpdateAsync(Guid id, HojaCirugiaRequestDto dto, Guid clinicaId);

    /// <summary>Desactiva una cirugía (activo = false). Nunca elimina.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
