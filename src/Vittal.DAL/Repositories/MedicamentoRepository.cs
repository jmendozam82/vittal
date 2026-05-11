using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.medicamentos.
/// Implementa IMedicamentoRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU08 — Catálogo de Medicamentos
/// </summary>
public class MedicamentoRepository : IMedicamentoRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<MedicamentoRepository> _logger;

    public MedicamentoRepository(DbConnectionFactory dbConnectionFactory, ILogger<MedicamentoRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT (sin JOIN, tabla simple) ───────────────────
    private const string SelectColumns = @"
        m.id                AS Id,
        m.clinica_id        AS ClinicaId,
        m.nombre            AS Nombre,
        m.descripcion       AS Descripcion,
        m.concentracion     AS Concentracion,
        m.unidad_medida     AS UnidadMedida,
        m.activo            AS Activo,
        m.fecha_creacion    AS FechaCreacion,
        m.fecha_modificacion AS FechaModificacion,
        m.creado_por        AS CreadoPor,
        m.modificado_por    AS ModificadoPor";

    private const string FromTable = "FROM public.medicamentos m";

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todos los medicamentos activos de una clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Medicamento>> GetAllAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE m.clinica_id = @ClinicaId AND m.activo = true
            ORDER BY m.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Medicamento>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener medicamentos activos de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1b. GetAllIncludingInactiveAsync — Lista TODOS (activos + inactivos)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Medicamento>> GetAllIncludingInactiveAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE m.clinica_id = @ClinicaId
            ORDER BY m.activo DESC, m.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Medicamento>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los medicamentos (incluyendo inactivos) de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un medicamento por ID validando clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Medicamento?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE m.id = @Id AND m.clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Medicamento>(sql, new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener medicamento {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo medicamento. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Medicamento medicamento)
    {
        const string sql = @"
            INSERT INTO public.medicamentos (
                clinica_id, nombre, descripcion, concentracion, unidad_medida,
                activo, fecha_creacion, creado_por
            )
            VALUES (
                @ClinicaId, @Nombre, @Descripcion, @Concentracion, @UnidadMedida,
                true, NOW(), @CreadoPor
            )
            RETURNING id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, medicamento);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear medicamento en clínica {ClinicaId}", medicamento.ClinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del medicamento
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(Medicamento medicamento)
    {
        const string sql = @"
            UPDATE public.medicamentos
            SET nombre              = @Nombre,
                descripcion         = @Descripcion,
                concentracion       = @Concentracion,
                unidad_medida       = @UnidadMedida,
                fecha_modificacion  = NOW(),
                modificado_por      = @ModificadoPor
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, medicamento);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar medicamento {Id}", medicamento.Id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva medicamento (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.medicamentos
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
            _logger.LogError(ex, "Error al desactivar medicamento {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5b. ReactivateAsync — Reactiva medicamento (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ReactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.medicamentos
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
            _logger.LogError(ex, "Error al reactivar medicamento {Id}", id);
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
            FROM public.medicamentos
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
            _logger.LogError(ex, "Error al verificar existencia de nombre de medicamento en clínica {ClinicaId}", clinicaId);
            throw;
        }
    }
}
