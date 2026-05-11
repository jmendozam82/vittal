using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Usuario;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de usuarios.
/// </summary>
public interface IUsuarioService
{
    /// <summary>Obtiene usuario autenticado por su ID de Supabase Auth.</summary>
    Task<ServiceResult<UsuarioResponseDto>> GetByAuthUserIdAsync(Guid authUserId);

    /// <summary>Lista usuarios de la clínica. Por defecto solo activos.</summary>
    Task<ServiceResult<IEnumerable<UsuarioResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);

    /// <summary>Detalle de un usuario por ID.</summary>
    Task<ServiceResult<UsuarioResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Crea un usuario nuevo.
    /// Flujo: 1) Supabase Auth signup (email_confirm: true), 2) Insert en BD.
    /// Si el paso 2 falla, se revierte el signup en Auth.
    /// </summary>
    Task<ServiceResult<UsuarioResponseDto>> CreateAsync(UsuarioRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>
    /// Actualiza datos del usuario. Si Password viene populated, actualiza en Supabase Auth.
    /// </summary>
    Task<ServiceResult<UsuarioResponseDto>> UpdateAsync(Guid id, UsuarioRequestDto dto, Guid clinicaId, Guid modificadoPor);

    /// <summary>
    /// Desactiva usuario (activo = false) y lo banea en Supabase Auth.
    /// Valida que no tenga expedientes/citas activas antes de desactivar.
    /// </summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Reactiva usuario (activo = true) y desbanea en Supabase Auth.
    /// </summary>
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);

    /// <summary>Lista solo doctores activos de la clínica (para dropdowns).</summary>
    Task<ServiceResult<IEnumerable<UsuarioResponseDto>>> GetDoctoresAsync(Guid clinicaId);
}
