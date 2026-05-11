using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.TipoCirugia;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de tipos de cirugía.
/// Historia de Usuario: HU11 — Gestión de Tipos de Cirugías
/// </summary>
public interface ITipoCirugiaService
{
    Task<ServiceResult<IEnumerable<TipoCirugiaResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);
    Task<ServiceResult<TipoCirugiaResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<TipoCirugiaResponseDto>> CreateAsync(TipoCirugiaRequestDto dto, Guid clinicaId, Guid creadoPor);
    Task<ServiceResult<TipoCirugiaResponseDto>> UpdateAsync(Guid id, TipoCirugiaRequestDto dto, Guid clinicaId, Guid modificadoPor);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<IEnumerable<TipoCirugiaResponseDto>>> SearchAsync(Guid clinicaId, string term);
}
