using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.HojaCita;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de hojas de cita médica.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaCitaService
{
    /// <summary>Lista todas las hojas de cita activas de la clínica.</summary>
    Task<ServiceResult<IEnumerable<HojaCitaResponseDto>>> GetAllAsync(Guid clinicaId);

    /// <summary>Obtiene detalle de una hoja de cita por ID.</summary>
    Task<ServiceResult<HojaCitaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todas las hojas de cita activas de un expediente.</summary>
    Task<ServiceResult<IEnumerable<HojaCitaResponseDto>>> GetByExpedienteIdAsync(Guid clinicaId, Guid expedienteId);

    /// <summary>Crea una nueva hoja de cita.</summary>
    Task<ServiceResult<HojaCitaResponseDto>> CreateAsync(HojaCitaRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>Actualiza los datos de una hoja de cita.</summary>
    Task<ServiceResult<HojaCitaResponseDto>> UpdateAsync(Guid id, HojaCitaRequestDto dto, Guid clinicaId);

    /// <summary>Desactiva una hoja de cita (activo = false). Nunca elimina.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);

    /// <summary>
    /// Determina si la consulta asociada a una hoja de cita ya fue finalizada (estado 'atendida').
    /// Las hojas finalizadas no deben admitir modificaciones (integridad clínica).
    /// </summary>
    Task<bool> EstaFinalizadaAsync(Guid clinicaId, Guid hojaCitaId);
}
