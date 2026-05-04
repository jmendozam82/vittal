using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Contrato de acceso a datos para la entidad Perfil.
/// Historia de Usuario: HU03 — Gestión de Perfiles
/// </summary>
public interface IPerfilRepository
{
    /// <summary>
    /// Obtiene todos los perfiles activos de la clínica especificada.
    /// </summary>
    Task<IEnumerable<Perfil>> GetAllAsync(Guid clinicaId);

    /// <summary>
    /// Obtiene un perfil por su ID validando que pertenece a la clínica.
    /// Incluye conteos de permisos y usuarios asociados.
    /// </summary>
    Task<Perfil?> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Crea un nuevo perfil y retorna el ID autogenerado por la BD.
    /// </summary>
    Task<Guid> CreateAsync(Perfil perfil);

    /// <summary>
    /// Actualiza un perfil existente. Retorna true si se actualizó.
    /// </summary>
    Task<bool> UpdateAsync(Perfil perfil);

    /// <summary>
    /// Desactiva un perfil (activo = false). NUNCA elimina.
    /// Retorna false si no existe, ya estaba inactivo, o tiene usuarios asignados.
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Verifica si existe un perfil con ese nombre en la clínica.
    /// </summary>
    Task<bool> ExistsByNameAsync(Guid clinicaId, string nombre, Guid? excludeId = null);

    /// <summary>
    /// Cuenta cuántos usuarios tienen asignado este perfil.
    /// </summary>
    Task<int> CountUsuariosAsync(Guid perfilId, Guid clinicaId);
}
