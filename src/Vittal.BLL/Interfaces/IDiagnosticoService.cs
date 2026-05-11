using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Diagnostico;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de diagnósticos.
/// Historia de Usuario: HU14 — Gestión de Diagnósticos
/// </summary>
public interface IDiagnosticoService
{
    Task<ServiceResult<IEnumerable<DiagnosticoResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);
    Task<ServiceResult<DiagnosticoResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<DiagnosticoResponseDto>> CreateAsync(DiagnosticoRequestDto dto, Guid clinicaId, Guid creadoPor);
    Task<ServiceResult<DiagnosticoResponseDto>> UpdateAsync(Guid id, DiagnosticoRequestDto dto, Guid clinicaId, Guid modificadoPor);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<IEnumerable<DiagnosticoResponseDto>>> SearchAsync(Guid clinicaId, string term);
}
