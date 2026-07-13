using Microsoft.AspNetCore.Mvc;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.ContactoLanding;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para el formulario de contacto de la Landing Page.
/// Endpoint público — no requiere autenticación.
/// Historia de Usuario: HU-L01 — Landing Page Informativa
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContactoLandingController : ControllerBase
{
    private readonly IContactoLandingService _service;
    private readonly ILogger<ContactoLandingController> _logger;

    public ContactoLandingController(
        IContactoLandingService service,
        ILogger<ContactoLandingController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Registra un contacto enviado desde el formulario de la landing.
    /// Endpoint público — no requiere token JWT.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ContactoLandingResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ContactoLandingRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Error de validación.",
                Errors = errors,
                Timestamp = DateTime.UtcNow
            });
        }

        var result = await _service.CreateAsync(dto);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Error al procesar formulario de contacto desde {Email}: {Error}",
                dto.Email, result.Message);

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.Message ?? "Error al enviar el formulario.",
                Timestamp = DateTime.UtcNow
            });
        }

        _logger.LogInformation(
            "Formulario de contacto enviado exitosamente desde {Email} (Rol: {Rol})",
            dto.Email, dto.Rol);

        var response = new ApiResponse<ContactoLandingResponseDto>
        {
            Success = true,
            Message = "Contacto registrado exitosamente.",
            Data = result.Data,
            Timestamp = DateTime.UtcNow
        };

        return CreatedAtAction(nameof(Create), new { id = result.Data?.Id }, response);
    }
}
