using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interface para repositorio de clínicas. Tabla raíz: public.clinicas
/// CASO ESPECIAL: Tabla raíz multi-tenant — NO tiene ClinicaId.
/// Los métodos NO reciben clinicaId como parámetro.
/// Historia de Usuario: HU09 — Gestión de Clínicas
/// </summary>
public interface IClinicaRepository
{
    /// <summary>Lista todas las clínicas activas. Sin filtro de tenant.</summary>
    Task<IEnumerable<Clinica>> GetAllAsync();

    /// <summary>Lista TODAS las clínicas (activas + inactivas). Activas primero.</summary>
    Task<IEnumerable<Clinica>> GetAllIncludingInactiveAsync();

    /// <summary>Obtiene una clínica por ID.</summary>
    Task<Clinica?> GetByIdAsync(Guid id);

    /// <summary>Obtiene la clínica del contexto actual (app.current_clinica_id).</summary>
    Task<Clinica?> GetCurrentClinicaAsync();

    /// <summary>Inserta una nueva clínica. Retorna el ID autogenerado.</summary>
    Task<Guid> CreateAsync(Clinica clinica);

    /// <summary>Actualiza datos de la clínica.</summary>
    Task<bool> UpdateAsync(Clinica clinica);

    /// <summary>Desactiva clínica (activo = false). Nunca DELETE.</summary>
    Task<bool> DeactivateAsync(Guid id);

    /// <summary>Reactiva clínica (activo = true).</summary>
    Task<bool> ReactivateAsync(Guid id);

    /// <summary>Verifica si ya existe una clínica con el mismo nombre.</summary>
    Task<bool> ExistsByNameAsync(string nombre, Guid? excludeId = null);
}
