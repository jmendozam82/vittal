using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de citas médicas.
/// Historia de Usuario: HU21 — Agenda (HU-E01 — hora_fin)
/// </summary>
public interface ICitaRepository : IPaginatedRepository<Cita>
{
    /// <summary>Obtiene todas las citas activas de una clínica.</summary>
    Task<IEnumerable<Cita>> GetAllAsync(Guid clinicaId);

    /// <summary>Obtiene una cita por ID dentro de una clínica.</summary>
    Task<Cita?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Crea una nueva cita y retorna su ID.</summary>
    Task<Guid> CreateAsync(Cita entity);

    /// <summary>Actualiza una cita existente.</summary>
    Task<bool> UpdateAsync(Cita entity);

    /// <summary>Desactiva una cita (activo = false). No elimina.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene citas en estado 'en_espera' y activas para una clínica.</summary>
    Task<IEnumerable<Cita>> GetCitasEnEsperaAsync(Guid clinicaId);

    /// <summary>
    /// Verifica si existe una cita activa que se solape con el rango horario solicitado
    /// para el MISMO doctor en la misma fecha. Permite citas simultáneas de doctores
    /// distintos (varias salas atendiendo a la vez), pero no del mismo doctor.
    /// </summary>
    /// <param name="clinicaId">Clínica (tenant).</param>
    /// <param name="doctorId">Doctor cuyo horario se verifica.</param>
    /// <param name="fechaCita">Fecha de la cita.</param>
    /// <param name="horaCita">Hora de inicio.</param>
    /// <param name="horaFin">Hora de fin (opcional; si es null se asume 30 min de duración).</param>
    /// <param name="excluirId">ID de cita a excluir (útil al actualizar para no chocar consigo misma).</param>
    Task<bool> ExisteCitaSolapadaAsync(
        Guid clinicaId, Guid doctorId, DateOnly fechaCita,
        TimeOnly horaCita, TimeOnly? horaFin, Guid? excluirId = null);

}
