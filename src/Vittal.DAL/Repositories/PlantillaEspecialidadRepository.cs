using System.Data;
using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity;

namespace Vittal.DAL.Repositories;

public class PlantillaEspecialidadRepository : IPlantillaEspecialidadRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public PlantillaEspecialidadRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<PlantillaEspecialidad>> GetAllAsync()
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            SELECT p.*, i.*
            FROM plantillas_especialidad p
            LEFT JOIN plantilla_items i ON p.id = i.plantilla_id AND i.activo = true
            WHERE p.activo = true
            ORDER BY p.nombre, i.orden";

        var plantillaDictionary = new Dictionary<Guid, PlantillaEspecialidad>();

        var result = await connection.QueryAsync<PlantillaEspecialidad, PlantillaItem, PlantillaEspecialidad>(
            sql,
            (plantilla, item) =>
            {
                if (!plantillaDictionary.TryGetValue(plantilla.Id, out var currentPlantilla))
                {
                    currentPlantilla = plantilla;
                    currentPlantilla.Items = new List<PlantillaItem>();
                    plantillaDictionary.Add(currentPlantilla.Id, currentPlantilla);
                }

                if (item != null)
                {
                    currentPlantilla.Items.Add(item);
                }

                return currentPlantilla;
            },
            splitOn: "id"
        );

        return plantillaDictionary.Values;
    }

    public async Task<PlantillaEspecialidad?> GetByIdAsync(Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            SELECT p.*, i.*
            FROM plantillas_especialidad p
            LEFT JOIN plantilla_items i ON p.id = i.plantilla_id AND i.activo = true
            WHERE p.id = @Id AND p.activo = true
            ORDER BY i.orden";

        PlantillaEspecialidad? resultPlantilla = null;

        await connection.QueryAsync<PlantillaEspecialidad, PlantillaItem, PlantillaEspecialidad>(
            sql,
            (plantilla, item) =>
            {
                if (resultPlantilla == null)
                {
                    resultPlantilla = plantilla;
                    resultPlantilla.Items = new List<PlantillaItem>();
                }

                if (item != null)
                {
                    resultPlantilla.Items.Add(item);
                }

                return resultPlantilla;
            },
            new { Id = id },
            splitOn: "id"
        );

        return resultPlantilla;
    }

    public async Task<Guid> CreateAsync(PlantillaEspecialidad entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var sqlPlantilla = @"
                INSERT INTO plantillas_especialidad (nombre, descripcion, icono, activo, fecha_creacion)
                VALUES (@Nombre, @Descripcion, @Icono, @Activo, @FechaCreacion)
                RETURNING id";

            var id = await connection.ExecuteScalarAsync<Guid>(sqlPlantilla, entity, transaction);
            entity.Id = id;

            if (entity.Items != null && entity.Items.Any())
            {
                var sqlItem = @"
                    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, unidad, valor_min, valor_max, es_obligatorio, orden, activo, fecha_creacion)
                    VALUES (@PlantillaId, @TipoItem, @Nombre, @Categoria, @TipoDato, @Unidad, @ValorMin, @ValorMax, @EsObligatorio, @Orden, @Activo, @FechaCreacion)";

                foreach (var item in entity.Items)
                {
                    item.PlantillaId = id;
                    await connection.ExecuteAsync(sqlItem, item, transaction);
                }
            }

            transaction.Commit();
            return id;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(PlantillaEspecialidad entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE plantillas_especialidad 
            SET nombre = @Nombre, 
                descripcion = @Descripcion, 
                icono = @Icono,
                fecha_modificacion = @FechaModificacion
            WHERE id = @Id AND activo = true";

        var result = await connection.ExecuteAsync(sql, entity);
        return result > 0;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = "UPDATE plantillas_especialidad SET activo = false, fecha_modificacion = CURRENT_TIMESTAMP WHERE id = @Id";
        var result = await connection.ExecuteAsync(sql, new { Id = id });
        return result > 0;
    }

    public async Task<bool> ReactivateAsync(Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = "UPDATE plantillas_especialidad SET activo = true, fecha_modificacion = CURRENT_TIMESTAMP WHERE id = @Id";
        var result = await connection.ExecuteAsync(sql, new { Id = id });
        return result > 0;
    }
}
