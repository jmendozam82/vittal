using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Medicamento;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Interface para servicio de medicamentos.
/// Historia de Usuario: HU08 — Gestión de Medicamentos
/// </summary>
public interface IMedicamentoService
{
    Task<ServiceResult<IEnumerable<MedicamentoResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);
    Task<ServiceResult<MedicamentoResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<MedicamentoResponseDto>> CreateAsync(MedicamentoRequestDto dto, Guid clinicaId, Guid creadoPor);
    Task<ServiceResult<MedicamentoResponseDto>> UpdateAsync(Guid id, MedicamentoRequestDto dto, Guid clinicaId, Guid modificadoPor);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);
    Task<ServiceResult<IEnumerable<MedicamentoResponseDto>>> SearchAsync(Guid clinicaId, string term);
}
