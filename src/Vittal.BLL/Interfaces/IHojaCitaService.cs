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
    /// <summary>
    /// Lista todas las hojas de cita activas de la clínica.
    /// Si doctorId no es null (usuario doctor), se filtra por el doctor asignado
    /// al EXPEDIENTE (e.doctor_id), de modo que el doctor dueño del paciente ve
    /// todo el historial del expediente (incluso hojas creadas por otros médicos).
    /// </summary>
    Task<ServiceResult<IEnumerable<HojaCitaResponseDto>>> GetAllAsync(Guid clinicaId, Guid? doctorId = null);

    /// <summary>
    /// Obtiene detalle de una hoja de cita por ID.
    /// Si doctorId no es null, solo devuelve la hoja cuando el doctor es el asignado al expediente.
    /// </summary>
    Task<ServiceResult<HojaCitaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id, Guid? doctorId = null);

    /// <summary>
    /// Obtiene todas las hojas de cita activas de un expediente.
    /// Si doctorId no es null, filtra por el doctor asignado al expediente (e.doctor_id).
    /// </summary>
    Task<ServiceResult<IEnumerable<HojaCitaResponseDto>>> GetByExpedienteIdAsync(Guid clinicaId, Guid expedienteId, Guid? doctorId = null);

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
