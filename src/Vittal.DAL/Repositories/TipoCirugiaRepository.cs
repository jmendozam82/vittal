using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.tipos_cirugia.
/// Implementa ITipoCirugiaRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU11 — Catálogo de Tipos de Cirugías
/// </summary>
public class TipoCirugiaRepository : ITipoCirugiaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<TipoCirugiaRepository> _logger;

    public TipoCirugiaRepository(DbConnectionFactory dbConnectionFactory, ILogger<TipoCirugiaRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT (tabla simple, sin JOIN) ─────────────────
    private const string SelectColumns = @"
        tc.id                AS Id,
        tc.clinica_id        AS ClinicaId,
        tc.nombre            AS Nombre,
        tc.descripcion       AS Descripcion,
        tc.activo            AS Activo,
        tc.fecha_creacion    AS FechaCreacion,
        tc.fecha_modificacion AS FechaModificacion,
        tc.creado_por        AS CreadoPor,
        tc.modificado_por    AS ModificadoPor";

    private const string FromTable = "FROM public.tipos_cirugia tc";

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todos los tipos de cirugía activos de una clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<TipoCirugia>> GetAllAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE tc.clinica_id = @ClinicaId AND tc.activo = true
            ORDER BY tc.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<TipoCirugia>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipos de cirugía activos de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1b. GetAllIncludingInactiveAsync — Lista TODOS (activos + inactivos)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<TipoCirugia>> GetAllIncludingInactiveAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE tc.clinica_id = @ClinicaId
            ORDER BY tc.activo DESC, tc.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<TipoCirugia>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los tipos de cirugía (incluyendo inactivos) de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un tipo de cirugía por ID validando clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<TipoCirugia?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE tc.id = @Id AND tc.clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<TipoCirugia>(sql, new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipo de cirugía {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo tipo de cirugía. Retorna el ID.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(TipoCirugia tipoCirugia)
    {
        const string sql = @"
            INSERT INTO public.tipos_cirugia (
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
            return await connection.ExecuteScalarAsync<Guid>(sql, tipoCirugia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de cirugía en clínica {ClinicaId}", tipoCirugia.ClinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del tipo de cirugía
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(TipoCirugia tipoCirugia)
    {
        const string sql = @"
            UPDATE public.tipos_cirugia
            SET nombre              = @Nombre,
                descripcion         = @Descripcion,
                fecha_modificacion  = NOW(),
                modificado_por      = @ModificadoPor
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, tipoCirugia);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tipo de cirugía {Id}", tipoCirugia.Id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva tipo de cirugía (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.tipos_cirugia
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
            _logger.LogError(ex, "Error al desactivar tipo de cirugía {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5b. ReactivateAsync — Reactiva tipo de cirugía (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ReactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.tipos_cirugia
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
            _logger.LogError(ex, "Error al reactivar tipo de cirugía {Id}", id);
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
            FROM public.tipos_cirugia
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
            _logger.LogError(ex, "Error al verificar existencia de nombre de tipo de cirugía en clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de tipos de cirugía por término (ILIKE SQL)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<TipoCirugia>> SearchAsync(Guid clinicaId, string term, int limit = 20)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE tc.clinica_id = @ClinicaId AND tc.activo = true
              AND (tc.nombre ILIKE '%' || @Term || '%'
                   OR tc.descripcion ILIKE '%' || @Term || '%')
            ORDER BY tc.nombre
            LIMIT @Limit;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<TipoCirugia>(sql, new
            {
                ClinicaId = clinicaId,
                Term = term,
                Limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar tipos de cirugía con término '{Term}' en clínica {ClinicaId}", term, clinicaId);
            throw;
        }
    }
}
