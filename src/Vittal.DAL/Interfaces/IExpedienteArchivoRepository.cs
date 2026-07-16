using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para operaciones CRUD de archivos adjuntos a expedientes.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public interface IExpedienteArchivoRepository
{
    /// <summary>Obtiene todos los archivos activos de una clínica.</summary>
    Task<IEnumerable<ExpedienteArchivo>> GetAllAsync(Guid clinicaId);

    /// <summary>Obtiene un archivo por ID dentro de una clínica.</summary>
    Task<ExpedienteArchivo?> GetByIdAsync(Guid clinicaId, Guid id);

    /// <summary>Obtiene todos los archivos activos de un expediente.</summary>
    Task<IEnumerable<ExpedienteArchivo>> GetByExpedienteIdAsync(Guid clinicaId, Guid expedienteId);

    /// <summary>Obtiene todos los archivos activos de una hoja de cita.</summary>
    Task<IEnumerable<ExpedienteArchivo>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId);

    /// <summary>Crea un nuevo archivo y retorna su ID.</summary>
    Task<Guid> CreateAsync(ExpedienteArchivo entity);

    /// <summary>Actualiza el nombre de un archivo existente.</summary>
    Task<bool> UpdateAsync(ExpedienteArchivo entity);

    /// <summary>Desactiva un archivo (activo = false). No elimina el archivo físico.</summary>
    Task<bool> DeactivateAsync(Guid clinicaId, Guid id);
}
