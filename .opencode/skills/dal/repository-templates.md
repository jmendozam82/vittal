# DAL — Repository Templates

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar:** Para implementar interfaces y repositories con Dapper.
> **Prerequisito:** skills/dal/SKILL.md, skills/dal/connection.md

---

## Interfaz de Repository

```csharp
// src/Vittal.DAL/Interfaces/I[Entidad]Repository.cs
namespace Vittal.DAL.Interfaces;

public interface I[Entidad]Repository
{
    Task<IEnumerable<[Entidad]>> GetAllAsync(Guid clinicaId);
    Task<[Entidad]?> GetByIdAsync(Guid id, Guid clinicaId);
    Task<Guid> CreateAsync([Entidad] entidad);
    Task<bool> UpdateAsync([Entidad] entidad);
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);
    Task<bool> ExistsAsync(Guid clinicaId, string campo, string valor, Guid? excludeId = null);
}
```

---

## Implementación de Repository (Plantilla Maestra)

```csharp
// src/Vittal.DAL/Repositories/[Entidad]Repository.cs
namespace Vittal.DAL.Repositories;

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
                id                  AS Id,
                clinica_id          AS ClinicaId,
                -- [mapear todos los campos]
                activo              AS Activo,
                fecha_creacion      AS FechaCreacion,
                fecha_modificacion  AS FechaModificacion
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
            _logger.LogError(ex, "Error al obtener [Entidad]s de clínica {ClinicaId}", clinicaId);
            throw new RepositoryException("Error al obtener el listado.", ex);
        }
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────
    public async Task<[Entidad]?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            SELECT id AS Id, clinica_id AS ClinicaId,
                   -- [mapear campos]
                   activo AS Activo,
                   fecha_creacion AS FechaCreacion,
                   fecha_modificacion AS FechaModificacion
            FROM [nombre_tabla]
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";

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
            throw new RepositoryException($"Error al obtener el registro.", ex);
        }
    }

    // ── CreateAsync ──────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync([Entidad] entidad)
    {
        const string sql = @"
            INSERT INTO [nombre_tabla] (
                clinica_id,
                -- [columnas de negocio],
                activo, fecha_creacion
            ) VALUES (
                @ClinicaId,
                -- [@Propiedades],
                true, NOW()
            )
            RETURNING id";

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            await _connectionFactory.SetTenantContextAsync(connection, entidad.ClinicaId);
            return await connection.ExecuteScalarAsync<Guid>(sql, entidad);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new DuplicateEntityException("Ya existe un registro con esos datos.", ex);
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
              AND clinica_id = @ClinicaId
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
    // REGLA: NUNCA implementar DeleteAsync
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE [nombre_tabla]
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";

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
        // campo viene de lista blanca — nunca de entrada del usuario
        var sql = $@"
            SELECT COUNT(1) FROM [nombre_tabla]
            WHERE clinica_id = @ClinicaId AND {campo} = @Valor
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
            _logger.LogError(ex, "Error al verificar existencia");
            throw new RepositoryException("Error al verificar duplicados.", ex);
        }
    }
}
```

---

## Checklist de Calidad — Repository Templates

### Interfaz
- [ ] Nombre: `I[Entidad]Repository`
- [ ] Incluye `GetAllAsync(Guid clinicaId)`
- [ ] Incluye `GetByIdAsync(Guid id, Guid clinicaId)` retorna `[Entidad]?`
- [ ] Incluye `CreateAsync` retorna `Task<Guid>`
- [ ] Incluye `UpdateAsync` retorna `Task<bool>`
- [ ] Incluye `DeactivateAsync` retorna `Task<bool>`
- [ ] **NO incluye DeleteAsync**

### Implementación
- [ ] Constructor recibe `IDbConnectionFactory` + `ILogger<T>`
- [ ] Usa `await using var connection = await _connectionFactory.CreateConnectionAsync()`
- [ ] Llama `SetTenantContextAsync` antes de cada query
- [ ] SQL usa alias explícitos (ej: `primer_nombre AS PrimerNombre`)
- [ ] WHERE incluye `AND clinica_id = @ClinicaId`
- [ ] WHERE incluye `AND activo = true` en consultas de lectura
- [ ] UpdateAsync y DeactivateAsync incluyen clinica_id en WHERE como guard
- [ ] CreateAsync usa `RETURNING id`
- [ ] DeactivateAsync usa `SET activo = false`, nunca DELETE
- [ ] PostgresException 23505 mapeado a `DuplicateEntityException`
- [ ] Errores genéricos mapeados a `RepositoryException` con logging

---

*skills/dal/repository-templates.md — Vittal v1.0.0*
