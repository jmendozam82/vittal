using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Exceptions;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Shared;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.reportes y reporte_parametros.
/// Implementa IReporteRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU22 — Reportes
/// </summary>
public class ReporteRepository : IReporteRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ReporteRepository> _logger;

    public ReporteRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<ReporteRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    private const string SelectColumns = @"
        r.id                AS Id,
        r.clinica_id        AS ClinicaId,
        r.nombre            AS Nombre,
        r.tipo              AS Tipo,
        r.descripcion       AS Descripcion,
        r.formato           AS Formato,
        r.contenido_json    AS ContenidoJson,
        r.fecha_inicio      AS FechaInicio,
        r.fecha_fin         AS FechaFin,
        r.activo            AS Activo,
        r.fecha_creacion    AS FechaCreacion,
        r.fecha_modificacion AS FechaModificacion,
        r.creado_por        AS CreadoPor";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetAllByClinicaIdAsync — Lista todos los reportes activos
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Reporte>> GetAllByClinicaIdAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT " + SelectColumns + @"
            FROM public.reportes r
            WHERE r.clinica_id = @ClinicaId
              AND r.activo = true
            ORDER BY r.fecha_creacion DESC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Reporte>(sql,
                new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener reportes de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. CreateAsync — Crea un nuevo reporte. Retorna el ID.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Reporte entity)
    {
        const string sql = @"
            INSERT INTO public.reportes (
                clinica_id, nombre, tipo, descripcion, formato,
                contenido_json, fecha_inicio, fecha_fin,
                activo, fecha_creacion, creado_por
            )
            VALUES (
                @ClinicaId, @Nombre, @Tipo, @Descripcion, @Formato,
                @ContenidoJson::jsonb, @FechaInicio, @FechaFin,
                true, NOW(), @CreadoPor
            )
            RETURNING id";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear reporte en clínica {ClinicaId}",
                entity.ClinicaId);
            throw new RepositoryException("Error al crear el reporte.", ex);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. GetByIdAsync — Obtiene un reporte por ID
    // ────────────────────────────────────────────────────────────────────
    public async Task<Reporte?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        const string sql = @"
            SELECT " + SelectColumns + @"
            FROM public.reportes r
            WHERE r.id = @Id
              AND r.clinica_id = @ClinicaId
              AND r.activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Reporte>(sql,
                new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reporte {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. DeactivateAsync — Desactiva reporte (activo = false)
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        const string sql = @"
            UPDATE public.reportes
            SET activo = false, fecha_modificacion = NOW()
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
            _logger.LogError(ex, "Error al desactivar reporte {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. GetAllPaginatedAsync — Página de reportes con búsqueda ILIKE
    // ────────────────────────────────────────────────────────────────────
    public async Task<PaginatedResultDto<Reporte>> GetAllPaginatedAsync(
        Guid clinicaId, PaginationFilterDto filter)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize > 100 ? 100 : filter.PageSize;
        var offset = (page - 1) * pageSize;
        var searchTerm = string.IsNullOrWhiteSpace(filter.SearchTerm)
            ? null
            : $"%{filter.SearchTerm.Trim()}%";

        const string baseWhere = @"
            WHERE r.clinica_id = @ClinicaId
              AND r.activo = true
              AND (@SearchTerm IS NULL
                   OR r.nombre ILIKE @SearchTerm
                   OR r.tipo ILIKE @SearchTerm
                   OR r.descripcion ILIKE @SearchTerm)";

        var sql = $@"
            WITH filtered AS (
                SELECT 1
                FROM public.reportes r
                {baseWhere}
            )
            SELECT COUNT(1) FROM filtered;

            SELECT {SelectColumns}
            FROM public.reportes r
            {baseWhere}
            ORDER BY r.fecha_creacion DESC
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
            var items = await multi.ReadAsync<Reporte>();

            return new PaginatedResultDto<Reporte>
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
                "Error al obtener reportes paginados de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. ExecuteReportQueryAsync — Consulta dinámica según tipo de reporte
    // ────────────────────────────────────────────────────────────────────
    public async Task<string> ExecuteReportQueryAsync(
        string tipo,
        Guid clinicaId,
        DateTime fechaInicio,
        DateTime fechaFin,
        Guid? doctorId = null,
        Guid? salaId = null)
    {
        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var sql = BuildReportQuery(tipo);

            var data = await connection.QueryAsync(sql,
                new
                {
                    ClinicaId = clinicaId,
                    FechaInicio = DateOnly.FromDateTime(fechaInicio),
                    FechaFin = DateOnly.FromDateTime(fechaFin),
                    DoctorId = doctorId,
                    SalaId = salaId
                });

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al ejecutar consulta de reporte tipo {Tipo} para clínica {ClinicaId}",
                tipo, clinicaId);
            throw new RepositoryException("Error al generar los datos del reporte.", ex);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Construye la consulta SQL según el tipo de reporte.
    /// </summary>
    private static string BuildReportQuery(string tipo)
    {
        return tipo.ToLowerInvariant() switch
        {
            "pacientes_por_dia" => @"
                SELECT
                    c.fecha_cita::text AS Etiqueta,
                    COUNT(DISTINCT c.paciente_id) AS Valor
                FROM public.citas c
                WHERE c.clinica_id = @ClinicaId
                  AND c.fecha_cita BETWEEN @FechaInicio AND @FechaFin
                  AND c.estado = 'atendida'
                  AND c.activo = true
                GROUP BY c.fecha_cita
                ORDER BY c.fecha_cita ASC",

            "citas_por_estado" => @"
                SELECT
                    c.estado AS Etiqueta,
                    COUNT(*)::int AS Valor
                FROM public.citas c
                WHERE c.clinica_id = @ClinicaId
                  AND c.fecha_cita BETWEEN @FechaInicio AND @FechaFin
                  AND c.activo = true
                GROUP BY c.estado
                ORDER BY Valor DESC",

            "doctores_mas_activos" => @"
                SELECT
                    u.nombres || ' ' || u.apellidos AS Etiqueta,
                    COUNT(*)::int AS Valor
                FROM public.citas c
                INNER JOIN public.usuarios u ON u.id = c.doctor_id
                WHERE c.clinica_id = @ClinicaId
                  AND c.fecha_cita BETWEEN @FechaInicio AND @FechaFin
                  AND c.estado = 'atendida'
                  AND c.activo = true
                  AND (@DoctorId IS NULL OR c.doctor_id = @DoctorId)
                GROUP BY u.nombres, u.apellidos
                ORDER BY Valor DESC
                LIMIT 10",

            "tiempo_promedio_espera" => @"
                SELECT
                    c.fecha_cita::text AS Etiqueta,
                    COALESCE(
                        AVG(
                            EXTRACT(EPOCH FROM (
                                (c.fecha_cita + c.hora_llegada) - (c.fecha_cita + c.hora_cita)
                            )) / 60
                        ), 0
                    ) AS Valor
                FROM public.citas c
                WHERE c.clinica_id = @ClinicaId
                  AND c.fecha_cita BETWEEN @FechaInicio AND @FechaFin
                  AND c.hora_llegada IS NOT NULL
                  AND c.estado IN ('atendida', 'en_atencion')
                  AND c.activo = true
                GROUP BY c.fecha_cita
                ORDER BY c.fecha_cita ASC",

            // ── Reportes del UI (tabs) ──────────────────────────────

            "citas_atendidas" => @"
                SELECT
                    c.fecha_cita AS Fecha,
                    COUNT(*)::int AS Cantidad
                FROM public.citas c
                WHERE c.clinica_id = @ClinicaId
                  AND c.fecha_cita BETWEEN @FechaInicio AND @FechaFin
                  AND c.estado = 'atendida'
                  AND c.activo = true
                  AND (@DoctorId IS NULL OR c.doctor_id = @DoctorId)
                  AND (@SalaId IS NULL OR c.sala_id = @SalaId)
                GROUP BY c.fecha_cita
                ORDER BY c.fecha_cita ASC",

            "pacientes_atendidos" => @"
                SELECT
                    c.fecha_cita AS Fecha,
                    COUNT(DISTINCT c.paciente_id)::int AS Cantidad
                FROM public.citas c
                WHERE c.clinica_id = @ClinicaId
                  AND c.fecha_cita BETWEEN @FechaInicio AND @FechaFin
                  AND c.estado = 'atendida'
                  AND c.activo = true
                  AND (@DoctorId IS NULL OR c.doctor_id = @DoctorId)
                  AND (@SalaId IS NULL OR c.sala_id = @SalaId)
                GROUP BY c.fecha_cita
                ORDER BY c.fecha_cita ASC",

            "ingresos" => @"
                SELECT
                    c.fecha_cita AS Fecha,
                    COUNT(*)::int AS Total
                FROM public.citas c
                WHERE c.clinica_id = @ClinicaId
                  AND c.fecha_cita BETWEEN @FechaInicio AND @FechaFin
                  AND c.estado = 'atendida'
                  AND c.activo = true
                  AND (@DoctorId IS NULL OR c.doctor_id = @DoctorId)
                  AND (@SalaId IS NULL OR c.sala_id = @SalaId)
                GROUP BY c.fecha_cita
                ORDER BY c.fecha_cita ASC",

            "tiempos_espera" => @"
                SELECT
                    c.fecha_cita AS Fecha,
                    COALESCE(
                        ROUND(
                            AVG(
                                EXTRACT(EPOCH FROM (
                                    (c.fecha_cita + c.hora_llegada) - (c.fecha_cita + c.hora_cita)
                                )) / 60
                            ), 1
                        ), 0
                    ) AS Promedio
                FROM public.citas c
                WHERE c.clinica_id = @ClinicaId
                  AND c.fecha_cita BETWEEN @FechaInicio AND @FechaFin
                  AND c.hora_llegada IS NOT NULL
                  AND c.estado IN ('atendida', 'en_atencion')
                  AND c.activo = true
                  AND (@DoctorId IS NULL OR c.doctor_id = @DoctorId)
                  AND (@SalaId IS NULL OR c.sala_id = @SalaId)
                GROUP BY c.fecha_cita
                ORDER BY c.fecha_cita ASC",

            // ── Reporte detallado: Historial de Citas ───────────────

            "historial_citas" => @"
                SELECT
                    TO_CHAR(c.fecha_cita, 'DD/MM/YYYY')              AS FechaCita,
                    c.hora_cita::text                                  AS HoraCita,
                    COALESCE(c.hora_llegada::text, '—')               AS HoraLlegada,
                    CASE c.estado
                        WHEN 'agendada'    THEN 'Agendada'
                        WHEN 'en_espera'   THEN 'En Espera'
                        WHEN 'en_atencion' THEN 'En Atención'
                        WHEN 'atendida'    THEN 'Atendida'
                        WHEN 'cancelada'   THEN 'Cancelada'
                        ELSE c.estado
                    END                                               AS Estado,
                    p.primer_nombre || ' ' ||
                        COALESCE(p.segundo_nombre || ' ', '') ||
                        p.primer_apellido || ' ' ||
                        COALESCE(p.segundo_apellido, '')             AS Paciente,
                    u.nombres || ' ' || u.apellidos                  AS Doctor,
                    COALESCE(s.nombre, 'Sin asignar')                AS Sala,
                    COALESCE(c.motivo, '')                            AS Motivo
                FROM public.citas c
                INNER JOIN public.pacientes p ON p.id = c.paciente_id
                INNER JOIN public.usuarios  u ON u.id = c.doctor_id
                LEFT  JOIN public.salas     s ON s.id = c.sala_id
                WHERE c.clinica_id = @ClinicaId
                  AND c.fecha_cita BETWEEN @FechaInicio AND @FechaFin
                  AND c.activo = true
                  AND (@DoctorId IS NULL OR c.doctor_id = @DoctorId)
                  AND (@SalaId IS NULL OR c.sala_id = @SalaId)
                ORDER BY c.fecha_cita DESC, c.hora_cita DESC",

            _ => throw new ArgumentException($"Tipo de reporte no soportado: {tipo}")
        };
    }
}
