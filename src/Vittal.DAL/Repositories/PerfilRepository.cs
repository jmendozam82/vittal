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
/// Implementación del acceso a datos para Perfil usando Dapper + PostgreSQL.
/// Historia de Usuario: HU03 — Gestión de Perfiles
/// </summary>
public class PerfilRepository : IPerfilRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<PerfilRepository> _logger;

    public PerfilRepository(DbConnectionFactory dbConnectionFactory, ILogger<PerfilRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<Perfil>> GetAllAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT
                p.id              AS Id,
                p.clinica_id      AS ClinicaId,
                p.nombre          AS Nombre,
                p.descripcion     AS Descripcion,
                p.es_admin        AS EsAdmin,
                p.activo          AS Activo,
                p.fecha_creacion  AS FechaCreacion,
                p.fecha_modificacion AS FechaModificacion,
                COUNT(DISTINCT pm.id) AS CantidadPermisos,
                COUNT(DISTINCT u.id)  AS CantidadUsuarios
            FROM public.perfiles p
            LEFT JOIN public.permisos pm ON pm.perfil_id = p.id AND pm.clinica_id = p.clinica_id
            LEFT JOIN public.usuarios u   ON u.perfil_id  = p.id AND u.clinica_id  = p.clinica_id AND u.activo = true
            WHERE p.clinica_id = @ClinicaId
              AND p.activo = true
            GROUP BY p.id, p.clinica_id, p.nombre, p.descripcion, p.es_admin,
                     p.activo, p.fecha_creacion, p.fecha_modificacion
            ORDER BY p.nombre ASC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Perfil>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener perfiles de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    public async Task<Perfil?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            SELECT
                p.id              AS Id,
                p.clinica_id      AS ClinicaId,
                p.nombre          AS Nombre,
                p.descripcion     AS Descripcion,
                p.es_admin        AS EsAdmin,
                p.activo          AS Activo,
                p.fecha_creacion  AS FechaCreacion,
                p.fecha_modificacion AS FechaModificacion,
                COUNT(DISTINCT pm.id) AS CantidadPermisos,
                COUNT(DISTINCT u.id)  AS CantidadUsuarios
            FROM public.perfiles p
            LEFT JOIN public.permisos pm ON pm.perfil_id = p.id AND pm.clinica_id = p.clinica_id
            LEFT JOIN public.usuarios u   ON u.perfil_id  = p.id AND u.clinica_id  = p.clinica_id AND u.activo = true
            WHERE p.id = @Id
              AND p.clinica_id = @ClinicaId
              AND p.activo = true
            GROUP BY p.id, p.clinica_id, p.nombre, p.descripcion, p.es_admin,
                     p.activo, p.fecha_creacion, p.fecha_modificacion";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Perfil>(sql,
                new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener perfil {Id}", id);
            throw;
        }
    }

    public async Task<Guid> CreateAsync(Perfil perfil)
    {
        const string sql = @"
            INSERT INTO public.perfiles (
                clinica_id, nombre, descripcion, es_admin,
                activo, fecha_creacion
            ) VALUES (
                @ClinicaId, @Nombre, @Descripcion, @EsAdmin,
                true, NOW()
            )
            RETURNING id";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, perfil);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning("Perfil duplicado en clínica {ClinicaId}: {Nombre}", perfil.ClinicaId, perfil.Nombre);
            throw new DuplicateEntityException(
                $"Ya existe un perfil con el nombre '{perfil.Nombre}' en esta clínica.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear perfil en clínica {ClinicaId}", perfil.ClinicaId);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Perfil perfil)
    {
        const string sql = @"
            UPDATE public.perfiles
            SET
                nombre             = @Nombre,
                descripcion        = @Descripcion,
                es_admin           = @EsAdmin,
                fecha_modificacion = NOW()
            WHERE id = @Id
              AND clinica_id = @ClinicaId
              AND activo = true
            RETURNING id";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var updatedId = await connection.ExecuteScalarAsync<Guid?>(sql, perfil);
            return updatedId.HasValue;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning("Nombre de perfil duplicado en actualización: {Nombre}", perfil.Nombre);
            throw new DuplicateEntityException(
                $"Ya existe un perfil con el nombre '{perfil.Nombre}' en esta clínica.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar perfil {Id}", perfil.Id);
            throw;
        }
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        // Primero verificar si tiene usuarios asignados
        var usuarioCount = await CountUsuariosAsync(id, clinicaId);
        if (usuarioCount > 0)
        {
            _logger.LogWarning("No se puede desactivar perfil {Id}: tiene {Count} usuarios asignados", id, usuarioCount);
            return false; // Will be handled by BLL as a validation error
        }

        const string sql = @"
            UPDATE public.perfiles
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
            _logger.LogError(ex, "Error al desactivar perfil {Id}", id);
            throw;
        }
    }

    public async Task<bool> ExistsByNameAsync(Guid clinicaId, string nombre, Guid? excludeId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.perfiles
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
            _logger.LogError(ex, "Error al verificar existencia de perfil");
            throw;
        }
    }

    public async Task<int> CountUsuariosAsync(Guid perfilId, Guid clinicaId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.usuarios
            WHERE perfil_id = @PerfilId
              AND clinica_id = @ClinicaId
              AND activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql,
                new { PerfilId = perfilId, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al contar usuarios del perfil {PerfilId}", perfilId);
            throw;
        }
    }
}
