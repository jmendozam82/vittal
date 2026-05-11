using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Vittal.DAL.Context;

/// <summary>
/// TypeHandler para que Dapper pueda serializar/deserializar DateOnly correctamente
/// a través de Npgsql (PostgreSQL DATE → C# DateOnly).
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value)
    {
        return value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            string s => DateOnly.Parse(s),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateOnly")
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.Value = value.ToDateTime(new TimeOnly(0, 0));
        parameter.DbType = DbType.Date;
    }
}

/// <summary>
/// TypeHandler para DateOnly? (nullable).
/// </summary>
public class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
{
    public override DateOnly? Parse(object value)
    {
        if (value is null or DBNull) return null;
        return value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            string s => DateOnly.Parse(s),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateOnly?")
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateOnly? value)
    {
        if (value.HasValue)
        {
            parameter.Value = value.Value.ToDateTime(new TimeOnly(0, 0));
            parameter.DbType = DbType.Date;
        }
        else
        {
            parameter.Value = DBNull.Value;
        }
    }
}

public class DbConnectionFactory
{
    /// <summary>
    /// Inicializador estático: registra los TypeHandlers de Dapper para DateOnly.
    /// Se ejecuta una sola vez al cargar el tipo.
    /// </summary>
    static DbConnectionFactory()
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
    }

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
