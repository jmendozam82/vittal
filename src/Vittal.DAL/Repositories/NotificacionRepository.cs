using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Exceptions;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Shared;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.notificaciones.
/// Modelo estándar: la notificación es un mensaje compartido por clínica y el
/// estado de lectura es individual por usuario en notificaciones_usuario.
/// Implementa INotificacionRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class NotificacionRepository : INotificacionRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<NotificacionRepository> _logger;

    public NotificacionRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<NotificacionRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    private const string SelectColumns = @"
        n.id                    AS Id,
        n.clinica_id            AS ClinicaId,
        n.alerta_id             AS AlertaId,
        n.tipo                  AS Tipo,
        n.titulo                AS Titulo,
        n.mensaje               AS Mensaje,
        n.icono                 AS Icono,
        n.color                 AS Color,
        nu.leida                AS Leida,
        n.usuario_destino_id    AS UsuarioDestinoId,
        nu.fecha_lectura        AS FechaLectura,
        n.activo                AS Activo,
        n.fecha_creacion        AS FechaCreacion";

    // Columnas para consultas paginadas (a nivel notificación, sin estado por usuario)
    private const string SelectColumnsLegacy = @"
        n.id                    AS Id,
        n.clinica_id            AS ClinicaId,
        n.alerta_id             AS AlertaId,
        n.tipo                  AS Tipo,
        n.titulo                AS Titulo,
        n.mensaje               AS Mensaje,
        n.icono                 AS Icono,
        n.color                 AS Color,
        n.leida                 AS Leida,
        n.usuario_destino_id    AS UsuarioDestinoId,
        n.fecha_lectura         AS FechaLectura,
        n.activo                AS Activo,
        n.fecha_creacion        AS FechaCreacion";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetByClinicaIdAsync — Notificaciones del usuario (vía asignación)
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Notificacion>> GetByClinicaIdAsync(
        Guid clinicaId, Guid usuarioId, bool? leida = null, int? limit = null)
    {
        var sql = $@"
            SELECT {SelectColumns}
            FROM public.notificaciones n
            JOIN public.notificaciones_usuario nu ON nu.notificacion_id = n.id
                 AND nu.usuario_id = @UsuarioId
            WHERE n.clinica_id = @ClinicaId
              AND n.activo = true
              {BuildLeidaFilter(leida)}
            ORDER BY n.fecha_creacion DESC
            {BuildLimitClause(limit)}";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Notificacion>(sql,
                new { ClinicaId = clinicaId, UsuarioId = usuarioId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener notificaciones de usuario {UsuarioId} en clínica {ClinicaId}",
                usuarioId, clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. CreateAsync — Crea la notificación y la asigna a los destinos.
    //    Retorna el ID. Es transaccional (creación + asignación).
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Notificacion entity)
    {
        const string insertSql = @"
            INSERT INTO public.notificaciones (
                clinica_id, alerta_id,
                tipo, titulo, mensaje, icono, color,
                leida, usuario_destino_id, fecha_lectura,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @AlertaId,
                @Tipo, @Titulo, @Mensaje, @Icono, @Color,
                false, @UsuarioDestinoId, NULL,
                true, NOW()
            )
            RETURNING id";

        const string assignUsuarioSql = @"
            INSERT INTO public.notificaciones_usuario (notificacion_id, usuario_id, leida, fecha_lectura, fecha_creacion)
            VALUES (@NotificacionId, @UsuarioId, false, NULL, NOW())
            ON CONFLICT (notificacion_id, usuario_id) DO NOTHING";

        const string assignClinicaSql = @"
            INSERT INTO public.notificaciones_usuario (notificacion_id, usuario_id, leida, fecha_lectura, fecha_creacion)
            SELECT @NotificacionId, u.id, false, NULL, NOW()
            FROM public.usuarios u
            WHERE u.clinica_id = @ClinicaId
              AND u.activo = true
            ON CONFLICT (notificacion_id, usuario_id) DO NOTHING";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                var id = await connection.ExecuteScalarAsync<Guid>(insertSql, entity, tx);

                if (entity.UsuarioDestinoId.HasValue)
                {
                    await connection.ExecuteAsync(assignUsuarioSql,
                        new { NotificacionId = id, UsuarioId = entity.UsuarioDestinoId.Value },
                        tx);
                }
                else
                {
                    await connection.ExecuteAsync(assignClinicaSql,
                        new { NotificacionId = id, ClinicaId = entity.ClinicaId },
                        tx);
                }

                tx.Commit();
                return id;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear notificación en clínica {ClinicaId}",
                entity.ClinicaId);
            throw new RepositoryException("Error al crear la notificación.", ex);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. MarcarLeidaAsync — Marca una notificación como leída SOLO para el usuario
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> MarcarLeidaAsync(Guid clinicaId, Guid usuarioId, Guid id)
    {
        const string sql = @"
            UPDATE public.notificaciones_usuario nu
            SET leida = true,
                fecha_lectura = NOW()
            FROM public.notificaciones n
            WHERE n.id = nu.notificacion_id
              AND n.clinica_id = @ClinicaId
              AND n.activo = true
              AND nu.notificacion_id = @Id
              AND nu.usuario_id = @UsuarioId";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql,
                new { Id = id, ClinicaId = clinicaId, UsuarioId = usuarioId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar notificación {Id} como leída", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. MarcarTodasLeidasAsync — Marca todas las del usuario como leídas
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> MarcarTodasLeidasAsync(Guid clinicaId, Guid usuarioId)
    {
        const string sql = @"
            UPDATE public.notificaciones_usuario nu
            SET leida = true,
                fecha_lectura = NOW()
            FROM public.notificaciones n
            WHERE n.id = nu.notificacion_id
              AND n.clinica_id = @ClinicaId
              AND n.activo = true
              AND nu.usuario_id = @UsuarioId
              AND nu.leida = false";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql,
                new { ClinicaId = clinicaId, UsuarioId = usuarioId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al marcar todas las notificaciones como leídas para usuario {UsuarioId} en clínica {ClinicaId}",
                usuarioId, clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. GetNoLeidasCountAsync — Conteo no leídas del usuario
    // ────────────────────────────────────────────────────────────────────
    public async Task<int> GetNoLeidasCountAsync(Guid clinicaId, Guid usuarioId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.notificaciones_usuario nu
            JOIN public.notificaciones n ON n.id = nu.notificacion_id
            WHERE n.clinica_id = @ClinicaId
              AND n.activo = true
              AND nu.usuario_id = @UsuarioId
              AND nu.leida = false";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql,
                new { ClinicaId = clinicaId, UsuarioId = usuarioId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al contar notificaciones no leídas de usuario {UsuarioId} en clínica {ClinicaId}",
                usuarioId, clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. GetAllPaginatedAsync — Página de notificaciones (por notificación,
    //    no por usuario). Se conserva como parte del contrato IPaginatedRepository.
    // ────────────────────────────────────────────────────────────────────
    public async Task<PaginatedResultDto<Notificacion>> GetAllPaginatedAsync(
        Guid clinicaId, PaginationFilterDto filter)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize > 100 ? 100 : filter.PageSize;
        var offset = (page - 1) * pageSize;
        var searchTerm = string.IsNullOrWhiteSpace(filter.SearchTerm)
            ? null
            : $"%{filter.SearchTerm.Trim()}%";

        const string baseWhere = @"
            WHERE n.clinica_id = @ClinicaId
              AND n.activo = true
              AND (@SearchTerm IS NULL
                   OR n.titulo ILIKE @SearchTerm
                   OR n.mensaje ILIKE @SearchTerm
                   OR n.tipo ILIKE @SearchTerm)";

        var sql = $@"
            WITH filtered AS (
                SELECT 1
                FROM public.notificaciones n
                {baseWhere}
            )
            SELECT COUNT(1) FROM filtered;

            SELECT {SelectColumnsLegacy}
            FROM public.notificaciones n
            {baseWhere}
            ORDER BY n.fecha_creacion DESC
            LIMIT @PageSize OFFSET @Offset;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var multi = await connection.QueryMultipleAsync(sql, new
            {
                ClinicaId = clinicaId,
                SearchTerm = searchTerm,
                PageSize = pageSize,
                Offset = offset
            });

            var totalCount = await multi.ReadSingleAsync<int>();
            var items = await multi.ReadAsync<Notificacion>();

            return new PaginatedResultDto<Notificacion>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener notificaciones paginadas de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private static string BuildLeidaFilter(bool? leida)
    {
        if (!leida.HasValue) return string.Empty;
        return leida.Value ? "AND nu.leida = true" : "AND nu.leida = false";
    }

    private static string BuildLimitClause(int? limit)
    {
        if (!limit.HasValue || limit.Value <= 0) return string.Empty;
        return $"LIMIT {limit.Value}";
    }
}