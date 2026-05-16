using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Usuario;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para que el usuario autenticado gestione su propio perfil.
/// No requiere permisos especiales — solo autenticación.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class MiPerfilController : ControllerBase
{
    private readonly IUsuarioService _service;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MiPerfilController> _logger;

    public MiPerfilController(
        IUsuarioService service,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MiPerfilController> logger)
    {
        _service = service;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Obtiene los datos del perfil del usuario autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get()
    {
        var userId = User.GetInternalUserId();
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(userId, clinicaId);
        return result.ToActionResult();
    }

    /// <summary>Actualiza los datos editables del perfil del usuario autenticado.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromBody] MiPerfilUpdateRequestDto dto)
    {
        var userId = User.GetInternalUserId();
        var clinicaId = User.GetClinicaId();

        var result = await _service.UpdateProfileAsync(userId, dto, clinicaId, userId);
        return result.ToActionResult();
    }

    /// <summary>Sube una foto de perfil (avatar) a Supabase Storage y actualiza el perfil.</summary>
    [HttpPost("avatar")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Debe seleccionar un archivo de imagen."
                });
            }

            // Validar tipo MIME
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Solo se permiten imágenes JPEG, PNG o WebP."
                });
            }

            // Validar tamaño (5MB max)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "La imagen no debe superar los 5MB."
                });
            }

            var userId = User.GetInternalUserId();
            var clinicaId = User.GetClinicaId();

            // Construir path en Storage: avatares/{clinica_id}/{user_id}.{extension}
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension)) extension = ".jpg";
            var storagePath = $"{clinicaId}/{userId}{extension}";

            // Subir a Supabase Storage via REST API
            var supabaseUrl = _configuration["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase:Url not configured");
            var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException("Supabase:ServiceRoleKey not configured");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");

            var content = new ByteArrayContent(fileBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            var uploadUrl = $"{supabaseUrl}/storage/v1/object/avatares/{storagePath}";
            var response = await client.PutAsync(uploadUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error subiendo avatar a Supabase Storage: {Status} - {Error}",
                    response.StatusCode, errorBody);
                return StatusCode((int)response.StatusCode, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error al subir la imagen. Intente nuevamente."
                });
            }

            // Construir URL pública
            var publicUrl = $"{supabaseUrl}/storage/v1/object/public/avatares/{storagePath}";

            // Actualizar perfil con la nueva foto URL
            var updateDto = new MiPerfilUpdateRequestDto
            {
                Nombres = User.Identity?.Name?.Split(' ').FirstOrDefault() ?? "",
                Apellidos = "",
                Email = "",
                FotoUrl = publicUrl
            };

            // Obtener datos actuales para no perderlos al hacer merge
            var currentUser = await _service.GetByIdAsync(userId, clinicaId);
            if (currentUser.IsSuccess && currentUser.Data != null)
            {
                updateDto.Nombres = currentUser.Data.Nombres;
                updateDto.Apellidos = currentUser.Data.Apellidos;
                updateDto.Email = currentUser.Data.Email;
                updateDto.Sexo = currentUser.Data.Sexo;
                updateDto.Celular = currentUser.Data.Celular;
                updateDto.Direccion = currentUser.Data.Direccion;
            }

            var profileResult = await _service.UpdateProfileAsync(userId, updateDto, clinicaId, userId);

            if (!profileResult.IsSuccess)
            {
                // La foto se subió pero no se pudo actualizar el perfil
                _logger.LogWarning("Avatar subido pero falló actualización del perfil: {Error}", profileResult.Message);
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Foto de perfil actualizada exitosamente.",
                Data = publicUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir avatar");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno al procesar la imagen."
            });
        }
    }
}
