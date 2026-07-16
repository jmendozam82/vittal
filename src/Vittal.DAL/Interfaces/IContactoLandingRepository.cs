using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interface del repositorio para contactos de landing.
/// Tabla global del sistema (sin clinica_id) — solo Super Admin gestiona.
/// Excepción a CLAUDE.md §12: no requiere clinica_id al ser global.
/// Historia de Usuario: HU-L01 — Landing Page Informativa
/// </summary>
public interface IContactoLandingRepository
{
    /// <summary>Crea un nuevo contacto de landing y retorna su ID</summary>
    Task<Guid> CreateAsync(ContactoLanding contacto);

    /// <summary>Obtiene un contacto por ID (solo activos)</summary>
    Task<ContactoLanding?> GetByIdAsync(Guid id);

    /// <summary>Obtiene todos los contactos activos ordenados por fecha de creación descendente</summary>
    Task<IEnumerable<ContactoLanding>> GetAllAsync();

    /// <summary>Marca un contacto como leído por el admin</summary>
    Task<bool> MarkAsReadAsync(Guid id);

    /// <summary>Desactiva un contacto (no elimina) — CLAUDE.md regla #1: solo deactivate</summary>
    Task<bool> DeactivateAsync(Guid id);
}
