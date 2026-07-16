using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.UsuarioSala;
using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos para la asignación de doctores a salas.
/// Tabla: public.usuarios_salas
/// Historia de Usuario: HU06 — Asignar Doctores a Salas
/// </summary>
public interface IUsuarioSalaRepository
{
    /// <summary>Obtiene todas las asignaciones activas de una sala.</summary>
    Task<IEnumerable<UsuarioSalaResponseDto>> GetBySalaAsync(Guid clinicaId, Guid salaId);

    /// <summary>Obtiene una asignación por su ID validando la clínica.</summary>
    Task<UsuarioSalaResponseDto?> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>Crea una nueva asignación doctor-sala y retorna el ID autogenerado.</summary>
    Task<Guid> CreateAsync(UsuarioSala entity);

    /// <summary>Desasigna un doctor de una sala (baja lógica: activo = false).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);
}
