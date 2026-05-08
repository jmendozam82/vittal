using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Exceptions;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Implementación del acceso a datos para Sala usando Dapper + PostgreSQL.
/// Historia de Usuario: HU06 — Gestión de Salas
/// Tabla: public.salas
/// </summary>
public class SalaRepository : ISalaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<SalaRepository> _logger;

    public SalaRepository(DbConnectionFactory dbConnectionFactory, ILogger<SalaRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    #region ── Consultas (Lectura) ────────────────────────────────────

    public async Task<IEnumerable<Sala>> GetAllAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT
                s.id                AS Id,
                s.clinica_id        AS ClinicaId,
                s.nombre            AS Nombre,
                s.descripcion       AS Descripcion,
                s.activo            AS Activo,
                s.fecha_creacion    AS FechaCreacion,
                s.fecha_modificacion AS FechaModificacion
            FROM public.salas s
            WHERE s.clinica_id = @ClinicaId
              AND s.activo = true
            ORDER BY s.nombre ASC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Sala>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener salas activas de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    public async Task<IEnumerable<Sala>> GetAllIncludingInactiveAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT
                s.id                AS Id,
                s.clinica_id        AS ClinicaId,
                s.nombre            AS Nombre,
                s.descripcion       AS Descripcion,
                s.activo            AS Activo,
                s.fecha_creacion    AS FechaCreacion,
                s.fecha_modificacion AS FechaModificacion
            FROM public.salas s
            WHERE s.clinica_id = @ClinicaId
            ORDER BY s.activo DESC, s.nombre ASC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Sala>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las salas (incluyendo inactivas) de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    public async Task<Sala?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            SELECT
                s.id                AS Id,
                s.clinica_id        AS ClinicaId,
                s.nombre            AS Nombre,
                s.descripcion       AS Descripcion,
                s.activo            AS Activo,
                s.fecha_creacion    AS FechaCreacion,
                s.fecha_modificacion AS FechaModificacion
            FROM public.salas s
            WHERE s.id = @Id
              AND s.clinica_id = @ClinicaId
              AND s.activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Sala>(sql,
                new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sala {Id}", id);
            throw;
        }
    }

    #endregion

    #region ── Comandos (Escritura) ───────────────────────────────────

    public async Task<Guid> CreateAsync(Sala sala)
    {
        const string sql = @"
            INSERT INTO public.salas (
                clinica_id, nombre, descripcion,
                activo, fecha_creacion
            ) VALUES (
                @ClinicaId, @Nombre, @Descripcion,
                true, NOW()
            )
            RETURNING id";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, sala);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning("Sala duplicada en clínica {ClinicaId}: {Nombre}", sala.ClinicaId, sala.Nombre);
            throw new DuplicateEntityException(
                $"Ya existe una sala con el nombre '{sala.Nombre}' en esta clínica.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear sala en clínica {ClinicaId}", sala.ClinicaId);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Sala sala)
    {
        const string sql = @"
            UPDATE public.salas
            SET
                nombre             = @Nombre,
                descripcion        = @Descripcion,
                fecha_modificacion = NOW()
            WHERE id = @Id
              AND clinica_id = @ClinicaId
              AND activo = true
            RETURNING id";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var updatedId = await connection.ExecuteScalarAsync<Guid?>(sql, sala);
            return updatedId.HasValue;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning("Nombre de sala duplicado en actualización: {Nombre}", sala.Nombre);
            throw new DuplicateEntityException(
                $"Ya existe una sala con el nombre '{sala.Nombre}' en esta clínica.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar sala {Id}", sala.Id);
            throw;
        }
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.salas
            SET
                activo = false,
                fecha_modificacion = NOW()
            WHERE id = @Id
              AND clinica_id = @ClinicaId
              AND activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql,
                new { Id = id, ClinicaId = clinicaId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar sala {Id}", id);
            throw;
        }
    }

    public async Task<bool> ReactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.salas
            SET
                activo = true,
                fecha_modificacion = NOW()
            WHERE id = @Id
              AND clinica_id = @ClinicaId
              AND activo = false";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql,
                new { Id = id, ClinicaId = clinicaId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar sala {Id}", id);
            throw;
        }
    }

    #endregion

    #region ── Validaciones ───────────────────────────────────────────

    public async Task<bool> ExistsByNameAsync(Guid clinicaId, string nombre, Guid? excludeId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.salas
            WHERE clinica_id = @ClinicaId
              AND LOWER(nombre) = LOWER(@Nombre)
              AND activo = true
              AND (@ExcludeId IS NULL OR id != @ExcludeId)";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql,
                new { ClinicaId = clinicaId, Nombre = nombre, ExcludeId = excludeId });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de sala por nombre");
            throw;
        }
    }

    #endregion
}
