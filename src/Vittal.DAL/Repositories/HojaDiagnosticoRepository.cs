using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.hoja_diagnosticos.
/// Implementa IHojaDiagnosticoRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaDiagnosticoRepository : IHojaDiagnosticoRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public HojaDiagnosticoRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ── Columnas base para SELECT con JOIN ──────────────────────────────
    // NOTA: observaciones AS Observaciones (mapeo entity ↔ sql)
    private const string SelectColumns = @"
        hd.id                       AS Id,
        hd.clinica_id               AS ClinicaId,
        hd.hoja_cita_id             AS HojaCitaId,
        hd.diagnostico_id           AS DiagnosticoId,
        hd.observaciones            AS Observaciones,
        hd.activo                   AS Activo,
        hd.fecha_creacion           AS FechaCreacion,
        hd.fecha_modificacion       AS FechaModificacion,
        d.nombre                    AS DiagnosticoNombre,
        td.nombre                   AS TipoDiagnosticoNombre";

    private const string FromJoin = @"
        FROM public.hoja_diagnosticos hd
        LEFT JOIN public.diagnosticos d ON hd.diagnostico_id = d.id
        LEFT JOIN public.tipos_diagnostico td ON d.tipo_diagnostico_id = td.id";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetByIdAsync — Obtiene un diagnóstico de hoja por ID validando clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<HojaDiagnostico?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE hd.id = @Id AND hd.clinica_id = @ClinicaId AND hd.activo = true";

        return await connection.QuerySingleOrDefaultAsync<HojaDiagnostico>(sql, 
            new { Id = id, ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByHojaCitaIdAsync — Obtiene todos los diagnósticos de una hoja de cita
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<HojaDiagnostico>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE hd.clinica_id = @ClinicaId 
              AND hd.hoja_cita_id = @HojaCitaId 
              AND hd.activo = true
            ORDER BY hd.fecha_creacion";

        return await connection.QueryAsync<HojaDiagnostico>(sql, 
            new { ClinicaId = clinicaId, HojaCitaId = hojaCitaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo diagnóstico en hoja. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(HojaDiagnostico entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.hoja_diagnosticos (
                clinica_id, hoja_cita_id, diagnostico_id,
                observaciones,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @HojaCitaId, @DiagnosticoId,
                @Observaciones,
                true, NOW()
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza un diagnóstico existente
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(HojaDiagnostico entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hoja_diagnosticos
            SET observaciones         = @Observaciones,
                fecha_modificacion    = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva diagnóstico (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hoja_diagnosticos
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
