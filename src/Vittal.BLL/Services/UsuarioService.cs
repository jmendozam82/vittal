using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Services;
using Vittal.DAL.Repositories;
using Vittal.DTO.Usuario;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<UsuarioService> _logger;

    public UsuarioService(IUsuarioRepository usuarioRepository, ILogger<UsuarioService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<UsuarioResponseDto>> GetByAuthUserIdAsync(Guid authUserId)
    {
        try
        {
            _logger.LogInformation("Buscando usuario con auth_user_id: {AuthUserId}", authUserId);
            var usuario = await _usuarioRepository.GetByAuthUserIdAsync(authUserId);
            _logger.LogInformation("Resultado repo: Usuario encontrado = {Found}", usuario != null);

            if (usuario == null)
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "Usuario no encontrado o inactivo", ServiceErrorType.NotFound);
            }

            // Mapeo Entity → DTO
            var dto = new UsuarioResponseDto
            {
                UsuarioId = usuario.Id,
                ClinicaId = usuario.ClinicaId,
                Nombres = usuario.Nombres,
                Apellidos = usuario.Apellidos,
                Email = usuario.Email,
                Celular = usuario.Celular,
                Sexo = usuario.Sexo,
                EsDoctor = usuario.EsDoctor,
                PerfilNombre = usuario.PerfilNombre,
                EsAdmin = usuario.EsAdmin
            };

            return ServiceResult<UsuarioResponseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuario por auth_user_id: {AuthUserId}", authUserId);
            return ServiceResult<UsuarioResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }
}
