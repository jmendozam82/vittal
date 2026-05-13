using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para la tabla public.signos_vitales_hoja.
/// Historia de Usuario: HU-E06 — Signos Vitales por Consulta
/// </summary>
public interface ISignosVitalesHojaRepository
{
    /// <summary>
    /// Obtiene todos los signos vitales activos de una hoja de cita.
    /// </summary>
    Task<IEnumerable<SignosVitalesHoja>> GetAllAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>
    /// Obtiene un signo vital por su ID.
    /// </summary>
    Task<SignosVitalesHoja?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>
    /// Crea un nuevo registro de signo vital y retorna su ID.
    /// </summary>
    Task<Guid> CreateAsync(SignosVitalesHoja entity);

    /// <summary>
    /// Actualiza un registro de signo vital existente.
    /// </summary>
    Task<bool> UpdateAsync(SignosVitalesHoja entity);

    /// <summary>
    /// Desactiva (soft-delete) un registro de signo vital.
    /// </summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
