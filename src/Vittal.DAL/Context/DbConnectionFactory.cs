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
/// TypeHandler para DateTime — acepta DateOnly de Npgsql 8+ para columnas DATE.
/// Convierte DateOnly → DateTime (con hora 00:00:00 UTC).
/// </summary>
public class DateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override DateTime Parse(object value)
    {
        return value switch
        {
            DateTime dt => dt,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            string s => DateTime.Parse(s),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateTime")
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value;
        parameter.DbType = DbType.DateTime;
    }
}

/// <summary>
/// TypeHandler para DateTime? — acepta DateOnly de Npgsql 8+ para columnas DATE.
/// </summary>
public class NullableDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime?>
{
    public override DateTime? Parse(object value)
    {
        if (value is null or DBNull) return null;
        return value switch
        {
            DateTime dt => dt,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            string s => DateTime.Parse(s),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateTime?")
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateTime? value)
    {
        if (value.HasValue)
        {
            parameter.Value = value.Value;
            parameter.DbType = DbType.DateTime;
        }
        else
        {
            parameter.Value = DBNull.Value;
        }
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

/// <summary>
/// TypeHandler para que Dapper pueda serializar/deserializar TimeOnly correctamente
/// a través de Npgsql (PostgreSQL TIME → C# TimeOnly).
/// </summary>
public class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override TimeOnly Parse(object value)
    {
        return value switch
        {
            TimeOnly t => t,
            TimeSpan ts => TimeOnly.FromTimeSpan(ts),
            string s => TimeOnly.Parse(s),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to TimeOnly")
        };
    }

    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.Value = value.ToTimeSpan();
        parameter.DbType = DbType.Time;
    }
}

/// <summary>
/// TypeHandler para TimeOnly? (nullable).
/// </summary>
public class NullableTimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly?>
{
    public override TimeOnly? Parse(object value)
    {
        if (value is null or DBNull) return null;
        return value switch
        {
            TimeOnly t => t,
            TimeSpan ts => TimeOnly.FromTimeSpan(ts),
            string s => TimeOnly.Parse(s),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to TimeOnly?")
        };
    }

    public override void SetValue(IDbDataParameter parameter, TimeOnly? value)
    {
        if (value.HasValue)
        {
            parameter.Value = value.Value.ToTimeSpan();
            parameter.DbType = DbType.Time;
        }
        else
        {
            parameter.Value = DBNull.Value;
        }
    }
}

/// <summary>
/// TypeHandler para que Dapper pueda deserializar TimeSpan desde TimeOnly.
/// Npgsql 8+ retorna TimeOnly para columnas TIME en PostgreSQL.
/// Las entidades Vittal usan TimeSpan para estos campos, por lo que
/// este handler convierte TimeOnly → TimeSpan automáticamente.
/// </summary>
public class TimeSpanTypeHandler : SqlMapper.TypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value)
    {
        return value switch
        {
            TimeSpan ts => ts,
            TimeOnly to => to.ToTimeSpan(),
            string s => TimeSpan.Parse(s),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to TimeSpan")
        };
    }

    public override void SetValue(IDbDataParameter parameter, TimeSpan value)
    {
        parameter.Value = value;
        parameter.DbType = DbType.Time;
    }
}

/// <summary>
/// TypeHandler para TimeSpan? (nullable) — también acepta TimeOnly de Npgsql.
/// </summary>
public class NullableTimeSpanTypeHandler : SqlMapper.TypeHandler<TimeSpan?>
{
    public override TimeSpan? Parse(object value)
    {
        if (value is null or DBNull) return null;
        return value switch
        {
            TimeSpan ts => ts,
            TimeOnly to => to.ToTimeSpan(),
            string s => TimeSpan.Parse(s),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to TimeSpan?")
        };
    }

    public override void SetValue(IDbDataParameter parameter, TimeSpan? value)
    {
        if (value.HasValue)
        {
            parameter.Value = value.Value;
            parameter.DbType = DbType.Time;
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
    /// Inicializador estático: registra los TypeHandlers de Dapper para DateOnly, TimeOnly y TimeSpan.
    /// Npgsql 8+ retorna TimeOnly para TIME; los handlers de TimeSpan convierten TimeOnly → TimeSpan.
    /// Se ejecuta una sola vez al cargar el tipo.
    /// </summary>
    static DbConnectionFactory()
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new NullableTimeOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new TimeSpanTypeHandler());
        SqlMapper.AddTypeHandler(new NullableTimeSpanTypeHandler());
        SqlMapper.AddTypeHandler(new DateTimeTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeTypeHandler());

        // Mapeo snake_case → PascalCase automático para Dapper.
        // Hace que clinica_id → ClinicaId, fecha_creacion → FechaCreacion, etc.
        // Es una red de seguridad complementaria a los AS alias explícitos.
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private readonly IConfiguration _configuration;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Crea una conexión a PostgreSQL. Si se provee clinicaId,
    /// establece app.current_clinica_id en la sesión para activar RLS.
    /// </summary>
    public IDbConnection CreateConnection(Guid? clinicaId = null)
    {
        var connectionString = _configuration.GetConnectionString("Supabase");
        var connection = new NpgsqlConnection(connectionString);

        if (clinicaId.HasValue)
        {
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT set_config('app.current_clinica_id', @clinicaId, true);";
            var param = cmd.CreateParameter();
            param.ParameterName = "@clinicaId";
            param.Value = clinicaId.Value.ToString();
            cmd.Parameters.Add(param);
            cmd.ExecuteNonQuery();
        }

        return connection;
    }
}
