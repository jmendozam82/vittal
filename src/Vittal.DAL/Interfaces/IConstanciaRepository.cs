using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interface para repositorio de constancias médicas.
/// NOTA: No existe UpdateAsync — las constancias son documentos legales.
///       Una vez emitidas, solo se pueden anular (activo = false).
/// Historia de Usuario: HU-E07 — Constancias Médicas
/// </summary>
public interface IConstanciaRepository
{
    /// <summary>Lista todas las constancias de la clínica.
    /// Si se especifica expedienteId, filtra por ese paciente.</summary>
    Task<IEnumerable<Constancia>> GetAllAsync(Guid clinicaId, Guid? expedienteId = null);

    /// <summary>Obtiene detalle de una constancia por ID.</summary>
    Task<Constancia?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Crea una nueva constancia médica.</summary>
    Task<Guid> CreateAsync(Constancia entity);

    /// <summary>Anula una constancia (activo = false). NO elimina el registro.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
