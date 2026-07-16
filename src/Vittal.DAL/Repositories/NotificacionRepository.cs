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
        n.leida                 AS Leida,
        n.usuario_destino_id    AS UsuarioDestinoId,
        n.fecha_lectura         AS FechaLectura,
        n.activo                AS Activo,
        n.fecha_creacion        AS FechaCreacion";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetByClinicaIdAsync — Notificaciones filtradas por clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Notificacion>> GetByClinicaIdAsync(
        Guid clinicaId, bool? leida = null, int? limit = null)
    {
        var sql = $@"
            SELECT {SelectColumns}
            FROM public.notificaciones n
            WHERE n.clinica_id = @ClinicaId
              AND n.activo = true
              {BuildLeidaFilter(leida)}
            ORDER BY n.fecha_creacion DESC
            {BuildLimitClause(limit)}";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Notificacion>(sql,
                new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener notificaciones de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. CreateAsync — Crea una nueva notificación. Retorna el ID.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Notificacion entity)
    {
        const string sql = @"
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

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear notificación en clínica {ClinicaId}",
                entity.ClinicaId);
            throw new RepositoryException("Error al crear la notificación.", ex);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. MarcarLeidaAsync — Marca una notificación como leída
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> MarcarLeidaAsync(Guid clinicaId, Guid id)
    {
        const string sql = @"
            UPDATE public.notificaciones
            SET leida = true,
                fecha_lectura = NOW()
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
            _logger.LogError(ex, "Error al marcar notificación {Id} como leída", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. MarcarTodasLeidasAsync — Marca todas las no leídas como leídas
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> MarcarTodasLeidasAsync(Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.notificaciones
            SET leida = true,
                fecha_lectura = NOW()
            WHERE clinica_id = @ClinicaId
              AND leida = false
              AND activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql,
                new { ClinicaId = clinicaId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al marcar todas las notificaciones como leídas en clínica {ClinicaId}",
                clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. GetNoLeidasCountAsync — Conteo de notificaciones no leídas
    // ────────────────────────────────────────────────────────────────────
    public async Task<int> GetNoLeidasCountAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.notificaciones
            WHERE clinica_id = @ClinicaId
              AND leida = false
              AND activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql,
                new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al contar notificaciones no leídas de clínica {ClinicaId}",
                clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. GetAllPaginatedAsync — Página de notificaciones con búsqueda ILIKE
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

            SELECT {SelectColumns}
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
        return leida.Value ? "AND n.leida = true" : "AND n.leida = false";
    }

    private static string BuildLimitClause(int? limit)
    {
        if (!limit.HasValue || limit.Value <= 0) return string.Empty;
        return $"LIMIT {limit.Value}";
    }
}
