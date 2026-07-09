using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.LineaTiempo;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de línea de tiempo de atención de citas.
/// Gestiona los pasos secuenciales por los que pasa un paciente durante su consulta.
/// Historia de Usuario: HU19 — Línea de Tiempo
/// </summary>
public interface ILineaTiempoService
{
    /// <summary>Obtiene la línea de tiempo completa de una cita, ordenada por orden del paso.</summary>
    Task<ServiceResult<List<LineaTiempoResponseDto>>> GetTimelineByCitaAsync(Guid clinicaId, Guid citaId);

    /// <summary>Obtiene la línea de tiempo del día para una clínica, opcionalmente filtrada por doctor.</summary>
    Task<ServiceResult<List<LineaTiempoResponseDto>>> GetTimelineDelDiaAsync(Guid clinicaId, Guid? doctorId, DateTime fecha);

    /// <summary>Inicia un paso de la línea de tiempo (cambia estado a "en_sala" y registra hora de llegada).</summary>
    Task<ServiceResult<LineaTiempoResponseDto>> IniciarPasoAsync(Guid clinicaId, Guid pasoId, Guid usuarioId);

    /// <summary>Finaliza un paso de la línea de tiempo (cambia estado a "completado" y registra hora de salida).</summary>
    Task<ServiceResult<LineaTiempoResponseDto>> FinalizarPasoAsync(Guid clinicaId, Guid pasoId, Guid usuarioId);

    /// <summary>Salta un paso de la línea de tiempo (cambia estado a "saltado" sin registrar tiempos).</summary>
    Task<ServiceResult<bool>> SaltarPasoAsync(Guid clinicaId, Guid pasoId);

    /// <summary>Genera los pasos predeterminados de línea de tiempo para una cita basados en la sala/asignación.</summary>
    Task<ServiceResult<List<LineaTiempoResponseDto>>> GenerarPasosParaCitaAsync(Guid clinicaId, Guid citaId);

    /// <summary>
    /// Fuerza la verificación y completado de una cita (cambia estado a "atendida" si todos los pasos están finalizados).
    /// Método de reparación para citas atascadas.
    /// </summary>
    Task<ServiceResult<bool>> ForzarCompletarCitaAsync(Guid clinicaId, Guid citaId);
}
