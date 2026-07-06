using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

public class TipoAntecedenteRepository : ITipoAntecedenteRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public TipoAntecedenteRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<TipoAntecedente>> GetAllAsync(Guid clinicaId, Guid salaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            SELECT * FROM tipos_antecedente 
            WHERE clinica_id = @ClinicaId AND sala_id = @SalaId AND activo = true 
            ORDER BY orden, nombre";
            
        return await connection.QueryAsync<TipoAntecedente>(sql, new { ClinicaId = clinicaId, SalaId = salaId });
    }

    public async Task<TipoAntecedente?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = "SELECT * FROM tipos_antecedente WHERE clinica_id = @ClinicaId AND id = @Id AND activo = true";
        return await connection.QuerySingleOrDefaultAsync<TipoAntecedente>(sql, new { ClinicaId = clinicaId, Id = id });
    }

    public async Task<TipoAntecedente?> GetBySalaAndNameAsync(Guid clinicaId, Guid salaId, string nombre)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = "SELECT * FROM tipos_antecedente WHERE clinica_id = @ClinicaId AND sala_id = @SalaId AND LOWER(nombre) = LOWER(@Nombre)";
        return await connection.QuerySingleOrDefaultAsync<TipoAntecedente>(sql, new { ClinicaId = clinicaId, SalaId = salaId, Nombre = nombre });
    }

    public async Task<Guid> CreateAsync(TipoAntecedente entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO tipos_antecedente (clinica_id, sala_id, nombre, categoria, tipo_dato, orden, activo, fecha_creacion, creado_por)
            VALUES (@ClinicaId, @SalaId, @Nombre, @Categoria, @TipoDato, @Orden, @Activo, @FechaCreacion, @CreadoPor)
            RETURNING id";
            
        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    public async Task<bool> UpdateAsync(TipoAntecedente entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE tipos_antecedente 
            SET nombre = @Nombre, 
                categoria = @Categoria, 
                tipo_dato = @TipoDato,
                orden = @Orden,
                fecha_modificacion = @FechaModificacion
            WHERE clinica_id = @ClinicaId AND id = @Id AND activo = true";
            
        var result = await connection.ExecuteAsync(sql, entity);
        return result > 0;
    }

    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = "UPDATE tipos_antecedente SET activo = false, fecha_modificacion = CURRENT_TIMESTAMP WHERE clinica_id = @ClinicaId AND id = @Id";
        var result = await connection.ExecuteAsync(sql, new { ClinicaId = clinicaId, Id = id });
        return result > 0;
    }

    public async Task<bool> ReactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = "UPDATE tipos_antecedente SET activo = true, fecha_modificacion = CURRENT_TIMESTAMP WHERE clinica_id = @ClinicaId AND id = @Id";
        var result = await connection.ExecuteAsync(sql, new { ClinicaId = clinicaId, Id = id });
        return result > 0;
    }
}
