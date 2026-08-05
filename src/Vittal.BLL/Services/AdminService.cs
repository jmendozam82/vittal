using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Clinica;
using Vittal.DTO.Usuario;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de administración global del sistema Vittal.
/// Implementa el provisionamiento completo de nuevas clínicas y
/// operaciones de consulta multi-tenant para el Super Admin.
/// </summary>
public class AdminService : IAdminService
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminService> _logger;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IClinicaRepository _clinicaRepo;

    public AdminService(
        DbConnectionFactory dbConnectionFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<AdminService> logger,
        IUsuarioRepository usuarioRepo,
        IClinicaRepository clinicaRepo)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
        _usuarioRepo = usuarioRepo;
        _clinicaRepo = clinicaRepo;
    }

    // ────────────────────────────────────────────────────────────────
    // 1. ProvisionClinicaAsync — Creación completa de clínica
    // ────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<ClinicaProvisionResponseDto>> ProvisionClinicaAsync(
        ClinicaProvisionRequestDto dto, Guid superAdminUsuarioId)
    {
        _logger.LogInformation("=== INICIO PROVISIONAMIENTO: Clínica '{Nombre}' ===", dto.Nombre);

        try
        {
            // ── Validaciones previas ───────────────────────────────
            if (await _clinicaRepo.ExistsByNameAsync(dto.Nombre))
            {
                return ServiceResult<ClinicaProvisionResponseDto>.Failure(
                    $"Ya existe una clínica con el nombre '{dto.Nombre}'.",
                    ServiceErrorType.Conflict);
            }

            // ── Variables para rollback manual ─────────────────────
            Guid? creadoClinicaId = null;
            Guid? creadoPerfilId = null;
            int permisosSeedeados = 0;
            string? authUserId = null;

            // ── PASO 1: Crear clínica ──────────────────────────────
            using (var connection = new NpgsqlConnection(
                _config.GetConnectionString("Supabase")))
            {
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // 1a. Insertar clínica
                    const string sqlClinica = @"
                        INSERT INTO public.clinicas (
                            nombre, direccion, telefono, email, logo_url,
                            tiempo_espera_minutos, bd_externa_1, bd_externa_2,
                            activo, fecha_creacion
                        )
                        VALUES (
                            @Nombre, @Direccion, @Telefono, @Email, @LogoUrl,
                            @TiempoEsperaMinutos, @BdExterna1, @BdExterna2,
                            true, NOW()
                        )
                        RETURNING id;";

                    var clinicaId = await connection.ExecuteScalarAsync<Guid>(
                        sqlClinica, new
                        {
                            dto.Nombre,
                            dto.Direccion,
                            dto.Telefono,
                            dto.Email,
                            dto.LogoUrl,
                            dto.TiempoEsperaMinutos,
                            dto.BdExterna1,
                            dto.BdExterna2
                        }, transaction);

                    _logger.LogInformation("Clínica creada: {ClinicaId}", clinicaId);
                    creadoClinicaId = clinicaId;

                    // 1b. Crear perfil admin
                    const string sqlPerfil = @"
                        INSERT INTO public.perfiles (clinica_id, nombre, descripcion, es_admin, activo, fecha_creacion)
                        VALUES (@ClinicaId, 'Administrador', 'Perfil administrador de la clínica (generado automáticamente)', true, true, NOW())
                        RETURNING id;";

                    var perfilId = await connection.ExecuteScalarAsync<Guid>(
                        sqlPerfil, new { ClinicaId = clinicaId }, transaction);

                    _logger.LogInformation("Perfil admin creado: {PerfilId}", perfilId);
                    creadoPerfilId = perfilId;

                    // 1c. Seedear permisos (todos READ + CREATE + UPDATE)
                    const string sqlPermisos = @"
                        INSERT INTO public.permisos (clinica_id, perfil_id, modulo_id, puede_leer, puede_crear, puede_actualizar, fecha_modificacion, modificado_por)
                        SELECT
                            @ClinicaId, @PerfilId, m.id, true, true, true, NOW(), @ModificadoPor
                        FROM public.modulos_sistema m
                        WHERE m.activo = true
                        ON CONFLICT (clinica_id, perfil_id, modulo_id) DO NOTHING;";

                    permisosSeedeados = await connection.ExecuteAsync(
                        sqlPermisos, new
                        {
                            ClinicaId = clinicaId,
                            PerfilId = perfilId,
                            ModificadoPor = superAdminUsuarioId
                        }, transaction);

                    _logger.LogInformation("Permisos seedeados: {Count}", permisosSeedeados);

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error en transacción inicial de provisionamiento (clínica + perfil + permisos)");
                    return ServiceResult<ClinicaProvisionResponseDto>.Failure(
                        "Error al crear la clínica durante el provisionamiento. Intente de nuevo o contacte al administrador.");
                }
            }

            // ── PASO 2: Crear usuario Supabase Auth ────────────────
            try
            {
                authUserId = await CreateSupabaseAuthUserAsync(dto.AdminEmail, dto.AdminPassword);
                _logger.LogInformation("Usuario creado en Supabase Auth: {AuthUserId}", authUserId);
            }
            catch (Exception authEx)
            {
                _logger.LogError(authEx, "Error al crear usuario en Supabase Auth");
                // Rollback: eliminar clínica y perfil creados
                await RollbackClinicaAsync(creadoClinicaId!.Value);
                return ServiceResult<ClinicaProvisionResponseDto>.Failure(
                    "Error al crear la cuenta de autenticación del administrador. Intente de nuevo o contacte al administrador.");
            }

            // ── PASO 3: Crear usuario local ────────────────────────
            Guid? adminUsuarioId = null;
            try
            {
                var usuarioEntity = new Usuario
                {
                    ClinicaId = creadoClinicaId!.Value,
                    PerfilId = creadoPerfilId!.Value,
                    AuthUserId = Guid.TryParse(authUserId, out var parsedId) ? parsedId : (Guid?)null,
                    Username = dto.AdminUsername,
                    Nombres = dto.AdminNombres,
                    Apellidos = dto.AdminApellidos,
                    Email = dto.AdminEmail,
                    Celular = dto.AdminCelular,
                    EsDoctor = false,
                    Activo = true,
                    CreadoPor = superAdminUsuarioId
                };

                adminUsuarioId = await _usuarioRepo.CreateAsync(usuarioEntity);
                _logger.LogInformation("Usuario admin creado en BD: {UsuarioId}", adminUsuarioId);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Error al crear usuario admin en BD, realizando rollback");

                // Rollback Supabase Auth
                if (!string.IsNullOrEmpty(authUserId))
                {
                    try { await DeleteSupabaseAuthUserAsync(authUserId); }
                    catch (Exception rEx) { _logger.LogCritical(rEx, "FALLO CRÍTICO en rollback de Supabase Auth para AuthUserId={AuthUserId}. Se requiere intervención manual.", authUserId); }
                }

                // Rollback clínica + perfil
                await RollbackClinicaAsync(creadoClinicaId!.Value);

                return ServiceResult<ClinicaProvisionResponseDto>.Failure(
                    "Error al guardar el usuario administrador. Intente de nuevo o contacte al administrador.");
            }

            // ── PASO 4: Seedear configuraciones por defecto ────────
            bool configAlertasOk = false;
            bool dashboardConfigOk = false;

            try
            {
                using (var connection = new NpgsqlConnection(
                    _config.GetConnectionString("Supabase")))
                {
                    await connection.OpenAsync();

                    // 4a. Configuración de alertas
                    // Nota: la columna real de configuracion_alertas es tiempo_espera_maximo_minutos
                    // (no tiempo_espera_minutos, que pertenece a clinicas).
                    const string sqlAlertas = @"
                        INSERT INTO public.configuracion_alertas (
                            clinica_id, tiempo_espera_maximo_minutos, activo,
                            notificacion_sonido, intervalo_revision_segundos, fecha_creacion, creado_por
                        )
                        VALUES (@ClinicaId, @TiempoEspera, true, false, 60, NOW(), @CreadoPor)
                        ON CONFLICT (clinica_id) DO NOTHING;";

                    var alertasRows = await connection.ExecuteAsync(sqlAlertas, new
                    {
                        ClinicaId = creadoClinicaId!.Value,
                        TiempoEspera = dto.TiempoEsperaMinutos,
                        CreadoPor = superAdminUsuarioId
                    });
                    configAlertasOk = alertasRows > 0;
                    _logger.LogInformation("Config alertas seedeada para clínica {ClinicaId}", creadoClinicaId);

                    // 4b. Configuración de dashboard
                    // Nota: dashboard_config usa columnas booleanas mostrar_* (no columna jsonb "config").
                    const string sqlDashboard = @"
                        INSERT INTO public.dashboard_config (
                            clinica_id, mostrar_pacientes_del_dia, mostrar_citas_pendientes,
                            mostrar_pacientes_en_espera, mostrar_tiempo_promedio_espera,
                            mostrar_grafico_citas_por_hora, mostrar_ultimas_alertas, layout,
                            activo, fecha_creacion
                        )
                        VALUES (@ClinicaId, true, true, true, true, true, true, 'default', true, NOW())
                        ON CONFLICT (clinica_id) DO NOTHING;";

                    var dashboardRows = await connection.ExecuteAsync(sqlDashboard, new
                    {
                        ClinicaId = creadoClinicaId!.Value
                    });
                    dashboardConfigOk = dashboardRows > 0;
                    _logger.LogInformation("Dashboard config seedeada para clínica {ClinicaId}", creadoClinicaId);
                }
            }
            catch (Exception cfgEx)
            {
                // Las configuraciones por defecto no son críticas — solo loguear
                _logger.LogWarning(cfgEx, "Error al seedear configuraciones por defecto para clínica {ClinicaId}", creadoClinicaId);
            }

            // ── RESULTADO FINAL ────────────────────────────────────
            var resultDto = new ClinicaProvisionResponseDto
            {
                ClinicaId = creadoClinicaId!.Value,
                ClinicaNombre = dto.Nombre,
                PerfilAdminId = creadoPerfilId!.Value,
                PerfilAdminNombre = "Administrador",
                AdminUsuarioId = adminUsuarioId!.Value,
                AdminAuthUserId = Guid.TryParse(authUserId, out var authParsed) ? authParsed : null,
                AdminNombreCompleto = $"{dto.AdminNombres} {dto.AdminApellidos}",
                AdminEmail = dto.AdminEmail,
                AdminUsername = dto.AdminUsername,
                PermisosSeedeados = permisosSeedeados,
                ConfigAlertasCreada = configAlertasOk,
                DashboardConfigCreada = dashboardConfigOk,
                FechaCreacion = DateTime.UtcNow
            };

            _logger.LogInformation("=== PROVISIONAMIENTO EXITOSO: Clínica '{Nombre}' (ID: {Id}) ===",
                dto.Nombre, creadoClinicaId);

            return ServiceResult<ClinicaProvisionResponseDto>.Success(
                resultDto, "Clínica creada exitosamente con administrador, permisos y configuraciones por defecto.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en provisionamiento de clínica '{Nombre}'", dto.Nombre);
            return ServiceResult<ClinicaProvisionResponseDto>.Failure("Error interno al provisionar la clínica. Intente de nuevo o contacte al administrador.");
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 2. CreateUsuarioAsync — Crear usuario en una clínica específica (Super Admin)
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un usuario en una clínica específica (Super Admin).
    /// A diferencia de UsuarioService.CreateAsync, el clinicaId viene del DTO explícitamente.
    /// Incluye creación en Supabase Auth + BD con rollback automático.
    /// </summary>
    public async Task<ServiceResult<UsuarioResponseDto>> CreateUsuarioAsync(
        AdminCreateUsuarioRequestDto dto, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Super Admin creando usuario '{Username}' en clínica {ClinicaId}",
                dto.Username, dto.ClinicaId);

            // ── Validar unicidad de username ────────────────────────
            if (await _usuarioRepo.ExistsByUsernameAsync(dto.ClinicaId, dto.Username))
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "El nombre de usuario ya está en uso en esta clínica.", ServiceErrorType.Conflict);
            }

            // ── Validar unicidad de email ───────────────────────────
            if (await _usuarioRepo.ExistsByEmailAsync(dto.ClinicaId, dto.Email))
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "El correo electrónico ya está registrado en esta clínica.", ServiceErrorType.Conflict);
            }

            // ── PASO 1: Crear en Supabase Auth ─────────────────────
            string? authUserId = null;
            try
            {
                authUserId = await CreateSupabaseAuthUserAsync(dto.Email, dto.Password ?? "TempPass123!");
                _logger.LogInformation("Usuario creado en Supabase Auth: {AuthUserId}", authUserId);
            }
            catch (Exception authEx)
            {
                _logger.LogError(authEx, "Error al crear usuario en Supabase Auth");
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "Error al crear la cuenta de autenticación. Intente de nuevo o contacte al administrador.");
            }

            // ── PASO 2: Crear en BD ────────────────────────────────
            try
            {
                var entity = new Usuario
                {
                    ClinicaId = dto.ClinicaId,
                    PerfilId = dto.PerfilId,
                    AuthUserId = Guid.TryParse(authUserId, out var parsedId) ? parsedId : null,
                    Username = dto.Username,
                    Nombres = dto.Nombres,
                    Apellidos = dto.Apellidos,
                    Email = dto.Email,
                    Sexo = dto.Sexo,
                    Direccion = dto.Direccion,
                    Celular = dto.Celular,
                    EsDoctor = dto.EsDoctor,
                    CreadoPor = creadoPor,
                    Activo = true
                };

                var newId = await _usuarioRepo.CreateAsync(entity);
                _logger.LogInformation("Usuario creado en BD con ID: {NewId}", newId);

                // Recuperar la entidad creada para retornar el DTO completo
                var created = await _usuarioRepo.GetByIdAsync(newId, dto.ClinicaId);
                if (created == null)
                {
                    return ServiceResult<UsuarioResponseDto>.Failure(
                        "Usuario creado pero no se pudo recuperar la información.");
                }

                return ServiceResult<UsuarioResponseDto>.Success(
                    MapUsuarioToDto(created), "Usuario creado exitosamente en la clínica especificada.");
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Error al crear usuario en BD, realizando rollback en Supabase Auth");

                // Rollback en Supabase Auth
                if (!string.IsNullOrEmpty(authUserId))
                {
                    try
                    {
                        await DeleteSupabaseAuthUserAsync(authUserId);
                        _logger.LogInformation("Rollback exitoso: usuario eliminado de Supabase Auth");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogCritical(rollbackEx, "FALLO CRÍTICO en rollback de Supabase Auth para AuthUserId={AuthUserId}. Se requiere intervención manual.", authUserId);
                    }
                }

                return ServiceResult<UsuarioResponseDto>.Failure(
                    "Error al guardar el usuario. Intente de nuevo o contacte al administrador.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear usuario como Super Admin");
            return ServiceResult<UsuarioResponseDto>.Failure("Error interno al crear el usuario. Intente de nuevo o contacte al administrador.");
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 3. GetUsuariosByClinicaAsync — Usuarios de una clínica (Super Admin)
    // ────────────────────────────────────────────────────────────────
    public async Task<ServiceResult<IEnumerable<UsuarioResponseDto>>> GetUsuariosByClinicaAsync(
        Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Super Admin consultando usuarios de clínica {ClinicaId}", clinicaId);

            var entities = incluirInactivos
                ? await _usuarioRepo.GetAllIncludingInactiveAsync(clinicaId)
                : await _usuarioRepo.GetAllAsync(clinicaId);

            var dtos = new List<UsuarioResponseDto>();
            foreach (var entity in entities)
            {
                dtos.Add(MapUsuarioToDto(entity));
            }

            return ServiceResult<IEnumerable<UsuarioResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar usuarios de clínica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<UsuarioResponseDto>>.Failure("Error al consultar los usuarios de la clínica. Intente de nuevo o contacte al administrador.");
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Rollback: Elimina una clínica recién creada (fallback de provisionamiento)
    // ────────────────────────────────────────────────────────────────
    private async Task RollbackClinicaAsync(Guid clinicaId)
    {
        try
        {
            // Desactivar en lugar de eliminar (consistente con política del sistema)
            await _clinicaRepo.DeactivateAsync(clinicaId);
            _logger.LogInformation("Rollback: clínica {ClinicaId} desactivada", clinicaId);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "FALLO CRÍTICO en rollback de clínica {ClinicaId}. Se requiere intervención manual.", clinicaId);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Supabase Auth Helpers
    // ────────────────────────────────────────────────────────────────

    private async Task<string> CreateSupabaseAuthUserAsync(string email, string password)
    {
        var supabaseUrl = _config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
        var serviceRoleKey = _config["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase:ServiceRoleKey not configured");

        // Intentar Admin API primero
        var client = _httpClientFactory.CreateClient("SupabaseAuth");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");

        var body = new
        {
            email = email,
            password = password,
            email_confirm = true,
            user_metadata = new { }
        };

        var jsonBody = JsonSerializer.Serialize(body);
        var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{supabaseUrl}/auth/v1/admin/users")
        {
            Content = content
        };

        var response = await client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("id", out var idProp))
            {
                var authId = idProp.GetString();
                if (!string.IsNullOrEmpty(authId))
                {
                    _logger.LogInformation("Supabase Admin API: usuario creado {AuthUserId}", authId);
                    return authId;
                }
            }
        }
        else
        {
            _logger.LogWarning("Supabase Admin API falló ({Status}), intentando signup público. Response: {Resp}",
                response.StatusCode, responseContent);
        }

        // Fallback: signup público
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);

        var signupContent = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
        var signupRequest = new HttpRequestMessage(HttpMethod.Post, $"{supabaseUrl}/auth/v1/signup")
        {
            Content = signupContent
        };

        var signupResponse = await client.SendAsync(signupRequest);
        var signupResponseContent = await signupResponse.Content.ReadAsStringAsync();

        if (!signupResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Supabase Auth falló ({signupResponse.StatusCode}): {signupResponseContent}");
        }

        using var signupDoc = JsonDocument.Parse(signupResponseContent);
        var signupRoot = signupDoc.RootElement;

        if (signupRoot.TryGetProperty("user", out var userProp) &&
            userProp.TryGetProperty("id", out var userIdProp))
        {
            var authId = userIdProp.GetString();
            if (!string.IsNullOrEmpty(authId)) return authId;
        }

        if (signupRoot.TryGetProperty("id", out var signupIdProp))
        {
            var authId = signupIdProp.GetString();
            if (!string.IsNullOrEmpty(authId)) return authId;
        }

        throw new InvalidOperationException($"No se pudo obtener el ID del usuario de Supabase. Response: {signupResponseContent}");
    }

    private async Task DeleteSupabaseAuthUserAsync(string authUserId)
    {
        var supabaseUrl = _config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
        var serviceRoleKey = _config["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase:ServiceRoleKey not configured");

        var client = _httpClientFactory.CreateClient("SupabaseAuth");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");

        var response = await client.DeleteAsync($"{supabaseUrl}/auth/v1/admin/users/{authUserId}");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Rollback Supabase Auth falló ({Status}): {Error}", response.StatusCode, errorContent);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Mapping
    // ────────────────────────────────────────────────────────────────

    private static UsuarioResponseDto MapUsuarioToDto(Usuario u)
    {
        return new UsuarioResponseDto
        {
            UsuarioId = u.Id,
            ClinicaId = u.ClinicaId,
            AuthUserId = u.AuthUserId,
            PerfilId = u.PerfilId,
            Username = u.Username,
            Nombres = u.Nombres,
            Apellidos = u.Apellidos,
            Email = u.Email,
            Sexo = u.Sexo,
            Celular = u.Celular,
            Direccion = u.Direccion,
            EsDoctor = u.EsDoctor,
            PerfilNombre = u.PerfilNombre,
            ClinicaNombre = u.ClinicaNombre,
            EsAdmin = u.EsAdmin,
            EsSuperAdmin = u.EsSuperAdmin,
            Activo = u.Activo,
            FechaCreacion = u.FechaCreacion,
            FechaModificacion = u.FechaModificacion
        };
    }
}
