using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Interface para repositorio de recomendaciones. Tabla: public.recomendaciones
/// Historia de Usuario: HU16 — Gestión de Recomendaciones
/// </summary>
public interface IRecomendacionRepository
{
    Task<IEnumerable<Recomendacion>> GetAllAsync(Guid clinicaId);
    Task<IEnumerable<Recomendacion>> GetAllIncludingInactiveAsync(Guid clinicaId);
    Task<Recomendacion?> GetByIdAsync(Guid id, Guid clinicaId);
    Task<Guid> CreateAsync(Recomendacion recomendacion);
    Task<bool> UpdateAsync(Recomendacion recomendacion);
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);
    Task<bool> ExistsByNombreAsync(Guid clinicaId, string nombre, Guid? excludeId = null);
}
