using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Interface para repositorio de tipos de diagnóstico. Tabla: public.tipos_diagnostico
/// Historia de Usuario: HU13 — Gestión de Tipos de Diagnóstico
/// </summary>
public interface ITipoDiagnosticoRepository
{
    /// <summary>
    /// Obtiene todos los tipos de diagnóstico activos de la clínica especificada.
    /// </summary>
    Task<IEnumerable<TipoDiagnostico>> GetAllAsync(Guid clinicaId);

    /// <summary>
    /// Obtiene TODOS los tipos de diagnóstico (activos + inactivos) de la clínica.
    /// </summary>
    Task<IEnumerable<TipoDiagnostico>> GetAllIncludingInactiveAsync(Guid clinicaId);

    /// <summary>
    /// Obtiene un tipo de diagnóstico por su ID validando que pertenece a la clínica.
    /// </summary>
    Task<TipoDiagnostico?> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Crea un nuevo tipo de diagnóstico y retorna el ID autogenerado por la BD.
    /// </summary>
    Task<Guid> CreateAsync(TipoDiagnostico tipoDiagnostico);

    /// <summary>
    /// Actualiza un tipo de diagnóstico existente. Retorna true si se actualizó.
    /// </summary>
    Task<bool> UpdateAsync(TipoDiagnostico tipoDiagnostico);

    /// <summary>
    /// Desactiva un tipo de diagnóstico (activo = false). NUNCA elimina.
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Reactiva un tipo de diagnóstico (activo = true).
    /// </summary>
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Verifica si existe un tipo de diagnóstico con ese nombre en la clínica.
    /// </summary>
    Task<bool> ExistsByNombreAsync(Guid clinicaId, string nombre, Guid? excludeId = null);
}
