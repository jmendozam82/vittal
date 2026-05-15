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
/// Repositorio para la tabla public.pacientes.
/// Implementa IPacienteRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU07 — Gestión de Pacientes
/// </summary>
public class PacienteRepository : IPacienteRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<PacienteRepository> _logger;

    public PacienteRepository(DbConnectionFactory dbConnectionFactory, ILogger<PacienteRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    // ── Columnas base para SELECT con JOIN a usuarios (doctor) ──────────────
    private const string SelectColumns = @"
        p.id                    AS Id,
        p.clinica_id            AS ClinicaId,
        p.doctor_id             AS DoctorId,
        p.primer_nombre         AS PrimerNombre,
        p.segundo_nombre        AS SegundoNombre,
        p.primer_apellido       AS PrimerApellido,
        p.segundo_apellido      AS SegundoApellido,
        p.email                 AS Email,
        p.celular               AS Celular,
        p.direccion             AS Direccion,
        p.sexo                  AS Sexo,
        p.fecha_nacimiento      AS FechaNacimiento,
        p.foto_url              AS FotoUrl,
        p.observaciones         AS Observaciones,
        p.activo                AS Activo,
        p.fecha_creacion        AS FechaCreacion,
        p.fecha_modificacion    AS FechaModificacion,
        p.creado_por            AS CreadoPor,
        p.modificado_por        AS ModificadoPor,
        u.nombres || ' ' || u.apellidos AS DoctorNombre";

    private const string FromJoin = @"
        FROM public.pacientes p
        INNER JOIN public.usuarios u ON p.doctor_id = u.id";

    // ────────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Lista todos los pacientes activos de una clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Paciente>> GetAllAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE p.clinica_id = @ClinicaId AND p.activo = true
            ORDER BY p.primer_apellido, p.primer_nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Paciente>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pacientes activos de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1b. GetAllIncludingInactiveAsync — Lista TODOS (activos + inactivos)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<Paciente>> GetAllIncludingInactiveAsync(Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE p.clinica_id = @ClinicaId
            ORDER BY p.activo DESC, p.primer_apellido, p.primer_nombre;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QueryAsync<Paciente>(sql, new { ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los pacientes (incluyendo inactivos) de la clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un paciente por ID validando clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Paciente?> GetByIdAsync(Guid id, Guid clinicaId)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE p.id = @Id AND p.clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Paciente>(sql, new { Id = id, ClinicaId = clinicaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener paciente {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo paciente. Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(Paciente paciente)
    {
        const string sql = @"
            INSERT INTO public.pacientes (
                clinica_id, doctor_id,
                primer_nombre, segundo_nombre, primer_apellido, segundo_apellido,
                email, celular, direccion, sexo, fecha_nacimiento,
                foto_url, observaciones,
                activo, fecha_creacion, creado_por
            )
            VALUES (
                @ClinicaId, @DoctorId,
                @PrimerNombre, @SegundoNombre, @PrimerApellido, @SegundoApellido,
                @Email, @Celular, @Direccion, @Sexo, @FechaNacimiento,
                @FotoUrl, @Observaciones,
                true, NOW(), @CreadoPor
            )
            RETURNING id;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<Guid>(sql, paciente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear paciente en clínica {ClinicaId}", paciente.ClinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza datos del paciente
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(Paciente paciente)
    {
        const string sql = @"
            UPDATE public.pacientes
            SET doctor_id           = @DoctorId,
                primer_nombre       = @PrimerNombre,
                segundo_nombre      = @SegundoNombre,
                primer_apellido     = @PrimerApellido,
                segundo_apellido    = @SegundoApellido,
                email               = @Email,
                celular             = @Celular,
                direccion           = @Direccion,
                sexo                = @Sexo,
                fecha_nacimiento    = @FechaNacimiento,
                foto_url            = @FotoUrl,
                observaciones       = @Observaciones,
                fecha_modificacion  = NOW(),
                modificado_por      = @ModificadoPor
            WHERE id = @Id AND clinica_id = @ClinicaId;";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, paciente);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar paciente {Id}", paciente.Id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva paciente (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.pacientes
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
            _logger.LogError(ex, "Error al desactivar paciente {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5b. ReactivateAsync — Reactiva paciente (activo = true)
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ReactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE public.pacientes
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
            _logger.LogError(ex, "Error al reactivar paciente {Id}", id);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. ExistsByEmailAsync — Verifica duplicado de email en la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ExistsByEmailAsync(Guid clinicaId, string email, Guid? excludeId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.pacientes
            WHERE clinica_id = @ClinicaId
              AND LOWER(email) = LOWER(@Email)
              AND (@ExcludeId IS NULL OR id != @ExcludeId);";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                ClinicaId = clinicaId,
                Email = email,
                ExcludeId = excludeId
            });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de email en clínica {ClinicaId}", clinicaId);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. ExistsByCelularAsync — Verifica duplicado de celular en la clínica
    // ────────────────────────────────────────────────────────────────────────
    public async Task<bool> ExistsByCelularAsync(Guid clinicaId, string celular, Guid? excludeId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM public.pacientes
            WHERE clinica_id = @ClinicaId
              AND celular = @Celular
              AND (@ExcludeId IS NULL OR id != @ExcludeId);";

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                ClinicaId = clinicaId,
                Celular = celular,
                ExcludeId = excludeId
            });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de celular en clínica {ClinicaId}", clinicaId);
            throw;
        }
    }
}
