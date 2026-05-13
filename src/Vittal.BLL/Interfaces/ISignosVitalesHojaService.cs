using Vittal.DTO.SignosVitalesHoja;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Servicio de lógica de negocio para signos vitales por consulta.
/// Historia de Usuario: HU-E06 — Signos Vitales por Consulta
/// </summary>
public interface ISignosVitalesHojaService
{
    /// <summary>
    /// Obtiene todos los signos vitales activos de una hoja de cita.
    /// </summary>
    Task<ServiceResult<IEnumerable<SignosVitalesHojaResponseDto>>> GetAllAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>
    /// Obtiene un signo vital por su ID.
    /// </summary>
    Task<ServiceResult<SignosVitalesHojaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>
    /// Crea un nuevo registro de signo vital.
    /// </summary>
    Task<ServiceResult<SignosVitalesHojaResponseDto>> CreateAsync(SignosVitalesHojaRequestDto dto, Guid clinicaId, Guid usuarioId);

    /// <summary>
    /// Actualiza un registro de signo vital existente.
    /// </summary>
    Task<ServiceResult<SignosVitalesHojaResponseDto>> UpdateAsync(Guid id, SignosVitalesHojaRequestDto dto, Guid clinicaId);

    /// <summary>
    /// Desactiva (soft-delete) un registro de signo vital.
    /// </summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
