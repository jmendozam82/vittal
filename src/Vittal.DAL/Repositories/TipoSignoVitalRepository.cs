using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

public class TipoSignoVitalRepository : ITipoSignoVitalRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public TipoSignoVitalRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    private const string SelectColumns = @"
        id                  AS Id,
        clinica_id          AS ClinicaId,
        sala_id             AS SalaId,
        nombre              AS Nombre,
        unidad              AS Unidad,
        valor_min           AS ValorMin,
        valor_max           AS ValorMax,
        orden               AS Orden,
        es_obligatorio      AS EsObligatorio,
        activo              AS Activo,
        fecha_creacion      AS FechaCreacion,
        fecha_modificacion  AS FechaModificacion,
        creado_por          AS CreadoPor";

    public async Task<IEnumerable<TipoSignoVital>> GetAllAsync(Guid clinicaId, Guid? salaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $"SELECT {SelectColumns} FROM tipos_signo_vital WHERE clinica_id = @ClinicaId AND activo = true";
        object param = new { ClinicaId = clinicaId };

        if (salaId.HasValue && salaId.Value != Guid.Empty)
        {
            sql += " AND sala_id = @SalaId";
            param = new { ClinicaId = clinicaId, SalaId = salaId.Value };
        }

        sql += " ORDER BY orden, nombre";
        return await connection.QueryAsync<TipoSignoVital>(sql, param);
    }

    public async Task<TipoSignoVital?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $"SELECT {SelectColumns} FROM tipos_signo_vital WHERE clinica_id = @ClinicaId AND id = @Id AND activo = true";
        return await connection.QuerySingleOrDefaultAsync<TipoSignoVital>(sql, new { ClinicaId = clinicaId, Id = id });
    }

    public async Task<TipoSignoVital?> GetBySalaAndNameAsync(Guid clinicaId, Guid salaId, string nombre)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $"SELECT {SelectColumns} FROM tipos_signo_vital WHERE clinica_id = @ClinicaId AND sala_id = @SalaId AND LOWER(nombre) = LOWER(@Nombre)";
        return await connection.QuerySingleOrDefaultAsync<TipoSignoVital>(sql, new { ClinicaId = clinicaId, SalaId = salaId, Nombre = nombre });
    }

    public async Task<Guid> CreateAsync(TipoSignoVital entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO tipos_signo_vital (clinica_id, sala_id, nombre, unidad, valor_min, valor_max, orden, es_obligatorio, activo, fecha_creacion, creado_por)
            VALUES (@ClinicaId, @SalaId, @Nombre, @Unidad, @ValorMin, @ValorMax, @Orden, @EsObligatorio, @Activo, @FechaCreacion, @CreadoPor)
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    public async Task<bool> UpdateAsync(TipoSignoVital entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE tipos_signo_vital 
            SET nombre = @Nombre, 
                unidad = @Unidad, 
                valor_min = @ValorMin,
                valor_max = @ValorMax,
                orden = @Orden,
                es_obligatorio = @EsObligatorio,
                fecha_modificacion = @FechaModificacion
            WHERE clinica_id = @ClinicaId AND id = @Id AND activo = true";

        var result = await connection.ExecuteAsync(sql, entity);
        return result > 0;
    }

    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = "UPDATE tipos_signo_vital SET activo = false, fecha_modificacion = CURRENT_TIMESTAMP WHERE clinica_id = @ClinicaId AND id = @Id";
        var result = await connection.ExecuteAsync(sql, new { ClinicaId = clinicaId, Id = id });
        return result > 0;
    }

    public async Task<bool> ReactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = "UPDATE tipos_signo_vital SET activo = true, fecha_modificacion = CURRENT_TIMESTAMP WHERE clinica_id = @ClinicaId AND id = @Id";
        var result = await connection.ExecuteAsync(sql, new { ClinicaId = clinicaId, Id = id });
        return result > 0;
    }
}
