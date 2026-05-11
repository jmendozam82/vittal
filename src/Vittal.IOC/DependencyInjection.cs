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
        services.AddScoped<IMedicamentoRepository, MedicamentoRepository>();
        services.AddScoped<IClinicaRepository, ClinicaRepository>();
        services.AddScoped<ITipoCirugiaRepository, TipoCirugiaRepository>();
        services.AddScoped<ICirugiaRepository, CirugiaRepository>();
        services.AddScoped<ITipoDiagnosticoRepository, TipoDiagnosticoRepository>();
        services.AddScoped<IDiagnosticoRepository, DiagnosticoRepository>();
        services.AddScoped<IExamenRepository, ExamenRepository>();
        services.AddScoped<IRecomendacionRepository, RecomendacionRepository>();
        services.AddScoped<ITratamientoRepository, TratamientoRepository>();

        // Services
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IPermisoService, PermisoService>();
        services.AddScoped<IPacienteService, PacienteService>();
        services.AddScoped<ISalaService, SalaService>();
        services.AddScoped<IMedicamentoService, MedicamentoService>();
        services.AddScoped<IClinicaService, ClinicaService>();
        services.AddScoped<ITipoCirugiaService, TipoCirugiaService>();
        services.AddScoped<ICirugiaService, CirugiaService>();
        services.AddScoped<ITipoDiagnosticoService, TipoDiagnosticoService>();
        services.AddScoped<IDiagnosticoService, DiagnosticoService>();
        services.AddScoped<IExamenService, ExamenService>();
        services.AddScoped<IRecomendacionService, RecomendacionService>();
        services.AddScoped<ITratamientoService, TratamientoService>();
        
        return services;
    }
}
