# DAL — Connection Factory

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar:** Para configurar la conexión a Supabase PostgreSQL.
> **Prerequisito:** skills/dal/SKILL.md

---

## Interfaz IDbConnectionFactory

```csharp
// src/Vittal.DAL/Connections/IDbConnectionFactory.cs
namespace Vittal.DAL.Connections;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
    Task SetTenantContextAsync(IDbConnection connection, Guid clinicaId);
}
```

---

## Implementación SupabaseConnectionFactory

```csharp
// src/Vittal.DAL/Connections/SupabaseConnectionFactory.cs
namespace Vittal.DAL.Connections;

public class SupabaseConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SupabaseConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Supabase")
            ?? throw new InvalidOperationException(
                "Connection string 'Supabase' no encontrada en appsettings.json");
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task SetTenantContextAsync(IDbConnection connection, Guid clinicaId)
    {
        await connection.ExecuteAsync(
            "SELECT set_config('app.current_clinica_id', @ClinicaId, true)",
            new { ClinicaId = clinicaId.ToString() }
        );
    }
}
```

---

## Guid Type Handler (en Program.cs)

```csharp
// Registrar en Program.cs para mapear UUID de PostgreSQL a Guid de C#
SqlMapper.AddTypeHandler(new GuidTypeHandler());
SqlMapper.RemoveTypeMap(typeof(Guid));
SqlMapper.RemoveTypeMap(typeof(Guid?));

public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid guid)
        => parameter.Value = guid.ToString();

    public override Guid Parse(object value)
        => Guid.Parse(value.ToString()!);
}
```

---

## Excepciones del DAL

```csharp
// src/Vittal.DAL/Exceptions/RepositoryException.cs
namespace Vittal.DAL.Exceptions;

public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception inner) : base(message, inner) { }
}

// DuplicateEntityException
public class DuplicateEntityException : RepositoryException
{
    public DuplicateEntityException(string message) : base(message) { }
    public DuplicateEntityException(string message, Exception inner) : base(message, inner) { }
}

// TenantViolationException
public class TenantViolationException : RepositoryException
{
    public Guid AttemptedClinicaId { get; }
    public Guid ActualClinicaId { get; }

    public TenantViolationException(Guid attempted, Guid actual)
        : base($"Violación de tenant: intento de acceso a clínica {attempted} desde clínica {actual}.")
    {
        AttemptedClinicaId = attempted;
        ActualClinicaId = actual;
    }
}
```

---

## Checklist de Calidad — Connection

- [ ] Connection string obtenida de `appsettings.json`
- [ ] Excepción si connection string no existe
- [ ] `SetTenantContextAsync` establece `app.current_clinica_id`
- [ ] GuidTypeHandler registrado en Program.cs
- [ ] IDbConnectionFactory registrado como Singleton
- [ ] Excepciones de dominio creadas en `DAL/Exceptions/`

---

*skills/dal/connection.md — Vittal v1.0.0*
