using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Interface para repositorio de medicamentos. Tabla: public.medicamentos
/// Historia de Usuario: HU08 — Gestión de Medicamentos
/// </summary>
public interface IMedicamentoRepository
{
    Task<IEnumerable<Medicamento>> GetAllAsync(Guid clinicaId);
    Task<IEnumerable<Medicamento>> GetAllIncludingInactiveAsync(Guid clinicaId);
    Task<Medicamento?> GetByIdAsync(Guid id, Guid clinicaId);
    Task<Guid> CreateAsync(Medicamento medicamento);
    Task<bool> UpdateAsync(Medicamento medicamento);
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);
    Task<bool> ExistsByNombreAsync(Guid clinicaId, string nombre, Guid? excludeId = null);
}
