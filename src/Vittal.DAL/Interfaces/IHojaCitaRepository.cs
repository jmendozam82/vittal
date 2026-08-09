using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de hojas de cita médica.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaCitaRepository : IPaginatedRepository<HojaCita>
{
    /// <summary>Obtiene todas las hojas de cita activas de una clínica.</summary>
    /// <param name="clinicaId">Clínica (tenant).</param>
    /// <param name="doctorId">
    /// Si se indica (consulta de doctor), filtra por el doctor asignado al EXPEDIENTE (e.doctor_id),
    /// de modo que el doctor dueño ve todo el historial del paciente aunque la hoja la haya creado otro médico.
    /// Si es null (recepción/admin), no se filtra y se ven todas.
    /// </param>
    Task<IEnumerable<HojaCita>> GetAllAsync(Guid clinicaId, Guid? doctorId = null);

    /// <summary>Obtiene una hoja de cita por ID dentro de una clínica.</summary>
    /// <param name="clinicaId">Clínica (tenant).</param>
    /// <param name="id">Identificador de la hoja de cita.</param>
    /// <param name="doctorId">
    /// Si se indica (consulta de doctor), solo devuelve la hoja cuando el doctor es el asignado al expediente.
    /// Si es null (recepción/admin), se valida solo clínica.
    /// </param>
    Task<HojaCita?> GetByIdAsync(Guid clinicaId, Guid id, Guid? doctorId = null);

    /// <summary>Obtiene todas las hojas de cita activas de un expediente.</summary>
    /// <param name="clinicaId">Clínica (tenant).</param>
    /// <param name="expedienteId">Identificador del expediente.</param>
    /// <param name="doctorId">
    /// Si se indica (consulta de doctor), filtra por el doctor asignado al expediente (e.doctor_id),
    /// de modo que el doctor dueño ve todo el historial del paciente aunque la hoja la haya creado otro médico.
    /// Si es null (recepción/admin), no se filtra.
    /// </param>
    Task<IEnumerable<HojaCita>> GetByExpedienteIdAsync(Guid clinicaId, Guid expedienteId, Guid? doctorId = null);

    /// <summary>Crea una nueva hoja de cita y retorna su ID.</summary>
    Task<Guid> CreateAsync(HojaCita entity);

    /// <summary>Actualiza una hoja de cita existente.</summary>
    Task<bool> UpdateAsync(HojaCita entity);

    /// <summary>Desactiva una hoja de cita (activo = false). No elimina.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
