# DAL — Implemented Repositories (Core)

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar:** Como referencia para repositorios implementados.
> **Prerequisito:** skills/dal/SKILL.md, skills/dal/repository-templates.md

---

## PacienteRepository (HU07) — Métodos Especializados

```csharp
public class PacienteRepository : IPacienteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PacienteRepository> _logger;

    public PacienteRepository(IDbConnectionFactory connectionFactory,
        ILogger<PacienteRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    // GetByDoctorAsync — pacientes de un doctor específico
    public async Task<IEnumerable<Paciente>> GetByDoctorAsync(Guid doctorId, Guid clinicaId)
    {
        const string sql = @"
            SELECT id AS Id, clinica_id AS ClinicaId, doctor_id AS DoctorId,
                   primer_nombre AS PrimerNombre, primer_apellido AS PrimerApellido,
                   email AS Email, celular AS Celular, sexo AS Sexo,
                   foto_url AS FotoUrl, activo AS Activo
            FROM pacientes
            WHERE doctor_id = @DoctorId AND clinica_id = @ClinicaId AND activo = true
            ORDER BY primer_apellido, primer_nombre ASC";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        return await connection.QueryAsync<Paciente>(sql,
            new { DoctorId = doctorId, ClinicaId = clinicaId });
    }

    // SearchAsync — búsqueda por nombre, apellido o email
    public async Task<IEnumerable<Paciente>> SearchAsync(
        string termino, Guid clinicaId, int limit = 20)
    {
        const string sql = @"
            SELECT id AS Id, clinica_id AS ClinicaId, doctor_id AS DoctorId,
                   primer_nombre AS PrimerNombre, primer_apellido AS PrimerApellido,
                   email AS Email, celular AS Celular, foto_url AS FotoUrl
            FROM pacientes
            WHERE clinica_id = @ClinicaId AND activo = true
              AND (
                  LOWER(primer_nombre)   LIKE LOWER(@Termino) OR
                  LOWER(primer_apellido) LIKE LOWER(@Termino) OR
                  LOWER(email)           LIKE LOWER(@Termino) OR
                  LOWER(CONCAT(primer_nombre, ' ', primer_apellido)) LIKE LOWER(@Termino)
              )
            ORDER BY primer_apellido, primer_nombre LIMIT @Limit";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        return await connection.QueryAsync<Paciente>(sql, new
        { ClinicaId = clinicaId, Termino = $"%{termino}%", Limit = limit });
    }

    // ExistsAsync con lista blanca de campos
    public async Task<bool> ExistsAsync(
        Guid clinicaId, string campo, string valor, Guid? excludeId = null)
    {
        var camposPermitidos = new HashSet<string> { "email", "celular" };
        if (!camposPermitidos.Contains(campo.ToLower()))
            throw new ArgumentException($"Campo '{campo}' no permitido.");

        var sql = $@"
            SELECT COUNT(1) FROM pacientes
            WHERE clinica_id = @ClinicaId AND {campo} = @Valor AND activo = true
            AND (@ExcludeId IS NULL OR id != @ExcludeId)";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        var count = await connection.ExecuteScalarAsync<int>(sql,
            new { ClinicaId = clinicaId, Valor = valor, ExcludeId = excludeId });
        return count > 0;
    }
}
```

---

## CitaRepository (HU21 + HU18) — Métodos Especializados

```csharp
public class CitaRepository : ICitaRepository
{
    // GetColaEsperaAsync — citas del día para Cola de Espera
    public async Task<IEnumerable<Cita>> GetColaEsperaAsync(
        Guid clinicaId, Guid? doctorId = null)
    {
        var sql = @"
            SELECT
                c.id AS Id, c.clinica_id AS ClinicaId, c.paciente_id AS PacienteId,
                c.doctor_id AS DoctorId, c.sala_id AS SalaId, c.fecha_cita AS FechaCita,
                c.hora_cita AS HoraCita, c.hora_llegada AS HoraLlegada,
                c.estado AS Estado,
                p.primer_nombre AS PacientePrimerNombre,
                p.primer_apellido AS PacientePrimerApellido,
                p.foto_url AS PacienteFotoUrl,
                u.nombres AS DoctorNombres, u.apellidos AS DoctorApellidos
            FROM citas c
            INNER JOIN pacientes p ON p.id = c.paciente_id
            INNER JOIN usuarios u ON u.id = c.doctor_id
            WHERE c.clinica_id = @ClinicaId
              AND c.fecha_cita = CURRENT_DATE
              AND c.estado IN ('agendada', 'en_espera')
              AND c.activo = true
              AND (@DoctorId IS NULL OR c.doctor_id = @DoctorId)
            ORDER BY c.hora_cita ASC";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        return await connection.QueryAsync<Cita>(sql,
            new { ClinicaId = clinicaId, DoctorId = doctorId });
    }

    // CambiarEstadoAsync — para Cola de Espera y Agenda
    public async Task<bool> CambiarEstadoAsync(
        Guid citaId, Guid clinicaId, string nuevoEstado, TimeOnly? horaLlegada = null)
    {
        const string sql = @"
            UPDATE citas SET
                estado = @NuevoEstado,
                hora_llegada = COALESCE(@HoraLlegada, hora_llegada),
                fecha_modificacion = NOW()
            WHERE id = @CitaId AND clinica_id = @ClinicaId AND activo = true
            RETURNING id";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        var updatedId = await connection.ExecuteScalarAsync<Guid?>(sql, new
        { CitaId = citaId, ClinicaId = clinicaId, NuevoEstado = nuevoEstado, HoraLlegada = horaLlegada });
        return updatedId.HasValue;
    }

    // DeactivateAsync para citas → también cambia estado a 'cancelada'
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE citas SET
                activo = false, estado = 'cancelada', fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId AND activo = true";

        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
        var rows = await connection.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rows > 0;
    }
}
```

---

## PermisoRepository — Verificación de Permisos

```csharp
public async Task<PermisoUsuario?> GetPermisoPorUsuarioYModuloAsync(
    Guid usuarioId, Guid clinicaId, string moduloClave)
{
    const string sql = @"
        SELECT
            p.puede_leer AS PuedeLeer,
            p.puede_crear AS PuedeCrear,
            p.puede_actualizar AS PuedeActualizar,
            pf.es_admin AS EsAdmin
        FROM permisos p
        INNER JOIN usuarios u ON u.perfil_id = p.perfil_id
        INNER JOIN perfiles pf ON pf.id = p.perfil_id
        INNER JOIN modulos_sistema m ON m.id = p.modulo_id
        WHERE u.id = @UsuarioId AND p.clinica_id = @ClinicaId
          AND m.clave = @ModuloClave AND u.activo = true AND pf.activo = true";

    await using var connection = await _connectionFactory.CreateConnectionAsync();
    await _connectionFactory.SetTenantContextAsync(connection, clinicaId);
    return await connection.QueryFirstOrDefaultAsync<PermisoUsuario>(sql, new
    { UsuarioId = usuarioId, ClinicaId = clinicaId, ModuloClave = moduloClave });
}
```

---

## Checklist de Calidad — Repositories Implementados

- [ ] Métodos especializados documentados con XML summary
- [ ] JOINs en queries de Cola de Espera para datos de paciente y doctor
- [ ] EXISTSAsync usa lista blanca de campos permitidos
- [ ] DeactivateAsync de citas también actualiza estado a 'cancelada'
- [ ] SearchAsync usa `LIKE LOWER()` con wildcards
- [ ] Transacciones usadas en operaciones multi-tabla (ExpedienteRepository)
- [ ] No hay lógica de negocio en ningún método

---

*skills/dal/repositories-core.md — Vittal v1.0.0*
