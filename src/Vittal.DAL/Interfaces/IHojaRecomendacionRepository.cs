using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de recomendaciones en hojas de cita.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IHojaRecomendacionRepository
{
    /// <summary>Obtiene una recomendación de hoja de cita por ID dentro de una clínica.</summary>
    Task<HojaRecomendacion?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todas las recomendaciones activas de una hoja de cita.</summary>
    Task<IEnumerable<HojaRecomendacion>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea una nueva recomendación en la hoja de cita y retorna su ID.</summary>
    Task<Guid> CreateAsync(HojaRecomendacion entity);

    /// <summary>Actualiza una recomendación existente.</summary>
    Task<bool> UpdateAsync(HojaRecomendacion entity);

    /// <summary>Desactiva una recomendación (activo = false). No elimina.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
