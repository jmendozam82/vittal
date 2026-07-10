using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.hojas_recomendaciones.
/// Implementa IHojaRecomendacionRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaRecomendacionRepository : IHojaRecomendacionRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public HojaRecomendacionRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ── Columnas base para SELECT con JOIN ──────────────────────────────
    private const string SelectColumns = @"
        hr.id                       AS Id,
        hr.clinica_id               AS ClinicaId,
        hr.hoja_cita_id            AS HojaCitaId,
        hr.recomendacion_id          AS RecomendacionId,
        hr.observaciones              AS Observaciones,
        hr.activo                     AS Activo,
        hr.fecha_creacion           AS FechaCreacion,
        hr.fecha_modificacion       AS FechaModificacion,
        r.nombre                      AS RecomendacionNombre";

    private const string FromJoin = @"
        FROM public.hojas_recomendaciones hr
        LEFT JOIN public.recomendaciones r ON hr.recomendacion_id = r.id";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetByIdAsync — Obtiene una recomendación de hoja por ID validando clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<HojaRecomendacion?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE hr.id = @Id AND hr.clinica_id = @ClinicaId AND hr.activo = true";

        return await connection.QuerySingleOrDefaultAsync<HojaRecomendacion>(sql,
            new { Id = id, ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByHojaCitaIdAsync — Obtiene todas las recomendaciones de una hoja de cita
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<HojaRecomendacion>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE hr.clinica_id = @ClinicaId
              AND hr.hoja_cita_id = @HojaCitaId
              AND hr.activo = true
            ORDER BY hr.fecha_creacion";

        return await connection.QueryAsync<HojaRecomendacion>(sql,
            new { ClinicaId = clinicaId, HojaCitaId = hojaCitaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta una nueva recomendación en hoja. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(HojaRecomendacion entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.hojas_recomendaciones (
                clinica_id, hoja_cita_id, recomendacion_id,
                observaciones,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @HojaCitaId, @RecomendacionId,
                @Observaciones,
                true, NOW()
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza una recomendación existente
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(HojaRecomendacion entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hojas_recomendaciones
            SET recomendacion_id    = @RecomendacionId,
                observaciones       = @Observaciones,
                fecha_modificacion    = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva recomendación (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hojas_recomendaciones
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
