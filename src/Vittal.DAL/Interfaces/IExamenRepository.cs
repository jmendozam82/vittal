using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interface para repositorio de exámenes. Tabla: public.examenes
/// Historia de Usuario: HU17 — Gestión de Exámenes
/// </summary>
public interface IExamenRepository
{
    Task<IEnumerable<Examen>> GetAllAsync(Guid clinicaId);
    Task<IEnumerable<Examen>> GetAllIncludingInactiveAsync(Guid clinicaId);
    Task<Examen?> GetByIdAsync(Guid id, Guid clinicaId);
    Task<Guid> CreateAsync(Examen examen);
    Task<bool> UpdateAsync(Examen examen);
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);
    Task<bool> ExistsByNombreAsync(Guid clinicaId, string nombre, Guid? excludeId = null);
}
