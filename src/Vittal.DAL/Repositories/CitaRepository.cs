using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.citas.
/// Implementa ICitaRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU21 — Agenda (HU-E01 — hora_fin)
/// </summary>
public class CitaRepository : ICitaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public CitaRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ── Columnas SELECT con JOINs ──────────────────────────────────────
    private const string SelectColumns = @"
        c.id                    AS Id,
        c.clinica_id            AS ClinicaId,
        c.paciente_id           AS PacienteId,
        c.doctor_id             AS DoctorId,
        c.sala_id               AS SalaId,
        c.fecha_cita            AS FechaCita,
        c.hora_cita             AS HoraCita,
        c.hora_fin              AS HoraFin,
        c.hora_llegada          AS HoraLlegada,
        c.lugar                 AS Lugar,
        c.motivo                AS Motivo,
        c.estado                AS Estado,
        c.notas                 AS Notas,
        c.activo                AS Activo,
        c.fecha_creacion        AS FechaCreacion,
        c.fecha_modificacion    AS FechaModificacion,
        c.creado_por            AS CreadoPor,
        c.modificado_por        AS ModificadoPor,
        p.primer_nombre || ' ' || p.primer_apellido AS PacienteNombre,
        u.nombres || ' ' || u.apellidos             AS DoctorNombre,
        s.nombre                                    AS SalaNombre";

    private const string FromJoin = @"
        FROM public.citas c
        INNER JOIN public.pacientes p ON c.paciente_id = p.id
        INNER JOIN public.usuarios u ON c.doctor_id = u.id
        LEFT JOIN public.salas s ON c.sala_id = s.id";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todas las citas activas de una clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Cita>> GetAllAsync(Guid clinicaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE c.clinica_id = @ClinicaId AND c.activo = true
            ORDER BY c.fecha_cita DESC, c.hora_cita ASC";

        return await connection.QueryAsync<Cita>(sql, new { ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene una cita por ID validando clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<Cita?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE c.id = @Id AND c.clinica_id = @ClinicaId AND c.activo = true";

        return await connection.QuerySingleOrDefaultAsync<Cita>(sql, new { Id = id, ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta una nueva cita. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Cita entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.citas (
                clinica_id, paciente_id, doctor_id, sala_id,
                fecha_cita, hora_cita, hora_fin, hora_llegada,
                lugar, motivo, estado, notas,
                activo, fecha_creacion, creado_por
            )
            VALUES (
                @ClinicaId, @PacienteId, @DoctorId, @SalaId,
                @FechaCita, @HoraCita, @HoraFin, @HoraLlegada,
                @Lugar, @Motivo, @Estado, @Notas,
                true, NOW(), @CreadoPor
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos de la cita
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(Cita entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.citas
            SET paciente_id    = @PacienteId,
                doctor_id      = @DoctorId,
                sala_id        = @SalaId,
                fecha_cita     = @FechaCita,
                hora_cita      = @HoraCita,
                hora_fin       = @HoraFin,
                hora_llegada   = @HoraLlegada,
                lugar          = @Lugar,
                motivo         = @Motivo,
                estado         = @Estado,
                notas          = @Notas,
                fecha_modificacion = NOW(),
                modificado_por = @ModificadoPor
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva cita (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.citas
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
