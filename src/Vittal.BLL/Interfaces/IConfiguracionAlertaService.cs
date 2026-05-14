using System;
using System.Threading.Tasks;
using Vittal.DTO.ConfiguracionAlerta;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de configuración de alertas de tiempo de espera.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public interface IConfiguracionAlertaService
{
    /// <summary>Obtiene la configuración de alertas de la clínica.</summary>
    Task<ServiceResult<ConfiguracionAlertaResponseDto>> GetAsync(Guid clinicaId);

    /// <summary>Guarda (crea o actualiza) la configuración de alertas de la clínica.</summary>
    Task<ServiceResult<ConfiguracionAlertaResponseDto>> SaveAsync(ConfiguracionAlertaRequestDto dto, Guid clinicaId, Guid usuarioId);
}
