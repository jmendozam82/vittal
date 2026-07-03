# Plan Multi-Tenant: Workspace Switcher para Super Admin

> **Fecha:** 2026-07-02
> **Propósito:** Documentar el diseño, la situación actual y el plan de implementación para que el Super Admin pueda gestionar múltiples clínicas desde una misma sesión.

---

## 1. Situación Actual

### 1.1 Modelo Multi-Tenant

El sistema Vittal es multi-tenant por diseño: toda tabla de negocio tiene `clinica_id` como discriminador de tenant, con RLS habilitado en PostgreSQL para aislamiento de datos.

### 1.2 Limitación Identificada

| Aspecto | Estado Actual |
|---------|:-------------:|
| **Super Admin** | Tiene un `clinica_id` fijo en su JWT ("Vittal Clinic Central") |
| **Crear clínica + admin inicial** | ✅ `POST /api/Clinicas/provisionar` — Funciona correctamente |
| **Crear usuarios en clínica específica** | ❌ No existe — `POST /api/Usuarios` hereda `clinica_id` del JWT |
| **Cambiar de clínica en sesión** | ❌ No hay mecanismo |
| **Selector visual de clínica** | ❌ No existe en la UI |

### 1.3 Consecuencia

El Super Admin hoy solo puede operar dentro de "Vittal Clinic Central" (su clínica asignada). Para gestionar otras clínicas, necesita:

1. Un **selector de clínica** (workspace switcher) dentro de la sesión
2. Un **endpoint** para crear usuarios especificando la clínica destino
3. Un **mecanismo** para que el backend sepa que el Super Admin está operando en otra clínica

---

## 2. Análisis de Alternativas

| Enfoque | Descripción | Pros | Contras |
|---------|-------------|------|---------|
| **A: Workspace Switcher** | Dropdown en navbar para cambiar de clínica en la misma sesión | • Sin cambio en login • Intuitivo • Rápido de implementar | • El Super Admin debe saber en qué clínica está |
| **B: Login por clínica** | Cada clínica tiene su dominio/subdominio y login propio | • Aislamiento total • Experiencia limpia | • Requiere infraestructura DNS • Mucho más desarrollo • Rompe flujo actual |
| **C: Super Admin sin clínica fija** | `clinica_id` nullable para Super Admin, permisos globales | • Simplifica la lógica | • Rompe esquema BD (FK NOT NULL) • Afecta RLS • Migración compleja |

**Decisión:** Adoptamos **Enfoque A (Workspace Switcher)** por ser el de menor impacto, mayor velocidad de implementación y alineado con SaaS reales (Slack, Asana, Monday).

---

## 3. Solución Propuesta

### 3.1 Arquitectura

```
┌───────────────────────────────────────────────────────────┐
│                     FRONTEND (MVC)                         │
│                                                           │
│   Layout/_Layout.cshtml                                   │
│   ┌──────────────────────────────────────────────────┐    │
│   │  🏥 [Vittal Clinic Central  ▼]  ⚙️ Admin  👤 Usuario│    │
│   │       ┌────────────────────┐                      │    │
│   │       │ ○ Vittal Clinic    │ ← Actual             │    │
│   │       │ ○ Clínica Los      │                      │    │
│   │       │   Andes            │                      │    │
│   │       │ ○ Clínica del      │                      │    │
│   │       │   Norte            │                      │    │
│   │       │ ────────────       │                      │    │
│   │       │ ➕ Crear nueva      │                      │    │
│   │       └────────────────────┘                      │    │
│   └──────────────────────────────────────────────────┘    │
│                                                           │
│   ApiClientHelper agrega header:                          │
│   X-Clinica-Override: {clinicaId}                         │
└───────────────────────────────────────────────────────────┘
        │                          │
        │  cookie vittal_jwt       │  header X-Clinica-Override
        ▼                          ▼
┌───────────────────────────────────────────────────────────┐
│                   API (Vittal.API)                         │
│                                                           │
│   TenantMiddleware                                         │
│   ┌──────────────────────────────────────────────────┐    │
│   │  1. Extrae clinica_id del JWT                     │    │
│   │  2. Si Super Admin + header X-Clinica-Override    │    │
│   │     → USA el clinica_id del override              │    │
│   │  3. Configura RLS con ese clinica_id              │    │
│   └──────────────────────────────────────────────────┘    │
│                                                           │
│   AdminController (API)                                    │
│   ┌──────────────────────────────────────────────────┐    │
│   │  [RequireSuperAdmin]                              │    │
│   │  GET /api/Admin/clinicas         → Lista todas    │    │
│   │  POST /api/Admin/usuarios        → Crea en clínica│    │
│   │  GET /api/Admin/perfiles?clinicaId= → Perfiles    │    │
│   └──────────────────────────────────────────────────┘    │
└───────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────────┐
│                    SUPABASE / PostgreSQL                   │
│                                                           │
│   RLS: clinica_isolation                                   │
│   Se aplica con el clinica_id del header                  │
│   (override o JWT según el caso)                          │
└───────────────────────────────────────────────────────────┘
```

### 3.2 Flujo del Workspace Switcher

```
1. Super Admin inicia sesión (login normal)
   → JWT contiene su clinica_id original

2. Ve el dropdown de clínicas en la barra superior
   → Solo visible si esSuperAdmin = true

3. Selecciona "Clínica Los Andes"
   → Frontend guarda clinicaId en sesión (Session["ClinicaOverride"])
   → Frontend recarga la página

4. Cada request del frontend al API incluye:
   Header: X-Clinica-Override: {id-de-los-andes}

5. TenantMiddleware detecta el override:
   → Verifica que el usuario sea Super Admin
   → Usa el clinica_id del override en lugar del JWT
   → Configura RLS con ese clinica_id

6. Todo lo que haga el Super Admin ahora opera en
   "Clínica Los Andes" como si fuera un admin local

7. Para volver, selecciona otra clínica del dropdown
   o "Vittal Clinic Central" (su clínica original)
```

---

## 4. Componentes a Implementar

### 4.1 API — Backend

| # | Componente | Archivo | Descripción |
|---|-----------|---------|-------------|
| 1 | **Endpoint: Crear usuario en clínica específica** | `AdminController.cs` | `POST /api/Admin/usuarios` — Crea usuario en la clínica indicada. Body incluye `clinicaId`. Protegido con `[RequireSuperAdmin]` |
| 2 | **Endpoint: Listar perfiles por clínica** | `AdminController.cs` | `GET /api/Admin/perfiles?clinicaId={id}` — Los perfiles dependen de la clínica (cada clínica tiene sus propios perfiles) |
| 3 | **Modificar TenantMiddleware** | `TenantMiddleware.cs` | Si el usuario es Super Admin y existe header `X-Clinica-Override`, usar ese `clinica_id` en lugar del del JWT |
| 4 | **Agregar método en IAdminService** | `IAdminService.cs` | `Task<ServiceResult<UsuarioResponseDto>> CreateUsuarioAsync(UsuarioRequestDto dto, Guid clinicaId, Guid creadoPor)` |
| 5 | **Implementar en AdminService** | `AdminService.cs` | Lógica de creación similar a UsuarioService.CreateAsync, pero con `clinicaId` explícito |
| 6 | **Endpoint: Listar clínicas para Super Admin** | `AdminController.cs` | `GET /api/Admin/clinicas` — Lista todas las clínicas del sistema (ya existe pero verificar) |

### 4.2 Frontend — MVC

| # | Componente | Archivo | Descripción |
|---|-----------|---------|-------------|
| 7 | **Dropdown de clínicas en Layout** | `_Layout.cshtml` o `Sidebar/Default.cshtml` | Dropdown en navbar visible solo si `esSuperAdmin`, con lista de clínicas y opción "Crear nueva" |
| 8 | **Modelo para el dropdown** | `SidebarViewComponent.cs` o nuevo ViewComponent | Obtener lista de clínicas desde `GET /api/Admin/clinicas` |
| 9 | **Controlador proxy: AdminController MVC** | `Areas/Admin/Controllers/AdminController.cs` | Endpoints proxy para el selector y gestión de clínicas |
| 10 | **Modificar ApiClientHelper** | `ApiClientHelper.cs` | Agregar header `X-Clinica-Override` desde `Session["ClinicaOverride"]` a cada request |
| 11 | **Vista: Crear clínica** | `Areas/Admin/Views/Clinica/Create.cshtml` | Formulario para provisionar nueva clínica |
| 12 | **Vista: Gestión de usuarios cross-clínica** | `Areas/Admin/Views/Usuario/Index.cshtml` | Lista de usuarios con selector de clínica |

### 4.3 Modelo de Datos

| Tabla | Cambio |
|-------|--------|
| `usuarios` | ❌ Sin cambios (ya tiene `clinica_id NOT NULL`) |
| `clinicas` | ❌ Sin cambios (tabla raíz, no tiene `clinica_id`) |
| `perfiles` | ❌ Sin cambios (ya tiene `clinica_id`) |

**No se requiere ninguna migración de base de datos.**

---

## 5. Detalle de Implementación

### 5.1 Endpoint: `POST /api/Admin/usuarios`

```csharp
// AdminController.cs
[HttpPost("usuarios")]
[RequireSuperAdmin]
[ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status201Created)]
public async Task<IActionResult> CreateUsuario([FromBody] AdminCreateUsuarioRequestDto dto)
{
    // El Super Admin especifica explícitamente clinicaId
    var creadoPor = User.GetInternalUserId();
    var result = await _adminService.CreateUsuarioAsync(dto, dto.ClinicaId, creadoPor);
    return result.ToActionResult();
}
```

**DTO de entrada:**
```csharp
public class AdminCreateUsuarioRequestDto : UsuarioRequestDto
{
    [Required(ErrorMessage = "Debe especificar la clínica del usuario.")]
    public Guid ClinicaId { get; set; }
}
```

Diferencia con `UsuarioRequestDto`: **agrega `ClinicaId`** como campo requerido (en el DTO normal no existe).

### 5.2 Modificación en TenantMiddleware

```csharp
// TenantMiddleware.cs — dentro de InvokeAsync
if (context.User.IsAuthenticated && context.User.EsSuperAdmin())
{
    // Super Admin puede sobrescribir clinica_id vía header
    var overrideHeader = context.Request.Headers["X-Clinica-Override"].FirstOrDefault();
    if (!string.IsNullOrEmpty(overrideHeader) && Guid.TryParse(overrideHeader, out var overrideClinicaId))
    {
        // Usar la clínica del override en lugar de la del JWT
        dbFactory.SetTenantContext(overrideClinicaId);
        
        // Agregar claim de override para que el resto del pipeline lo use
        var overrideClaim = new Claim("app_clinica_override", overrideClinicaId.ToString());
        context.User.AddIdentity(new ClaimsIdentity(new[] { overrideClaim }));
        
        return; // Saltar la búsqueda del usuario en BD
    }
}
```

**Importante:** El header `X-Clinica-Override` solo se respeta si el usuario autenticado es Super Admin. Un admin local no puede usar este header para saltarse el tenant.

### 5.3 Extensión de ClaimsPrincipalExtensions

```csharp
// ClaimsPrincipalExtensions.cs
public static Guid GetEffectiveClinicaId(this ClaimsPrincipal user)
{
    // Si hay override y es Super Admin, usar el override
    var overrideClaim = user.FindFirst("app_clinica_override");
    if (overrideClaim != null && Guid.TryParse(overrideClaim.Value, out var overrideId))
        return overrideId;
    
    // Si no, usar la clínica del JWT
    return user.GetClinicaId();
}
```

Luego reemplazar en todos los controllers existentes:
- `User.GetClinicaId()` → `User.GetEffectiveClinicaId()` 

Esto permite que **sin cambiar la lógica de negocio**, el Super Admin opere en cualquier clínica.

### 5.4 Dropdown en Frontend

```html
<!-- _Layout.cshtml o Sidebar/Default.cshtml -->
@if (User.IsInRole("SuperAdmin"))
{
    <li class="nav-item dropdown">
        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
            🏥 @ViewBag.ClinicaActual
        </a>
        <ul class="dropdown-menu">
            @foreach (var clinica in Model.Clinicas)
            {
                <li>
                    <a class="dropdown-item" href="@Url.Action("SwitchClinica", "Admin", new { id = clinica.Id })">
                        @clinica.Nombre
                    </a>
                </li>
            }
            <li><hr class="dropdown-divider"></li>
            <li>
                <a class="dropdown-item" href="@Url.Action("Create", "Clinica", new { area = "Admin" })">
                    ➕ Crear nueva clínica
                </a>
            </li>
        </ul>
    </li>
}
```

### 5.5 Controlador para Switch

```csharp
// Areas/Admin/Controllers/AdminController.cs (MVC)
[Area("Admin")]
[Authorize]
public class AdminController : Controller
{
    [HttpGet("switch-clinica/{id:guid}")]
    public async Task<IActionResult> SwitchClinica(Guid id)
    {
        // Verificar que el usuario es Super Admin
        if (!User.EsSuperAdmin())
            return RedirectToAction("AccessDenied", "Home");

        // Guardar la clínica seleccionada en sesión
        HttpContext.Session.SetString("ClinicaOverride", id.ToString());
        
        // Redirigir al dashboard
        return RedirectToAction("Index", "Dashboard");
    }
}
```

---

## 6. Archivos a Modificar/Crear

### API (Vittal.API)

| Archivo | Acción | Líneas aprox. |
|---------|--------|:-------------:|
| `Controllers/AdminController.cs` | **Modificar** — Agregar endpoint POST /api/Admin/usuarios | +40 |
| `Middleware/TenantMiddleware.cs` | **Modificar** — Agregar lógica de override por header | +15 |
| `Extensions/ClaimsPrincipalExtensions.cs` | **Modificar** — Agregar GetEffectiveClinicaId() | +10 |

### BLL (Vittal.BLL)

| Archivo | Acción | Líneas aprox. |
|---------|--------|:-------------:|
| `Interfaces/IAdminService.cs` | **Modificar** — Agregar CreateUsuarioAsync | +2 |
| `Services/AdminService.cs` | **Modificar** — Implementar CreateUsuarioAsync | +40 |
| `Services/UsuarioService.cs` | **Modificar** — Extraer lógica común reutilizable | +0 (refactor) |

### DTO (Vittal.DTO)

| Archivo | Acción | Líneas aprox. |
|---------|--------|:-------------:|
| `Usuario/AdminCreateUsuarioRequestDto.cs` | **Crear** — DTO con ClinicaId incluido | +25 |

### Frontend (Vittal.Aplicacion)

| Archivo | Acción | Líneas aprox. |
|---------|--------|:-------------:|
| `Helpers/ApiClientHelper.cs` | **Modificar** — Agregar header X-Clinica-Override | +5 |
| `Areas/Admin/Controllers/AdminController.cs` | **Crear** — Controlador proxy para switch | +50 |
| `Areas/Admin/Views/Clinica/Create.cshtml` | **Crear** — Vista para nueva clínica | +200 |
| `Areas/Admin/Views/Clinica/Index.cshtml` | **Crear** — Vista de clínicas | +100 |
| `Areas/Admin/_ViewStart.cshtml` | **Crear** — Layout de área Admin | +5 |
| `Views/Shared/Components/Sidebar/Default.cshtml` | **Modificar** — Agregar dropdown de clínicas | +30 |
| `Components/SidebarViewComponent.cs` | **Modificar** — Poblar lista de clínicas para dropdown | +15 |

---

## 7. Cómo Quedará — Estado Final

### 7.1 Experiencia del Super Admin

```
┌──────────────────────────────────────────────────────────────┐
│  🏥 [Vittal Clinic Central  ▼]    🔔    👤 Admin Vittal     │
│  ┌────────────────────────────┐                              │
│  │ ○ Vittal Clinic Central    │ ← Check si es la actual     │
│  │ ○ Clínica Los Andes        │                              │
│  │ ○ Clínica del Norte        │                              │
│  │ ─────────────────────      │                              │
│  │ ➕ Crear nueva clínica     │ → Formulario de provisión   │
│  └────────────────────────────┘                              │
│                                                              │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  Dashboard de Vittal Clinic Central                      ││
│  │                                                          ││
│  │  Pacientes: 150 | Doctores: 8 | Citas hoy: 23          ││
│  │                                                          ││
│  │  [Usuarios] [Catálogos] [Agenda] [Reportes]             ││
│  └──────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────┘

Si selecciona "Clínica Los Andes":

┌──────────────────────────────────────────────────────────────┐
│  🏥 [Clínica Los Andes  ▼]        🔔    👤 Admin Vittal     │
│                                                              │
│  ┌──────────────────────────────────────────────────────────┐│
│  │  Dashboard de Clínica Los Andes                          ││
│  │                                                          ││
│  │  Pacientes: 0 | Doctores: 1 | Citas hoy: 0             ││
│  │                                                          ││
│  │  [Usuarios] [Catálogos] [Agenda] [Reportes]             ││
│  │   ┌─ Crear usuario ─────────────────────────────────┐   ││
│  │   │ 👤 Juan Pérez                                  │   ││
│  │   │ 📧 juan@losandes.com                           │   ││
│  │   │ 🏥 Clínica Los Andes  ← fijo, no editable       │   ││
│  │   │ 🎭 Perfil: Doctor                               │   ││
│  │   │ [Guardar]                                       │   ││
│  │   └────────────────────────────────────────────────┘   ││
│  └──────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────┘
```

### 7.2 Resumen de Capacidades Finales

| Capacidad | Antes | Después |
|-----------|:-----:|:-------:|
| Super Admin puede ver todas las clínicas | ❌ | ✅ |
| Super Admin puede crear usuarios en cualquier clínica | ❌ | ✅ |
| Super Admin puede cambiar de clínica sin cerrar sesión | ❌ | ✅ |
| Admin local gestiona SOLO su clínica (sin cambios) | ✅ | ✅ |
| Login sin cambios | ✅ | ✅ |
| RLS y permisos se mantienen intactos | ✅ | ✅ |
| Crear clínica + admin inicial | ✅ | ✅ (desde dropdown) |

---

## 8. Plan de Implementación por Fases

### Fase 1: Endpoint `POST /api/Admin/usuarios` ⏱ ~1h

**Objetivo:** Que el Super Admin pueda crear usuarios en cualquier clínica vía API.

| Tarea | Dependencia |
|-------|:-----------:|
| 1.1 Crear `AdminCreateUsuarioRequestDto` | Ninguna |
| 1.2 Agregar `CreateUsuarioAsync` en `IAdminService` | 1.1 |
| 1.3 Implementar en `AdminService` | 1.2 |
| 1.4 Agregar `POST /api/Admin/usuarios` en `AdminController` | 1.3 |
| 1.5 Probar con PowerShell | 1.4 |

### Fase 2: Override en TenantMiddleware ⏱ ~30min

**Objetivo:** Que el endpoint existente `GET /api/Usuarios` (y todos los demás) respondan con los datos de la clínica override.

| Tarea | Dependencia |
|-------|:-----------:|
| 2.1 Modificar `TenantMiddleware` para header `X-Clinica-Override` | Ninguna |
| 2.2 Agregar `GetEffectiveClinicaId()` en `ClaimsPrincipalExtensions` | 2.1 |
| 2.3 Probar que el API responde con datos de la clínica override | 2.2 |

### Fase 3: Dropdown en Frontend ⏱ ~1h

**Objetivo:** Interfaz visual para que el Super Admin cambie de clínica.

| Tarea | Dependencia |
|-------|:-----------:|
| 3.1 Agregar endpoint `GET /api/Admin/clinicas` si no existe | Fase 1 |
| 3.2 Crear controlador `AdminController` (MVC) con acción SwitchClinica | 3.1 |
| 3.3 Modificar `SidebarViewComponent` para incluir lista de clínicas | 3.2 |
| 3.4 Agregar dropdown en `Sidebar/Default.cshtml` | 3.3 |
| 3.5 Probar flujo completo | 3.4 |

### Fase 4: Pruebas y Ajustes ⏱ ~1h

**Objetivo:** Validar que todo funciona correctamente.

| Tarea | Dependencia |
|-------|:-----------:|
| 4.1 Probar: Super Admin crea clínica → crea usuarios → cambia de clínica | Fase 3 |
| 4.2 Probar: Admin local NO puede usar override | Fase 3 |
| 4.3 Probar: RLS sigue funcionando correctamente | Fase 3 |
| 4.4 Probar: Frontend muestra datos correctos por clínica | Fase 3 |

---

## 9. Riesgos y Consideraciones

### 9.1 Riesgos

| Riesgo | Impacto | Mitigación |
|--------|:-------:|------------|
| **Cache de sesión:** Sidebar y datos cacheados pueden mostrar datos de la clínica anterior | Alto | Recargar UI al cambiar de clínica. No cachear datos sensibles en Session |
| **RLS mal configurado:** El override podría no propagarse a conexiones hijas | Alto | Verificar que `DbConnectionFactory.SetTenantContext` se llame antes de cualquier query |
| **URLs compartidas:** Si un usuario copia una URL mientras está en "Clínica Los Andes" y la comparte, otro podría ver datos incorrectos | Bajo | Responsabilidad del usuario (estándar en SaaS multi-tenant) |
| **Super Admin olvida qué clínica tiene seleccionada:** Podría crear datos en la clínica incorrecta | Medio | Indicador visual claro del nombre de clínica en navbar + confirmación en operaciones críticas |

### 9.2 Consideraciones

- **Solo Super Admin** puede usar el override. Los admins locales y demás usuarios **no** tienen acceso a esta funcionalidad.
- El header `X-Clinica-Override` se ignora si el usuario no es Super Admin (seguridad por diseño).
- La clínica por defecto del Super Admin sigue siendo la que tiene en su JWT. El override es temporal (dura lo que dura la sesión).
- **No hay cambios en la base de datos.** Todo el cambio es a nivel de aplicación.

---

## 10. Referencias

- **CLAUDE.md** — Arquitectura general del proyecto
- **ORCHESTRATOR.md** — Roles y flujo de trabajo del equipo
- **AGENTS.md** — Reglas de desarrollo
- `src/Vittal.API/Middleware/TenantMiddleware.cs` — Middleware a modificar
- `src/Vittal.API/Controllers/AdminController.cs` — Endpoints de administración
- `src/Vittal.BLL/Services/AdminService.cs` — Lógica de negocio de administración
- `src/Vittal.Aplicacion/Helpers/ApiClientHelper.cs` — Helper HTTP del frontend
- `/skills/` — Instrucciones por capa para desarrollo asistido

---

*Documento mantenido por @PM — Última actualización: 2026-07-02*
