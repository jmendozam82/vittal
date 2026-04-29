# skill-dal.md — Skill: Data Access Layer (DAL)

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar este skill:** Antes de implementar cualquier Repository,
> interfaz de acceso a datos, configuración de conexión o query con Dapper
> en el proyecto Vittal.
> **Prerequisito:** Haber leído CLAUDE.md y skill-supabase.md. La tabla
> en PostgreSQL debe existir antes de implementar su Repository.

---

## 1. Principios Fundamentales del DAL

```
1. El DAL NUNCA contiene lógica de negocio — solo operaciones de datos
2. El DAL NUNCA retorna Entities directamente a capas superiores al BLL
3. Toda query SIEMPRE filtra por clinica_id — sin excepción
4. No existe DeleteAsync en ningún Repository — solo DeactivateAsync
5. Toda query usa parámetros Dapper (@Param) — nunca interpolación de strings
6. El Repository depende de su interfaz — nunca al revés
7. Un Repository por entidad principal — no repositorios genéricos
8. Las transacciones se manejan a nivel de Repository cuando involucran
   múltiples tablas en una sola operación atómica
9. Los errores de BD se capturan en el Repository y se relanzán como
   excepciones de dominio, nunca como excepciones de PostgreSQL crudas
10. Async/await en todos los métodos — no operaciones síncronas de BD
```

---

## 2. Estructura del Proyecto Vittal.DAL

```
src/Vittal.DAL/
├── Connections/
│   ├── IDbConnectionFactory.cs        ← Interfaz de la fábrica de conexiones
│   └── SupabaseConnectionFactory.cs   ← Implementación con Npgsql
├── Interfaces/
│   ├── IClinicaRepository.cs
│   ├── IPerfilRepository.cs
│   ├── IUsuarioRepository.cs
│   ├── IPermisoRepository.cs
│   ├── ISalaRepository.cs
│   ├── IPacienteRepository.cs
│   ├── IMedicamentoRepository.cs
│   ├── ITipoCirugiaRepository.cs
│   ├── ICirugiaRepository.cs
│   ├── ITipoDiagnosticoRepository.cs
│   ├── IDiagnosticoRepository.cs
│   ├── ITratamientoRepository.cs
│   ├── IRecomendacionRepository.cs
│   ├── IExamenRepository.cs
│   ├── ICitaRepository.cs
│   ├── IExpedienteRepository.cs
│   └── IAlertaEsperaRepository.cs
└── Repositories/
    ├── ClinicaRepository.cs
    ├── PerfilRepository.cs
    ├── UsuarioRepository.cs
    ├── PermisoRepository.cs
    ├── SalaRepository.cs
    ├── PacienteRepository.cs
    ├── MedicamentoRepository.cs
    ├── TipoCirugiaRepository.cs
    ├── CirugiaRepository.cs
    ├── TipoDiagnosticoRepository.cs
    ├── DiagnosticoRepository.cs
    ├── TratamientoRepository.cs
    ├── RecomendacionRepository.cs
    ├── ExamenRepository.cs
    ├── CitaRepository.cs
    ├── ExpedienteRepository.cs
    └── AlertaEsperaRepository.cs
```

---

## 3. Configuración de Conexión a Supabase (PostgreSQL)

### 3.1 NuGet Packages requeridos

```xml
<!-- src/Vittal.DAL/Vittal.DAL.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- ORM ligero — consultas SQL directas con mapeo automático -->
    <PackageReference Include="Dapper" Version="2.1.35" />

    <!-- Driver PostgreSQL para .NET -->
    <PackageReference Include="Npgsql" Version="8.0.3" />

    <!-- Dapper + Npgsql para tipos de PostgreSQL (UUID, JSONB, etc.) -->
    <PackageReference Include="Dapper.NodaTime" Version="2.0.0" />

    <!-- Referencia a las capas de entidades e interfaces -->
    <ProjectReference Include="..\Vittal.Entity\Vittal.Entity.csproj" />
  </ItemGroup>
</Project>
```

### 3.2 Interfaz de la fábrica de conexiones

```csharp
// src/Vittal.DAL/Connections/IDbConnectionFactory.cs
namespace Vittal.DAL.Connections;

public interface IDbConnectionFactory
{
    /// <summary>
    /// Crea y abre una nueva conexión a la base de datos PostgreSQL de Supabase.
    /// La conexión es responsabilidad del llamador — usar dentro de un using.
    /// </summary>
    Task<IDbConnection> CreateConnectionAsync();

    /// <summary>
    /// Configura el clinica_id en la sesión de PostgreSQL para que las
    /// políticas RLS de Supabase puedan aplicar el aislamiento de tenant.
    /// SIEMPRE llamar antes de cualquier operación de datos.
    /// </summary>
    Task SetTenantContextAsync(IDbConnection connection, Guid clinicaId);
}
```

### 3.3 Implementación de la fábrica de conexiones

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
        // Establece la variable de sesión que usan las políticas RLS
        // Esto permite que Supabase aísle datos por clinica_id automáticamente
        await connection.ExecuteAsync(
            "SELECT set_config('app.current_clinica_id', @ClinicaId, true)",
            new { ClinicaId = clinicaId.ToString() }
        );
    }
}
```

### 3.4 Registro global de tipos Dapper (en Program.cs)

```csharp
// Agregar en src/Vittal.API/Program.cs (o Vittal.Aplicacion)
// Registrar el mapeo de UUID de PostgreSQL a Guid de C#
SqlMapper.AddTypeHandler(new GuidTypeHandler());
SqlMapper.RemoveTypeMap(typeof(Guid));
SqlMapper.RemoveTypeMap(typeof(Guid?));

// Handler para mapear UUID de PostgreSQL a Guid de C#
public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid guid)
        => parameter.Value = guid.ToString();

    public override Guid Parse(object value)
        => Guid.Parse(value.ToString()!);
}
```

---

## 4. Plantilla Maestra de Interfaz de Repository

Usar esta plantilla para TODA interfaz de Repository. Los métodos son el contrato
mínimo obligatorio. Agregar métodos especializados según la HU.

```csharp
// src/Vittal.DAL/Interfaces/I[Entidad]Repository.cs
namespace Vittal.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos para la entidad [Entidad].
/// Historia de Usuario: HU[XX] — [Nombre de la HU]
/// </summary>
public interface I[Entidad]Repository
{
    // ── Consultas ────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene todos los registros activos de la clínica especificada.
    /// </summary>
    Task<IEnumerable<[Entidad]>> GetAllAsync(Guid clinicaId);

    /// <summary>
    /// Obtiene un registro por su ID validando que pertenece a la clínica.
    /// Retorna null si no existe o si pertenece a otro tenant.
    /// </summary>
    Task<[Entidad]?> GetByIdAsync(Guid id, Guid clinicaId);

    // ── Escritura ────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo registro y retorna el ID autogenerado por la BD.
    /// </summary>
    Task<Guid> CreateAsync([Entidad] entidad);

    /// <summary>
    /// Actualiza un registro existente. Retorna true si se actualizó, false si no existe.
    /// </summary>
    Task<bool> UpdateAsync([Entidad] entidad);

    /// <summary>
    /// Desactiva un registro (activo = false). NUNCA elimina.
    /// Retorna true si se desactivó, false si no existe o ya estaba inactivo.
    /// </summary>
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);

    // ── Validaciones ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifica si existe un registro con los criterios dados en la clínica.
    /// Usado para validar duplicados antes de crear.
    /// </summary>
    Task<bool> ExistsAsync(Guid clinicaId, string campo, string valor, Guid? excludeId = null);
}
```

---

## 5. Plantilla Maestra de Implementación de Repository

```csharp
// src/Vittal.DAL/Repositories/[Entidad]Repository.cs
namespace Vittal.DAL.Repositories;

/// <summary>
/// Implementación del acceso a datos para [Entidad] usando Dapper + PostgreSQL.
/// Historia de Usuario: HU[XX] — [Nombre de la HU]
/// </summary>
public class [Entidad]Repository : I[Entidad]Repository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<[Entidad]Repository> _logger;

    public [Entidad]Repository(
        IDbConnectionFactory connectionFactory,
        ILogger<[Entidad]Repository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────
    public async Task<IEnumerable<[Entidad]>> GetAllAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT
                id              AS Id,
                clinica_id      AS ClinicaId,
                -- [mapear todos los campos de la tabla]
                activo          AS Activo,
                fecha_creacion  AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM [nombre_tabla]
            WHERE clinica_id = @ClinicaId
              AND activo = true
            ORDER BY [campo_orden] ASC";

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
            return await connection.QueryAsync<[Entidad]>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener [Entidad]s de la clínica {ClinicaId}", clinicaId);
            throw new RepositoryException("Error al obtener el listado de [entidad]s.", ex);
        }
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────
    public async Task<[Entidad]?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            SELECT
                id              AS Id,
                clinica_id      AS ClinicaId,
                -- [mapear todos los campos]
                activo          AS Activo,
                fecha_creacion  AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM [nombre_tabla]
            WHERE id = @Id
              AND clinica_id = @ClinicaId
              AND activo = true";

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
            return await connection.QueryFirstOrDefaultAsync<[Entidad]>(sql,
                new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener [Entidad] {Id}", id);
            throw new RepositoryException($"Error al obtener el registro con ID {id}.", ex);
        }
    }

    // ── CreateAsync ──────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync([Entidad] entidad)
    {
        const string sql = @"
            INSERT INTO [nombre_tabla] (
                clinica_id,
                -- [columnas de negocio],
                activo,
                fecha_creacion
            ) VALUES (
                @ClinicaId,
                -- [@Propiedades],
                true,
                NOW()
            )
            RETURNING id";

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await _connectionFactory.SetTenantContextAsync(connection, entidad.ClinicaId);
            return await connection.ExecuteScalarAsync<Guid>(sql, entidad);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // unique_violation
        {
            throw new DuplicateEntityException(
                "Ya existe un registro con esos datos en la clínica.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear [Entidad] en clínica {ClinicaId}", entidad.ClinicaId);
            throw new RepositoryException("Error al crear el registro.", ex);
        }
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync([Entidad] entidad)
    {
        const string sql = @"
            UPDATE [nombre_tabla]
            SET
                -- [campo] = @[Propiedad],
                fecha_modificacion = NOW()
            WHERE id = @Id
              AND clinica_id = @ClinicaId  -- Guard de tenant en escritura
              AND activo = true
            RETURNING id";

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await _connectionFactory.SetTenantContextAsync(connection, entidad.ClinicaId);
            var updatedId = await connection.ExecuteScalarAsync<Guid?>(sql, entidad);
            return updatedId.HasValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar [Entidad] {Id}", entidad.Id);
            throw new RepositoryException("Error al actualizar el registro.", ex);
        }
    }

    // ── DeactivateAsync ──────────────────────────────────────────────────
    // REGLA ABSOLUTA: NUNCA implementar DeleteAsync
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE [nombre_tabla]
            SET
                activo = false,
                fecha_modificacion = NOW()
            WHERE id = @Id
              AND clinica_id = @ClinicaId
              AND activo = true";   -- Solo desactiva si estaba activo

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
            var rowsAffected = await connection.ExecuteAsync(sql,
                new { Id = id, ClinicaId = clinicaId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar [Entidad] {Id}", id);
            throw new RepositoryException("Error al desactivar el registro.", ex);
        }
    }

    // ── ExistsAsync ──────────────────────────────────────────────────────
    public async Task<bool> ExistsAsync(
        Guid clinicaId, string campo, string valor, Guid? excludeId = null)
    {
        // NOTA: campo viene de una lista blanca — no de entrada del usuario
        var sql = $@"
            SELECT COUNT(1)
            FROM [nombre_tabla]
            WHERE clinica_id = @ClinicaId
              AND {campo} = @Valor
              AND activo = true
              AND (@ExcludeId IS NULL OR id != @ExcludeId)";

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
            var count = await connection.ExecuteScalarAsync<int>(sql,
                new { ClinicaId = clinicaId, Valor = valor, ExcludeId = excludeId });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia en [nombre_tabla]");
            throw new RepositoryException("Error al verificar duplicados.", ex);
        }
    }
}
```

---

## 6. Excepciones de Dominio del DAL

```csharp
// src/Vittal.DAL/Exceptions/RepositoryException.cs
namespace Vittal.DAL.Exceptions;

/// <summary>
/// Excepción base del DAL. Envuelve errores de PostgreSQL/Dapper
/// en excepciones de dominio comprensibles para la capa BLL.
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception inner) : base(message, inner) { }
}

// src/Vittal.DAL/Exceptions/DuplicateEntityException.cs
/// <summary>
/// Se lanza cuando una operación CREATE viola una restricción UNIQUE.
/// El BLL la captura y retorna un error de validación al usuario.
/// </summary>
public class DuplicateEntityException : RepositoryException
{
    public DuplicateEntityException(string message) : base(message) { }
    public DuplicateEntityException(string message, Exception inner) : base(message, inner) { }
}

// src/Vittal.DAL/Exceptions/TenantViolationException.cs
/// <summary>
/// Se lanza cuando se detecta un intento de acceso a datos de otro tenant.
/// Caso crítico de seguridad — debe ser logueado y auditado.
/// </summary>
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

## 7. Repositories Implementados — Módulos Core

### 7.1 PacienteRepository (HU07)

```csharp
// src/Vittal.DAL/Repositories/PacienteRepository.cs
namespace Vittal.DAL.Repositories;

public class PacienteRepository : IPacienteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PacienteRepository> _logger;

    public PacienteRepository(IDbConnectionFactory connectionFactory,
        ILogger<PacienteRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<Paciente>> GetAllAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT
                p.id              AS Id,
                p.clinica_id      AS ClinicaId,
                p.doctor_id       AS DoctorId,
                p.primer_nombre   AS PrimerNombre,
                p.segundo_nombre  AS SegundoNombre,
                p.primer_apellido AS PrimerApellido,
                p.segundo_apellido AS SegundoApellido,
                p.email           AS Email,
                p.celular         AS Celular,
                p.direccion       AS Direccion,
                p.sexo            AS Sexo,
                p.fecha_nacimiento AS FechaNacimiento,
                p.foto_url        AS FotoUrl,
                p.observaciones   AS Observaciones,
                p.activo          AS Activo,
                p.fecha_creacion  AS FechaCreacion,
                p.fecha_modificacion AS FechaModificacion
            FROM pacientes p
            WHERE p.clinica_id = @ClinicaId
              AND p.activo = true
            ORDER BY p.primer_apellido, p.primer_nombre ASC";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        return await connection.QueryAsync<Paciente>(sql, new { ClinicaId = clinicaId });
    }

    public async Task<IEnumerable<Paciente>> GetByDoctorAsync(Guid doctorId, Guid clinicaId)
    {
        const string sql = @"
            SELECT
                id AS Id, clinica_id AS ClinicaId, doctor_id AS DoctorId,
                primer_nombre AS PrimerNombre, primer_apellido AS PrimerApellido,
                email AS Email, celular AS Celular, sexo AS Sexo,
                foto_url AS FotoUrl, activo AS Activo
            FROM pacientes
            WHERE doctor_id = @DoctorId
              AND clinica_id = @ClinicaId
              AND activo = true
            ORDER BY primer_apellido, primer_nombre ASC";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        return await connection.QueryAsync<Paciente>(sql,
            new { DoctorId = doctorId, ClinicaId = clinicaId });
    }

    public async Task<IEnumerable<Paciente>> SearchAsync(
        string termino, Guid clinicaId, int limit = 20)
    {
        // Búsqueda por nombre, apellido o email — usada en buscador de la Agenda
        const string sql = @"
            SELECT
                id AS Id, clinica_id AS ClinicaId, doctor_id AS DoctorId,
                primer_nombre AS PrimerNombre, segundo_nombre AS SegundoNombre,
                primer_apellido AS PrimerApellido, segundo_apellido AS SegundoApellido,
                email AS Email, celular AS Celular, foto_url AS FotoUrl
            FROM pacientes
            WHERE clinica_id = @ClinicaId
              AND activo = true
              AND (
                  LOWER(primer_nombre)   LIKE LOWER(@Termino) OR
                  LOWER(primer_apellido) LIKE LOWER(@Termino) OR
                  LOWER(email)           LIKE LOWER(@Termino) OR
                  LOWER(CONCAT(primer_nombre, ' ', primer_apellido)) LIKE LOWER(@Termino)
              )
            ORDER BY primer_apellido, primer_nombre
            LIMIT @Limit";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        return await connection.QueryAsync<Paciente>(sql, new
        {
            ClinicaId = clinicaId,
            Termino = $"%{termino}%",
            Limit = limit
        });
    }

    public async Task<Paciente?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            SELECT
                id AS Id, clinica_id AS ClinicaId, doctor_id AS DoctorId,
                primer_nombre AS PrimerNombre, segundo_nombre AS SegundoNombre,
                primer_apellido AS PrimerApellido, segundo_apellido AS SegundoApellido,
                email AS Email, celular AS Celular, direccion AS Direccion,
                sexo AS Sexo, fecha_nacimiento AS FechaNacimiento,
                foto_url AS FotoUrl, observaciones AS Observaciones,
                activo AS Activo, fecha_creacion AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM pacientes
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        return await connection.QueryFirstOrDefaultAsync<Paciente>(sql,
            new { Id = id, ClinicaId = clinicaId });
    }

    public async Task<Guid> CreateAsync(Paciente paciente)
    {
        const string sql = @"
            INSERT INTO pacientes (
                clinica_id, doctor_id, primer_nombre, segundo_nombre,
                primer_apellido, segundo_apellido, email, celular,
                direccion, sexo, fecha_nacimiento, foto_url, observaciones,
                activo, fecha_creacion
            ) VALUES (
                @ClinicaId, @DoctorId, @PrimerNombre, @SegundoNombre,
                @PrimerApellido, @SegundoApellido, @Email, @Celular,
                @Direccion, @Sexo, @FechaNacimiento, @FotoUrl, @Observaciones,
                true, NOW()
            )
            RETURNING id";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, paciente.ClinicaId);
        return await connection.ExecuteScalarAsync<Guid>(sql, paciente);
    }

    public async Task<bool> UpdateAsync(Paciente paciente)
    {
        const string sql = @"
            UPDATE pacientes SET
                doctor_id          = @DoctorId,
                primer_nombre      = @PrimerNombre,
                segundo_nombre     = @SegundoNombre,
                primer_apellido    = @PrimerApellido,
                segundo_apellido   = @SegundoApellido,
                email              = @Email,
                celular            = @Celular,
                direccion          = @Direccion,
                sexo               = @Sexo,
                fecha_nacimiento   = @FechaNacimiento,
                foto_url           = @FotoUrl,
                observaciones      = @Observaciones,
                fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true
            RETURNING id";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, paciente.ClinicaId);
        var updatedId = await connection.ExecuteScalarAsync<Guid?>(sql, paciente);
        return updatedId.HasValue;
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE pacientes
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        var rows = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(
        Guid clinicaId, string campo, string valor, Guid? excludeId = null)
    {
        // Lista blanca de campos permitidos para búsqueda de existencia
        var camposPermitidos = new HashSet<string> { "email", "celular" };
        if (!camposPermitidos.Contains(campo.ToLower()))
            throw new ArgumentException($"Campo '{campo}' no permitido para búsqueda de existencia.");

        var sql = $@"
            SELECT COUNT(1) FROM pacientes
            WHERE clinica_id = @ClinicaId AND {campo} = @Valor AND activo = true
            AND (@ExcludeId IS NULL OR id != @ExcludeId)";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        var count = await connection.ExecuteScalarAsync<int>(sql,
            new { ClinicaId = clinicaId, Valor = valor, ExcludeId = excludeId });
        return count > 0;
    }
}
```

### 7.2 CitaRepository (HU21 + HU18 Cola de Espera)

```csharp
// src/Vittal.DAL/Repositories/CitaRepository.cs
namespace Vittal.DAL.Repositories;

public class CitaRepository : ICitaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<CitaRepository> _logger;

    public CitaRepository(IDbConnectionFactory connectionFactory,
        ILogger<CitaRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene las citas del día actual para la Cola de Espera.
    /// Filtra por doctor si se provee, ordena por hora_cita ASC.
    /// </summary>
    public async Task<IEnumerable<Cita>> GetColaEsperaAsync(
        Guid clinicaId, Guid? doctorId = null)
    {
        var sql = @"
            SELECT
                c.id AS Id, c.clinica_id AS ClinicaId,
                c.paciente_id AS PacienteId, c.doctor_id AS DoctorId,
                c.sala_id AS SalaId, c.fecha_cita AS FechaCita,
                c.hora_cita AS HoraCita, c.hora_llegada AS HoraLlegada,
                c.lugar AS Lugar, c.motivo AS Motivo,
                c.estado AS Estado, c.notas AS Notas,
                -- Datos del paciente para la vista
                p.primer_nombre   AS PacientePrimerNombre,
                p.primer_apellido AS PacientePrimerApellido,
                p.foto_url        AS PacienteFotoUrl,
                -- Datos del doctor
                u.nombres   AS DoctorNombres,
                u.apellidos AS DoctorApellidos
            FROM citas c
            INNER JOIN pacientes p ON p.id = c.paciente_id
            INNER JOIN usuarios  u ON u.id = c.doctor_id
            WHERE c.clinica_id = @ClinicaId
              AND c.fecha_cita = CURRENT_DATE
              AND c.estado IN ('agendada', 'en_espera')
              AND c.activo = true
              AND (@DoctorId IS NULL OR c.doctor_id = @DoctorId)
            ORDER BY c.hora_cita ASC";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        return await connection.QueryAsync<Cita>(sql,
            new { ClinicaId = clinicaId, DoctorId = doctorId });
    }

    /// <summary>
    /// Cambia el estado de una cita. Usado en la Cola de Espera y la Agenda.
    /// </summary>
    public async Task<bool> CambiarEstadoAsync(
        Guid citaId, Guid clinicaId, string nuevoEstado, TimeOnly? horaLlegada = null)
    {
        const string sql = @"
            UPDATE citas SET
                estado             = @NuevoEstado,
                hora_llegada       = COALESCE(@HoraLlegada, hora_llegada),
                fecha_modificacion = NOW()
            WHERE id = @CitaId AND clinica_id = @ClinicaId AND activo = true
            RETURNING id";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        var updatedId = await connection.ExecuteScalarAsync<Guid?>(sql, new
        {
            CitaId = citaId,
            ClinicaId = clinicaId,
            NuevoEstado = nuevoEstado,
            HoraLlegada = horaLlegada
        });
        return updatedId.HasValue;
    }

    public async Task<IEnumerable<Cita>> GetByDoctorAndFechaAsync(
        Guid doctorId, Guid clinicaId, DateOnly fecha)
    {
        const string sql = @"
            SELECT
                c.id AS Id, c.clinica_id AS ClinicaId, c.paciente_id AS PacienteId,
                c.doctor_id AS DoctorId, c.sala_id AS SalaId,
                c.fecha_cita AS FechaCita, c.hora_cita AS HoraCita,
                c.hora_llegada AS HoraLlegada, c.lugar AS Lugar,
                c.estado AS Estado, c.motivo AS Motivo, c.notas AS Notas,
                p.primer_nombre AS PacientePrimerNombre,
                p.primer_apellido AS PacientePrimerApellido
            FROM citas c
            INNER JOIN pacientes p ON p.id = c.paciente_id
            WHERE c.doctor_id = @DoctorId
              AND c.clinica_id = @ClinicaId
              AND c.fecha_cita = @Fecha
              AND c.activo = true
            ORDER BY c.hora_cita ASC";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        return await connection.QueryAsync<Cita>(sql,
            new { DoctorId = doctorId, ClinicaId = clinicaId, Fecha = fecha });
    }

    public async Task<Guid> CreateAsync(Cita cita)
    {
        const string sql = @"
            INSERT INTO citas (
                clinica_id, paciente_id, doctor_id, sala_id,
                fecha_cita, hora_cita, lugar, motivo, estado,
                notas, activo, fecha_creacion
            ) VALUES (
                @ClinicaId, @PacienteId, @DoctorId, @SalaId,
                @FechaCita, @HoraCita, @Lugar, @Motivo, 'agendada',
                @Notas, true, NOW()
            )
            RETURNING id";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, cita.ClinicaId);
        return await connection.ExecuteScalarAsync<Guid>(sql, cita);
    }

    public async Task<bool> UpdateAsync(Cita cita)
    {
        const string sql = @"
            UPDATE citas SET
                paciente_id        = @PacienteId,
                doctor_id          = @DoctorId,
                sala_id            = @SalaId,
                fecha_cita         = @FechaCita,
                hora_cita          = @HoraCita,
                lugar              = @Lugar,
                motivo             = @Motivo,
                notas              = @Notas,
                fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true
            RETURNING id";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, cita.ClinicaId);
        var updatedId = await connection.ExecuteScalarAsync<Guid?>(sql, cita);
        return updatedId.HasValue;
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE citas SET
                activo = false, estado = 'cancelada', fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        var rows = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(
        Guid clinicaId, string campo, string valor, Guid? excludeId = null)
        => throw new NotSupportedException("Use GetByDoctorAndFechaAsync para verificar disponibilidad.");

    public async Task<Guid?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            SELECT id FROM citas
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        return await connection.ExecuteScalarAsync<Guid?>(sql, new { Id = id, ClinicaId = clinicaId });
    }
}
```

### 7.3 ExpedienteRepository (HU20)

```csharp
// src/Vittal.DAL/Repositories/ExpedienteRepository.cs
namespace Vittal.DAL.Repositories;

public class ExpedienteRepository : IExpedienteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ExpedienteRepository> _logger;

    public ExpedienteRepository(IDbConnectionFactory connectionFactory,
        ILogger<ExpedienteRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el expediente completo de un paciente con todas sus hojas de cita.
    /// Usa multi-mapping de Dapper para cargar el grafo de objetos en una sola query.
    /// </summary>
    public async Task<Expediente?> GetByPacienteAsync(Guid pacienteId, Guid clinicaId)
    {
        const string sql = @"
            SELECT
                e.id AS Id, e.clinica_id AS ClinicaId,
                e.paciente_id AS PacienteId, e.doctor_id AS DoctorId,
                e.notas_generales AS NotasGenerales,
                e.fecha_creacion AS FechaCreacion,
                -- Datos del paciente
                p.primer_nombre AS PrimerNombre, p.primer_apellido AS PrimerApellido,
                p.email AS Email, p.celular AS Celular,
                p.foto_url AS FotoUrl, p.fecha_nacimiento AS FechaNacimiento,
                p.sexo AS Sexo
            FROM expedientes e
            INNER JOIN pacientes p ON p.id = e.paciente_id
            WHERE e.paciente_id = @PacienteId
              AND e.clinica_id = @ClinicaId
              AND e.activo = true";

        const string sqlHojas = @"
            SELECT
                hc.id AS Id, hc.expediente_id AS ExpedienteId,
                hc.cita_id AS CitaId, hc.doctor_id AS DoctorId,
                hc.fecha_consulta AS FechaConsulta,
                hc.motivo_consulta AS MotivoConsulta,
                hc.notas_consulta AS NotasConsulta
            FROM hojas_cita hc
            WHERE hc.expediente_id = @ExpedienteId
              AND hc.clinica_id = @ClinicaId
              AND hc.activo = true
            ORDER BY hc.fecha_consulta DESC";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);

        var expediente = await connection.QueryFirstOrDefaultAsync<Expediente>(sql,
            new { PacienteId = pacienteId, ClinicaId = clinicaId });

        if (expediente is null) return null;

        // Cargar hojas de cita del expediente
        expediente.HojasCita = (await connection.QueryAsync<HojaCita>(sqlHojas,
            new { ExpedienteId = expediente.Id, ClinicaId = clinicaId })).ToList();

        return expediente;
    }

    /// <summary>
    /// Crea un expediente y su primera hoja de cita en una transacción atómica.
    /// </summary>
    public async Task<Guid> CreateConPrimeraHojaAsync(
        Expediente expediente, HojaCita primeraHoja)
    {
        const string sqlExpediente = @"
            INSERT INTO expedientes (clinica_id, paciente_id, doctor_id, notas_generales, activo, fecha_creacion)
            VALUES (@ClinicaId, @PacienteId, @DoctorId, @NotasGenerales, true, NOW())
            RETURNING id";

        const string sqlHoja = @"
            INSERT INTO hojas_cita (
                clinica_id, expediente_id, cita_id, doctor_id,
                fecha_consulta, motivo_consulta, notas_consulta, activo, fecha_creacion
            ) VALUES (
                @ClinicaId, @ExpedienteId, @CitaId, @DoctorId,
                @FechaConsulta, @MotivoConsulta, @NotasConsulta, true, NOW()
            )";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, expediente.ClinicaId);

        // Transacción para garantizar atomicidad
        await using var transaction = await ((NpgsqlConnection)connection)
            .BeginTransactionAsync();
        try
        {
            var expedienteId = await connection.ExecuteScalarAsync<Guid>(
                sqlExpediente, expediente, transaction);

            primeraHoja.ExpedienteId = expedienteId;
            await connection.ExecuteAsync(sqlHoja, primeraHoja, transaction);

            await transaction.CommitAsync();
            return expedienteId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Guid> CreateHojaAsync(HojaCita hoja)
    {
        const string sql = @"
            INSERT INTO hojas_cita (
                clinica_id, expediente_id, cita_id, doctor_id,
                fecha_consulta, motivo_consulta, notas_consulta, activo, fecha_creacion
            ) VALUES (
                @ClinicaId, @ExpedienteId, @CitaId, @DoctorId,
                @FechaConsulta, @MotivoConsulta, @NotasConsulta, true, NOW()
            )
            RETURNING id";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, hoja.ClinicaId);
        return await connection.ExecuteScalarAsync<Guid>(sql, hoja);
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE expedientes SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        var rows = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rows > 0;
    }
}
```

---

## 8. Queries Especializadas por Módulo

### 8.1 PermisoRepository — verificación de permisos por usuario

```csharp
public async Task<PermisoUsuario?> GetPermisoPorUsuarioYModuloAsync(
    Guid usuarioId, Guid clinicaId, string moduloClave)
{
    const string sql = @"
        SELECT
            p.puede_leer      AS PuedeLeer,
            p.puede_crear     AS PuedeCrear,
            p.puede_actualizar AS PuedeActualizar,
            pf.es_admin       AS EsAdmin
        FROM permisos p
        INNER JOIN usuarios u  ON u.perfil_id = p.perfil_id
        INNER JOIN perfiles pf ON pf.id = p.perfil_id
        INNER JOIN modulos_sistema m ON m.id = p.modulo_id
        WHERE u.id = @UsuarioId
          AND p.clinica_id = @ClinicaId
          AND m.clave = @ModuloClave
          AND u.activo = true
          AND pf.activo = true";

    await using var connection = await _connectionFactory.CreateConnectionAsync();
    await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
    return await connection.QueryFirstOrDefaultAsync<PermisoUsuario>(sql, new
    {
        UsuarioId = usuarioId,
        ClinicaId = clinicaId,
        ModuloClave = moduloClave
    });
}
```

### 8.2 LineaTiempoRepository (HU19)

```csharp
public async Task<IEnumerable<LineaTiempoPaciente>> GetLineaTiempoAsync(
    Guid clinicaId, Guid? doctorId, DateOnly fecha)
{
    var sql = @"
        SELECT
            c.id          AS CitaId,
            c.hora_cita   AS HoraCita,
            c.hora_llegada AS HoraLlegada,
            c.estado      AS Estado,
            p.primer_nombre   AS PacientePrimerNombre,
            p.primer_apellido AS PacientePrimerApellido,
            u.nombres   AS DoctorNombres,
            s.nombre    AS SalaNombre
        FROM citas c
        INNER JOIN pacientes p ON p.id = c.paciente_id
        INNER JOIN usuarios  u ON u.id = c.doctor_id
        LEFT  JOIN salas     s ON s.id = c.sala_id
        WHERE c.clinica_id = @ClinicaId
          AND c.fecha_cita = @Fecha
          AND c.activo = true
          AND (@DoctorId IS NULL OR c.doctor_id = @DoctorId)
        ORDER BY c.hora_cita ASC";

    await using var connection = await _connectionFactory.CreateConnectionAsync();
    await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
    return await connection.QueryAsync<LineaTiempoPaciente>(sql, new
    {
        ClinicaId = clinicaId,
        DoctorId = doctorId,
        Fecha = fecha
    });
}
```

---

## 9. Registro en IOC (Vittal.IOC)

```csharp
// src/Vittal.IOC/DependencyInjection.cs — sección DAL
public static IServiceCollection AddVittalDAL(this IServiceCollection services)
{
    // Fábrica de conexiones — Singleton (una sola instancia, thread-safe)
    services.AddSingleton<IDbConnectionFactory, SupabaseConnectionFactory>();

    // Repositorios — Scoped (una instancia por request HTTP)
    services.AddScoped<IClinicaRepository,         ClinicaRepository>();
    services.AddScoped<IPerfilRepository,          PerfilRepository>();
    services.AddScoped<IUsuarioRepository,         UsuarioRepository>();
    services.AddScoped<IPermisoRepository,         PermisoRepository>();
    services.AddScoped<ISalaRepository,            SalaRepository>();
    services.AddScoped<IPacienteRepository,        PacienteRepository>();
    services.AddScoped<IMedicamentoRepository,     MedicamentoRepository>();
    services.AddScoped<ITipoCirugiaRepository,     TipoCirugiaRepository>();
    services.AddScoped<ICirugiaRepository,         CirugiaRepository>();
    services.AddScoped<ITipoDiagnosticoRepository, TipoDiagnosticoRepository>();
    services.AddScoped<IDiagnosticoRepository,     DiagnosticoRepository>();
    services.AddScoped<ITratamientoRepository,     TratamientoRepository>();
    services.AddScoped<IRecomendacionRepository,   RecomendacionRepository>();
    services.AddScoped<IExamenRepository,          ExamenRepository>();
    services.AddScoped<ICitaRepository,            CitaRepository>();
    services.AddScoped<IExpedienteRepository,      ExpedienteRepository>();
    services.AddScoped<IAlertaEsperaRepository,    AlertaEsperaRepository>();

    return services;
}
```

---

## 10. Checklist de Calidad — @IngenieroDatos (DAL)

Antes de notificar al @PM que el Repository está listo:

### Interfaz

- [ ] Nombre sigue patrón `I[Entidad]Repository` en `Vittal.DAL/Interfaces/`
- [ ] Incluye `GetAllAsync(Guid clinicaId)` como primer método
- [ ] Incluye `GetByIdAsync(Guid id, Guid clinicaId)` que retorna `[Entidad]?`
- [ ] Incluye `CreateAsync` que retorna `Task<Guid>`
- [ ] Incluye `UpdateAsync` que retorna `Task<bool>`
- [ ] Incluye `DeactivateAsync(Guid id, Guid clinicaId)` que retorna `Task<bool>`
- [ ] **NO incluye `DeleteAsync`** en ninguna forma
- [ ] Métodos especializados adicionales según la HU documentados con XML summary

### Implementación

- [ ] Constructor recibe `IDbConnectionFactory` y `ILogger<T>`
- [ ] Toda operación usa `await using var connection = await _connectionFactory.CreateConnectionAsync()`
- [ ] Toda operación llama `await _connectionFactory.SetTenantContextAsync(connection, clinicaId)` antes de la query
- [ ] Todas las queries SQL usan alias explícitos (ej: `primer_nombre AS PrimerNombre`)
- [ ] Todas las queries incluyen `AND clinica_id = @ClinicaId` en el WHERE
- [ ] Todas las queries de listado incluyen `AND activo = true`
- [ ] `UpdateAsync` y `DeactivateAsync` incluyen `AND clinica_id = @ClinicaId` en el WHERE como segundo guard de seguridad
- [ ] `CreateAsync` usa `RETURNING id` para obtener el UUID generado
- [ ] `DeactivateAsync` hace `SET activo = false` — **nunca DELETE**
- [ ] Excepciones de PostgreSQL (23505 unique) mapeadas a `DuplicateEntityException`
- [ ] Errores genéricos mapeados a `RepositoryException` con logging
- [ ] Transacciones usadas cuando la operación involucra múltiples tablas
- [ ] No existe lógica de negocio en ningún método del Repository

### Registro

- [ ] Repository e interfaz registrados en `Vittal.IOC/DependencyInjection.cs`
- [ ] Lifetime es `Scoped` (no Singleton ni Transient)

---

*skill-dal.md — Vittal v1.0.0 | Agente: @IngenieroDatos*
*Para contexto del proyecto: CLAUDE.md | Para migraciones SQL: skill-supabase.md*
*Para coordinación de agentes: ORCHESTRATOR.md*
