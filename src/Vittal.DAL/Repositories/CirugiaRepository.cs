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
/// Repositorio para la tabla public.cirugias.
/// Implementa ICirugiaRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU12 — Catálogo de Cirugías
/// </summary>
public class CirugiaRepository : ICirugiaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<CirugiaRepository> _logger;

    public CirugiaRepository(DbConnectionFactory dbConnectionFactory, ILogger<CirugiaRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT con JOIN a tipos_cirugia ──────────────
    private const string SelectColumns = @"
        c.id                AS Id,
        c.clinica_id        AS ClinicaId,
        c.tipo_cirugia_id   AS TipoCirugiaId,
        c.nombre            AS Nombre,
        c.descripcion       AS Descripcion,
        c.activo            AS Activo,
        c.fecha_creacion    AS FechaCreacion,
        c.fecha_modificacion AS FechaModificacion,
        c.creado_por        AS CreadoPor,
        c.modificado_por    AS ModificadoPor,
        tc.nombre           AS TipoCirugiaNombre";

    private const string FromJoin = @"
        FROM public.cirugias c
        INNER JOIN public.tipos_cirugia tc ON c.tipo_cirugia_id = tc.id";

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todas las cirugías activas de una clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Cirugia>> GetAllAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE c.clinica_id = @ClinicaId AND c.activo = true
            ORDER BY tc.nombre, c.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Cirugia>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cirugías activas de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1b. GetAllIncludingInactiveAsync — Lista TODAS (activas + inactivas)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Cirugia>> GetAllIncludingInactiveAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE c.clinica_id = @ClinicaId
            ORDER BY c.activo DESC, tc.nombre, c.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Cirugia>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las cirugías (incluyendo inactivas) de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene una cirugía por ID validando clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Cirugia?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE c.id = @Id AND c.clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Cirugia>(sql, new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cirugía {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta una nueva cirugía. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Cirugia cirugia)
    {
        const string sql = @"
            INSERT INTO public.cirugias (
                clinica_id, tipo_cirugia_id, nombre, descripcion,
                activo, fecha_creacion, creado_por
            )
            VALUES (
                @ClinicaId, @TipoCirugiaId, @Nombre, @Descripcion,
                true, NOW(), @CreadoPor
            )
            RETURNING id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, cirugia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cirugía en clínica {ClinicaId}", cirugia.ClinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos de la cirugía
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(Cirugia cirugia)
    {
        const string sql = @"
            UPDATE public.cirugias
            SET tipo_cirugia_id    = @TipoCirugiaId,
                nombre              = @Nombre,
                descripcion         = @Descripcion,
                fecha_modificacion  = NOW(),
                modificado_por      = @ModificadoPor
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, cirugia);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cirugía {Id}", cirugia.Id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva cirugía (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.cirugias
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
            _logger.LogError(ex, "Error al desactivar cirugía {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5b. ReactivateAsync — Reactiva cirugía (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ReactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.cirugias
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
            _logger.LogError(ex, "Error al reactivar cirugía {Id}", id);
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
            FROM public.cirugias
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
            _logger.LogError(ex, "Error al verificar existencia de nombre de cirugía en clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda de cirugías por término (ILIKE SQL)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Cirugia>> SearchAsync(Guid clinicaId, string term, int limit = 20)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE c.clinica_id = @ClinicaId AND c.activo = true
              AND (c.nombre ILIKE '%' || @Term || '%'
                   OR c.descripcion ILIKE '%' || @Term || '%'
                   OR tc.nombre ILIKE '%' || @Term || '%')
            ORDER BY c.nombre
            LIMIT @Limit;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Cirugia>(sql, new
            {
                ClinicaId = clinicaId,
                Term = term,
                Limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar cirugías con término '{Term}' en clínica {ClinicaId}", term, clinicaId);
            throw;
        }
    }
}
