using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interface para repositorio de tipos de cirugía. Tabla: public.tipos_cirugia
/// Historia de Usuario: HU11 — Gestión de Tipos de Cirugías
/// </summary>
public interface ITipoCirugiaRepository
{
    /// <summary>
    /// Obtiene todos los tipos de cirugía activos de la clínica especificada.
    /// </summary>
    Task<IEnumerable<TipoCirugia>> GetAllAsync(Guid clinicaId);

    /// <summary>
    /// Obtiene TODOS los tipos de cirugía (activos + inactivos) de la clínica.
    /// </summary>
    Task<IEnumerable<TipoCirugia>> GetAllIncludingInactiveAsync(Guid clinicaId);

    /// <summary>
    /// Obtiene un tipo de cirugía por su ID validando que pertenece a la clínica.
    /// </summary>
    Task<TipoCirugia?> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Crea un nuevo tipo de cirugía y retorna el ID autogenerado por la BD.
    /// </summary>
    Task<Guid> CreateAsync(TipoCirugia tipoCirugia);

    /// <summary>
    /// Actualiza un tipo de cirugía existente. Retorna true si se actualizó.
    /// </summary>
    Task<bool> UpdateAsync(TipoCirugia tipoCirugia);

    /// <summary>
    /// Desactiva un tipo de cirugía (activo = false). NUNCA elimina.
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Reactiva un tipo de cirugía (activo = true).
    /// </summary>
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Verifica si existe un tipo de cirugía con ese nombre en la clínica.
    /// </summary>
    Task<bool> ExistsByNombreAsync(Guid clinicaId, string nombre, Guid? excludeId = null);
}
