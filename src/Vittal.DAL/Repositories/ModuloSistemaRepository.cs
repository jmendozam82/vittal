using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.modulos_sistema (catálogo global del sistema).
/// NO tiene clinicaId — es un catálogo de solo lectura compartido por todos los tenants.
/// </summary>
public class ModuloSistemaRepository : IModuloSistemaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ModuloSistemaRepository> _logger;

    public ModuloSistemaRepository(DbConnectionFactory dbConnectionFactory, ILogger<ModuloSistemaRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<ModuloSistema>> GetAllActiveAsync()
    {
        const string sql = @"
            SELECT
                id          AS Id,
                clave       AS Clave,
                nombre      AS Nombre,
                descripcion AS Descripcion,
                activo      AS Activo,
                fecha_creacion AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM public.modulos_sistema
            WHERE activo = true
            ORDER BY nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<ModuloSistema>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener módulos activos del sistema");
            throw;
        }
    }
}
