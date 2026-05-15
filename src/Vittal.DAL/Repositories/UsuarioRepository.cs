using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Context;
using Vittal.DAL.Exceptions;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.usuarios.
/// Implementa IUsuarioRepository con Dapper y PostgreSQL.
/// </summary>
public class UsuarioRepository : IUsuarioRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<UsuarioRepository> _logger;

    public UsuarioRepository(DbConnectionFactory dbConnectionFactory, ILogger<UsuarioRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT con JOIN a perfiles ──────────────────────
    private const string SelectColumns = @"
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
        u.es_super_admin      AS EsSuperAdmin,
        u.activo              AS Activo,
        u.fecha_creacion      AS FechaCreacion,
        u.fecha_modificacion  AS FechaModificacion,
        u.creado_por          AS CreadoPor,
        u.modificado_por      AS ModificadoPor,
        p.nombre              AS PerfilNombre,
        p.es_admin            AS EsAdmin";

    private const string FromJoin = @"
        FROM public.usuarios u
        INNER JOIN public.perfiles p ON u.perfil_id = p.id";

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetByAuthUserIdAsync — Obtiene usuario por su ID de Supabase Auth
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Usuario?> GetByAuthUserIdAsync(Guid authUserId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE u.auth_user_id = @AuthUserId AND u.activo = true;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Usuario>(sql, new { AuthUserId = authUserId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario por AuthUserId {AuthUserId}", authUserId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetAllAsync — Lista todos los usuarios activos de una clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Usuario>> GetAllAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE u.clinica_id = @ClinicaId AND u.activo = true
            ORDER BY u.nombres, u.apellidos;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Usuario>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios activos de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2b. GetAllIncludingInactiveAsync — Lista TODOS (activos + inactivos)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Usuario>> GetAllIncludingInactiveAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE u.clinica_id = @ClinicaId
            ORDER BY u.activo DESC, u.nombres, u.apellidos;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Usuario>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los usuarios (incluyendo inactivos) de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. GetByIdAsync — Obtiene un usuario por ID validando clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Usuario?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE u.id = @Id AND u.clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Usuario>(sql, new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. CreateAsync — Inserta un nuevo usuario. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Usuario usuario)
    {
        const string sql = @"
            INSERT INTO public.usuarios (
                clinica_id, perfil_id, auth_user_id, usuario, nombres, apellidos,
                email, sexo, direccion, celular, es_doctor, activo,
                fecha_creacion, creado_por
            )
            VALUES (
                @ClinicaId, @PerfilId, @AuthUserId, @Username, @Nombres, @Apellidos,
                @Email, @Sexo, @Direccion, @Celular, @EsDoctor, true,
                NOW(), @CreadoPor
            )
            RETURNING id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, usuario);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning("Usuario duplicado en clínica {ClinicaId}: {Username}", usuario.ClinicaId, usuario.Username);
            throw new DuplicateEntityException(
                $"Ya existe un usuario con el username '{usuario.Username}' o email '{usuario.Email}' en esta clínica.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear usuario en clínica {ClinicaId}", usuario.ClinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. UpdateAsync — Actualiza datos del usuario (sin auth_user_id)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(Usuario usuario)
    {
        const string sql = @"
            UPDATE public.usuarios
            SET perfil_id          = @PerfilId,
                usuario            = @Username,
                nombres            = @Nombres,
                apellidos          = @Apellidos,
                email              = @Email,
                sexo               = @Sexo,
                direccion          = @Direccion,
                celular            = @Celular,
                es_doctor          = @EsDoctor,
                fecha_modificacion = NOW(),
                modificado_por     = @ModificadoPor
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, usuario);
            return rowsAffected > 0;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning("Usuario duplicado en actualización: {Username}", usuario.Username);
            throw new DuplicateEntityException(
                $"Ya existe un usuario con el username '{usuario.Username}' o email '{usuario.Email}' en esta clínica.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario {Id}", usuario.Id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. DeactivateAsync — Desactiva usuario (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.usuarios
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar usuario {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6b. ReactivateAsync — Reactiva usuario (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ReactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.usuarios
            SET activo = true, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar usuario {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. ExistsByUsernameAsync — Verifica duplicado de username en la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ExistsByUsernameAsync(Guid clinicaId, string username, Guid? excludeId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.usuarios
            WHERE clinica_id = @ClinicaId
              AND LOWER(usuario) = LOWER(@Username)
              AND (@ExcludeId IS NULL OR id != @ExcludeId);";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new { ClinicaId = clinicaId, Username = username, ExcludeId = excludeId });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de username en clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 8. ExistsByEmailAsync — Verifica duplicado de email en la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ExistsByEmailAsync(Guid clinicaId, string email, Guid? excludeId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.usuarios
            WHERE clinica_id = @ClinicaId
              AND LOWER(email) = LOWER(@Email)
              AND (@ExcludeId IS NULL OR id != @ExcludeId);";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new { ClinicaId = clinicaId, Email = email, ExcludeId = excludeId });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de email en clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 9. CountExpedientesAsync — Cuenta expedientes del usuario (validación)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<int> CountExpedientesAsync(Guid usuarioId, Guid clinicaId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.expedientes
            WHERE doctor_id = @UsuarioId AND clinica_id = @ClinicaId AND activo = true;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { UsuarioId = usuarioId, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al contar expedientes del usuario {UsuarioId}", usuarioId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 10. CountCitasAsync — Cuenta citas futuras del usuario (validación)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<int> CountCitasAsync(Guid usuarioId, Guid clinicaId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.citas
            WHERE doctor_id = @UsuarioId
              AND clinica_id = @ClinicaId
              AND activo = true
              AND fecha_cita >= NOW();";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { UsuarioId = usuarioId, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al contar citas del usuario {UsuarioId}", usuarioId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 11. GetDoctoresAsync — Lista solo usuarios con es_doctor = true
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Usuario>> GetDoctoresAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE u.clinica_id = @ClinicaId AND u.es_doctor = true AND u.activo = true
            ORDER BY u.nombres, u.apellidos;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Usuario>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener doctores de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }
}
