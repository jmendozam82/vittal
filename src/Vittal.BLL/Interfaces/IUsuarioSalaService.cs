using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.UsuarioSala;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Contrato de lógica de negocio para la asignación de doctores a salas/áreas.
/// Tabla: public.usuarios_salas
/// Historia de Usuario: HU06 — Asignar Doctores a Salas
/// </summary>
public interface IUsuarioSalaService
{
    /// <summary>
    /// Obtiene todas las asignaciones activas de una sala.
    /// </summary>
    Task<ServiceResult<IEnumerable<UsuarioSalaResponseDto>>> GetAllBySalaAsync(Guid clinicaId, Guid salaId);

    /// <summary>
    /// Obtiene una asignación por su ID.
    /// </summary>
    Task<ServiceResult<UsuarioSalaResponseDto?>> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Asigna un doctor a una sala.
    /// </summary>
    Task<ServiceResult<UsuarioSalaResponseDto>> CreateAsync(UsuarioSalaRequestDto dto, Guid clinicaId);

    /// <summary>
    /// Desasigna un doctor de una sala (baja lógica: activo = false).
    /// </summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);
}
