using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para reportes generados por clínica.
/// Historia de Usuario: HU22 — Reportes
/// </summary>
public interface IReporteRepository
{
    /// <summary>Obtiene todos los reportes activos de una clínica.</summary>
    Task<IEnumerable<Reporte>> GetAllByClinicaIdAsync(Guid clinicaId);

    /// <summary>Crea un nuevo reporte y retorna su ID.</summary>
    Task<Guid> CreateAsync(Reporte entity);

    /// <summary>Obtiene un reporte por ID dentro de una clínica.</summary>
    Task<Reporte?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Desactiva un reporte (soft delete, activo = false).</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);

    /// <summary>Ejecuta una consulta dinámica basada en tipo de reporte y filtros. Retorna JSON con resultados.</summary>
    Task<string> ExecuteReportQueryAsync(string tipo, Guid clinicaId, DateTime fechaInicio, DateTime fechaFin, Guid? doctorId = null, Guid? salaId = null);
}
