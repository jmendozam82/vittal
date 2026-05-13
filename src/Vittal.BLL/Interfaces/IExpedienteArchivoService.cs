using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.ExpedienteArchivo;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface para servicio de archivos adjuntos a expedientes.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IExpedienteArchivoService
{
    /// <summary>Lista todos los archivos activos de la clínica.</summary>
    Task<ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>> GetAllAsync(Guid clinicaId);

    /// <summary>Obtiene detalle de un archivo por ID.</summary>
    Task<ServiceResult<ExpedienteArchivoResponseDto>> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todos los archivos activos de un expediente.</summary>
    Task<ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>> GetByExpedienteIdAsync(Guid clinicaId, Guid expedienteId);

    /// <summary>Obtiene todos los archivos activos de una hoja de cita.</summary>
    Task<ServiceResult<IEnumerable<ExpedienteArchivoResponseDto>>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Sube un nuevo archivo al expediente.</summary>
    Task<ServiceResult<ExpedienteArchivoResponseDto>> CreateAsync(ExpedienteArchivoRequestDto dto, Guid clinicaId, Guid creadoPor);

    /// <summary>Actualiza los metadatos de un archivo.</summary>
    Task<ServiceResult<ExpedienteArchivoResponseDto>> UpdateAsync(Guid id, ExpedienteArchivoRequestDto dto, Guid clinicaId);

    /// <summary>Desactiva un archivo y lo elimina del storage físico.</summary>
    Task<ServiceResult<bool>> DeleteFromStorageAsync(Guid clinicaId, Guid id);

    /// <summary>Desactiva un archivo (activo = false). No elimina el archivo físico.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
