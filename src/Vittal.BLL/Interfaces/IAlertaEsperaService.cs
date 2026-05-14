using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Alerta;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de alertas de tiempo de espera de pacientes.
/// Detecta automáticamente pacientes que exceden el tiempo máximo de espera configurado.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public interface IAlertaEsperaService
{
    /// <summary>Obtiene todas las alertas de espera de la clínica, opcionalmente filtradas.</summary>
    Task<ServiceResult<List<AlertaEsperaResponseDto>>> GetAllAsync(Guid clinicaId, bool? resuelta = null);

    /// <summary>Obtiene las alertas de espera no resueltas.</summary>
    Task<ServiceResult<List<AlertaEsperaResponseDto>>> GetNoResueltasAsync(Guid clinicaId);

    /// <summary>Resuelve una alerta de tiempo de espera manualmente.</summary>
    Task<ServiceResult<bool>> ResolverAlertaAsync(Guid clinicaId, AlertaEsperaResolveDto dto, Guid usuarioId);

    /// <summary>Verifica los tiempos de espera de todas las citas activas y genera alertas si es necesario.</summary>
    Task<ServiceResult<int>> VerificarTiemposEsperaAsync(Guid clinicaId);
}
