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
/// Repositorio para contactos de landing page.
/// Tabla global del sistema (sin clinica_id) — excepción CLAUDE.md §12.
/// Solo el Super Admin puede gestionar estos contactos.
/// Historia de Usuario: HU-L01 — Landing Page Informativa
/// </summary>
public class ContactoLandingRepository : IContactoLandingRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ContactoLandingRepository> _logger;

    public ContactoLandingRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<ContactoLandingRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas SELECT ────────────────────────────────────────────────────
    private const string SelectColumns = @"
        id                  AS Id,
        nombre_completo     AS NombreCompleto,
        email               AS Email,
        telefono            AS Telefono,
        rol                 AS Rol,
        mensaje             AS Mensaje,
        leido               AS Leido,
        activo              AS Activo,
        fecha_creacion      AS FechaCreacion,
        fecha_modificacion  AS FechaModificacion";

    // ────────────────────────────────────────────────────────────────────────
    // 1. CreateAsync — Inserta un nuevo contacto de landing. Retorna el ID.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(ContactoLanding contacto)
    {
        const string sql = @"
            INSERT INTO contactos_landing (
                nombre_completo, email, telefono, rol, mensaje,
                activo, leido, fecha_creacion
            )
            VALUES (
                @NombreCompleto, @Email, @Telefono, @Rol, @Mensaje,
                true, false, NOW()
            )
            RETURNING id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, contacto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear contacto de landing para email {Email}", contacto.Email);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un contacto por ID (solo activos)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<ContactoLanding?> GetByIdAsync(Guid id)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            FROM contactos_landing
            WHERE id = @Id AND activo = true;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<ContactoLanding>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener contacto de landing {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. GetAllAsync — Lista todos los contactos activos (vista admin)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ContactoLanding>> GetAllAsync()
    {
        const string sql = $@"
            SELECT {SelectColumns}
            FROM contactos_landing
            WHERE activo = true
            ORDER BY fecha_creacion DESC;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<ContactoLanding>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener contactos de landing");
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. MarkAsReadAsync — Marca un contacto como leído por el admin
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> MarkAsReadAsync(Guid id)
    {
        const string sql = @"
            UPDATE contactos_landing
            SET leido = true, fecha_modificacion = NOW()
            WHERE id = @Id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar contacto de landing como leído {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva contacto (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id)
    {
        const string sql = @"
            UPDATE contactos_landing
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
            _logger.LogError(ex, "Error al desactivar contacto de landing {Id}", id);
            throw;
        }
    }
}
