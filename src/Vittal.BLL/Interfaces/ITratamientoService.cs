using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Tratamiento;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de tratamientos.
/// Historia de Usuario: HU15 — Gestión de Tratamientos
/// </summary>
public interface ITratamientoService
{
    Task<ServiceResult<IEnumerable<TratamientoResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);
    Task<ServiceResult<TratamientoResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<TratamientoResponseDto>> CreateAsync(TratamientoRequestDto dto, Guid clinicaId, Guid creadoPor);
    Task<ServiceResult<TratamientoResponseDto>> UpdateAsync(Guid id, TratamientoRequestDto dto, Guid clinicaId, Guid modificadoPor);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<IEnumerable<TratamientoResponseDto>>> SearchAsync(Guid clinicaId, string term);
}
