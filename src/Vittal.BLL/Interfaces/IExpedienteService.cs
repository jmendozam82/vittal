using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Expediente;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de expedientes médicos.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IExpedienteService
{
    /// <summary>
    /// Lista todos los expedientes activos de la clínica.
    /// Si doctorId no es null, filtra solo los expedientes del doctor (regla 6).
    /// </summary>
    Task<ServiceResult<IEnumerable<ExpedienteResponseDto>>> GetAllAsync(Guid clinicaId, Guid? doctorId = null);

    /// <summary>
    /// Obtiene detalle de un expediente por ID.
    /// Si doctorId no es null, valida que el expediente sea del doctor (regla 6).
    /// </summary>
    Task<ServiceResult<ExpedienteResponseDto>> GetByIdAsync(Guid clinicaId, Guid id, Guid? doctorId = null);

    /// <summary>Obtiene el expediente activo de un paciente.</summary>
    Task<ServiceResult<ExpedienteResponseDto>> GetByPacienteIdAsync(Guid clinicaId, Guid pacienteId);

    /// <summary>Crea un nuevo expediente médico.</summary>
    Task<ServiceResult<ExpedienteResponseDto>> CreateAsync(ExpedienteRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>Actualiza los datos de un expediente.</summary>
    Task<ServiceResult<ExpedienteResponseDto>> UpdateAsync(Guid id, ExpedienteRequestDto dto, Guid clinicaId);

    /// <summary>Desactiva un expediente (activo = false). Nunca elimina.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
