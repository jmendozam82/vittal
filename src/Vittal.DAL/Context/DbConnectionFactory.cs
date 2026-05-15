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
    private Guid? _currentClinicaId;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Establece el tenant activo para el resto del request actual.
    /// Llamado desde TenantMiddleware. El valor se usa en cada conexión
    /// creada durante el request para activar las políticas RLS.
    /// </summary>
    public void SetTenantContext(Guid clinicaId)
    {
        _currentClinicaId = clinicaId;
    }

    /// <summary>
    /// Crea una conexión a PostgreSQL. Si hay un tenant activo,
    /// establece app.current_clinica_id en la sesión para activar RLS.
    /// </summary>
    public IDbConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("Supabase");
        var connection = new NpgsqlConnection(connectionString);

        if (_currentClinicaId.HasValue)
        {
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT set_config('app.current_clinica_id', '{_currentClinicaId.Value}', true);";
            cmd.ExecuteNonQuery();
        }

        return connection;
    }
}
