using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vittal.BLL.Services;
using Vittal.DAL.Context;
using Vittal.DAL.Repositories;

namespace Vittal.IOC;

public static class DependencyInjection
{
    public static IServiceCollection AddVittalServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DAL Setup
        services.AddSingleton<DbConnectionFactory>();
        
        // Repositories
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        
        // Services
        services.AddScoped<IUsuarioService, UsuarioService>();
        
        return services;
    }
}
