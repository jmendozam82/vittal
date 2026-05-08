using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Contrato de acceso a datos para la entidad Sala.
/// Historia de Usuario: HU06 — Gestión de Salas
/// </summary>
public interface ISalaRepository
{
    /// <summary>
    /// Obtiene todas las salas activas de la clínica especificada.
    /// </summary>
    Task<IEnumerable<Sala>> GetAllAsync(Guid clinicaId);

    /// <summary>
    /// Obtiene TODAS las salas (activas + inactivas) de la clínica. Ordena activas primero.
    /// </summary>
    Task<IEnumerable<Sala>> GetAllIncludingInactiveAsync(Guid clinicaId);

    /// <summary>
    /// Obtiene una sala por su ID validando que pertenece a la clínica.
    /// </summary>
    Task<Sala?> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Crea una nueva sala y retorna el ID autogenerado por la BD.
    /// </summary>
    Task<Guid> CreateAsync(Sala sala);

    /// <summary>
    /// Actualiza una sala existente. Retorna true si se actualizó.
    /// </summary>
    Task<bool> UpdateAsync(Sala sala);

    /// <summary>
    /// Desactiva una sala (activo = false). NUNCA elimina.
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Reactiva una sala (activo = true).
    /// </summary>
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Verifica si existe una sala con ese nombre en la clínica.
    /// </summary>
    Task<bool> ExistsByNameAsync(Guid clinicaId, string nombre, Guid? excludeId = null);
}
