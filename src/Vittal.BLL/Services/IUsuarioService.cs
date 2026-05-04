using System;
using System.Threading.Tasks;
using Vittal.DTO.Usuario;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

public interface IUsuarioService
{
    Task<ServiceResult<UsuarioResponseDto>> GetByAuthUserIdAsync(Guid authUserId);
}
