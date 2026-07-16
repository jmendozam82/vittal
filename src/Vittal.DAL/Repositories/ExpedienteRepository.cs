using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Shared;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.expedientes.
/// Implementa IExpedienteRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class ExpedienteRepository : IExpedienteRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ExpedienteRepository> _logger;

    public ExpedienteRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<ExpedienteRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT con JOIN ──────────────────────────────
    private const string SelectColumns = @"
        e.id                    AS Id,
        e.clinica_id            AS ClinicaId,
        e.paciente_id           AS PacienteId,
        e.doctor_id             AS DoctorId,
        e.notas_generales       AS NotasGenerales,
        e.activo                AS Activo,
        e.fecha_creacion        AS FechaCreacion,
        e.fecha_modificacion    AS FechaModificacion,
        CONCAT(p.primer_nombre, ' ', p.primer_apellido) AS PacienteNombre,
        CONCAT(u.nombres, ' ', u.apellidos) AS DoctorNombre";

    private const string FromJoin = @"
        FROM public.expedientes e
        LEFT JOIN public.pacientes p ON e.paciente_id = p.id
        LEFT JOIN public.usuarios u ON e.doctor_id = u.id";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Obtiene todos los expedientes activos de una clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Expediente>> GetAllAsync(Guid clinicaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE e.clinica_id = @ClinicaId AND e.activo = true
            ORDER BY e.fecha_creacion DESC";

        return await connection.QueryAsync<Expediente>(sql, new { ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un expediente por ID validando clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<Expediente?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE e.id = @Id AND e.clinica_id = @ClinicaId AND e.activo = true";

        return await connection.QuerySingleOrDefaultAsync<Expediente>(sql, new { Id = id, ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. GetByPacienteIdAsync — Obtiene el expediente de un paciente
    // ────────────────────────────────────────────────────────────────────
    public async Task<Expediente?> GetByPacienteIdAsync(Guid clinicaId, Guid pacienteId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE e.clinica_id = @ClinicaId 
              AND e.paciente_id = @PacienteId 
              AND e.activo = true";

        return await connection.QuerySingleOrDefaultAsync<Expediente>(sql,
            new { ClinicaId = clinicaId, PacienteId = pacienteId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. CreateAsync — Inserta un nuevo expediente. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Expediente entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.expedientes (
                clinica_id, paciente_id, doctor_id,
                notas_generales,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @PacienteId, @DoctorId,
                @NotasGenerales,
                true, NOW()
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. UpdateAsync — Actualiza un expediente existente
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(Expediente entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.expedientes
            SET doctor_id             = @DoctorId,
                notas_generales       = @NotasGenerales,
                fecha_modificacion    = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. DeactivateAsync — Desactiva expediente (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.expedientes
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 7. GetAllPaginatedAsync — Página de expedientes con búsqueda ILIKE
    // ────────────────────────────────────────────────────────────────────
    public async Task<PaginatedResultDto<Expediente>> GetAllPaginatedAsync(
        Guid clinicaId, PaginationFilterDto filter)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize > 100 ? 100 : filter.PageSize;
        var offset = (page - 1) * pageSize;
        var searchTerm = string.IsNullOrWhiteSpace(filter.SearchTerm)
            ? null
            : $"%{filter.SearchTerm.Trim()}%";

        const string baseWhere = @"
            WHERE e.clinica_id = @ClinicaId
              AND e.activo = true
              AND (@SearchTerm IS NULL
                   OR p.primer_nombre ILIKE @SearchTerm
                   OR p.primer_apellido ILIKE @SearchTerm
                   OR p.segundo_nombre ILIKE @SearchTerm
                   OR p.segundo_apellido ILIKE @SearchTerm
                   OR CONCAT(p.primer_nombre, ' ', p.primer_apellido) ILIKE @SearchTerm)";

        var sql = $@"
            WITH filtered AS (
                SELECT 1 {FromJoin}
                {baseWhere}
            )
            SELECT COUNT(1) FROM filtered;

            SELECT {SelectColumns}
            {FromJoin}
            {baseWhere}
            ORDER BY e.fecha_creacion DESC
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
            var items = await multi.ReadAsync<Expediente>();

            return new PaginatedResultDto<Expediente>
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
                "Error al obtener expedientes paginados de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }
}
