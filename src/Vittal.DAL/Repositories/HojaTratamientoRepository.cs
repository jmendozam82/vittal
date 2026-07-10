using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.hoja_tratamientos.
/// Implementa IHojaTratamientoRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaTratamientoRepository : IHojaTratamientoRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public HojaTratamientoRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ── Columnas base para SELECT con JOIN ──────────────────────────────
    // NOTA: instrucciones AS Instrucciones (mapeo entity ↔ sql)
    private const string SelectColumns = @"
        ht.id                       AS Id,
        ht.clinica_id               AS ClinicaId,
        ht.hoja_cita_id            AS HojaCitaId,
        ht.medicamento_id            AS MedicamentoId,
        ht.tratamiento_id            AS TratamientoId,
        ht.dosis                     AS Dosis,
        ht.frecuencia                AS Frecuencia,
        ht.duracion                  AS Duracion,
        ht.instrucciones              AS Instrucciones,
        ht.activo                     AS Activo,
        ht.fecha_creacion           AS FechaCreacion,
        ht.fecha_modificacion       AS FechaModificacion,
        m.nombre                      AS MedicamentoNombre,
        t.nombre                      AS TratamientoNombre";

    private const string FromJoin = @"
        FROM public.hoja_tratamientos ht
        LEFT JOIN public.medicamentos m ON ht.medicamento_id = m.id
        LEFT JOIN public.tratamientos t ON ht.tratamiento_id = t.id";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetByIdAsync — Obtiene un tratamiento de hoja por ID validando clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<HojaTratamiento?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE ht.id = @Id AND ht.clinica_id = @ClinicaId AND ht.activo = true";

        return await connection.QuerySingleOrDefaultAsync<HojaTratamiento>(sql, 
            new { Id = id, ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByHojaCitaIdAsync — Obtiene todos los tratamientos de una hoja de cita
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<HojaTratamiento>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE ht.clinica_id = @ClinicaId 
              AND ht.hoja_cita_id = @HojaCitaId 
              AND ht.activo = true
            ORDER BY ht.fecha_creacion";

        return await connection.QueryAsync<HojaTratamiento>(sql, 
            new { ClinicaId = clinicaId, HojaCitaId = hojaCitaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo tratamiento en hoja de cita. Retorna ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(HojaTratamiento entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.hoja_tratamientos (
                clinica_id, hoja_cita_id, medicamento_id, tratamiento_id,
                dosis, frecuencia, duracion, instrucciones,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @HojaCitaId, @MedicamentoId, @TratamientoId,
                @Dosis, @Frecuencia, @Duracion, @Instrucciones,
                true, NOW()
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza un tratamiento existente
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(HojaTratamiento entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hoja_tratamientos
            SET medicamento_id        = @MedicamentoId,
                tratamiento_id        = @TratamientoId,
                dosis                 = @Dosis,
                frecuencia            = @Frecuencia,
                duracion              = @Duracion,
                instrucciones         = @Instrucciones,
                fecha_modificacion    = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva tratamiento (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hoja_tratamientos
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
