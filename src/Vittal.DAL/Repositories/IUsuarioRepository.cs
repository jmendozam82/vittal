using System;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByAuthUserIdAsync(Guid authUserId);
}
