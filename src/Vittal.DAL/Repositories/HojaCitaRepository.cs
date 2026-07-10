using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.hojas_cita.
/// Implementa IHojaCitaRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU20 — Expedientes
/// </summary>
public class HojaCitaRepository : IHojaCitaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public HojaCitaRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ── Columnas base para SELECT con JOIN ──────────────────────────────
    private const string SelectColumns = @"
        h.id                    AS Id,
        h.clinica_id            AS ClinicaId,
        h.expediente_id         AS ExpedienteId,
        h.cita_id               AS CitaId,
        h.doctor_id             AS DoctorId,
        h.fecha_consulta        AS FechaConsulta,
        h.motivo_consulta       AS MotivoConsulta,
        h.notas_consulta        AS NotasConsulta,
        h.activo                AS Activo,
        h.fecha_creacion        AS FechaCreacion,
        h.fecha_modificacion    AS FechaModificacion,
        CONCAT(p.primer_nombre, ' ', p.primer_apellido) AS PacienteNombre,
        CONCAT(u.nombres, ' ', u.apellidos) AS DoctorNombre";

    private const string FromJoin = @"
        FROM public.hojas_cita h
        LEFT JOIN public.expedientes e ON h.expediente_id = e.id
        LEFT JOIN public.pacientes p ON e.paciente_id = p.id
        LEFT JOIN public.usuarios u ON h.doctor_id = u.id";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Obtiene todas las hojas de cita activas de una clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<HojaCita>> GetAllAsync(Guid clinicaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE h.clinica_id = @ClinicaId AND h.activo = true
            ORDER BY h.fecha_consulta DESC";

        return await connection.QueryAsync<HojaCita>(sql, new { ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene una hoja de cita por ID validando clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<HojaCita?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE h.id = @Id AND h.clinica_id = @ClinicaId AND h.activo = true";

        return await connection.QuerySingleOrDefaultAsync<HojaCita>(sql, new { Id = id, ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. GetByExpedienteIdAsync — Obtiene todas las hojas de un expediente
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<HojaCita>> GetByExpedienteIdAsync(Guid clinicaId, Guid expedienteId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE h.clinica_id = @ClinicaId 
              AND h.expediente_id = @ExpedienteId 
              AND h.activo = true
            ORDER BY h.fecha_consulta DESC";

        return await connection.QueryAsync<HojaCita>(sql, 
            new { ClinicaId = clinicaId, ExpedienteId = expedienteId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. CreateAsync — Inserta una nueva hoja de cita. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(HojaCita entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.hojas_cita (
                clinica_id, expediente_id, cita_id, doctor_id,
                fecha_consulta, motivo_consulta, notas_consulta,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @ExpedienteId, @CitaId, @DoctorId,
                @FechaConsulta, @MotivoConsulta, @NotasConsulta,
                true, NOW()
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. UpdateAsync — Actualiza una hoja de cita existente
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(HojaCita entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hojas_cita
            SET fecha_consulta        = @FechaConsulta,
                motivo_consulta       = @MotivoConsulta,
                notas_consulta        = @NotasConsulta,
                fecha_modificacion    = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. DeactivateAsync — Desactiva hoja de cita (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.hojas_cita
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
