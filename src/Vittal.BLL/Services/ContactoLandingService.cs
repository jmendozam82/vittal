using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.ContactoLanding;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio BLL para contactos de landing page.
/// Implementa patrón ServiceResult&lt;T&gt; para respuestas consistentes.
/// Historia de Usuario: HU-L01 — Landing Page Informativa
/// </summary>
public class ContactoLandingService : IContactoLandingService
{
    private readonly IContactoLandingRepository _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactoLandingService> _logger;

    public ContactoLandingService(
        IContactoLandingRepository repository,
        IEmailService emailService,
        ILogger<ContactoLandingService> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<ContactoLandingResponseDto>> CreateAsync(ContactoLandingRequestDto dto)
    {
        try
        {
            var contacto = new ContactoLanding
            {
                NombreCompleto = dto.NombreCompleto.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Telefono = dto.Telefono?.Trim() ?? string.Empty,
                Rol = dto.Rol.Trim().ToLowerInvariant(),
                Mensaje = dto.Mensaje.Trim()
            };

            var id = await _repository.CreateAsync(contacto);

            _logger.LogInformation(
                "Nuevo contacto de landing creado: {Email} (Rol: {Rol}, Id: {Id})",
                contacto.Email, contacto.Rol, id);

            // Enviar correo de notificación al admin (fire-and-forget, no bloquea la respuesta)
            _ = Task.Run(async () =>
            {
                try
                {
                    contacto.Id = id;
                    var emailSent = await _emailService.SendLandingContactNotificationAsync(contacto);
                    if (emailSent)
                        _logger.LogInformation("Notificación de landing enviada al admin para {Email}", contacto.Email);
                    else
                        _logger.LogWarning("No se pudo enviar notificación de landing al admin para {Email}", contacto.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar notificación de landing al admin para {Email}", contacto.Email);
                }
            });

            return ServiceResult<ContactoLandingResponseDto>.Success(
                new ContactoLandingResponseDto
                {
                    Id = id,
                    NombreCompleto = contacto.NombreCompleto,
                    Email = contacto.Email,
                    Rol = contacto.Rol,
                    FechaCreacion = contacto.FechaCreacion
                },
                "Su mensaje ha sido enviado con éxito. Nos pondremos en contacto pronto.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear contacto de landing desde {Email}", dto.Email);
            return ServiceResult<ContactoLandingResponseDto>.Failure(
                "Error al procesar el contacto. Intente nuevamente.");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<ContactoLandingResponseDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var contacto = await _repository.GetByIdAsync(id);
            if (contacto == null)
                return ServiceResult<ContactoLandingResponseDto>.Failure("Contacto no encontrado.");

            return ServiceResult<ContactoLandingResponseDto>.Success(new ContactoLandingResponseDto
            {
                Id = contacto.Id,
                NombreCompleto = contacto.NombreCompleto,
                Email = contacto.Email,
                Rol = contacto.Rol,
                FechaCreacion = contacto.FechaCreacion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener contacto por ID: {Id}", id);
            return ServiceResult<ContactoLandingResponseDto>.Failure("Error al obtener el contacto.");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<IEnumerable<ContactoLandingResponseDto>>> GetAllAsync()
    {
        try
        {
            var contactos = await _repository.GetAllAsync();
            var result = contactos.Select(c => new ContactoLandingResponseDto
            {
                Id = c.Id,
                NombreCompleto = c.NombreCompleto,
                Email = c.Email,
                Rol = c.Rol,
                FechaCreacion = c.FechaCreacion
            });

            return ServiceResult<IEnumerable<ContactoLandingResponseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los contactos de landing");
            return ServiceResult<IEnumerable<ContactoLandingResponseDto>>.Failure(
                "Error al obtener los contactos.");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> MarkAsReadAsync(Guid id)
    {
        try
        {
            var success = await _repository.MarkAsReadAsync(id);
            if (!success)
                return ServiceResult<bool>.Failure("Contacto no encontrado.");

            _logger.LogInformation("Contacto de landing marcado como leído: {Id}", id);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar contacto como leído: {Id}", id);
            return ServiceResult<bool>.Failure("Error al marcar el contacto como leído.");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id)
    {
        try
        {
            var success = await _repository.DeactivateAsync(id);
            if (!success)
                return ServiceResult<bool>.Failure("Contacto no encontrado.");

            _logger.LogInformation("Contacto de landing desactivado: {Id}", id);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar contacto de landing: {Id}", id);
            return ServiceResult<bool>.Failure("Error al desactivar el contacto.");
        }
    }
}
