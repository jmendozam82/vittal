# skill-controller.md — Skill: API REST Controllers

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar este skill:** Antes de implementar cualquier Controller en
> Vittal.API, middleware de autenticación, atributo de permiso o extensión
> del ClaimsPrincipal en el proyecto Vittal.
> **Prerequisito:** Haber leído CLAUDE.md, skill-dal.md y skill-bll.md.
> El Service (BLL) debe existir antes de implementar su Controller.

---

## 1. Principios Fundamentales del API Controller

```
1. El Controller NUNCA contiene lógica de negocio — solo orquesta: recibe,
   delega al Service y transforma ServiceResult en IActionResult
2. El Controller NUNCA accede directamente al DAL o a la BD
3. Todo endpoint está protegido con [Authorize] — no existen endpoints públicos
   excepto Login (/api/auth/login)
4. El clinicaId SIEMPRE se extrae del JWT via User.GetClinicaId() — nunca
   del body del request ni de query params
5. Toda respuesta usa ApiResponse<T> como wrapper — nunca retornar el DTO crudo
6. Cada endpoint verifica el permiso correspondiente (READ, CREATE, UPDATE)
   antes de ejecutar — los admins bypasean esta verificación
7. Los códigos HTTP deben ser semánticamente correctos:
   200 OK / 201 Created / 400 Bad Request / 401 Unauthorized /
   403 Forbidden / 404 Not Found / 409 Conflict / 422 Unprocessable / 500 Error
8. Swagger debe documentar cada endpoint con ProducesResponseType
9. Los métodos del Controller son siempre async Task<IActionResult>
10. El Controller maneja la traducción ServiceErrorType → código HTTP
    usando el helper ToActionResult()
```

---

## 2. Estructura del Proyecto Vittal.API

```
src/Vittal.API/
├── Attributes/
│   └── RequirePermissionAttribute.cs   ← Atributo personalizado de permisos
├── Controllers/
│   ├── AuthController.cs               ← Login / Logout / Refresh Token
│   ├── ClinicasController.cs
│   ├── PerfilesController.cs
│   ├── UsuariosController.cs
│   ├── PermisosController.cs
│   ├── SalasController.cs
│   ├── PacientesController.cs
│   ├── MedicamentosController.cs
│   ├── TiposCirugiaController.cs
│   ├── CirugiasController.cs
│   ├── TiposDiagnosticoController.cs
│   ├── DiagnosticosController.cs
│   ├── TratamientosController.cs
│   ├── RecomendacionesController.cs
│   ├── ExamenesController.cs
│   ├── CitasController.cs
│   ├── ExpedientesController.cs
│   ├── ColaEsperaController.cs
│   ├── LineaTiempoController.cs
│   ├── AlertasController.cs
│   ├── ReportesController.cs
│   └── DashboardController.cs
├── Extensions/
│   ├── ClaimsPrincipalExtensions.cs    ← User.GetClinicaId(), User.GetUsuarioId(), etc.
│   ├── ServiceResultExtensions.cs      ← ServiceResult<T>.ToActionResult()
│   └── SwaggerExtensions.cs            ← Configuración de Swagger con JWT
├── Middleware/
│   ├── TenantMiddleware.cs             ← Inyecta clinicaId en sesión PostgreSQL
│   ├── ExceptionMiddleware.cs          ← Manejo global de excepciones no capturadas
│   └── RequestLoggingMiddleware.cs     ← Log de cada request con clinicaId y userId
├── Models/
│   └── ApiResponse.cs                  ← Wrapper de respuesta estándar
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

---

## 3. ApiResponse — Wrapper de Respuesta Estándar

```csharp
// src/Vittal.API/Models/ApiResponse.cs
namespace Vittal.API.Models;

/// <summary>
/// Wrapper estándar para todas las respuestas de la API Vittal.
/// Garantiza consistencia en el contrato de la API (BaaS).
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "")
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Created(T data, string message = "Registro creado exitosamente.")
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors?.ToList() ?? new() };
}
```

---

## 4. Extensiones del ClaimsPrincipal

```csharp
// src/Vittal.API/Extensions/ClaimsPrincipalExtensions.cs
namespace Vittal.API.Extensions;

/// <summary>
/// Extensiones para extraer datos del JWT de Supabase Auth.
/// Todos los Controllers usan estos métodos — nunca acceder a Claims directamente.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    // Claim con el clinicaId inyectado por el TenantMiddleware desde la BD
    private const string ClinicaIdClaim   = "app_clinica_id";
    private const string UsuarioIdClaim   = "app_usuario_id";
    private const string PerfilIdClaim    = "app_perfil_id";
    private const string EsAdminClaim     = "app_es_admin";

    /// <summary>
    /// Retorna el clinicaId del tenant del usuario autenticado.
    /// Lanza InvalidOperationException si el claim no existe (no debería ocurrir
    /// si TenantMiddleware está configurado).
    /// </summary>
    public static Guid GetClinicaId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClinicaIdClaim)
            ?? throw new InvalidOperationException(
                "Claim 'app_clinica_id' no encontrado en el JWT. Verificar TenantMiddleware.");
        return Guid.Parse(claim);
    }

    public static Guid GetUsuarioId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(UsuarioIdClaim)
            ?? throw new InvalidOperationException("Claim 'app_usuario_id' no encontrado.");
        return Guid.Parse(claim);
    }

    public static Guid GetPerfilId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(PerfilIdClaim)
            ?? throw new InvalidOperationException("Claim 'app_perfil_id' no encontrado.");
        return Guid.Parse(claim);
    }

    public static bool EsAdmin(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(EsAdminClaim);
        return bool.TryParse(claim, out var esAdmin) && esAdmin;
    }
}
```

---

## 5. Extensión ToActionResult — Traducción ServiceResult → HTTP

```csharp
// src/Vittal.API/Extensions/ServiceResultExtensions.cs
namespace Vittal.API.Extensions;

/// <summary>
/// Convierte ServiceResult del BLL al IActionResult HTTP correspondiente.
/// Centraliza el mapeo ServiceErrorType → código HTTP en un solo lugar.
/// </summary>
public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this ServiceResult<T> result, ControllerBase controller)
    {
        if (result.Success)
        {
            var response = ApiResponse<T>.Ok(result.Data!, result.Message);
            return controller.Ok(response);
        }

        var errorResponse = ApiResponse<T>.Fail(result.Message, result.Errors);

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound       => controller.NotFound(errorResponse),
            ServiceErrorType.ValidationError => controller.BadRequest(errorResponse),
            ServiceErrorType.Duplicate      => controller.Conflict(errorResponse),
            ServiceErrorType.Unauthorized   => controller.StatusCode(403, errorResponse),
            ServiceErrorType.BusinessError  => controller.UnprocessableEntity(errorResponse),
            ServiceErrorType.ServerError    => controller.StatusCode(500, errorResponse),
            _ => controller.StatusCode(500, errorResponse)
        };
    }

    public static IActionResult ToCreatedResult<T>(
        this ServiceResult<T> result, ControllerBase controller,
        string actionName, object routeValues)
    {
        if (result.Success)
        {
            var response = ApiResponse<T>.Created(result.Data!, result.Message);
            return controller.CreatedAtAction(actionName, routeValues, response);
        }
        return result.ToActionResult(controller);
    }
}
```

---

## 6. Atributo de Permiso Personalizado

```csharp
// src/Vittal.API/Attributes/RequirePermissionAttribute.cs
namespace Vittal.API.Attributes;

/// <summary>
/// Atributo que verifica que el usuario tiene el permiso requerido para el módulo.
/// Los usuarios con es_admin = true bypasean esta verificación.
/// Uso: [RequirePermission("pacientes", PermissionType.Create)]
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RequirePermissionAttribute : Attribute
{
    public string ModuloClave { get; }
    public PermissionType Tipo { get; }

    public RequirePermissionAttribute(string moduloClave, PermissionType tipo)
    {
        ModuloClave = moduloClave;
        Tipo = tipo;
    }
}

public enum PermissionType
{
    Read,
    Create,
    Update
    // No existe Delete — el sistema no permite eliminación
}

// src/Vittal.API/Filters/PermissionFilter.cs
public class PermissionFilter : IAsyncActionFilter
{
    private readonly IPermisoService _permisoService;

    public PermissionFilter(IPermisoService permisoService)
    {
        _permisoService = permisoService;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var attribute = context.ActionDescriptor
            .EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .FirstOrDefault();

        // Sin atributo = no se verifica permiso (solo autenticación JWT)
        if (attribute is null)
        {
            await next();
            return;
        }

        var user = context.HttpContext.User;

        // Admins tienen acceso total
        if (user.EsAdmin())
        {
            await next();
            return;
        }

        var usuarioId = user.GetUsuarioId();
        var clinicaId = user.GetClinicaId();

        var permiso = await _permisoService
            .GetPermisoPorUsuarioYModuloAsync(usuarioId, clinicaId, attribute.ModuloClave);

        if (permiso is null || !TienePermiso(permiso, attribute.Tipo))
        {
            context.Result = new ObjectResult(
                ApiResponse<object>.Fail(
                    "No tiene permisos para realizar esta operación."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }

    private static bool TienePermiso(PermisoUsuarioDto permiso, PermissionType tipo)
        => tipo switch
        {
            PermissionType.Read   => permiso.PuedeLeer,
            PermissionType.Create => permiso.PuedeCrear,
            PermissionType.Update => permiso.PuedeActualizar,
            _ => false
        };
}
```

---

## 7. Middleware de Tenant

```csharp
// src/Vittal.API/Middleware/TenantMiddleware.cs
namespace Vittal.API.Middleware;

/// <summary>
/// Middleware que enriquece el JWT de Supabase con datos del tenant
/// (clinicaId, usuarioId, perfilId, esAdmin) consultando la tabla usuarios.
/// Se ejecuta DESPUÉS de la autenticación JWT y ANTES de los Controllers.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next  = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUsuarioRepository usuarioRepo)
    {
        // Solo procesar requests autenticados
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // El auth_user_id de Supabase viene en el claim 'sub'
        var authUserIdStr = context.User.FindFirstValue("sub");
        if (!Guid.TryParse(authUserIdStr, out var authUserId))
        {
            await _next(context);
            return;
        }

        try
        {
            // Obtener datos del usuario desde la BD usando el auth_user_id de Supabase
            var usuario = await usuarioRepo.GetByAuthUserIdAsync(authUserId);
            if (usuario is not null)
            {
                // Enriquecer el ClaimsPrincipal con datos del tenant
                var claims = new List<Claim>
                {
                    new("app_clinica_id", usuario.ClinicaId.ToString()),
                    new("app_usuario_id", usuario.Id.ToString()),
                    new("app_perfil_id",  usuario.PerfilId.ToString()),
                    new("app_es_admin",   usuario.EsAdmin.ToString().ToLower())
                };

                var identity = new ClaimsIdentity(claims);
                context.User.AddIdentity(identity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TenantMiddleware para auth_user_id {AuthUserId}",
                authUserId);
        }

        await _next(context);
    }
}

// Extensión para registrar el middleware limpiamente en Program.cs
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app)
        => app.UseMiddleware<TenantMiddleware>();
}
```

---

## 8. Plantilla Maestra de Controller

```csharp
// src/Vittal.API/Controllers/[Entidad]sController.cs
namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de [Entidad]s.
/// Historia de Usuario: HU[XX] — [Nombre de la HU]
/// Todos los endpoints requieren autenticación JWT de Supabase.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("[Entidad]s")]
public class [Entidad]sController : ControllerBase
{
    private readonly I[Entidad]Service _service;
    private readonly ILogger<[Entidad]sController> _logger;

    public [Entidad]sController(
        I[Entidad]Service service,
        ILogger<[Entidad]sController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ── GET /api/[entidad]s ──────────────────────────────────────────────
    /// <summary>Obtiene todos los registros activos de la clínica.</summary>
    [HttpGet]
    [RequirePermission("[modulo_clave]", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<[Entidad]ResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();
        var result    = await _service.GetAllAsync(clinicaId);
        return result.ToActionResult(this);
    }

    // ── GET /api/[entidad]s/{id} ─────────────────────────────────────────
    /// <summary>Obtiene un registro por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("[modulo_clave]", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<[Entidad]ResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result    = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    // ── POST /api/[entidad]s ─────────────────────────────────────────────
    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("[modulo_clave]", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<[Entidad]ResponseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> Create([FromBody] [Entidad]RequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result    = await _service.CreateAsync(dto, clinicaId);
        return result.ToCreatedResult(this, nameof(GetById), new { id = result.Data?.Id });
    }

    // ── PUT /api/[entidad]s/{id} ─────────────────────────────────────────
    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("[modulo_clave]", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<[Entidad]ResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] [Entidad]RequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result    = await _service.UpdateAsync(id, dto, clinicaId);
        return result.ToActionResult(this);
    }

    // ── PATCH /api/[entidad]s/{id}/desactivar ────────────────────────────
    /// <summary>
    /// Desactiva un registro (activo = false).
    /// NUNCA elimina — los registros son permanentes en Vittal.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("[modulo_clave]", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result    = await _service.DeactivateAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    // ── NOTA: No existe DELETE ───────────────────────────────────────────
    // El sistema Vittal nunca elimina registros. Usar PATCH /desactivar.
}
```

---

## 9. Controllers Implementados — Módulos Core

### 9.1 AuthController (HU02 — Login)

```csharp
// src/Vittal.API/Controllers/AuthController.cs
namespace Vittal.API.Controllers;

/// <summary>
/// Autenticación via Supabase Auth.
/// Único controlador sin [Authorize] en sus endpoints públicos.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags("Autenticación")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUsuarioService usuarioService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _usuarioService = usuarioService;
        _configuration  = configuration;
        _logger         = logger;
    }

    /// <summary>
    /// Inicia sesión con usuario y contraseña.
    /// Delega la autenticación a Supabase Auth y retorna el JWT enriquecido
    /// con datos del tenant (clinicaId, perfilId, esAdmin).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Usuario) || string.IsNullOrWhiteSpace(dto.Contrasena))
            return BadRequest(ApiResponse<object>.Fail(
                "El usuario y la contraseña son obligatorios."));

        var supabaseUrl    = _configuration["Supabase:Url"]!;
        var supabaseAnonKey = _configuration["Supabase:AnonKey"]!;

        try
        {
            // 1. Autenticar contra Supabase Auth con el email del usuario
            var supabaseRequest = new
            {
                email    = dto.Usuario,   // En Vittal el usuario es el email
                password = dto.Contrasena
            };

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);

            var response = await httpClient.PostAsJsonAsync(
                $"{supabaseUrl}/auth/v1/token?grant_type=password",
                supabaseRequest);

            if (!response.IsSuccessStatusCode)
                return Unauthorized(ApiResponse<object>.Fail(
                    "Usuario o contraseña incorrectos."));

            var authData = await response.Content
                .ReadFromJsonAsync<SupabaseAuthResponse>();

            // 2. Obtener datos del tenant desde la BD
            var usuarioResult = await _usuarioService
                .GetByAuthUserIdAsync(Guid.Parse(authData!.User.Id));

            if (!usuarioResult.Success || usuarioResult.Data is null)
                return Unauthorized(ApiResponse<object>.Fail(
                    "El usuario no está registrado en el sistema."));

            var usuario = usuarioResult.Data;

            // 3. Retornar JWT + datos del tenant al cliente
            var loginResponse = new LoginResponseDto
            {
                AccessToken  = authData.AccessToken,
                RefreshToken = authData.RefreshToken,
                ExpiresIn    = authData.ExpiresIn,
                UsuarioId    = usuario.Id,
                ClinicaId    = usuario.ClinicaId,
                NombreCompleto = usuario.NombreCompleto,
                EsAdmin      = usuario.EsAdmin,
                PerfilNombre = usuario.PerfilNombre
            };

            return Ok(ApiResponse<LoginResponseDto>.Ok(loginResponse,
                $"Bienvenido, {usuario.NombreCompleto}."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en login para usuario {Usuario}", dto.Usuario);
            return StatusCode(500, ApiResponse<object>.Fail(
                "Error inesperado al iniciar sesión."));
        }
    }

    /// <summary>Renueva el access token usando el refresh token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RefreshResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
    {
        var supabaseUrl    = _configuration["Supabase:Url"]!;
        var supabaseAnonKey = _configuration["Supabase:AnonKey"]!;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);

            var response = await httpClient.PostAsJsonAsync(
                $"{supabaseUrl}/auth/v1/token?grant_type=refresh_token",
                new { refresh_token = dto.RefreshToken });

            if (!response.IsSuccessStatusCode)
                return Unauthorized(ApiResponse<object>.Fail("Token de renovación inválido."));

            var authData = await response.Content
                .ReadFromJsonAsync<SupabaseAuthResponse>();

            return Ok(ApiResponse<RefreshResponseDto>.Ok(new RefreshResponseDto
            {
                AccessToken  = authData!.AccessToken,
                RefreshToken = authData.RefreshToken,
                ExpiresIn    = authData.ExpiresIn
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al renovar token");
            return StatusCode(500, ApiResponse<object>.Fail("Error al renovar la sesión."));
        }
    }

    /// <summary>Cierra la sesión del usuario actual.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> Logout()
    {
        // Supabase invalida el token en el servidor
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var supabaseUrl    = _configuration["Supabase:Url"]!;
        var supabaseAnonKey = _configuration["Supabase:AnonKey"]!;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await httpClient.PostAsync($"{supabaseUrl}/auth/v1/logout", null);

        return Ok(ApiResponse<bool>.Ok(true, "Sesión cerrada exitosamente."));
    }
}
```

### 9.2 PacientesController (HU07)

```csharp
// src/Vittal.API/Controllers/PacientesController.cs
namespace Vittal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Pacientes")]
public class PacientesController : ControllerBase
{
    private readonly IPacienteService _service;
    private readonly ILogger<PacientesController> _logger;

    public PacientesController(IPacienteService service, ILogger<PacientesController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Obtiene todos los pacientes activos de la clínica.</summary>
    [HttpGet]
    [RequirePermission("pacientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PacienteResponseDto>>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetAllAsync(clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Obtiene los pacientes asignados a un doctor específico.
    /// Los doctores solo ven sus propios pacientes; los admins especifican doctorId.
    /// </summary>
    [HttpGet("doctor/{doctorId:guid}")]
    [RequirePermission("pacientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PacienteResponseDto>>), 200)]
    public async Task<IActionResult> GetByDoctor([FromRoute] Guid doctorId)
    {
        var clinicaId = User.GetClinicaId();
        // Regla: si no es admin, solo puede ver sus propios pacientes
        if (!User.EsAdmin() && User.GetUsuarioId() != doctorId)
            return StatusCode(403, ApiResponse<object>.Fail(
                "Solo puede visualizar sus propios pacientes."));

        var result = await _service.GetByDoctorAsync(doctorId, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Busca pacientes por nombre, apellido o email.
    /// Mínimo 2 caracteres requeridos. Usado en el buscador de la Agenda.
    /// </summary>
    [HttpGet("buscar")]
    [RequirePermission("pacientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PacienteResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Buscar([FromQuery] string termino)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.SearchAsync(termino, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>Obtiene un paciente por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("pacientes", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<PacienteResponseDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>Registra un nuevo paciente en la clínica.</summary>
    [HttpPost]
    [RequirePermission("pacientes", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<PacienteResponseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> Create([FromBody] PacienteRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.CreateAsync(dto, clinicaId);
        return result.ToCreatedResult(this, nameof(GetById), new { id = result.Data?.Id });
    }

    /// <summary>Actualiza los datos de un paciente existente.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("pacientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<PacienteResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] PacienteRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.UpdateAsync(id, dto, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Desactiva un paciente. NUNCA elimina.
    /// El paciente deja de aparecer en listados pero su historial se conserva.
    /// </summary>
    [HttpPatch("{id:guid}/desactivar")]
    [RequirePermission("pacientes", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Desactivar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(id, clinicaId);
        return result.ToActionResult(this);
    }
}
```

### 9.3 CitasController (HU21 + HU18)

```csharp
// src/Vittal.API/Controllers/CitasController.cs
namespace Vittal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Citas y Agenda")]
public class CitasController : ControllerBase
{
    private readonly ICitaService _service;
    private readonly ILogger<CitasController> _logger;

    public CitasController(ICitaService service, ILogger<CitasController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>
    /// Obtiene la cola de espera del día actual.
    /// Admins ven todas las citas; doctores solo las suyas.
    /// </summary>
    [HttpGet("cola-espera")]
    [RequirePermission("cola_espera", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CitaResponseDto>>), 200)]
    public async Task<IActionResult> GetColaEspera([FromQuery] Guid? doctorId)
    {
        var clinicaId = User.GetClinicaId();

        // Si no es admin, forzar el filtro al doctor autenticado
        var doctorIdFiltro = User.EsAdmin()
            ? doctorId
            : User.GetUsuarioId();

        var result = await _service.GetColaEsperaAsync(clinicaId, doctorIdFiltro);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Obtiene las citas de un doctor para una fecha específica (vista de Agenda).
    /// </summary>
    [HttpGet("agenda")]
    [RequirePermission("agenda", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CitaResponseDto>>), 200)]
    public async Task<IActionResult> GetAgenda(
        [FromQuery] Guid doctorId,
        [FromQuery] DateOnly fecha)
    {
        var clinicaId = User.GetClinicaId();

        if (!User.EsAdmin() && User.GetUsuarioId() != doctorId)
            return StatusCode(403, ApiResponse<object>.Fail(
                "Solo puede visualizar su propia agenda."));

        var result = await _service.GetByDoctorAndFechaAsync(doctorId, clinicaId, fecha);
        return result.ToActionResult(this);
    }

    /// <summary>Agenda una nueva cita médica.</summary>
    [HttpPost]
    [RequirePermission("agenda", PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<CitaResponseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 422)]
    public async Task<IActionResult> Create([FromBody] CitaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.CreateAsync(dto, clinicaId);
        return result.ToCreatedResult(this, nameof(GetById), new { id = result.Data?.Id });
    }

    /// <summary>Obtiene una cita por su ID.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("agenda", PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<CitaResponseDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.GetByIdAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>Actualiza los datos de una cita programada.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("agenda", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<CitaResponseDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] CitaRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.UpdateAsync(id, dto, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Registra la llegada de un paciente a la clínica.
    /// Cambia el estado de la cita a 'en_espera' y registra la hora de llegada.
    /// </summary>
    [HttpPatch("{id:guid}/llegada")]
    [RequirePermission("cola_espera", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RegistrarLlegada([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.RegistrarLlegadaAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Marca un paciente como "en atención" y lo saca de la Cola de Espera.
    /// Equivale al botón "Atender" del módulo Cola de Espera (HU18).
    /// </summary>
    [HttpPatch("{id:guid}/atender")]
    [RequirePermission("cola_espera", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Atender([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.AtenderPacienteAsync(id, clinicaId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Cancela una cita (activo = false, estado = 'cancelada'). NUNCA elimina.
    /// </summary>
    [HttpPatch("{id:guid}/cancelar")]
    [RequirePermission("agenda", PermissionType.Update)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancelar([FromRoute] Guid id)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.DeactivateAsync(id, clinicaId);
        return result.ToActionResult(this);
    }
}
```

---

## 10. Configuración de Program.cs

```csharp
// src/Vittal.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

// ── Servicios ────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger con soporte para JWT Bearer
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Vittal API",
        Version     = "v1",
        Description = "API REST para el sistema médico Vittal — SaaS multi-tenant"
    });

    // Configurar autenticación Bearer en Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Ingrese el token JWT de Supabase Auth: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Incluir comentarios XML de los Controllers en Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// Autenticación JWT de Supabase
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var supabaseUrl = builder.Configuration["Supabase:Url"]!;

        options.Authority = $"{supabaseUrl}/auth/v1";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = $"{supabaseUrl}/auth/v1",
            ValidateAudience         = true,
            ValidAudience            = "authenticated",
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            // Supabase usa JWT_SECRET de la configuración del proyecto
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Supabase:JwtSecret"]!))
        };
    });

builder.Services.AddAuthorization();

// ── Capas de la aplicación ────────────────────────────────────────────────
builder.Services.AddVittalDAL(builder.Configuration);  // skill-dal.md
builder.Services.AddVittalBLL();                         // skill-bll.md

// Filtro de permisos — registrar globalmente
builder.Services.AddScoped<PermissionFilter>();
builder.Services.AddControllers(options =>
    options.Filters.AddService<PermissionFilter>());

// CORS — para que el frontend MVC pueda consumir la API
builder.Services.AddCors(options =>
    options.AddPolicy("VittalFrontend", policy =>
        policy.WithOrigins(
                builder.Configuration["App:FrontendUrl"] ?? "https://localhost:7001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()));

// ── Build ────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Pipeline ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vittal API v1");
        c.RoutePrefix = "swagger";  // → http://localhost:PORT/swagger
    });
}

app.UseHttpsRedirection();
app.UseCors("VittalFrontend");
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();   // ← DESPUÉS de Authentication
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 11. Checklist de Calidad — @EspecialistaUI (Controllers)

Antes de notificar al @PM que el Controller está listo:

### Decoradores y estructura

- [ ] `[ApiController]` presente en la clase
- [ ] `[Route("api/[controller]")]` presente en la clase
- [ ] `[Authorize]` presente en la clase (no endpoint por endpoint)
- [ ] `[Produces("application/json")]` presente
- [ ] `[Tags("NombreGrupo")]` presente para organización en Swagger
- [ ] Clase hereda de `ControllerBase` (no de `Controller`)
- [ ] Constructor recibe el Service y el Logger — nada más

### Endpoints

- [ ] GET lista → `[HttpGet]` + `[RequirePermission(Read)]`
- [ ] GET por ID → `[HttpGet("{id:guid}")]` + `[RequirePermission(Read)]`
- [ ] POST crear → `[HttpPost]` + `[RequirePermission(Create)]`
- [ ] PUT actualizar → `[HttpPut("{id:guid}")]` + `[RequirePermission(Update)]`
- [ ] PATCH desactivar → `[HttpPatch("{id:guid}/desactivar")]` + `[RequirePermission(Update)]`
- [ ] **No existe `[HttpDelete]`** en ningún Controller de Vittal

### JWT y tenant

- [ ] `clinicaId = User.GetClinicaId()` en cada método — nunca del body
- [ ] `usuarioId = User.GetUsuarioId()` cuando aplica filtro por doctor
- [ ] `User.EsAdmin()` usado para verificar si se aplica restricción de doctor
- [ ] Ningún endpoint acepta `clinicaId` como parámetro de request

### Respuestas HTTP

- [ ] `[ProducesResponseType]` en cada endpoint con tipo y código correcto
- [ ] Crear usa `ToCreatedResult` → 201
- [ ] Leer usa `ToActionResult` → 200
- [ ] No encontrado retorna 404 via `ServiceResult.NotFound`
- [ ] Duplicado retorna 409 via `ServiceResult.Duplicate`
- [ ] Error de validación retorna 400 via `ServiceResult.ValidationError`
- [ ] Error de negocio retorna 422 via `ServiceResult.BusinessError`
- [ ] Toda respuesta usa `ApiResponse<T>` como wrapper

### Swagger

- [ ] Cada método tiene `/// <summary>` con descripción en español
- [ ] Endpoint visible y funcional en `/swagger`
- [ ] El botón "Authorize" de Swagger acepta el JWT de Supabase

---

*skill-controller.md — Vittal v1.0.0 | Agente: @EspecialistaUI*
*Para contexto del proyecto: CLAUDE.md | Para lógica de negocio: skill-bll.md*
*Para coordinación de agentes: ORCHESTRATOR.md | Siguiente: skill-view.md*
