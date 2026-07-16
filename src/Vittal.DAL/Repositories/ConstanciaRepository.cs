using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.constancias.
/// Implementa IConstanciaRepository con Dapper y PostgreSQL.
/// NOTA: No existe UpdateAsync — las constancias son documentos legales.
///       Una vez emitidas, solo se pueden anular (activo = false).
/// Historia de Usuario: HU-E07 — Constancias Médicas
/// </summary>
public class ConstanciaRepository : IConstanciaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public ConstanciaRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ── Columnas SELECT con JOINs ──────────────────────────────────────
    private const string SelectColumns = @"
        c.id                AS Id,
        c.clinica_id        AS ClinicaId,
        c.expediente_id     AS ExpedienteId,
        c.hoja_cita_id      AS HojaCitaId,
        c.doctor_id         AS DoctorId,
        c.tipo_constancia   AS TipoConstancia,
        c.contenido         AS Contenido,
        c.fecha_emision     AS FechaEmision,
        c.dias_reposo       AS DiasReposo,
        c.especialista_referido AS EspecialistaReferido,
        c.activo            AS Activo,
        c.fecha_creacion    AS FechaCreacion,
        c.fecha_modificacion AS FechaModificacion,
        c.creado_por        AS CreadoPor,
        u.nombres || ' ' || u.apellidos             AS DoctorNombre,
        p.primer_nombre || ' ' || p.primer_apellido AS PacienteNombre";

    private const string FromJoin = @"
        FROM public.constancias c
        INNER JOIN public.usuarios u ON c.doctor_id = u.id
        INNER JOIN public.expedientes e ON c.expediente_id = e.id
        INNER JOIN public.pacientes p ON e.paciente_id = p.id";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todas las constancias activas de una clínica.
    //    Si se especifica expedienteId, filtra por ese paciente.
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Constancia>> GetAllAsync(Guid clinicaId, Guid? expedienteId = null)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE c.clinica_id = @ClinicaId AND c.activo = true
              AND (@ExpedienteId IS NULL OR c.expediente_id = @ExpedienteId)
            ORDER BY c.fecha_emision DESC";

        return await connection.QueryAsync<Constancia>(sql, new { ClinicaId = clinicaId, ExpedienteId = expedienteId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene una constancia por ID validando clínica.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Constancia?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE c.id = @Id AND c.clinica_id = @ClinicaId AND c.activo = true";

        return await connection.QuerySingleOrDefaultAsync<Constancia>(sql, new { Id = id, ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta una nueva constancia médica.
    //    Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Constancia entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.constancias (
                clinica_id, expediente_id, hoja_cita_id, doctor_id,
                tipo_constancia, contenido, fecha_emision,
                dias_reposo, especialista_referido,
                activo, fecha_creacion, creado_por
            )
            VALUES (
                @ClinicaId, @ExpedienteId, @HojaCitaId, @DoctorId,
                @TipoConstancia, @Contenido, @FechaEmision,
                @DiasReposo, @EspecialistaReferido,
                true, NOW(), @CreadoPor
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. NO EXISTE UpdateAsync — Las constancias son documentos legales.
    //    Una vez emitidas no se modifican, solo se anulan.
    // ────────────────────────────────────────────────────────────────────

    // ────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Anula una constancia (activo = false).
    //    Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.constancias
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
