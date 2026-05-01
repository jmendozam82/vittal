using System.Data;
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
}
