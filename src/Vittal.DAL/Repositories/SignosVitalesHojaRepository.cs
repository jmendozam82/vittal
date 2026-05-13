using Dapper;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para la tabla public.signos_vitales_hoja.
/// Implementa ISignosVitalesHojaRepository con Dapper y PostgreSQL.
/// Historia de Usuario: HU-E06 — Signos Vitales por Consulta
/// </summary>
public class SignosVitalesHojaRepository : ISignosVitalesHojaRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public SignosVitalesHojaRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    // ────────────────────────────────────────────────────────────────────
    // 1. GetAllAsync — Obtiene los signos vitales activos de una hoja de
    //                  cita, con datos del tipo de signo vital.
    // ────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<SignosVitalesHoja>> GetAllAsync(Guid clinicaId, Guid hojaCitaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            SELECT 
                svh.id,
                svh.clinica_id,
                svh.hoja_cita_id,
                svh.sala_id,
                svh.tipo_signo_vital_id,
                svh.valor,
                svh.unidad,
                svh.fuera_de_rango,
                svh.fecha_hora,
                svh.registrado_por,
                svh.activo,
                svh.fecha_creacion,
                svh.fecha_modificacion,
                tsv.nombre,
                tsv.unidad,
                tsv.valor_min,
                tsv.valor_max,
                tsv.orden,
                tsv.es_obligatorio
            FROM public.signos_vitales_hoja svh
            INNER JOIN public.tipos_signo_vital tsv ON svh.tipo_signo_vital_id = tsv.id
            WHERE svh.clinica_id = @ClinicaId
              AND svh.hoja_cita_id = @HojaCitaId
              AND svh.activo = true
            ORDER BY svh.fecha_hora ASC";

        var result = await connection.QueryAsync<SignosVitalesHoja, TipoSignoVital, SignosVitalesHoja>(
            sql,
            (svh, tsv) =>
            {
                svh.TipoSignoVital = tsv;
                return svh;
            },
            splitOn: "nombre",
            param: new { ClinicaId = clinicaId, HojaCitaId = hojaCitaId }
        );

        return result;
    }

    // ────────────────────────────────────────────────────────────────────
    // 2. GetByIdAsync — Obtiene un signo vital por ID
    // ────────────────────────────────────────────────────────────────────
    public async Task<SignosVitalesHoja?> GetByIdAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            SELECT 
                svh.id,
                svh.clinica_id,
                svh.hoja_cita_id,
                svh.sala_id,
                svh.tipo_signo_vital_id,
                svh.valor,
                svh.unidad,
                svh.fuera_de_rango,
                svh.fecha_hora,
                svh.registrado_por,
                svh.activo,
                svh.fecha_creacion,
                svh.fecha_modificacion,
                tsv.nombre,
                tsv.unidad,
                tsv.valor_min,
                tsv.valor_max,
                tsv.orden,
                tsv.es_obligatorio
            FROM public.signos_vitales_hoja svh
            INNER JOIN public.tipos_signo_vital tsv ON svh.tipo_signo_vital_id = tsv.id
            WHERE svh.id = @Id AND svh.clinica_id = @ClinicaId AND svh.activo = true";

        var result = await connection.QueryAsync<SignosVitalesHoja, TipoSignoVital, SignosVitalesHoja>(
            sql,
            (svh, tsv) =>
            {
                svh.TipoSignoVital = tsv;
                return svh;
            },
            splitOn: "nombre",
            param: new { Id = id, ClinicaId = clinicaId }
        );

        return result.FirstOrDefault();
    }

    // ────────────────────────────────────────────────────────────────────
    // 3. CreateAsync — Inserta un nuevo registro de signo vital.
    //    Retorna el ID autogenerado.
    // ────────────────────────────────────────────────────────────────────
    public async Task<Guid> CreateAsync(SignosVitalesHoja entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO public.signos_vitales_hoja (
                clinica_id, hoja_cita_id, sala_id, tipo_signo_vital_id,
                valor, unidad, fuera_de_rango,
                fecha_hora, registrado_por,
                activo, fecha_creacion
            )
            VALUES (
                @ClinicaId, @HojaCitaId, @SalaId, @TipoSignoVitalId,
                @Valor, @Unidad, @FueraDeRango,
                @FechaHora, @RegistradoPor,
                true, NOW()
            )
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    // ────────────────────────────────────────────────────────────────────
    // 4. UpdateAsync — Actualiza un registro de signo vital existente.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> UpdateAsync(SignosVitalesHoja entity)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.signos_vitales_hoja
            SET valor            = @Valor,
                unidad           = @Unidad,
                fuera_de_rango   = @FueraDeRango,
                fecha_hora       = @FechaHora,
                registrado_por   = @RegistradoPor,
                fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    // ────────────────────────────────────────────────────────────────────
    // 5. DeactivateAsync — Desactiva registro (activo = false). Nunca DELETE.
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> DeactivateAsync(Guid clinicaId, Guid id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = @"
            UPDATE public.signos_vitales_hoja
            SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rowsAffected > 0;
    }
}
