using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.TipoDiagnostico;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Interface para servicio de tipos de diagnóstico.
/// Historia de Usuario: HU13 — Gestión de Tipos de Diagnóstico
/// </summary>
public interface ITipoDiagnosticoService
{
    Task<ServiceResult<IEnumerable<TipoDiagnosticoResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);
    Task<ServiceResult<TipoDiagnosticoResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<TipoDiagnosticoResponseDto>> CreateAsync(TipoDiagnosticoRequestDto dto, Guid clinicaId, Guid creadoPor);
    Task<ServiceResult<TipoDiagnosticoResponseDto>> UpdateAsync(Guid id, TipoDiagnosticoRequestDto dto, Guid clinicaId, Guid modificadoPor);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<IEnumerable<TipoDiagnosticoResponseDto>>> SearchAsync(Guid clinicaId, string term);
}
