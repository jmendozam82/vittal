using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.diagnosticos (catálogo de diagnósticos).
/// Implementa IDiagnosticoRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU14 — Gestión de Diagnósticos
/// </summary>
public class DiagnosticoRepository : IDiagnosticoRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<DiagnosticoRepository> _logger;

    public DiagnosticoRepository(DbConnectionFactory dbConnectionFactory, ILogger<DiagnosticoRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT con JOIN a tipos_diagnostico ──────────────
    private const string SelectColumns = @"
        d.id                     AS Id,
        d.clinica_id             AS ClinicaId,
        d.nombre                 AS Nombre,
        d.codigo_cie10           AS CodigoCie10,
        d.tipo_diagnostico_id    AS TipoDiagnosticoId,
        d.activo                 AS Activo,
        d.fecha_creacion         AS FechaCreacion,
        d.fecha_modificacion     AS FechaModificacion,
        d.creado_por             AS CreadoPor,
        d.modificado_por         AS ModificadoPor,
        td.nombre                AS TipoDiagnosticoNombre";

    private const string FromJoin = @"
        FROM public.diagnosticos d
        INNER JOIN public.tipos_diagnostico td ON d.tipo_diagnostico_id = td.id";

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todos los diagnósticos activos de una clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Diagnostico>> GetAllAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE d.clinica_id = @ClinicaId AND d.activo = true
            ORDER BY d.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Diagnostico>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener diagnósticos activos de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1b. GetAllIncludingInactiveAsync — Lista TODOS (activos + inactivos)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Diagnostico>> GetAllIncludingInactiveAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE d.clinica_id = @ClinicaId
            ORDER BY d.activo DESC, d.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Diagnostico>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los diagnósticos (incluyendo inactivos) de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un diagnóstico por ID validando clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Diagnostico?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE d.id = @Id AND d.clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Diagnostico>(sql, new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener diagnóstico {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo diagnóstico. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Diagnostico diagnostico)
    {
        const string sql = @"
            INSERT INTO public.diagnosticos (
                clinica_id, nombre, codigo_cie10, tipo_diagnostico_id,
                activo, fecha_creacion, creado_por
            )
            VALUES (
                @ClinicaId, @Nombre, @CodigoCie10, @TipoDiagnosticoId,
                true, NOW(), @CreadoPor
            )
            RETURNING id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, diagnostico);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear diagnóstico '{Nombre}' en clínica {ClinicaId}", diagnostico.Nombre, diagnostico.ClinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del diagnóstico
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(Diagnostico diagnostico)
    {
        const string sql = @"
            UPDATE public.diagnosticos
            SET nombre              = @Nombre,
                codigo_cie10        = @CodigoCie10,
                tipo_diagnostico_id = @TipoDiagnosticoId,
                fecha_modificacion  = NOW(),
                modificado_por      = @ModificadoPor
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, diagnostico);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar diagnóstico {Id}", diagnostico.Id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva diagnóstico (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.diagnosticos
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
            _logger.LogError(ex, "Error al desactivar diagnóstico {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5b. ReactivateAsync — Reactiva diagnóstico (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ReactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.diagnosticos
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
            _logger.LogError(ex, "Error al reactivar diagnóstico {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. ExistsByNombreAsync — Verifica duplicado por nombre en la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ExistsByNombreAsync(Guid clinicaId, string nombre, Guid? excludeId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.diagnosticos
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
            _logger.LogError(ex, "Error al verificar existencia de diagnóstico '{Nombre}' en clínica {ClinicaId}", nombre, clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. SearchAsync — Búsqueda por nombre, código CIE-10 o tipo de diagnóstico
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Diagnostico>> SearchAsync(Guid clinicaId, string term)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE d.clinica_id = @ClinicaId
              AND d.activo = true
              AND (
                  d.nombre ILIKE @Term
                  OR d.codigo_cie10 ILIKE @Term
                  OR td.nombre ILIKE @Term
              )
            ORDER BY d.nombre
            LIMIT 50;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var searchTerm = $"%{term}%";
            return await connection.QueryAsync<Diagnostico>(sql, new { ClinicaId = clinicaId, Term = searchTerm });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar diagnósticos con término '{Term}' en clínica {ClinicaId}", term, clinicaId);
            throw;
        }
    }
}
