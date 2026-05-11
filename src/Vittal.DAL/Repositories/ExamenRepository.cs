using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.examenes.
/// Implementa IExamenRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU17 — Catálogo de Exámenes
/// </summary>
public class ExamenRepository : IExamenRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ExamenRepository> _logger;

    public ExamenRepository(DbConnectionFactory dbConnectionFactory, ILogger<ExamenRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT (sin JOIN, tabla simple) ───────────────────
    private const string SelectColumns = @"
        e.id                AS Id,
        e.clinica_id        AS ClinicaId,
        e.nombre            AS Nombre,
        e.descripcion       AS Descripcion,
        e.activo            AS Activo,
        e.fecha_creacion    AS FechaCreacion,
        e.fecha_modificacion AS FechaModificacion,
        e.creado_por        AS CreadoPor,
        e.modificado_por    AS ModificadoPor";

    private const string FromTable = "FROM public.examenes e";

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todos los exámenes activos de una clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Examen>> GetAllAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE e.clinica_id = @ClinicaId AND e.activo = true
            ORDER BY e.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Examen>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener exámenes activos de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1b. GetAllIncludingInactiveAsync — Lista TODOS (activos + inactivos)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Examen>> GetAllIncludingInactiveAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE e.clinica_id = @ClinicaId
            ORDER BY e.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Examen>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los exámenes (incluyendo inactivos) de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un examen por ID validando clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Examen?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE e.id = @Id AND e.clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Examen>(sql, new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener examen {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo examen. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Examen examen)
    {
        const string sql = @"
            INSERT INTO public.examenes (
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
            return await connection.ExecuteScalarAsync<Guid>(sql, examen);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear examen en clínica {ClinicaId}", examen.ClinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del examen
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(Examen examen)
    {
        const string sql = @"
            UPDATE public.examenes
            SET nombre              = @Nombre,
                descripcion         = @Descripcion,
                fecha_modificacion  = NOW(),
                modificado_por      = @ModificadoPor
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, examen);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar examen {Id}", examen.Id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva examen (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.examenes
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
            _logger.LogError(ex, "Error al desactivar examen {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5b. ReactivateAsync — Reactiva examen (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ReactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.examenes
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
            _logger.LogError(ex, "Error al reactivar examen {Id}", id);
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
            FROM public.examenes
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
            _logger.LogError(ex, "Error al verificar existencia de nombre de examen en clínica {ClinicaId}", clinicaId);
            throw;
        }
    }
}
