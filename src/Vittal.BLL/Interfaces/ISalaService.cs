using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Sala;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Contrato de lógica de negocio para la entidad Sala.
/// Historia de Usuario: HU06 — Gestión de Salas
/// </summary>
public interface ISalaService
{
    /// <summary>
    /// Obtiene todas las salas de la clínica.
    /// Si incluirInactivos = false (default), solo retorna activas.
    /// Si incluirInactivos = true, retorna todas (activas + inactivas).
    /// </summary>
    Task<ServiceResult<IEnumerable<SalaResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false);

    /// <summary>
    /// Obtiene una sala por su ID.
    /// </summary>
    Task<ServiceResult<SalaResponseDto>> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Crea una nueva sala después de validar duplicados.
    /// </summary>
    Task<ServiceResult<SalaResponseDto>> CreateAsync(SalaRequestDto dto, Guid clinicaId);

    /// <summary>
    /// Actualiza una sala existente.
    /// </summary>
    Task<ServiceResult<SalaResponseDto>> UpdateAsync(Guid id, SalaRequestDto dto, Guid clinicaId);

    /// <summary>
    /// Desactiva una sala (activo = false).
    /// </summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Reactiva una sala desactivada (activo = true).
    /// </summary>
    Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Aplica una plantilla de especialidad a una sala.
    /// Copia los items de plantilla_items a tipos_antecedente y tipos_signo_vital.
    /// Es idempotente: si un item ya existe (mismo nombre + sala), lo salta o reactiva.
    /// Historia de Usuario: HU-E02 — Plantillas de Especialidad
    /// </summary>
    Task<ServiceResult<AplicarPlantillaResponseDto>> AplicarPlantillaAsync(
        Guid salaId, Guid plantillaId, Guid clinicaId, Guid usuarioId);
}
