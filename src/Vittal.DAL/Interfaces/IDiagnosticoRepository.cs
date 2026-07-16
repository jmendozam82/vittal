using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interface para repositorio de diagnósticos (catálogo). Tabla: public.diagnosticos
/// Historia de Usuario: HU14 — Gestión de Diagnósticos
/// </summary>
public interface IDiagnosticoRepository
{
    /// <summary>Lista todos los diagnósticos activos de una clínica (con nombre del tipo de diagnóstico).</summary>
    Task<IEnumerable<Diagnostico>> GetAllAsync(Guid clinicaId);

    /// <summary>Lista TODOS los diagnósticos (activos + inactivos) de una clínica. Ordena activos primero.</summary>
    Task<IEnumerable<Diagnostico>> GetAllIncludingInactiveAsync(Guid clinicaId);

    /// <summary>Obtiene un diagnóstico por ID validando que pertenece a la clínica.</summary>
    Task<Diagnostico?> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>Inserta un nuevo diagnóstico. Retorna el ID autogenerado.</summary>
    Task<Guid> CreateAsync(Diagnostico diagnostico);

    /// <summary>Actualiza datos del diagnóstico.</summary>
    Task<bool> UpdateAsync(Diagnostico diagnostico);

    /// <summary>Desactiva diagnóstico (activo = false). Nunca DELETE.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>Reactiva diagnóstico (activo = true).</summary>
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);

    /// <summary>
    /// Verifica si ya existe un diagnóstico con el mismo nombre en la clínica.
    /// UNIQUE (clinica_id, nombre).
    /// </summary>
    Task<bool> ExistsByNombreAsync(Guid clinicaId, string nombre, Guid? excludeId = null);

    /// <summary>Busca diagnósticos por nombre, código CIE-10 o tipo de diagnóstico.</summary>
    Task<IEnumerable<Diagnostico>> SearchAsync(Guid clinicaId, string term);
}
