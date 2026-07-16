using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.expediente_archivos.
/// Implementa IExpedienteArchivoRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU20 — Expedientes
/// 
/// NOTAS DE MAPEO Entity ↔ SQL:
/// - TipoMime → tipo_mime
/// - StoragePath → storage_path
/// - UrlPublica → url_publica
/// - TamanoBytes → tamano_bytes
/// - CreadoPor → creado_por
/// </summary>
public class ExpedienteArchivoRepository : IExpedienteArchivoRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public ExpedienteArchivoRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ── Columnas base para SELECT ──────────────────────────────────────
    // NOTA: Mapeo de nombres snake_case (sql) → PascalCase (entity)
    private const string SelectColumns = @"
        id                      AS Id,
        clinica_id              AS ClinicaId,
        expediente_id           AS ExpedienteId,
        hoja_cita_id            AS HojaCitaId,
        nombre_archivo          AS NombreArchivo,
        tipo_mime               AS TipoMime,
        storage_path            AS StoragePath,
        url_publica             AS UrlPublica,
        tamano_bytes            AS TamanoBytes,
        activo                  AS Activo,
        fecha_creacion          AS FechaCreacion,
        creado_por              AS CreadoPor";

    // ────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Obtiene todos los archivos activos de una clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ExpedienteArchivo>> GetAllAsync(Guid clinicaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            FROM public.expediente_archivos
            WHERE clinica_id = @ClinicaId AND activo = true
            ORDER BY fecha_creacion DESC";

        return await connection.QueryAsync<ExpedienteArchivo>(sql, new { ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un archivo por ID validando clínica
    // ────────────────────────────────────────────────────────────────────
    public async Task<ExpedienteArchivo?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            FROM public.expediente_archivos
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";

        return await connection.QuerySingleOrDefaultAsync<ExpedienteArchivo>(sql,
            new { Id = id, ClinicaId = clinicaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. GetByExpedienteIdAsync — Obtiene todos los archivos de un expediente
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ExpedienteArchivo>> GetByExpedienteIdAsync(Guid clinicaId, Guid expedienteId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            FROM public.expediente_archivos
            WHERE clinica_id = @ClinicaId 
              AND expediente_id = @ExpedienteId 
              AND activo = true
            ORDER BY fecha_creacion DESC";

        return await connection.QueryAsync<ExpedienteArchivo>(sql,
            new { ClinicaId = clinicaId, ExpedienteId = expedienteId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. GetByHojaCitaIdAsync — Obtiene todos los archivos de una hoja de cita
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<ExpedienteArchivo>> GetByHojaCitaIdAsync(Guid clinicaId, Guid hojaCitaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @$"
            SELECT {SelectColumns}
            FROM public.expediente_archivos
            WHERE clinica_id = @ClinicaId 
              AND hoja_cita_id = @HojaCitaId 
              AND activo = true
            ORDER BY fecha_creacion DESC";

        return await connection.QueryAsync<ExpedienteArchivo>(sql,
            new { ClinicaId = clinicaId, HojaCitaId = hojaCitaId });
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. CreateAsync — Inserta un nuevo archivo. Retorna el ID autogenerado.
    // NOTA: Mapeo de PascalCase (entity) → snake_case (sql)
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(ExpedienteArchivo entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.expediente_archivos (
                clinica_id, expediente_id, hoja_cita_id,
                nombre_archivo, tipo_mime, storage_path, url_publica, tamano_bytes,
                activo, fecha_creacion, creado_por
            )
            VALUES (
                @ClinicaId, @ExpedienteId, @HojaCitaId,
                @NombreArchivo, @TipoMime, @StoragePath, @UrlPublica, @TamanoBytes,
                true, NOW(), @CreadoPor
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 6. UpdateAsync — Actualiza el nombre de un archivo existente
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(ExpedienteArchivo entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.expediente_archivos
            SET nombre_archivo = @NombreArchivo
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 7. DeactivateAsync — Desactiva archivo (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.expediente_archivos
            SET activo = false
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
