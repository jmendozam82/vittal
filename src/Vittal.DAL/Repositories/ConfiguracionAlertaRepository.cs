using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Exceptions;
using Vittal.DAL.Interfaces;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.configuracion_alertas.
/// Implementa IConfiguracionAlertaRepository con Dapper y PostgreSQL.
/// Relación 1:1 con clinica_id — upsert para crear o actualizar.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class ConfiguracionAlertaRepository : IConfiguracionAlertaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ConfiguracionAlertaRepository> _logger;

    public ConfiguracionAlertaRepository(
        DbConnectionFactory dbConnectionFactory,
        ILogger<ConfiguracionAlertaRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    private const string SelectColumns = @"
        ca.id                           AS Id,
        ca.clinica_id                   AS ClinicaId,
        ca.tiempo_espera_maximo_minutos AS TiempoEsperaMaximoMinutos,
        ca.activo                       AS Activo,
        ca.notificacion_sonido          AS NotificacionSonido,
        ca.intervalo_revision_segundos  AS IntervaloRevisionSegundos,
        ca.fecha_creacion               AS FechaCreacion,
        ca.fecha_modificacion           AS FechaModificacion,
        ca.creado_por                   AS CreadoPor,
        ca.modificado_por               AS ModificadoPor";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetByClinicaIdAsync — Obtiene la configuración de una clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<ConfiguracionAlerta?> GetByClinicaIdAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT " + SelectColumns + @"
            FROM public.configuracion_alertas ca
            WHERE ca.clinica_id = @ClinicaId
              AND ca.activo = true";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<ConfiguracionAlerta>(sql,
                new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al obtener configuración de alertas de clínica {ClinicaId}",
                clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. CreateOrUpdateAsync — Upsert: inserta o actualiza si ya existe
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateOrUpdateAsync(ConfiguracionAlerta entity)
    {
        const string sql = @"
            INSERT INTO public.configuracion_alertas (
                clinica_id,
                tiempo_espera_maximo_minutos,
                activo,
                notificacion_sonido,
                intervalo_revision_segundos,
                fecha_creacion,
                creado_por
            )
            VALUES (
                @ClinicaId,
                @TiempoEsperaMaximoMinutos,
                @Activo,
                @NotificacionSonido,
                @IntervaloRevisionSegundos,
                NOW(),
                @CreadoPor
            )
            ON CONFLICT (clinica_id)
            DO UPDATE SET
                tiempo_espera_maximo_minutos = EXCLUDED.tiempo_espera_maximo_minutos,
                activo = EXCLUDED.activo,
                notificacion_sonido = EXCLUDED.notificacion_sonido,
                intervalo_revision_segundos = EXCLUDED.intervalo_revision_segundos,
                fecha_modificacion = NOW(),
                modificado_por = EXCLUDED.creado_por
            RETURNING id";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al crear o actualizar configuración de alertas de clínica {ClinicaId}",
                entity.ClinicaId);
            throw new RepositoryException("Error al guardar la configuración de alertas.", ex);
        }
    }
}
