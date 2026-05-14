using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.linea_tiempo.
/// Implementa ILineaTiempoRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU19 — Línea de Tiempo
/// </summary>
public class LineaTiempoRepository : ILineaTiempoRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<LineaTiempoRepository> _logger;

    public LineaTiempoRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<LineaTiempoRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    private const string SelectColumns = @"
        lt.id                   AS Id,
        lt.clinica_id           AS ClinicaId,
        lt.cita_id              AS CitaId,
        lt.paciente_id          AS PacienteId,
        lt.sala_id              AS SalaId,
        lt.nombre_paso          AS NombrePaso,
        lt.orden                AS Orden,
        lt.estado               AS Estado,
        lt.hora_llegada         AS HoraLlegada,
        lt.hora_salida          AS HoraSalida,
        lt.activo               AS Activo,
        lt.fecha_creacion       AS FechaCreacion,
        lt.fecha_modificacion   AS FechaModificacion";

    private const string FromTable = @"
        FROM public.linea_tiempo lt";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetByCitaIdAsync — Obtiene todos los pasos de una cita
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<LineaTiempo>> GetByCitaIdAsync(Guid clinicaId, Guid citaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE lt.clinica_id = @ClinicaId
              AND lt.cita_id = @CitaId
              AND lt.activo = true
            ORDER BY lt.orden ASC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<LineaTiempo>(sql,
                new { ClinicaId = clinicaId, CitaId = citaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener línea de tiempo para cita {CitaId} de clínica {ClinicaId}",
                citaId, clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByClinicaAndDateAsync — Pasos por clínica, fecha y doctor opcional
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<LineaTiempo>> GetByClinicaAndDateAsync(
        Guid clinicaId, Guid? doctorId, DateTime fecha)
    {
        var sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE lt.clinica_id = @ClinicaId
              AND lt.activo = true
              AND DATE(lt.fecha_creacion) = @Fecha
              AND (@DoctorId IS NULL OR lt.cita_id IN (
                  SELECT c.id FROM public.citas c
                  WHERE c.doctor_id = @DoctorId AND c.clinica_id = @ClinicaId
              ))
            ORDER BY lt.fecha_creacion DESC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<LineaTiempo>(sql,
                new { ClinicaId = clinicaId, DoctorId = doctorId, Fecha = fecha.Date });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener línea de tiempo de clínica {ClinicaId} para fecha {Fecha}",
                clinicaId, fecha);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo paso. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(LineaTiempo entity)
    {
        const string sql = @"
            INSERT INTO public.linea_tiempo (
                clinica_id, cita_id, paciente_id, sala_id,
                nombre_paso, orden, estado,
                hora_llegada, hora_salida,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @CitaId, @PacienteId, @SalaId,
                @NombrePaso, @Orden, @Estado,
                @HoraLlegada, @HoraSalida,
                true, NOW()
            )
            RETURNING id";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear paso de línea de tiempo en cita {CitaId}",
                entity.CitaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. UpdateEstadoAsync — Actualiza estado y hora de un paso
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateEstadoAsync(
        Guid clinicaId, Guid id, string estado, TimeSpan? hora)
    {
        const string sql = @"
            UPDATE public.linea_tiempo
            SET
                estado = @Estado,
                hora_llegada = CASE
                    WHEN @Estado = 'en_sala' AND hora_llegada IS NULL
                    THEN @Hora
                    ELSE hora_llegada
                END,
                hora_salida = CASE
                    WHEN @Estado IN ('completado', 'saltado') AND hora_salida IS NULL
                    THEN @Hora
                    ELSE hora_salida
                END,
                fecha_modificacion = NOW()
            WHERE id = @Id
              AND clinica_id = @ClinicaId
              AND activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql,
                new { Id = id, ClinicaId = clinicaId, Estado = estado, Hora = hora });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar estado del paso {Id} a {Estado}",
                id, estado);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. GetByIdAsync — Obtiene un paso por ID
    // ────────────────────────────────────────────────────────────────────
    public async Task<LineaTiempo?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE lt.id = @Id
              AND lt.clinica_id = @ClinicaId
              AND lt.activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<LineaTiempo>(sql,
                new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener paso de línea de tiempo {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. GetAllAsync — Lista todos los pasos activos de una clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<LineaTiempo>> GetAllAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromTable}
            WHERE lt.clinica_id = @ClinicaId
              AND lt.activo = true
            ORDER BY lt.fecha_creacion DESC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<LineaTiempo>(sql,
                new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener todos los pasos de línea de tiempo de clínica {ClinicaId}",
                clinicaId);
            throw;
        }
    }
}
