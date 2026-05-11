using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Recomendacion;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Interface para servicio de recomendaciones.
/// Historia de Usuario: HU16 — Gestión de Recomendaciones
/// </summary>
public interface IRecomendacionService
{
    Task<ServiceResult<IEnumerable<RecomendacionResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);
    Task<ServiceResult<RecomendacionResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<RecomendacionResponseDto>> CreateAsync(RecomendacionRequestDto dto, Guid clinicaId, Guid creadoPor);
    Task<ServiceResult<RecomendacionResponseDto>> UpdateAsync(Guid id, RecomendacionRequestDto dto, Guid clinicaId, Guid modificadoPor);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<IEnumerable<RecomendacionResponseDto>>> SearchAsync(Guid clinicaId, string term);
}
