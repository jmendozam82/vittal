using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Services;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Usuario;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UsuarioService> _logger;
    private readonly IConfiguration _config;

    public UsuarioService(
        IUsuarioRepository repo,
        IHttpClientFactory httpClientFactory,
        ILogger<UsuarioService> logger,
        IConfiguration config)
    {
        _repo = repo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task<ServiceResult<UsuarioResponseDto>> GetByAuthUserIdAsync(Guid authUserId)
    {
        try
        {
            _logger.LogInformation("Buscando usuario con auth_user_id: {AuthUserId}", authUserId);
            var usuario = await _repo.GetByAuthUserIdAsync(authUserId);
            _logger.LogInformation("Resultado repo: Usuario encontrado = {Found}", usuario != null);

            if (usuario == null)
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "Usuario no encontrado o inactivo", ServiceErrorType.NotFound);
            }

            var dto = MapUsuarioToDto(usuario);
            return ServiceResult<UsuarioResponseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuario por auth_user_id: {AuthUserId}", authUserId);
            return ServiceResult<UsuarioResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<UsuarioResponseDto>>> GetAllAsync(Guid clinicaId, bool incluirInactivos = false)
    {
        try
        {
            _logger.LogInformation("Obteniendo usuarios de la clinica {ClinicaId} (inactivos: {Incluir})", clinicaId, incluirInactivos);
            var entities = incluirInactivos
                ? await _repo.GetAllIncludingInactiveAsync(clinicaId)
                : await _repo.GetAllAsync(clinicaId);
            var dtos = new List<UsuarioResponseDto>();

            foreach (var entity in entities)
            {
                dtos.Add(MapUsuarioToDto(entity));
            }

            return ServiceResult<IEnumerable<UsuarioResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios de la clinica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<UsuarioResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UsuarioResponseDto>> GetByIdAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Buscando usuario {Id} en clinica {ClinicaId}", id, clinicaId);
            var entity = await _repo.GetByIdAsync(id, clinicaId);

            if (entity == null)
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "Usuario no encontrado", ServiceErrorType.NotFound);
            }

            return ServiceResult<UsuarioResponseDto>.Success(MapUsuarioToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuario {Id}", id);
            return ServiceResult<UsuarioResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UsuarioResponseDto>> CreateAsync(UsuarioRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            _logger.LogInformation("Creando usuario con username: {Username} en clinica {ClinicaId}", dto.Username, clinicaId);

            // Validate uniqueness
            if (await _repo.ExistsByUsernameAsync(clinicaId, dto.Username))
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "El nombre de usuario ya esta en uso.", ServiceErrorType.Conflict);
            }

            if (await _repo.ExistsByEmailAsync(clinicaId, dto.Email))
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "El correo electronico ya esta registrado.", ServiceErrorType.Conflict);
            }

            // Step 1: Create user in Supabase Auth
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
                    $"Error al crear la cuenta de autenticacion: {authEx.Message}");
            }

            // Step 2: Create user in database
            try
            {
                var entity = new Usuario
                {
                    ClinicaId = clinicaId,
                    PerfilId = dto.PerfilId,
                    AuthUserId = Guid.TryParse(authUserId, out var parsedId) ? parsedId : (Guid?)null,
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

                var newId = await _repo.CreateAsync(entity);
                _logger.LogInformation("Usuario creado en BD con ID: {NewId}", newId);

                // Fetch the created entity to return full DTO
                var created = await _repo.GetByIdAsync(newId, clinicaId);
                if (created == null)
                {
                    return ServiceResult<UsuarioResponseDto>.Failure(
                        "Usuario creado pero no se pudo recuperar la informacion.", ServiceErrorType.InternalError);
                }

                return ServiceResult<UsuarioResponseDto>.Success(
                    MapUsuarioToDto(created), "Usuario creado exitosamente.");
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Error al crear usuario en BD, realizando rollback en Supabase Auth");

                // Rollback: delete the Supabase Auth user
                if (!string.IsNullOrEmpty(authUserId))
                {
                    try
                    {
                        await DeleteSupabaseAuthUserAsync(authUserId);
                        _logger.LogInformation("Rollback exitoso: usuario eliminado de Supabase Auth");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Error al hacer rollback en Supabase Auth");
                    }
                }

                return ServiceResult<UsuarioResponseDto>.Failure(
                    $"Error al guardar el usuario: {dbEx.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear usuario");
            return ServiceResult<UsuarioResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UsuarioResponseDto>> UpdateAsync(Guid id, UsuarioRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando usuario {Id} en clinica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "Usuario no encontrado", ServiceErrorType.NotFound);
            }

            // Validate uniqueness (exclude current user)
            if (await _repo.ExistsByUsernameAsync(clinicaId, dto.Username, id))
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "El nombre de usuario ya esta en uso.", ServiceErrorType.Conflict);
            }

            if (await _repo.ExistsByEmailAsync(clinicaId, dto.Email, id))
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "El correo electronico ya esta registrado.", ServiceErrorType.Conflict);
            }

            // Update password in Supabase Auth if provided
            if (!string.IsNullOrWhiteSpace(dto.Password) && existing.AuthUserId.HasValue)
            {
                try
                {
                    await UpdateSupabaseAuthPasswordAsync(existing.AuthUserId.Value.ToString(), dto.Password);
                    _logger.LogInformation("Password actualizado en Supabase Auth para {AuthUserId}", existing.AuthUserId);
                }
                catch (Exception authEx)
                {
                    _logger.LogError(authEx, "Error al actualizar password en Supabase Auth");
                    return ServiceResult<UsuarioResponseDto>.Failure(
                        $"Error al actualizar la contraseña: {authEx.Message}");
                }
            }

            // Update entity
            existing.Username = dto.Username;
            existing.Nombres = dto.Nombres;
            existing.Apellidos = dto.Apellidos;
            existing.Email = dto.Email;
            existing.PerfilId = dto.PerfilId;
            existing.Sexo = dto.Sexo;
            existing.Direccion = dto.Direccion;
            existing.Celular = dto.Celular;
            existing.EsDoctor = dto.EsDoctor;
            existing.ModificadoPor = modificadoPor;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "No se pudo actualizar el usuario.", ServiceErrorType.InternalError);
            }

            // Fetch updated entity
            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "Usuario actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<UsuarioResponseDto>.Success(
                MapUsuarioToDto(refreshed), "Usuario actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario {Id}", id);
            return ServiceResult<UsuarioResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Desactivando usuario {Id} en clinica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Usuario no encontrado", ServiceErrorType.NotFound);
            }

            // Check if doctor has active expedientes
            if (existing.EsDoctor)
            {
                var expedientesCount = await _repo.CountExpedientesAsync(id, clinicaId);
                if (expedientesCount > 0)
                {
                    return ServiceResult<bool>.Failure(
                        $"No se puede desactivar. Tiene {expedientesCount} expediente(s) activo(s).",
                        ServiceErrorType.Conflict);
                }
            }

            // Check if user has future citas
            var citasCount = await _repo.CountCitasAsync(id, clinicaId);
            if (citasCount > 0)
            {
                return ServiceResult<bool>.Failure(
                    $"No se puede desactivar. Tiene {citasCount} cita(s) futura(s).",
                    ServiceErrorType.Conflict);
            }

            // Deactivate in database
            var deactivated = await _repo.DeactivateAsync(id, clinicaId);
            if (!deactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo desactivar el usuario.", ServiceErrorType.InternalError);
            }

            // Ban in Supabase Auth
            if (existing.AuthUserId.HasValue)
            {
                try
                {
                    await BanSupabaseAuthUserAsync(existing.AuthUserId.Value.ToString());
                    _logger.LogInformation("Usuario baneado en Supabase Auth: {AuthUserId}", existing.AuthUserId);
                }
                catch (Exception authEx)
                {
                    _logger.LogError(authEx, "Error al banear usuario en Supabase Auth");
                    // Continue - DB deactivation succeeded
                }
            }

            return ServiceResult<bool>.Success(true, "Usuario desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar usuario {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> ReactivateAsync(Guid id, Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Reactivando usuario {Id} en clinica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<bool>.Failure(
                    "Usuario no encontrado", ServiceErrorType.NotFound);
            }

            if (existing.Activo)
            {
                return ServiceResult<bool>.Failure(
                    "El usuario ya está activo.", ServiceErrorType.Validation);
            }

            // Reactivate in database
            var reactivated = await _repo.ReactivateAsync(id, clinicaId);
            if (!reactivated)
            {
                return ServiceResult<bool>.Failure(
                    "No se pudo reactivar el usuario.", ServiceErrorType.InternalError);
            }

            // Unban in Supabase Auth
            if (existing.AuthUserId.HasValue)
            {
                try
                {
                    await UnbanSupabaseAuthUserAsync(existing.AuthUserId.Value.ToString());
                    _logger.LogInformation("Usuario desbaneado en Supabase Auth: {AuthUserId}", existing.AuthUserId);
                }
                catch (Exception authEx)
                {
                    _logger.LogError(authEx, "Error al desbanear usuario en Supabase Auth");
                    // Continue - DB reactivation succeeded
                }
            }

            return ServiceResult<bool>.Success(true, "Usuario reactivado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar usuario {Id}", id);
            return ServiceResult<bool>.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<ServiceResult<UsuarioResponseDto>> UpdateProfileAsync(
        Guid id, MiPerfilUpdateRequestDto dto, Guid clinicaId, Guid modificadoPor)
    {
        try
        {
            _logger.LogInformation("Actualizando perfil del usuario {Id} en clínica {ClinicaId}", id, clinicaId);

            var existing = await _repo.GetByIdAsync(id, clinicaId);
            if (existing == null)
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "Usuario no encontrado", ServiceErrorType.NotFound);
            }

            // Actualizar solo campos editables por el propio usuario
            existing.Nombres = dto.Nombres;
            existing.Apellidos = dto.Apellidos;
            existing.Email = dto.Email;
            existing.Sexo = dto.Sexo;
            existing.Celular = dto.Celular;
            existing.Direccion = dto.Direccion;
            existing.FotoUrl = dto.FotoUrl;
            existing.ModificadoPor = modificadoPor;
            existing.FechaModificacion = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(existing);
            if (!updated)
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "No se pudo actualizar el perfil.", ServiceErrorType.InternalError);
            }

            var refreshed = await _repo.GetByIdAsync(id, clinicaId);
            if (refreshed == null)
            {
                return ServiceResult<UsuarioResponseDto>.Failure(
                    "Perfil actualizado pero no se pudo recuperar.", ServiceErrorType.InternalError);
            }

            return ServiceResult<UsuarioResponseDto>.Success(
                MapUsuarioToDto(refreshed), "Perfil actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar perfil del usuario {Id}", id);
            return ServiceResult<UsuarioResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<UsuarioResponseDto>>> GetDoctoresAsync(Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Obteniendo doctores de la clinica {ClinicaId}", clinicaId);
            var entities = await _repo.GetDoctoresAsync(clinicaId);
            var dtos = new List<UsuarioResponseDto>();

            foreach (var entity in entities)
            {
                dtos.Add(MapUsuarioToDto(entity));
            }

            return ServiceResult<IEnumerable<UsuarioResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener doctores de la clinica {ClinicaId}", clinicaId);
            return ServiceResult<IEnumerable<UsuarioResponseDto>>.Failure($"Error interno: {ex.Message}");
        }
    }

    // ==================== Supabase Auth Helpers ====================

    private async Task<string> CreateSupabaseAuthUserAsync(string email, string password)
    {
        var supabaseUrl = _config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
        var serviceRoleKey = _config["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase:ServiceRoleKey not configured");

        // Intentar primero con Admin API (sin rate limit de emails)
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
            _logger.LogDebug("Supabase Admin API create user response: {Response}", responseContent);

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("id", out var idProp))
            {
                var authId = idProp.GetString();
                if (!string.IsNullOrEmpty(authId))
                {
                    _logger.LogInformation("Supabase Admin API create user successful, user ID: {AuthUserId}", authId);
                    return authId;
                }
            }
        }
        else
        {
            _logger.LogWarning("Supabase Admin API failed ({StatusCode}), trying public signup. Response: {Response}", response.StatusCode, responseContent);
        }

        // Fallback: usar signup publico con service role para evitar rate limit
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
            throw new InvalidOperationException($"Supabase Auth create user failed ({signupResponse.StatusCode}): {signupResponseContent}");
        }

        _logger.LogDebug("Supabase public signup response: {Response}", signupResponseContent);

        using var signupDoc = JsonDocument.Parse(signupResponseContent);
        var signupRoot = signupDoc.RootElement;

        // Public signup returns: { "access_token": "...", "user": { "id": "...", ... } }
        // Try user.id first (most common for public signup)
        if (signupRoot.TryGetProperty("user", out var userProp) && userProp.TryGetProperty("id", out var userIdProp))
        {
            var authId = userIdProp.GetString();
            if (!string.IsNullOrEmpty(authId))
            {
                _logger.LogInformation("Supabase public signup successful, user ID: {AuthUserId}", authId);
                return authId;
            }
        }

        // Fallback: check root level id (for some API versions)
        if (signupRoot.TryGetProperty("id", out var signupIdProp))
        {
            var authId = signupIdProp.GetString();
            if (!string.IsNullOrEmpty(authId))
            {
                _logger.LogInformation("Supabase public signup successful (root id), user ID: {AuthUserId}", authId);
                return authId;
            }
        }

        throw new InvalidOperationException($"Could not parse auth user ID. Response: {signupResponseContent}");
    }

    private async Task UpdateSupabaseAuthPasswordAsync(string authUserId, string newPassword)
    {
        var supabaseUrl = _config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
        var serviceRoleKey = _config["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase:ServiceRoleKey not configured");

        var client = _httpClientFactory.CreateClient("SupabaseAuth");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");

        var body = new { password = newPassword };
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(new HttpMethod("PUT"), $"{supabaseUrl}/auth/v1/admin/users/{authUserId}")
        {
            Content = content
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Supabase Auth password update failed ({response.StatusCode}): {errorContent}");
        }
    }

    private async Task BanSupabaseAuthUserAsync(string authUserId)
    {
        var supabaseUrl = _config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
        var serviceRoleKey = _config["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase:ServiceRoleKey not configured");

        var client = _httpClientFactory.CreateClient("SupabaseAuth");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");

        var body = new { ban_until = "2099-12-31T23:59:59Z" };
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(new HttpMethod("PUT"), $"{supabaseUrl}/auth/v1/admin/users/{authUserId}")
        {
            Content = content
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Supabase Auth ban failed ({StatusCode}): {Error}", response.StatusCode, errorContent);
        }
    }

    private async Task UnbanSupabaseAuthUserAsync(string authUserId)
    {
        var supabaseUrl = _config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
        var serviceRoleKey = _config["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase:ServiceRoleKey not configured");

        var client = _httpClientFactory.CreateClient("SupabaseAuth");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");

        // ban_until = null quita el ban inmediatamente
        var body = new { ban_until = (string?)null };
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(new HttpMethod("PUT"), $"{supabaseUrl}/auth/v1/admin/users/{authUserId}")
        {
            Content = content
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Supabase Auth unban failed ({StatusCode}): {Error}", response.StatusCode, errorContent);
        }
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
            _logger.LogWarning("Supabase Auth rollback delete failed ({StatusCode}): {Error}", response.StatusCode, errorContent);
        }
    }

    // ==================== Mapping ====================

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
            FotoUrl = u.FotoUrl,
            EsDoctor = u.EsDoctor,
            PerfilNombre = u.PerfilNombre,
            EsAdmin = u.EsAdmin,
            EsSuperAdmin = u.EsSuperAdmin,
            Activo = u.Activo,
            FechaCreacion = u.FechaCreacion,
            FechaModificacion = u.FechaModificacion
        };
    }
}
