using System;
using System.Threading.Tasks;
using Dapper;
using Vittal.DAL.Context;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public UsuarioRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Usuario?> GetByAuthUserIdAsync(Guid authUserId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var query = @"
            SELECT
                u.id                  AS Id,
                u.clinica_id          AS ClinicaId,
                u.perfil_id           AS PerfilId,
                u.auth_user_id        AS AuthUserId,
                u.usuario             AS Username,
                u.nombres             AS Nombres,
                u.apellidos           AS Apellidos,
                u.email               AS Email,
                u.sexo                AS Sexo,
                u.direccion           AS Direccion,
                u.celular             AS Celular,
                u.es_doctor           AS EsDoctor,
                u.activo              AS Activo,
                u.fecha_creacion      AS FechaCreacion,
                u.fecha_modificacion  AS FechaModificacion,
                u.creado_por          AS CreadoPor,
                u.modificado_por      AS ModificadoPor,
                p.nombre              AS PerfilNombre,
                p.es_admin            AS EsAdmin
            FROM public.usuarios u
            INNER JOIN public.perfiles p ON u.perfil_id = p.id
            WHERE u.auth_user_id = @AuthUserId AND u.activo = true;
        ";

        return await connection.QuerySingleOrDefaultAsync<Usuario>(query, new { AuthUserId = authUserId });
    }
}
