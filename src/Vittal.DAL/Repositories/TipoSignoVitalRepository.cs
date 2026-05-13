using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

public class TipoSignoVitalRepository : ITipoSignoVitalRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public TipoSignoVitalRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<TipoSignoVital>> GetAllAsync(Guid clinicaId, Guid salaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            SELECT * FROM tipos_signo_vital 
            WHERE clinica_id = @ClinicaId AND sala_id = @SalaId AND activo = true 
            ORDER BY orden, nombre";
            
        return await connection.QueryAsync<TipoSignoVital>(sql, new { ClinicaId = clinicaId, SalaId = salaId });
    }

    public async Task<TipoSignoVital?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = "SELECT * FROM tipos_signo_vital WHERE clinica_id = @ClinicaId AND id = @Id AND activo = true";
        return await connection.QuerySingleOrDefaultAsync<TipoSignoVital>(sql, new { ClinicaId = clinicaId, Id = id });
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
}
