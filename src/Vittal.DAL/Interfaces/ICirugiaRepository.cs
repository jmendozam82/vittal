using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interface para repositorio de cirugías. Tabla: public.cirugias
/// Historia de Usuario: HU12 — Gestión de Cirugías
/// </summary>
public interface ICirugiaRepository
{
    /// <summary>Lista todas las cirugías activas de una clínica (con nombre del tipo de cirugía).</summary>
    Task<IEnumerable<Cirugia>> GetAllAsync(Guid clinicaId);

    /// <summary>Lista TODAS las cirugías (activas + inactivas) de una clínica. Ordena activos primero.</summary>
    Task<IEnumerable<Cirugia>> GetAllIncludingInactiveAsync(Guid clinicaId);

    /// <summary>Obtiene una cirugía por ID validando que pertenece a la clínica.</summary>
    Task<Cirugia?> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>Inserta una nueva cirugía. Retorna el ID autogenerado.</summary>
    Task<Guid> CreateAsync(Cirugia cirugia);

    /// <summary>Actualiza datos de la cirugía.</summary>
    Task<bool> UpdateAsync(Cirugia cirugia);

    /// <summary>Desactiva cirugía (activo = false). Nunca DELETE.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>Reactiva cirugía (activo = true).</summary>
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);

    /// <summary>Verifica si ya existe una cirugía con el mismo nombre en la clínica.</summary>
    Task<bool> ExistsByNombreAsync(Guid clinicaId, string nombre, Guid? excludeId = null);
}
