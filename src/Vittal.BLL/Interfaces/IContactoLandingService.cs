using Vittal.DTO.ContactoLanding;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interface del servicio BLL para contactos de landing.
/// Siguiendo patrón existente: retorna ServiceResult&lt;T&gt;.
/// Historia de Usuario: HU-L01 — Landing Page Informativa
/// </summary>
public interface IContactoLandingService
{
    /// <summary>Crea un nuevo contacto de landing desde el formulario público</summary>
    Task<ServiceResult<ContactoLandingResponseDto>> CreateAsync(ContactoLandingRequestDto dto);

    /// <summary>Obtiene un contacto por ID (vista admin)</summary>
    Task<ServiceResult<ContactoLandingResponseDto>> GetByIdAsync(Guid id);

    /// <summary>Obtiene todos los contactos activos (vista admin)</summary>
    Task<ServiceResult<IEnumerable<ContactoLandingResponseDto>>> GetAllAsync();

    /// <summary>Marca un contacto como leído por el admin</summary>
    Task<ServiceResult<bool>> MarkAsReadAsync(Guid id);

    /// <summary>Desactiva un contacto (no elimina) — CLAUDE.md regla #1</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid id);
}
