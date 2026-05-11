using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.recomendaciones.
/// Implementa IRecomendacionRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU16 — Catálogo de Recomendaciones
/// </summary>
public class RecomendacionRepository : IRecomendacionRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<RecomendacionRepository> _logger;

    public RecomendacionRepository(DbConnectionFactory dbConnectionFactory, ILogger<RecomendacionRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT (sin JOIN, tabla simple) ───────────────────
    private const string SelectColumns = @"
        r.id                AS Id,
        r.clinica_id        AS ClinicaId,
        r.nombre            AS Nombre,
        r.descripcion       AS Descripcion,
        r.activo            AS Activo,
        r.fecha_creacion    AS FechaCreacion,
        r.fecha_modificacion AS FechaModificacion,
        r.creado_por        AS CreadoPor,
        r.modificado_por    AS ModificadoPor";

    private const string FromTable = "FROM public.recomendaciones r";

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todas las recomendaciones activas de una clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Recomendacion>> GetAllAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE r.clinica_id = @ClinicaId AND r.activo = true
            ORDER BY r.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Recomendacion>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener recomendaciones activas de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1b. GetAllIncludingInactiveAsync — Lista TODAS (activas + inactivas)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Recomendacion>> GetAllIncludingInactiveAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE r.clinica_id = @ClinicaId
            ORDER BY r.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Recomendacion>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las recomendaciones (incluyendo inactivas) de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene una recomendación por ID validando clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Recomendacion?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE r.id = @Id AND r.clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Recomendacion>(sql, new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener recomendación {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta una nueva recomendación. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Recomendacion recomendacion)
    {
        const string sql = @"
            INSERT INTO public.recomendaciones (
                clinica_id, nombre, descripcion,
                activo, fecha_creacion, creado_por
            )
            VALUES (
                @ClinicaId, @Nombre, @Descripcion,
                true, NOW(), @CreadoPor
            )
            RETURNING id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, recomendacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear recomendación en clínica {ClinicaId}", recomendacion.ClinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos de la recomendación
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(Recomendacion recomendacion)
    {
        const string sql = @"
            UPDATE public.recomendaciones
            SET nombre              = @Nombre,
                descripcion         = @Descripcion,
                fecha_modificacion  = NOW(),
                modificado_por      = @ModificadoPor
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, recomendacion);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar recomendación {Id}", recomendacion.Id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva recomendación (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.recomendaciones
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar recomendación {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5b. ReactivateAsync — Reactiva recomendación (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ReactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.recomendaciones
            SET activo = true, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar recomendación {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. ExistsByNombreAsync — Verifica duplicado de nombre en la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ExistsByNombreAsync(Guid clinicaId, string nombre, Guid? excludeId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.recomendaciones
            WHERE clinica_id = @ClinicaId
              AND LOWER(nombre) = LOWER(@Nombre)
              AND (@ExcludeId IS NULL OR id != @ExcludeId);";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                ClinicaId = clinicaId,
                Nombre = nombre,
                ExcludeId = excludeId
            });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de nombre de recomendación en clínica {ClinicaId}", clinicaId);
            throw;
        }
    }
}
