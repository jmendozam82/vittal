using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Permiso;
using Vittal.Utility;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Servicio para verificación y gestión de permisos de perfiles sobre módulos del sistema.
/// </summary>
public interface IPermisoService
{
    /// <summary>
    /// Verifica si un perfil tiene un permiso específico para un módulo.
    /// Los administradores siempre tienen permiso (el bypass se hace en RequirePermissionAttribute).
    /// </summary>
    Task<ServiceResult<bool>> HasPermissionAsync(Guid clinicaId, Guid perfilId, string moduloClave, PermissionType tipoPermiso);

    /// <summary>
    /// Obtiene todos los permisos de un perfil, incluyendo módulos sin permiso explícito.
    /// </summary>
    Task<ServiceResult<List<PermisoResponseDto>>> GetPermisosByPerfilAsync(Guid clinicaId, Guid perfilId);

    /// <summary>
    /// Actualiza los permisos de un perfil (batch upsert).
    /// </summary>
    Task<ServiceResult<bool>> UpdatePermisosAsync(Guid clinicaId, Guid perfilId, PermisoUpdateRequestDto request, Guid usuarioId);
}
