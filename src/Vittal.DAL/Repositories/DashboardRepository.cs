using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Dashboard;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio de solo lectura para consultas de KPIs del dashboard.
/// No tiene operaciones de escritura — los datos provienen de agregaciones en tiempo real.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public class DashboardRepository : IDashboardRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<DashboardRepository> _logger;

    public DashboardRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<DashboardRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────
    // 1. GetPacientesDelDiaAsync — Cantidad de pacientes agendados hoy
    // ────────────────────────────────────────────────────────────────────
    public async Task<int> GetPacientesDelDiaAsync(Guid clinicaId, DateTime fecha)
    {
        const string sql = @"
            SELECT COUNT(DISTINCT c.paciente_id)
            FROM public.citas c
            WHERE c.clinica_id = @ClinicaId
              AND c.fecha_cita = @Fecha
              AND c.activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql,
                new { ClinicaId = clinicaId, Fecha = fecha.Date });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener pacientes del día de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetCitasPendientesAsync — Citas pendientes del día
    // ────────────────────────────────────────────────────────────────────
    public async Task<int> GetCitasPendientesAsync(Guid clinicaId, DateTime fecha)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM public.citas
            WHERE clinica_id = @ClinicaId
              AND fecha_cita = @Fecha
              AND estado IN ('agendada', 'en_espera')
              AND activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql,
                new { ClinicaId = clinicaId, Fecha = fecha.Date });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener citas pendientes de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. GetPacientesEnEsperaAsync — Pacientes actualmente en espera
    // ────────────────────────────────────────────────────────────────────
    public async Task<int> GetPacientesEnEsperaAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM public.citas
            WHERE clinica_id = @ClinicaId
              AND estado = 'en_espera'
              AND activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql,
                new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener pacientes en espera de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 4a. GetPacientesEnAtencionAsync — Pacientes actualmente en atención
    // ────────────────────────────────────────────────────────────────────
    public async Task<int> GetPacientesEnAtencionAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM public.citas
            WHERE clinica_id = @ClinicaId
              AND estado = 'en_atencion'
              AND activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql,
                new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener pacientes en atención de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 3b. GetCitasCanceladasAsync — Citas canceladas de una fecha
    // ────────────────────────────────────────────────────────────────────
    public async Task<int> GetCitasCanceladasAsync(Guid clinicaId, DateTime fecha)
    {
        const string sql = @"
            SELECT COUNT(*) FROM public.citas
            WHERE clinica_id = @ClinicaId
              AND fecha_cita = @Fecha
              AND estado = 'cancelada'
              AND activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql,
                new { ClinicaId = clinicaId, Fecha = fecha.Date });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener citas canceladas de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. GetTiempoPromedioEsperaAsync — Tiempo promedio en minutos
    // ────────────────────────────────────────────────────────────────────
    public async Task<double> GetTiempoPromedioEsperaAsync(Guid clinicaId, DateTime fecha)
    {
        // Tiempo de espera = desde que el paciente llega (hora_llegada) hasta que
        // inicia su consulta (paso "Consulta" en linea_tiempo). Si la consulta aún
        // no ha iniciado, se usa la hora programada (hora_cita) como respaldo.
        // GREATEST(0, ...) evita valores negativos cuando el paciente llega temprano
        // (nunca puede "esperar" un tiempo negativo).
        // ROUND(..., 0) devuelve minutos enteros para una presentación limpia.
        const string sql = @"
            SELECT COALESCE(
                ROUND(
                    AVG(
                        GREATEST(
                            EXTRACT(EPOCH FROM (
                                (c.fecha_cita + COALESCE(lt.hora_llegada, c.hora_cita))
                                - (c.fecha_cita + c.hora_llegada)
                            )) / 60,
                            0
                        )
                    ),
                    0
                ),
                0
            )
            FROM public.citas c
            LEFT JOIN LATERAL (
                SELECT lt.hora_llegada
                FROM public.linea_tiempo lt
                WHERE lt.cita_id = c.id
                  AND lt.nombre_paso = 'Consulta'
                  AND lt.estado IN ('en_sala', 'completado')
                  AND lt.hora_llegada IS NOT NULL
                ORDER BY lt.orden
                LIMIT 1
            ) lt ON true
            WHERE c.clinica_id = @ClinicaId
              AND c.fecha_cita = @Fecha::date
              AND c.hora_llegada IS NOT NULL
              AND c.estado IN ('atendida', 'en_atencion')
              AND c.activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<double>(sql,
                new { ClinicaId = clinicaId, Fecha = fecha });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener tiempo promedio de espera de clínica {ClinicaId}",
                clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. GetCitasPorHoraAsync — Distribución de citas por hora del día,
    //    segmentada por estado para el gráfico de barras apiladas.
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<DashboardCitaPorHoraDto>> GetCitasPorHoraAsync(
        Guid clinicaId, DateTime fecha)
    {
        const string sql = @"
            SELECT
                TO_CHAR(c.hora_cita, 'HH24:00') AS Etiqueta,
                COUNT(*) FILTER (WHERE c.estado = 'agendada')::int    AS Agendadas,
                COUNT(*) FILTER (WHERE c.estado = 'en_espera')::int   AS EnEspera,
                COUNT(*) FILTER (WHERE c.estado = 'en_atencion')::int AS EnAtencion,
                COUNT(*) FILTER (WHERE c.estado = 'atendida')::int    AS Atendidas,
                COUNT(*) FILTER (WHERE c.estado = 'cancelada')::int   AS Canceladas
            FROM public.citas c
            WHERE c.clinica_id = @ClinicaId
              AND c.fecha_cita = @Fecha
              AND c.activo = true
            GROUP BY TO_CHAR(c.hora_cita, 'HH24:00')
            ORDER BY TO_CHAR(c.hora_cita, 'HH24:00') ASC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<DashboardCitaPorHoraDto>(sql,
                new { ClinicaId = clinicaId, Fecha = fecha.Date });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener citas por hora de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. GetCitasPorMedicoAsync — Citas por médico segmentadas por estado
    //    (atendidas / pendientes) para el gráfico apilado del dashboard.
    //    Se cuenta cada cita según su estado actual:
    //      - Atendidas: estado = 'atendida'
    //      - Pendientes: agendada, en_espera, en_atencion
    //    Las citas canceladas no se cuentan en ninguna segmento.
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<DashboardCitaPorMedicoDto>> GetCitasPorMedicoAsync(
        Guid clinicaId, DateTime fecha)
    {
        const string sql = @"
            SELECT
                u.nombres || ' ' || u.apellidos AS DoctorNombre,
                COUNT(*) FILTER (WHERE c.estado = 'atendida')::int AS Atendidas,
                COUNT(*) FILTER (WHERE c.estado IN ('agendada', 'en_espera', 'en_atencion'))::int AS Pendientes
            FROM public.citas c
            INNER JOIN public.usuarios u ON u.id = c.doctor_id
            WHERE c.clinica_id = @ClinicaId
              AND c.fecha_cita = @Fecha
              AND c.activo = true
            GROUP BY u.nombres, u.apellidos
            ORDER BY u.nombres, u.apellidos ASC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<DashboardCitaPorMedicoDto>(sql,
                new { ClinicaId = clinicaId, Fecha = fecha.Date });
        }
catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener citas por médico de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }
}
