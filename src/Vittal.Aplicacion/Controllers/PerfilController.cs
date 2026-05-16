using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.Aplicacion.Helpers;
using Vittal.Aplicacion.Models;
using Vittal.DTO.Usuario;

namespace Vittal.Aplicacion.Controllers;

/// <summary>
/// Controller MVC para que el usuario autenticado vea y edite su propio perfil.
/// Ruta: /Perfil/MiPerfil
/// </summary>
[Authorize]
public class PerfilController : Controller
{
    private readonly ApiClientHelper _apiClient;
    private readonly ILogger<PerfilController> _logger;

    public PerfilController(ApiClientHelper apiClient, ILogger<PerfilController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> MiPerfil()
    {
        try
        {
            var (success, response, errorMessage) = await _apiClient.GetAsync<ApiResponse<UsuarioResponseDto>>("api/MiPerfil");

            if (success && response != null && response.Success && response.Data != null)
            {
                return View(response.Data);
            }

            _logger.LogWarning("Error al cargar perfil: {Error}", errorMessage ?? response?.Message);
            TempData["Error"] = "No se pudo cargar la información del perfil.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar página de perfil");
            TempData["Error"] = "Ocurrió un error al cargar el perfil.";
        }

        return View(new UsuarioResponseDto());
    }

    [HttpPost]
    public async Task<IActionResult> MiPerfil(MiPerfilUpdateRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(new UsuarioResponseDto
                {
                    Nombres = dto.Nombres,
                    Apellidos = dto.Apellidos,
                    Email = dto.Email,
                    Sexo = dto.Sexo,
                    Celular = dto.Celular,
                    Direccion = dto.Direccion,
                    FotoUrl = dto.FotoUrl
                });
            }

            var (success, response, errorMessage) = await _apiClient.PutAsync<ApiResponse<UsuarioResponseDto>>("api/MiPerfil", dto);

            if (success && response != null && response.Success && response.Data != null)
            {
                TempData["Success"] = "Perfil actualizado exitosamente.";

                // Refrescar el nombre en el claim de la cookie (opcional: requeriría re-login)
                return RedirectToAction(nameof(MiPerfil));
            }

            var error = errorMessage ?? response?.Message ?? "Error al actualizar el perfil.";
            _logger.LogWarning("Error actualizando perfil: {Error}", error);
            TempData["Error"] = error;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar perfil");
            TempData["Error"] = "Ocurrió un error al actualizar el perfil.";
        }

        return View(new UsuarioResponseDto
        {
            Nombres = dto.Nombres,
            Apellidos = dto.Apellidos,
            Email = dto.Email,
            Sexo = dto.Sexo,
            Celular = dto.Celular,
            Direccion = dto.Direccion,
            FotoUrl = dto.FotoUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
    {
        try
        {
            if (avatarFile == null || avatarFile.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar un archivo de imagen.";
                return RedirectToAction(nameof(MiPerfil));
            }

            using var stream = avatarFile.OpenReadStream();
            var (success, response, errorMessage) = await _apiClient.PostFileAsync<ApiResponse<string>>(
                "api/MiPerfil/avatar",
                avatarFile.FileName,
                stream,
                avatarFile.ContentType);

            if (success && response != null && response.Success)
            {
                TempData["Success"] = "Foto de perfil actualizada exitosamente.";
            }
            else
            {
                TempData["Error"] = errorMessage ?? "Error al subir la foto.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir avatar");
            TempData["Error"] = "Ocurrió un error al subir la foto.";
        }

        return RedirectToAction(nameof(MiPerfil));
    }
}
