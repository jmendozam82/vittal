using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Interface para repositorio de diagnósticos de citas. Tabla: public.diagnosticos
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
    /// Verifica si ya existe un diagnóstico con la misma cita y tipo de diagnóstico en la clínica.
    /// UNIQUE (clinica_id, cita_id, tipo_diagnostico_id).
    /// </summary>
    Task<bool> ExistsByDiagnosticoAsync(Guid clinicaId, Guid citaId, Guid tipoDiagnosticoId, Guid? excludeId = null);
}
