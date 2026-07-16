using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.hoja_cirugias.
/// Implementa IHojaCirugiaRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaCirugiaRepository : IHojaCirugiaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public HojaCirugiaRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ── Columnas base para SELECT con JOIN ──────────────────────────────
    private const string SelectColumns = @"
        hc.id                       AS Id,
        hc.clinica_id               AS ClinicaId,
        hc.hoja_cita_id            AS HojaCitaId,
        hc.cirugia_id               AS CirugiaId,
        hc.fecha_cirugia              AS FechaCirugia,
        hc.observaciones              AS Observaciones,
        hc.activo                     AS Activo,
        hc.fecha_creacion           AS FechaCreacion,
        hc.fecha_modificacion       AS FechaModificacion,
        c.nombre                      AS CirugiaNombre,
        tc.nombre                   AS TipoCirugiaNombre";

    private const string FromJoin = @"
        FROM public.hoja_cirugias hc
        LEFT JOIN public.cirugias c ON hc.cirugia_id = c.id
        LEFT JOIN public.tipos_cirugia tc ON c.tipo_cirugia_id = tc.id";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetByIdAsync — Obtiene una cirugía de hoja por ID validando clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<HojaCirugia?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE hc.id = @Id AND hc.clinica_id = @ClinicaId AND hc.activo = true";

        return await connection.QuerySingleOrDefaultAsync<HojaCirugia>(sql,
            new { Id = id, ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByHojaCitaIdAsync — Obtiene todas las cirugías de una hoja de cita
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<HojaCirugia>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE hc.clinica_id = @ClinicaId 
              AND hc.hoja_cita_id = @HojaCitaId 
              AND hc.activo = true
            ORDER BY hc.fecha_creacion";

        return await connection.QueryAsync<HojaCirugia>(sql,
            new { ClinicaId = clinicaId, HojaCitaId = hojaCitaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta una nueva cirugía en hoja. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(HojaCirugia entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.hoja_cirugias (
                clinica_id, hoja_cita_id, cirugia_id,
                fecha_cirugia, observaciones,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @HojaCitaId, @CirugiaId,
                @FechaCirugia, @Observaciones,
                true, NOW()
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza una cirugía existente
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(HojaCirugia entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hoja_cirugias
            SET cirugia_id            = @CirugiaId,
                fecha_cirugia           = @FechaCirugia,
                observaciones            = @Observaciones,
                fecha_modificacion    = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva cirugía (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hoja_cirugias
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
