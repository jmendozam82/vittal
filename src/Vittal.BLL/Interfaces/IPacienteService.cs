using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Paciente;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de pacientes.
/// Historia de Usuario: HU07 — Gestión de Pacientes
/// </summary>
public interface IPacienteService
{
    /// <summary>Lista pacientes de la clínica. Por defecto solo activos.</summary>
    Task<ServiceResult<IEnumerable<PacienteResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);

    /// <summary>Detalle de un paciente por ID.</summary>
    Task<ServiceResult<PacienteResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>Crea un nuevo paciente.</summary>
    Task<ServiceResult<PacienteResponseDto>> CreateAsync(PacienteRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>Actualiza datos del paciente.</summary>
    Task<ServiceResult<PacienteResponseDto>> UpdateAsync(Guid id, PacienteRequestDto dto, Guid clinicaId, Guid modificadoPor);

    /// <summary>Desactiva paciente (activo = false). Nunca elimina.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>Reactiva paciente (activo = true).</summary>
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);

    /// <summary>Búsqueda de pacientes por término (nombre, email, celular).</summary>
    Task<ServiceResult<IEnumerable<PacienteResponseDto>>> SearchAsync(Guid clinicaId, string term);

    /// <summary>Lista pacientes activos asignados a un doctor de la clínica.</summary>
    Task<ServiceResult<IEnumerable<PacienteResponseDto>>> GetByDoctorAsync(Guid clinicaId, Guid doctorId);
}
