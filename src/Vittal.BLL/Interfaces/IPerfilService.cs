using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Perfil;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Contrato de lógica de negocio para la entidad Perfil.
/// Historia de Usuario: HU03 — Gestión de Perfiles
/// </summary>
public interface IPerfilService
{
    /// <summary>
    /// Obtiene todos los perfiles de la clínica.
    /// Si incluirInactivos = false (default), solo retorna activos.
    /// Si incluirInactivos = true, retorna todos (activos + inactivos).
    /// </summary>
    Task<ServiceResult<IEnumerable<PerfilResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);

    /// <summary>
    /// Obtiene un perfil por su ID.
    /// </summary>
    Task<ServiceResult<PerfilResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Crea un nuevo perfil después de validar duplicados.
    /// </summary>
    Task<ServiceResult<PerfilResponseDto>> CreateAsync(PerfilRequestDto dto, Guid clinicaId);

    /// <summary>
    /// Actualiza un perfil existente.
    /// </summary>
    Task<ServiceResult<PerfilResponseDto>> UpdateAsync(Guid id, PerfilRequestDto dto, Guid clinicaId);

    /// <summary>
    /// Desactiva un perfil. Falla si tiene usuarios asignados.
    /// </summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Reactiva un perfil desactivado (activo = true).
    /// </summary>
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);
}
