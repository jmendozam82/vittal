using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interface para repositorio de pacientes. Tabla: public.pacientes
/// Historia de Usuario: HU07 — Gestión de Pacientes
/// </summary>
public interface IPacienteRepository
{
    /// <summary>Lista todos los pacientes activos de una clínica (con nombre del doctor).</summary>
    Task<IEnumerable<Paciente>> GetAllAsync(Guid clinicaId);

    /// <summary>Lista TODOS los pacientes (activos + inactivos) de una clínica. Ordena activos primero.</summary>
    Task<IEnumerable<Paciente>> GetAllIncludingInactiveAsync(Guid clinicaId);

    /// <summary>Obtiene un paciente por ID validando que pertenece a la clínica.</summary>
    Task<Paciente?> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>Inserta un nuevo paciente. Retorna el ID autogenerado.</summary>
    Task<Guid> CreateAsync(Paciente paciente);

    /// <summary>Actualiza datos del paciente.</summary>
    Task<bool> UpdateAsync(Paciente paciente);

    /// <summary>Desactiva paciente (activo = false). Nunca DELETE.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>Reactiva paciente (activo = true).</summary>
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);

    /// <summary>Verifica si ya existe un paciente con el mismo email en la clínica.</summary>
    Task<bool> ExistsByEmailAsync(Guid clinicaId, string email, Guid? excludeId = null);

    /// <summary>Verifica si ya existe un paciente con el mismo celular en la clínica.</summary>
    Task<bool> ExistsByCelularAsync(Guid clinicaId, string celular, Guid? excludeId = null);

    /// <summary>Verifica si ya existe un número de documento de identificación en la clínica. excludeId para ignorar el mismo paciente en update.</summary>
    Task<bool> ExistsByNumeroDocumentoAsync(Guid clinicaId, string numeroDocumento, Guid? excludeId);
}
