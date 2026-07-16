using Vittal.DTO.Shared;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interfaz opcional para repositorios que requieren paginación.
/// Los repositorios que implementan esta interfaz exponen GetAllPaginatedAsync
/// además del GetAllAsync estándar (sin paginación).
/// </summary>
/// <typeparam name="TEntity">Tipo de la entidad (ej: Paciente, Cita).</typeparam>
public interface IPaginatedRepository<TEntity>
{
    /// <summary>
    /// Obtiene una página de registros activos de la clínica, con filtro,
    /// ordenamiento y paginación. Retorna elementos + total count.
    /// </summary>
    Task<PaginatedResultDto<TEntity>> GetAllPaginatedAsync(Guid clinicaId, PaginationFilterDto filter);
}
