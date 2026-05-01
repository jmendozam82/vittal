using System;
using System.Threading.Tasks;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

public interface IUsuarioService
{
    Task<ServiceResult<Usuario>> GetByAuthUserIdAsync(Guid authUserId);
}
