using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.tipos_diagnostico.
/// Implementa ITipoDiagnosticoRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU13 — Catálogo de Tipos de Diagnóstico
/// </summary>
public class TipoDiagnosticoRepository : ITipoDiagnosticoRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<TipoDiagnosticoRepository> _logger;

    public TipoDiagnosticoRepository(DbConnectionFactory dbConnectionFactory, ILogger<TipoDiagnosticoRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT (tabla simple, sin JOIN) ─────────────────
    private const string SelectColumns = @"
        td.id                AS Id,
        td.clinica_id        AS ClinicaId,
        td.nombre            AS Nombre,
        td.descripcion       AS Descripcion,
        td.activo            AS Activo,
        td.fecha_creacion    AS FechaCreacion,
        td.fecha_modificacion AS FechaModificacion,
        td.creado_por        AS CreadoPor,
        td.modificado_por    AS ModificadoPor";

    private const string FromTable = "FROM public.tipos_diagnostico td";

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todos los tipos de diagnóstico activos de una clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<TipoDiagnostico>> GetAllAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE td.clinica_id = @ClinicaId AND td.activo = true
            ORDER BY td.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<TipoDiagnostico>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipos de diagnóstico activos de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1b. GetAllIncludingInactiveAsync — Lista TODOS (activos + inactivos)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<TipoDiagnostico>> GetAllIncludingInactiveAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE td.clinica_id = @ClinicaId
            ORDER BY td.activo DESC, td.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<TipoDiagnostico>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los tipos de diagnóstico (incluyendo inactivos) de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un tipo de diagnóstico por ID validando clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<TipoDiagnostico?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE td.id = @Id AND td.clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<TipoDiagnostico>(sql, new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipo de diagnóstico {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo tipo de diagnóstico. Retorna el ID.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(TipoDiagnostico tipoDiagnostico)
    {
        const string sql = @"
            INSERT INTO public.tipos_diagnostico (
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
            return await connection.ExecuteScalarAsync<Guid>(sql, tipoDiagnostico);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de diagnóstico en clínica {ClinicaId}", tipoDiagnostico.ClinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del tipo de diagnóstico
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(TipoDiagnostico tipoDiagnostico)
    {
        const string sql = @"
            UPDATE public.tipos_diagnostico
            SET nombre              = @Nombre,
                descripcion         = @Descripcion,
                fecha_modificacion  = NOW(),
                modificado_por      = @ModificadoPor
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, tipoDiagnostico);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tipo de diagnóstico {Id}", tipoDiagnostico.Id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva tipo de diagnóstico (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.tipos_diagnostico
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
            _logger.LogError(ex, "Error al desactivar tipo de diagnóstico {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5b. ReactivateAsync — Reactiva tipo de diagnóstico (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ReactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.tipos_diagnostico
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
            _logger.LogError(ex, "Error al reactivar tipo de diagnóstico {Id}", id);
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
            FROM public.tipos_diagnostico
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
            _logger.LogError(ex, "Error al verificar existencia de nombre de tipo de diagnóstico en clínica {ClinicaId}", clinicaId);
            throw;
        }
    }
}
