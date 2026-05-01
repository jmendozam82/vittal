using System;
using System.Threading.Tasks;
using Vittal.BLL.Services;
using Vittal.DAL.Repositories;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioService(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<ServiceResult<Usuario>> GetByAuthUserIdAsync(Guid authUserId)
    {
        try
        {
            var usuario = await _usuarioRepository.GetByAuthUserIdAsync(authUserId);
            if (usuario == null)
            {
                return ServiceResult<Usuario>.Failure("Usuario no encontrado o inactivo", ServiceErrorType.NotFound);
            }

            return ServiceResult<Usuario>.Success(usuario);
        }
        catch (Exception ex)
        {
            // In a real scenario, use ILogger
            return ServiceResult<Usuario>.Failure($"Error interno: {ex.Message}");
        }
    }
}
