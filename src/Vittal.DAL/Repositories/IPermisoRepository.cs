using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para verificar y gestionar permisos de un perfil sobre módulos del sistema.
/// </summary>
public interface IPermisoRepository
{
    /// <summary>
    /// Verifica si un perfil tiene un permiso específico para un módulo.
    /// </summary>
    Task<(bool puedeLeer, bool puedeCrear, bool puedeActualizar)> GetPermisosAsync(
        Guid clinicaId, Guid perfilId, string moduloClave);

    /// <summary>
    /// Obtiene todos los permisos de un perfil para todos los módulos del sistema.
    /// Retorna un registro por módulo (LEFT JOIN), incluso aquellos sin permiso explícito.
    /// </summary>
    Task<IEnumerable<(Guid permisoid, Guid moduloid, string clave, string nombre, string? descripcion, bool puedeLeer, bool puedeCrear, bool puedeActualizar)>> GetPermisosByPerfilAsync(
        Guid clinicaId, Guid perfilId);

    /// <summary>
    /// Inserta o actualiza (upsert) un permiso individual para un perfil sobre un módulo.
    /// </summary>
    Task<bool> UpsertPermisoAsync(Guid clinicaId, Guid perfilId, Guid moduloId,
        bool puedeLeer, bool puedeCrear, bool puedeActualizar, Guid modificadoPor);
}
