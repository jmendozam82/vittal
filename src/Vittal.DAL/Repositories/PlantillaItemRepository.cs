using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repository para items individuales de plantillas de especialidad.
/// Tabla global: plantilla_items — sin clinica_id.
/// </summary>
public class PlantillaItemRepository : IPlantillaItemRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public PlantillaItemRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<PlantillaItem>> GetByPlantillaIdAsync(Guid plantillaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        const string sql = @"
            SELECT
                id                AS Id,
                plantilla_id      AS PlantillaId,
                tipo_item         AS TipoItem,
                nombre            AS Nombre,
                categoria         AS Categoria,
                tipo_dato         AS TipoDato,
                unidad            AS Unidad,
                valor_min         AS ValorMin,
                valor_max         AS ValorMax,
                es_obligatorio    AS EsObligatorio,
                orden             AS Orden,
                activo            AS Activo,
                fecha_creacion    AS FechaCreacion
            FROM plantilla_items
            WHERE plantilla_id = @PlantillaId AND activo = true
            ORDER BY tipo_item, orden, nombre";

        return await connection.QueryAsync<PlantillaItem>(sql, new { PlantillaId = plantillaId });
    }

    public async Task<PlantillaItem?> GetByIdAsync(Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        const string sql = @"
            SELECT
                id                AS Id,
                plantilla_id      AS PlantillaId,
                tipo_item         AS TipoItem,
                nombre            AS Nombre,
                categoria         AS Categoria,
                tipo_dato         AS TipoDato,
                unidad            AS Unidad,
                valor_min         AS ValorMin,
                valor_max         AS ValorMax,
                es_obligatorio    AS EsObligatorio,
                orden             AS Orden,
                activo            AS Activo,
                fecha_creacion    AS FechaCreacion
            FROM plantilla_items
            WHERE id = @Id AND activo = true";
        return await connection.QuerySingleOrDefaultAsync<PlantillaItem>(sql, new { Id = id });
    }

    public async Task<Guid> CreateAsync(PlantillaItem entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato,
                                         unidad, valor_min, valor_max, es_obligatorio, orden,
                                         activo, fecha_creacion)
            VALUES (@PlantillaId, @TipoItem, @Nombre, @Categoria, @TipoDato,
                    @Unidad, @ValorMin, @ValorMax, @EsObligatorio, @Orden,
                    @Activo, @FechaCreacion)
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    public async Task<bool> UpdateAsync(PlantillaItem entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        const string sql = @"
            UPDATE plantilla_items
            SET tipo_item = @TipoItem,
                nombre = @Nombre,
                categoria = @Categoria,
                tipo_dato = @TipoDato,
                unidad = @Unidad,
                valor_min = @ValorMin,
                valor_max = @ValorMax,
                es_obligatorio = @EsObligatorio,
                orden = @Orden
            WHERE id = @Id AND activo = true";

        var result = await connection.ExecuteAsync(sql, entity);
        return result > 0;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        const string sql = "UPDATE plantilla_items SET activo = false WHERE id = @Id";
        var result = await connection.ExecuteAsync(sql, new { Id = id });
        return result > 0;
    }

    public async Task<bool> ReactivateAsync(Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        const string sql = "UPDATE plantilla_items SET activo = true WHERE id = @Id";
        var result = await connection.ExecuteAsync(sql, new { Id = id });
        return result > 0;
    }
}
