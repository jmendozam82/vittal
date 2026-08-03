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
    // 5. GetCitasPorHoraAsync — Distribución de citas por hora del día
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<DashboardGraficoDto>> GetCitasPorHoraAsync(
        Guid clinicaId, DateTime fecha)
    {
        const string sql = @"
            SELECT
                TO_CHAR(c.hora_cita, 'HH24:00') AS Etiqueta,
                COUNT(*)::int AS Valor
            FROM public.citas c
            WHERE c.clinica_id = @ClinicaId
              AND c.fecha_cita = @Fecha
              AND c.activo = true
            GROUP BY TO_CHAR(c.hora_cita, 'HH24:00')
            ORDER BY TO_CHAR(c.hora_cita, 'HH24:00') ASC";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<DashboardGraficoDto>(sql,
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
    // 6. GetUltimasAlertasAsync — Últimas N alertas no resueltas de una fecha
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<DashboardGraficoDto>> GetUltimasAlertasAsync(
        Guid clinicaId, DateTime fecha, int limit = 5)
    {
        const string sql = @"
            SELECT
                p.primer_nombre || ' ' || p.primer_apellido AS Etiqueta,
                ae.minutos_espera::int AS Valor
            FROM public.alertas_espera ae
            INNER JOIN public.pacientes p ON p.id = ae.paciente_id
            WHERE ae.clinica_id = @ClinicaId
              AND ae.fecha_alerta::date = @Fecha::date
              AND ae.resuelta = false
            ORDER BY ae.fecha_alerta DESC
            LIMIT @Limit";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<DashboardGraficoDto>(sql,
                new { ClinicaId = clinicaId, Fecha = fecha, Limit = limit });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener últimas alertas de clínica {ClinicaId}", clinicaId);
            throw;
        }
    }
}
