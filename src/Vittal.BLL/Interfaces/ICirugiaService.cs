using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Cirugia;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de cirugías.
/// Historia de Usuario: HU12 — Gestión de Cirugías
/// </summary>
public interface ICirugiaService
{
    Task<ServiceResult<IEnumerable<CirugiaResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);
    Task<ServiceResult<CirugiaResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<CirugiaResponseDto>> CreateAsync(CirugiaRequestDto dto, Guid clinicaId, Guid creadoPor);
    Task<ServiceResult<CirugiaResponseDto>> UpdateAsync(Guid id, CirugiaRequestDto dto, Guid clinicaId, Guid modificadoPor);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<IEnumerable<CirugiaResponseDto>>> SearchAsync(Guid clinicaId, string term);
}
