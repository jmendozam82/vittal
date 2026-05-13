using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Constancia;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de constancias médicas.
/// Las constancias son documentos legales: NO se pueden editar después de emitidas.
/// Solo se pueden crear y anular (no eliminar).
/// Historia de Usuario: HU-E07 — Constancias Médicas
/// </summary>
public interface IConstanciaService
{
    /// <summary>Lista constancias de la clínica.
    /// Opcionalmente filtra por expediente de un paciente específico.</summary>
    Task<ServiceResult<IEnumerable<ConstanciaResponseDto>>> GetAllAsync(Guid clinicaId, Guid? expedienteId = null);

    /// <summary>Detalle de una constancia por ID.</summary>
    Task<ServiceResult<ConstanciaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Crea/Emite una nueva constancia médica.</summary>
    Task<ServiceResult<ConstanciaResponseDto>> CreateAsync(ConstanciaRequestDto dto, Guid clinicaId, Guid usuarioId);

    /// <summary>Anula una constancia (activo = false).
    /// Las constancias son documentos legales, no se eliminan ni editan.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
