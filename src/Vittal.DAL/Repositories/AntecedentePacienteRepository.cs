using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.antecedentes_paciente.
/// Implementa IAntecedentePacienteRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU-E05 — Antecedentes del Paciente
/// </summary>
public class AntecedentePacienteRepository : IAntecedentePacienteRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public AntecedentePacienteRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Obtiene los antecedentes activos de un expediente
    //                  en una sala, con datos del tipo de antecedente.
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<AntecedentePaciente>> GetAllAsync(Guid clinicaId, Guid expedienteId, Guid salaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            SELECT 
                ap.id                    AS Id,
                ap.clinica_id            AS ClinicaId,
                ap.expediente_id         AS ExpedienteId,
                ap.sala_id               AS SalaId,
                ap.tipo_antecedente_id   AS TipoAntecedenteId,
                ap.valor                 AS Valor,
                ap.fecha_actualizacion   AS FechaActualizacion,
                ap.actualizado_por       AS ActualizadoPor,
                ap.activo                AS Activo,
                ap.fecha_creacion        AS FechaCreacion,
                ap.fecha_modificacion    AS FechaModificacion,
                ta.nombre                AS Nombre,
                ta.categoria             AS Categoria,
                ta.tipo_dato             AS TipoDato,
                ta.orden                 AS Orden
            FROM public.antecedentes_paciente ap
            INNER JOIN public.tipos_antecedente ta ON ap.tipo_antecedente_id = ta.id
            WHERE ap.clinica_id = @ClinicaId
              AND ap.expediente_id = @ExpedienteId
              AND ap.sala_id = @SalaId
              AND ap.activo = true
            ORDER BY ta.orden, ta.nombre";

        var result = await connection.QueryAsync<AntecedentePaciente, TipoAntecedente, AntecedentePaciente>(
            sql,
            (ap, ta) =>
            {
                ap.TipoAntecedente = ta;
                return ap;
            },
            splitOn: "Nombre",
            param: new { ClinicaId = clinicaId, ExpedienteId = expedienteId, SalaId = salaId }
        );

        return result;
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un antecedente por ID
    // ────────────────────────────────────────────────────────────────────
    public async Task<AntecedentePaciente?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            SELECT 
                ap.id                    AS Id,
                ap.clinica_id            AS ClinicaId,
                ap.expediente_id         AS ExpedienteId,
                ap.sala_id               AS SalaId,
                ap.tipo_antecedente_id   AS TipoAntecedenteId,
                ap.valor                 AS Valor,
                ap.fecha_actualizacion   AS FechaActualizacion,
                ap.actualizado_por       AS ActualizadoPor,
                ap.activo                AS Activo,
                ap.fecha_creacion        AS FechaCreacion,
                ap.fecha_modificacion    AS FechaModificacion,
                ta.nombre                AS Nombre,
                ta.categoria             AS Categoria,
                ta.tipo_dato             AS TipoDato,
                ta.orden                 AS Orden
            FROM public.antecedentes_paciente ap
            INNER JOIN public.tipos_antecedente ta ON ap.tipo_antecedente_id = ta.id
            WHERE ap.id = @Id AND ap.clinica_id = @ClinicaId AND ap.activo = true";

        var result = await connection.QueryAsync<AntecedentePaciente, TipoAntecedente, AntecedentePaciente>(
            sql,
            (ap, ta) =>
            {
                ap.TipoAntecedente = ta;
                return ap;
            },
            splitOn: "Nombre",
            param: new { Id = id, ClinicaId = clinicaId }
        );

        return result.FirstOrDefault();
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. UpsertAsync — Inserta o actualiza un antecedente.
    //    ON CONFLICT usa (expediente_id, sala_id, tipo_antecedente_id)
    //    con WHERE activo = true para evitar duplicados activos.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> UpsertAsync(AntecedentePaciente entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.antecedentes_paciente (
                clinica_id, expediente_id, sala_id, tipo_antecedente_id,
                valor, fecha_actualizacion, actualizado_por,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @ExpedienteId, @SalaId, @TipoAntecedenteId,
                @Valor, @FechaActualizacion, @ActualizadoPor,
                true, NOW()
            )
            ON CONFLICT (expediente_id, sala_id, tipo_antecedente_id) WHERE activo = true
            DO UPDATE SET
                valor = EXCLUDED.valor,
                fecha_actualizacion = NOW(),
                actualizado_por = EXCLUDED.actualizado_por,
                fecha_modificacion = NOW()
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. DeactivateAsync — Desactiva antecedente (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.antecedentes_paciente
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
