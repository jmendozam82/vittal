using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Examen;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Interface para servicio de exámenes.
/// Historia de Usuario: HU17 — Gestión de Exámenes
/// </summary>
public interface IExamenService
{
    Task<ServiceResult<IEnumerable<ExamenResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);
    Task<ServiceResult<ExamenResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<ExamenResponseDto>> CreateAsync(ExamenRequestDto dto, Guid clinicaId, Guid creadoPor);
    Task<ServiceResult<ExamenResponseDto>> UpdateAsync(Guid id, ExamenRequestDto dto, Guid clinicaId, Guid modificadoPor);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<IEnumerable<ExamenResponseDto>>> SearchAsync(Guid clinicaId, string term);
}
