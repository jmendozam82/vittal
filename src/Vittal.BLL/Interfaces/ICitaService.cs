using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Cita;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de citas médicas.
/// Historia de Usuario: HU21 — Agenda (HU-E01 — hora_fin)
/// </summary>
public interface ICitaService
{
    /// <summary>
    /// Lista todas las citas activas de la clínica.
    /// Si doctorId no es null (usuario doctor), filtra por el doctor de la cita (c.doctor_id).
    /// </summary>
    Task<ServiceResult<IEnumerable<CitaResponseDto>>> GetAllAsync(Guid clinicaId, Guid? doctorId = null);

    /// <summary>Obtiene detalle de una cita por ID.</summary>
    Task<ServiceResult<CitaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Crea una nueva cita médica.</summary>
    Task<ServiceResult<CitaResponseDto>> CreateAsync(CitaRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>
    /// Actualiza los datos de una cita.
    /// <paramref name="modificadoPor"/> es el ID del usuario autenticado que ejecuta la acción (auditoría y reasignación de médico del paciente).
    /// </summary>
    Task<ServiceResult<CitaResponseDto>> UpdateAsync(Guid id, CitaRequestDto dto, Guid clinicaId, Guid modificadoPor);

    /// <summary>Desactiva una cita (activo = false). Nunca elimina.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
