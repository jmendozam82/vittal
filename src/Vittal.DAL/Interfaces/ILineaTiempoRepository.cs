using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones de la línea de tiempo de atención de citas.
/// Historia de Usuario: HU19 — Línea de Tiempo
/// </summary>
public interface ILineaTiempoRepository
{
    /// <summary>Obtiene todos los pasos de la línea de tiempo para una cita específica, ordenados por orden.</summary>
    Task<IEnumerable<LineaTiempo>> GetByCitaIdAsync(Guid clinicaId, Guid citaId);

    /// <summary>Obtiene los pasos de línea de tiempo de una clínica para una fecha y doctor opcional.</summary>
    Task<IEnumerable<LineaTiempo>> GetByClinicaAndDateAsync(Guid clinicaId, Guid? doctorId, DateTime fecha);

    /// <summary>Crea un nuevo paso de línea de tiempo y retorna su ID.</summary>
    Task<Guid> CreateAsync(LineaTiempo entity);

    /// <summary>Actualiza el estado de un paso y su hora correspondiente.</summary>
    Task<bool> UpdateEstadoAsync(Guid clinicaId, Guid id, string estado, TimeSpan? hora);

    /// <summary>
    /// Resetea un paso a estado "pendiente" limpiando las horas de llegada/salida.
    /// Se usa cuando la hoja de cita no se crea y la cita vuelve a la cola (en_espera).
    /// </summary>
    Task<bool> ResetearEstadoAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene un paso de línea de tiempo por ID.</summary>
    Task<LineaTiempo?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todos los pasos de línea de tiempo de una clínica.</summary>
    Task<IEnumerable<LineaTiempo>> GetAllAsync(Guid clinicaId);
}
