using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.hoja_examenes.
/// Implementa IHojaExamenRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaExamenRepository : IHojaExamenRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public HojaExamenRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ── Columnas base para SELECT con JOIN ──────────────────────────────
    private const string SelectColumns = @"
        he.id                       AS Id,
        he.clinica_id               AS ClinicaId,
        he.hoja_cita_id            AS HojaCitaId,
        he.examen_id               AS ExamenId,
        he.resultado              AS Resultado,
        he.archivo_url              AS ArchivoUrl,
        he.activo                     AS Activo,
        he.fecha_creacion           AS FechaCreacion,
        he.fecha_modificacion       AS FechaModificacion,
        e.nombre                      AS ExamenNombre";

    private const string FromJoin = @"
        FROM public.hoja_examenes he
        LEFT JOIN public.examenes e ON he.examen_id = e.id";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetByIdAsync — Obtiene un examen de hoja por ID validando clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<HojaExamen?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE he.id = @Id AND he.clinica_id = @ClinicaId AND he.activo = true";

        return await connection.QuerySingleOrDefaultAsync<HojaExamen>(sql,
            new { Id = id, ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByHojaCitaIdAsync — Obtiene todos los exámenes de una hoja de cita
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<HojaExamen>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE he.clinica_id = @ClinicaId 
              AND he.hoja_cita_id = @HojaCitaId 
              AND he.activo = true
            ORDER BY he.fecha_creacion";

        return await connection.QueryAsync<HojaExamen>(sql,
            new { ClinicaId = clinicaId, HojaCitaId = hojaCitaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo examen en hoja. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(HojaExamen entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.hoja_examenes (
                clinica_id, hoja_cita_id, examen_id,
                resultado, archivo_url,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @HojaCitaId, @ExamenId,
                @Resultado, @ArchivoUrl,
                true, NOW()
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza un examen existente
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(HojaExamen entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hoja_examenes
            SET examen_id            = @ExamenId,
                resultado           = @Resultado,
                archivo_url         = @ArchivoUrl,
                fecha_modificacion    = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva examen (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hoja_examenes
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
