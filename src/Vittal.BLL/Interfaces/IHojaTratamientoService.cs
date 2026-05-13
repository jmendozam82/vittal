using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.HojaTratamiento;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de tratamientos y medicamentos en hojas de cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaTratamientoService
{
    /// <summary>Obtiene detalle de un tratamiento por ID.</summary>
    Task<ServiceResult<HojaTratamientoResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todos los tratamientos activos de una hoja de cita.</summary>
    Task<ServiceResult<IEnumerable<HojaTratamientoResponseDto>>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea un nuevo tratamiento en la hoja de cita.</summary>
    Task<ServiceResult<HojaTratamientoResponseDto>> CreateAsync(HojaTratamientoRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>Actualiza un tratamiento existente.</summary>
    Task<ServiceResult<HojaTratamientoResponseDto>> UpdateAsync(Guid id, HojaTratamientoRequestDto dto, Guid clinicaId);

    /// <summary>Desactiva un tratamiento (activo = false). Nunca elimina.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
