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
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IPermisoRepository, PermisoRepository>();
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<ISalaRepository, SalaRepository>();

        // Services
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IPermisoService, PermisoService>();
        services.AddScoped<IPacienteService, PacienteService>();
        services.AddScoped<ISalaService, SalaService>();
        
        return services;
    }
}
