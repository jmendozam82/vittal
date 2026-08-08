using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Exceptions;
using Vittal.DAL.Interfaces;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.dashboard_config.
/// Implementa IDashboardConfigRepository con Dapper y PostgreSQL.
/// Relación 1:1 con clinica_id — upsert para crear o actualizar.
/// Historia de Usuario: HU23 — Dashboard
/// </summary>
public class DashboardConfigRepository : IDashboardConfigRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<DashboardConfigRepository> _logger;

    public DashboardConfigRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<DashboardConfigRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    private const string SelectColumns = @"
        dc.id                           AS Id,
        dc.clinica_id                   AS ClinicaId,
        dc.mostrar_pacientes_del_dia    AS MostrarPacientesDelDia,
        dc.mostrar_citas_pendientes     AS MostrarCitasPendientes,
        dc.mostrar_pacientes_en_espera  AS MostrarPacientesEnEspera,
        dc.mostrar_tiempo_promedio_espera AS MostrarTiempoPromedioEspera,
        dc.mostrar_grafico_citas_por_hora AS MostrarGraficoCitasPorHora,
        dc.mostrar_citas_por_medico    AS MostrarCitasPorMedico,
        dc.mostrar_ultimas_alertas      AS MostrarUltimasAlertas,
        dc.layout                       AS Layout,
        dc.activo                       AS Activo,
        dc.fecha_creacion               AS FechaCreacion,
        dc.fecha_modificacion           AS FechaModificacion";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetByClinicaIdAsync — Obtiene la configuración del dashboard
    // ────────────────────────────────────────────────────────────────────
    public async Task<DashboardConfig?> GetByClinicaIdAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT " + SelectColumns + @"
            FROM public.dashboard_config dc
            WHERE dc.clinica_id = @ClinicaId
              AND dc.activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<DashboardConfig>(sql,
                new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener configuración del dashboard de clínica {ClinicaId}",
                clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. CreateOrUpdateAsync — Upsert: inserta o actualiza si ya existe
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateOrUpdateAsync(DashboardConfig entity)
    {
        const string sql = @"
            INSERT INTO public.dashboard_config (
                clinica_id,
                mostrar_pacientes_del_dia,
                mostrar_citas_pendientes,
                mostrar_pacientes_en_espera,
                mostrar_tiempo_promedio_espera,
                mostrar_grafico_citas_por_hora,
                mostrar_citas_por_medico,
                mostrar_ultimas_alertas,
                layout,
                activo,
                fecha_creacion
            )
            VALUES (
                @ClinicaId,
                @MostrarPacientesDelDia,
                @MostrarCitasPendientes,
                @MostrarPacientesEnEspera,
                @MostrarTiempoPromedioEspera,
                @MostrarGraficoCitasPorHora,
                @MostrarCitasPorMedico,
                @MostrarUltimasAlertas,
                @Layout,
                true,
                NOW()
            )
            ON CONFLICT (clinica_id)
            DO UPDATE SET
                mostrar_pacientes_del_dia       = EXCLUDED.mostrar_pacientes_del_dia,
                mostrar_citas_pendientes        = EXCLUDED.mostrar_citas_pendientes,
                mostrar_pacientes_en_espera     = EXCLUDED.mostrar_pacientes_en_espera,
                mostrar_tiempo_promedio_espera  = EXCLUDED.mostrar_tiempo_promedio_espera,
                mostrar_grafico_citas_por_hora  = EXCLUDED.mostrar_grafico_citas_por_hora,
                mostrar_citas_por_medico        = EXCLUDED.mostrar_citas_por_medico,
                mostrar_ultimas_alertas         = EXCLUDED.mostrar_ultimas_alertas,
                layout                          = EXCLUDED.layout,
                activo                          = EXCLUDED.activo,
                fecha_modificacion              = NOW()
            RETURNING id";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al crear o actualizar configuración del dashboard de clínica {ClinicaId}",
                entity.ClinicaId);
            throw new RepositoryException("Error al guardar la configuración del dashboard.", ex);
        }
    }
}
