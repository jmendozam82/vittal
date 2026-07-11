using System;
using System.Collections.Generic;
using System.IO;
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

    /// <summary>
    /// Sube un archivo a Supabase Storage y crea el registro en BD.
    /// storagePath = {clinicaId}/{expedienteId}/{Guid}{extension}
    /// El BLL recibe Stream + metadata (sin dependencia de ASP.NET Core).
    /// </summary>
    Task<ServiceResult<ExpedienteArchivoResponseDto>> UploadAsync(
        Stream fileStream, string fileName, string contentType, long fileSize,
        Guid expedienteId, Guid? hojaCitaId, Guid clinicaId, Guid creadoPor);

    /// <summary>Actualiza el nombre de un archivo.</summary>
    Task<ServiceResult<ExpedienteArchivoResponseDto>> UpdateAsync(Guid id, ExpedienteArchivoRequestDto dto, Guid clinicaId);

    /// <summary>Obtiene una URL firmada temporal (3600s) para descargar el archivo.</summary>
    Task<ServiceResult<string>> GetSignedUrlAsync(Guid clinicaId, Guid id);

    /// <summary>Elimina el archivo de Supabase Storage y desactiva el registro (activo = false).</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid clinicaId, Guid id);

    /// <summary>Desactiva un archivo (activo = false). No elimina el archivo físico.</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id);
}
