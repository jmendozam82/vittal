using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Vittal.DAL.Context;

public class DbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("Supabase");
        return new NpgsqlConnection(connectionString);
    }

    /// <summary>
    /// Establece el contexto de tenant (clinica_id) en la sesión de PostgreSQL
    /// mediante set_config. Esto permite que las políticas RLS filtren
    /// automáticamente los datos por clínica.
    /// </summary>
    public async Task SetTenantContextAsync(Guid clinicaId)
    {
        using var connection = CreateConnection();
        connection.Open();

        const string sql = "SELECT set_config('app.current_clinica_id', @ClinicaId::text, true);";
        await connection.ExecuteAsync(sql, new { ClinicaId = clinicaId.ToString() });
    }
}
