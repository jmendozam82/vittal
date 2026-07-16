using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Exceptions;
using Vittal.DAL.Interfaces;
using Vittal.DTO.UsuarioSala;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.usuarios_salas.
/// Implementa IUsuarioSalaRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU06 — Asignar Doctores a Salas
/// </summary>
public class UsuarioSalaRepository : IUsuarioSalaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<UsuarioSalaRepository> _logger;

    public UsuarioSalaRepository(DbConnectionFactory dbConnectionFactory, ILogger<UsuarioSalaRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Columnas SELECT con JOIN a usuarios y salas para retornar el DTO de lectura.
    /// </summary>
    private const string SelectColumns = @"
        us.id                AS Id,
        us.usuario_id        AS UsuarioId,
        u.nombres || ' ' || u.apellidos AS UsuarioNombre,
        u.email              AS UsuarioEmail,
        us.sala_id           AS SalaId,
        s.nombre             AS SalaNombre,
        us.activo            AS Activo,
        us.fecha_creacion    AS FechaCreacion";

    private const string FromJoin = @"
        FROM public.usuarios_salas us
        INNER JOIN public.usuarios u ON u.id = us.usuario_id AND u.activo = true
        INNER JOIN public.salas s ON s.id = us.sala_id AND s.activo = true";

    #region ── Consultas (Lectura) ────────────────────────────────────

    public async Task<IEnumerable<UsuarioSalaResponseDto>> GetBySalaAsync(Guid clinicaId, Guid salaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE us.sala_id = @SalaId
              AND us.clinica_id = @ClinicaId
              AND us.activo = true
            ORDER BY u.apellidos, u.nombres";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<UsuarioSalaResponseDto>(sql,
                new { ClinicaId = clinicaId, SalaId = salaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener asignaciones de la sala {SalaId} en clínica {ClinicaId}",
                salaId, clinicaId);
            throw;
        }
    }

    public async Task<UsuarioSalaResponseDto?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE us.id = @Id
              AND us.clinica_id = @ClinicaId
              AND us.activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<UsuarioSalaResponseDto>(sql,
                new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener asignación {Id}", id);
            throw;
        }
    }

    #endregion

    #region ── Comandos (Escritura) ───────────────────────────────────

    public async Task<Guid> CreateAsync(UsuarioSala entity)
    {
        const string sql = @"
            INSERT INTO public.usuarios_salas (
                usuario_id, sala_id, clinica_id,
                activo, fecha_creacion
            ) VALUES (
                @UsuarioId, @SalaId, @ClinicaId,
                true, NOW()
            )
            RETURNING id";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, entity);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning(
                "Asignación duplicada en clínica {ClinicaId}: usuario {UsuarioId} → sala {SalaId}",
                entity.ClinicaId, entity.UsuarioId, entity.SalaId);
            throw new DuplicateEntityException(
                "El doctor ya está asignado a esta sala.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar doctor {UsuarioId} a sala {SalaId} en clínica {ClinicaId}",
                entity.UsuarioId, entity.SalaId, entity.ClinicaId);
            throw;
        }
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.usuarios_salas
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
            _logger.LogError(ex, "Error al desasignar doctor de sala (Id: {Id})", id);
            throw;
        }
    }

    #endregion
}
