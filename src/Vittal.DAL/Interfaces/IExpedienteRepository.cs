using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de expedientes médicos.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IExpedienteRepository : IPaginatedRepository<Expediente>
{
    /// <summary>
    /// Obtiene todos los expedientes activos de una clínica.
    /// Si se indica doctorId, filtra solo los expedientes asignados a ese doctor
    /// (regla 6: el doctor solo ve sus pacientes). Admin/SuperAdmin pasan null.
    /// </summary>
    Task<IEnumerable<Expediente>> GetAllAsync(Guid clinicaId, Guid? doctorId = null);

    /// <summary>
    /// Obtiene un expediente por ID dentro de una clínica.
    /// Si se indica doctorId, valida que el expediente pertenezca a ese doctor
    /// (regla 6). Admin/SuperAdmin pasan null.
    /// </summary>
    Task<Expediente?> GetByIdAsync(Guid clinicaId, Guid id, Guid? doctorId = null);

    /// <summary>Obtiene el expediente activo de un paciente dentro de una clínica.</summary>
    Task<Expediente?> GetByPacienteIdAsync(Guid clinicaId, Guid pacienteId);

    /// <summary>Crea un nuevo expediente y retorna su ID.</summary>
    Task<Guid> CreateAsync(Expediente entity);

    /// <summary>Actualiza un expediente existente.</summary>
    Task<bool> UpdateAsync(Expediente entity);

    /// <summary>Desactiva un expediente (activo = false). No elimina.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
