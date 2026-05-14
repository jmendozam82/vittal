using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Exceptions;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.alertas_espera.
/// Implementa IAlertaEsperaRepository con Dapper y PostgreSQL.
/// Las alertas se generan cuando un paciente excede el tiempo de espera configurado.
/// Historia de Usuario: HU23 — Alertas Configurables
///
/// ⚠ ATENCIÓN @Arquitecto: La interfaz IAlertaEsperaRepository usa Notificacion como tipo,
///   pero la tabla alertas_espera requiere campos específicos (CitaId, PacienteId, DoctorId,
///   MinutosEspera, etc.) que Notificacion no posee. Se recomienda:
///   - Cambiar el tipo de retorno de los métodos de lectura a AlertaEspera (ya existe la entity)
///   - Cambiar CreateAsync a CreateAsync(AlertaEspera entity)
///   Mientras tanto, los métodos de lectura mapean AlertaEspera → Notificacion correctamente.
///   El método CreateAsync(Notificacion entity) inserta usando SOLO los campos disponibles
///   en Notificacion; los campos adicionales (CitaId, PacienteId, etc.) quedarán como NULL
///   y serán rellenados por el BLL o la migración de la interfaz.
/// </summary>
public class AlertaEsperaRepository : IAlertaEsperaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<AlertaEsperaRepository> _logger;

    public AlertaEsperaRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<AlertaEsperaRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    private const string SelectColumns = @"
        ae.id               AS Id,
        ae.clinica_id       AS ClinicaId,
        ae.cita_id          AS CitaId,
        ae.paciente_id      AS PacienteId,
        ae.doctor_id        AS DoctorId,
        ae.sala_id          AS SalaId,
        ae.hora_cita        AS HoraCita,
        ae.hora_llegada     AS HoraLlegada,
        ae.minutos_espera   AS MinutosEspera,
        ae.resuelta         AS Resuelta,
        ae.fecha_alerta     AS FechaAlerta,
        ae.fecha_resolucion AS FechaResolucion,
        p.primer_nombre || ' ' || p.primer_apellido AS PacienteNombre,
        u.nombres || ' ' || u.apellidos             AS DoctorNombre,
        s.nombre                                    AS SalaNombre";

    private const string FromJoin = @"
        FROM public.alertas_espera ae
        INNER JOIN public.pacientes p ON ae.paciente_id = p.id
        INNER JOIN public.usuarios u ON ae.doctor_id = u.id
        LEFT JOIN public.salas s ON ae.sala_id = s.id";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetAllByClinicaIdAsync — Alertas filtradas por estado resuelta
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<AlertaEspera>> GetAllByClinicaIdAsync(
        Guid clinicaId, bool? resuelta = null)
    {
        var sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE ae.clinica_id = @ClinicaId
              {BuildResueltaFilter(resuelta)}
            ORDER BY ae.fecha_alerta DESC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var alertas = await connection.QueryAsync<AlertaEspera>(sql,
                new { ClinicaId = clinicaId });
            return alertas.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener alertas de espera de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetNoResueltasAsync — Solo alertas no resueltas
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<AlertaEspera>> GetNoResueltasAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE ae.clinica_id = @ClinicaId
              AND ae.resuelta = false
            ORDER BY ae.fecha_alerta DESC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var alertas = await connection.QueryAsync<AlertaEspera>(sql,
                new { ClinicaId = clinicaId });
            return alertas.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener alertas no resueltas de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta una nueva alerta usando solo ClinciaId de Notificacion.
    //     ⚠️ Refactor pending: La interfaz debería usar AlertaEspera.
    //     Temporalmente, se usa ClinicaId de la entidad y valores default para
    //     el resto (el BLL debe completar mediante UPDATE posterior).
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(AlertaEspera entity)
    {
        const string sql = @"
            INSERT INTO public.alertas_espera (
                clinica_id, cita_id, paciente_id, doctor_id, sala_id,
                hora_cita, hora_llegada, minutos_espera,
                resuelta, fecha_alerta
            )
            VALUES (
                @ClinicaId, @CitaId, @PacienteId, @DoctorId, @SalaId,
                @HoraCita, @HoraLlegada, @MinutosEspera,
                false, NOW()
            )
            RETURNING id";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al crear alerta de espera en clínica {ClinicaId}", entity.ClinicaId);
            throw new RepositoryException("Error al crear la alerta de espera.", ex);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. MarcarResueltaAsync — Marca alerta como resuelta
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> MarcarResueltaAsync(Guid clinicaId, Guid id)
    {
        const string sql = @"
            UPDATE public.alertas_espera
            SET resuelta = true,
                fecha_resolucion = NOW()
            WHERE id = @Id
              AND clinica_id = @ClinicaId";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql,
                new { Id = id, ClinicaId = clinicaId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar alerta {Id} como resuelta", id);
            throw;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static string BuildResueltaFilter(bool? resuelta)
    {
        if (!resuelta.HasValue) return string.Empty;
        return resuelta.Value ? "AND ae.resuelta = true" : "AND ae.resuelta = false";
    }
}
