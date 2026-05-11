using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Clinica;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Interface para servicio de clínicas.
/// CASO ESPECIAL: Tabla raíz multi-tenant — no recibe clinicaId.
/// Historia de Usuario: HU09 — Gestión de Clínicas
/// </summary>
public interface IClinicaService
{
    Task<ServiceResult<IEnumerable<ClinicaResponseDto>>> GetAllAsync(bool incluirInactivos = false);
    Task<ServiceResult<ClinicaResponseDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<ClinicaResponseDto>> GetCurrentClinicaAsync();
    Task<ServiceResult<ClinicaResponseDto>> CreateAsync(ClinicaRequestDto dto);
    Task<ServiceResult<ClinicaResponseDto>> UpdateAsync(Guid id, ClinicaRequestDto dto);
    Task<ServiceResult<bool>> DeactivateAsync(Guid id);
    Task<ServiceResult<bool>> ReactivateAsync(Guid id);
}
