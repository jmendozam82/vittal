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
/// Repositorio para la tabla raíz public.clinicas.
/// CASO ESPECIAL: Tabla raíz multi-tenant — NO tiene ClinicaId.
/// Los métodos NO reciben clinicaId como parámetro.
/// Historia de Usuario: HU09 — Gestión de Clínicas
/// </summary>
public class ClinicaRepository : IClinicaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ClinicaRepository> _logger;

    public ClinicaRepository(DbConnectionFactory dbConnectionFactory, ILogger<ClinicaRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT ────────────────────────────────────────────
    private const string SelectColumns = @"
        c.id                    AS Id,
        c.nombre                AS Nombre,
        c.direccion             AS Direccion,
        c.telefono              AS Telefono,
        c.email                 AS Email,
        c.logo_url              AS LogoUrl,
        c.tiempo_espera_minutos AS TiempoEsperaMinutos,
        c.bd_externa_1          AS BdExterna1,
        c.bd_externa_2          AS BdExterna2,
        c.activo                AS Activo,
        c.fecha_creacion        AS FechaCreacion,
        c.fecha_modificacion    AS FechaModificacion";

    private const string FromTable = @"
        FROM public.clinicas c";

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todas las clínicas activas (sin filtro de tenant)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Clinica>> GetAllAsync()
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE c.activo = true
            ORDER BY c.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Clinica>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clínicas activas");
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetAllIncludingInactiveAsync — Lista TODAS (activas primero)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Clinica>> GetAllIncludingInactiveAsync()
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            ORDER BY c.activo DESC, c.nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Clinica>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las clínicas (incluyendo inactivas)");
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. GetByIdAsync — Obtiene una clínica por ID
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Clinica?> GetByIdAsync(Guid id)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE c.id = @Id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Clinica>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clínica {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. GetCurrentClinicaAsync — Obtiene la clínica del contexto actual
    //    Usa app.current_clinica_id de la sesión de PostgreSQL
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Clinica?> GetCurrentClinicaAsync()
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE c.id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Clinica>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clínica del contexto actual");
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. CreateAsync — Inserta una nueva clínica. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Clinica clinica)
    {
        const string sql = @"
            INSERT INTO public.clinicas (
                nombre, direccion, telefono, email, logo_url,
                tiempo_espera_minutos, bd_externa_1, bd_externa_2,
                activo, fecha_creacion
            )
            VALUES (
                @Nombre, @Direccion, @Telefono, @Email, @LogoUrl,
                @TiempoEsperaMinutos, @BdExterna1, @BdExterna2,
                true, NOW()
            )
            RETURNING id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, clinica);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear clínica {Nombre}", clinica.Nombre);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. UpdateAsync — Actualiza datos de la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(Clinica clinica)
    {
        const string sql = @"
            UPDATE public.clinicas
            SET nombre                  = @Nombre,
                direccion               = @Direccion,
                telefono                = @Telefono,
                email                   = @Email,
                logo_url                = @LogoUrl,
                tiempo_espera_minutos   = @TiempoEsperaMinutos,
                bd_externa_1            = @BdExterna1,
                bd_externa_2            = @BdExterna2,
                fecha_modificacion      = NOW()
            WHERE id = @Id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, clinica);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar clínica {Id}", clinica.Id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. DeactivateAsync — Desactiva clínica (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id)
    {
        const string sql = @"
            UPDATE public.clinicas
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar clínica {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 8. ReactivateAsync — Reactiva clínica (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ReactivateAsync(Guid id)
    {
        const string sql = @"
            UPDATE public.clinicas
            SET activo = true, fecha_modificacion = NOW()
            WHERE id = @Id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar clínica {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 9. ExistsByNameAsync — Verifica duplicado de nombre (case-insensitive)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ExistsByNameAsync(string nombre, Guid? excludeId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.clinicas
            WHERE LOWER(nombre) = LOWER(@Nombre)
              AND (@ExcludeId IS NULL OR id != @ExcludeId);";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Nombre = nombre,
                ExcludeId = excludeId
            });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de clínica por nombre {Nombre}", nombre);
            throw;
        }
    }
}
