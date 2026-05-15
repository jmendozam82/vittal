using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para verificar y gestionar permisos de un perfil sobre módulos del sistema.
/// Tablas involucradas: public.permisos, public.modulos_sistema
/// </summary>
public class PermisoRepository : IPermisoRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<PermisoRepository> _logger;

    public PermisoRepository(DbConnectionFactory dbConnectionFactory, ILogger<PermisoRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Verifica si un perfil tiene permisos de lectura, creación y/o actualización
    /// para un módulo específico en una clínica.
    /// </summary>
    public async Task<(bool puedeLeer, bool puedeCrear, bool puedeActualizar)> GetPermisosAsync(
        Guid clinicaId, Guid perfilId, string moduloClave)
    {
        const string sql = @"
            SELECT pm.puede_leer, pm.puede_crear, pm.puede_actualizar
            FROM public.permisos pm
            INNER JOIN public.modulos_sistema m ON m.id = pm.modulo_id
            WHERE pm.clinica_id = @ClinicaId
              AND pm.perfil_id = @PerfilId
              AND m.clave = @ModuloClave";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<(bool, bool, bool)>(sql,
                new { ClinicaId = clinicaId, PerfilId = perfilId, ModuloClave = moduloClave });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar permiso para perfil {PerfilId} módulo {ModuloClave}",
                perfilId, moduloClave);
            throw;
        }
    }

    /// <summary>
    /// Obtiene todos los permisos de un perfil para todos los módulos del sistema.
    /// LEFT JOIN para mostrar incluso módulos sin permiso explícito (valores false).
    /// </summary>
    public async Task<IEnumerable<(Guid permisoid, Guid moduloid, string clave, string nombre, string? descripcion, bool puedeLeer, bool puedeCrear, bool puedeActualizar)>> GetPermisosByPerfilAsync(
        Guid clinicaId, Guid perfilId)
    {
        const string sql = @"
            SELECT
                p.id AS permisoid,
                m.id AS moduloid,
                m.clave,
                m.nombre,
                m.descripcion,
                COALESCE(p.puede_leer, false) AS puede_leer,
                COALESCE(p.puede_crear, false) AS puede_crear,
                COALESCE(p.puede_actualizar, false) AS puede_actualizar
            FROM public.modulos_sistema m
            LEFT JOIN public.permisos p
                ON p.modulo_id = m.id
                AND p.perfil_id = @PerfilId
                AND p.clinica_id = @ClinicaId
            WHERE m.activo = true
            ORDER BY m.nombre";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<(Guid, Guid, string, string, string?, bool, bool, bool)>(sql,
                new { ClinicaId = clinicaId, PerfilId = perfilId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener permisos del perfil {PerfilId}", perfilId);
            throw;
        }
    }

    /// <summary>
    /// Inserta o actualiza (upsert) un permiso individual para un perfil sobre un módulo.
    /// Usa INSERT ... ON CONFLICT (clinica_id, perfil_id, modulo_id) DO UPDATE.
    /// </summary>
    public async Task<bool> UpsertPermisoAsync(Guid clinicaId, Guid perfilId, Guid moduloId,
        bool puedeLeer, bool puedeCrear, bool puedeActualizar, Guid modificadoPor)
    {
        const string sql = @"
            INSERT INTO public.permisos (clinica_id, perfil_id, modulo_id, puede_leer, puede_crear, puede_actualizar, fecha_modificacion, modificado_por)
            VALUES (@ClinicaId, @PerfilId, @ModuloId, @PuedeLeer, @PuedeCrear, @PuedeActualizar, NOW(), @ModificadoPor)
            ON CONFLICT (clinica_id, perfil_id, modulo_id)
            DO UPDATE SET
                puede_leer = EXCLUDED.puede_leer,
                puede_crear = EXCLUDED.puede_crear,
                puede_actualizar = EXCLUDED.puede_actualizar,
                fecha_modificacion = NOW(),
                modificado_por = EXCLUDED.modificado_por";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(sql, new
            {
                ClinicaId = clinicaId,
                PerfilId = perfilId,
                ModuloId = moduloId,
                PuedeLeer = puedeLeer,
                PuedeCrear = puedeCrear,
                PuedeActualizar = puedeActualizar,
                ModificadoPor = modificadoPor
            });
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al upsert permiso para perfil {PerfilId} módulo {ModuloId}",
                perfilId, moduloId);
            throw;
        }
    }
}
