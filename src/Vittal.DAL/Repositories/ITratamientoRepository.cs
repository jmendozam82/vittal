using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Interface para repositorio de tratamientos. Tabla: public.tratamientos
/// Historia de Usuario: HU15 — Gestión de Tratamientos
/// </summary>
public interface ITratamientoRepository
{
    Task<IEnumerable<Tratamiento>> GetAllAsync(Guid clinicaId);
    Task<IEnumerable<Tratamiento>> GetAllIncludingInactiveAsync(Guid clinicaId);
    Task<Tratamiento?> GetByIdAsync(Guid id, Guid clinicaId);
    Task<Guid> CreateAsync(Tratamiento tratamiento);
    Task<bool> UpdateAsync(Tratamiento tratamiento);
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);
    Task<bool> ExistsByNombreAsync(Guid clinicaId, string nombre, Guid? excludeId = null);
}
