using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vittal.BLL.Services;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
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
        
        services.AddScoped<IPlantillaEspecialidadRepository, PlantillaEspecialidadRepository>();
        services.AddScoped<ITipoAntecedenteRepository, TipoAntecedenteRepository>();
        services.AddScoped<ITipoSignoVitalRepository, TipoSignoVitalRepository>();
        services.AddScoped<ICitaRepository, CitaRepository>();
        services.AddScoped<IAntecedentePacienteRepository, AntecedentePacienteRepository>();
        services.AddScoped<ISignosVitalesHojaRepository, SignosVitalesHojaRepository>();
        services.AddScoped<IConstanciaRepository, ConstanciaRepository>();

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
        
        services.AddScoped<IPlantillaEspecialidadService, PlantillaEspecialidadService>();
        services.AddScoped<ITipoAntecedenteService, TipoAntecedenteService>();
        services.AddScoped<ITipoSignoVitalService, TipoSignoVitalService>();
        services.AddScoped<ICitaService, CitaService>();
        services.AddScoped<IAntecedentePacienteService, AntecedentePacienteService>();
        services.AddScoped<ISignosVitalesHojaService, SignosVitalesHojaService>();
        services.AddScoped<IConstanciaService, ConstanciaService>();
        
        return services;
    }
}
