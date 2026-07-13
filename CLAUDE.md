# CLAUDE.md — Archivo Maestro del Proyecto Vittal

> Este archivo es cargado automáticamente por todos los agentes de Claude Code.
> Contiene el contexto completo del proyecto, arquitectura, convenciones y reglas de negocio.
> **No modificar sin aprobación del @PM.**

---

## 1. Identidad del Proyecto

| Campo | Valor |
|---|---|
| **Nombre del sistema** | Software Vittal |
| **Cliente** | MedicCore (Clínicas Médicas) |
| **Tipo de sistema** | Sistema web de control de citas y expedientes médicos |
| **Modelo de negocio** | SaaS (Software as a Service) + BaaS (Backend as a Service) |
| **Versión inicial** | v1.0.0 |
| **Idioma del sistema** | Español (interfaz, base de datos, nombres de campos) |
| **Idioma del código** | Inglés (variables, métodos, clases en C#) |

### Descripción del sistema

Vittal es una plataforma médica web multi-tenant que centraliza la gestión de citas, expedientes clínicos, diagnósticos, tratamientos, cirugías y toda la información médica de los pacientes. Está diseñada para ser adoptada por múltiples clínicas médicas como servicio SaaS, y expone su API como BaaS para que sistemas externos puedan integrarse. Incluye una Landing Page informativa como punto de entrada público para prospectos y futuros socios.

---

## 2. Stack Tecnológico Completo

### Frontend — Capa de Presentación

```
Framework:    ASP.NET Core MVC (.NET 8)
Lenguaje:     C# + Razor Pages (.cshtml)
Estructura:   Areas por módulo (Login, Admin, Catalogos, Expedientes, Agenda, etc.)
UI Kit:       Bootstrap 5.3
JavaScript:   Vanilla JS + jQuery 3.x
Tiempo real:  Supabase JS Client (alertas y cola de espera en tiempo real)
Validación:   FluentValidation (server) + jQuery Validate (client)
```

### Backend — API REST

```
Framework:    ASP.NET Core Web API (.NET 8)
Lenguaje:     C#
Documentación: Swagger / OpenAPI 3.0 (Swashbuckle)
Autenticación: JWT via Supabase Auth
Tiempo real:  SignalR (alertas configurables push)
ORM:          Dapper (consultas SQL directas a PostgreSQL)
Validación:   FluentValidation
```

### Base de Datos y BaaS — Supabase

```
Motor:        PostgreSQL 15 (via Supabase)
Auth:         Supabase Auth (JWT tokens, manejo de sesiones)
Realtime:     Supabase Realtime (cola de espera, alertas)
Storage:      Supabase Storage (archivos de expedientes: PDF, imágenes)
API auto:     PostgREST (endpoints REST autogenerados)
Seguridad:    Row Level Security (RLS) — aislamiento multi-tenant
Serverless:   Edge Functions (lógica auxiliar sin servidor)
CLI:          Supabase CLI (migraciones y gestión local)
```

### DevOps y Control de Versiones

```
Repositorio:  GitHub
CI/CD:        GitHub Actions
IDE:          VS Code + C# Dev Kit Extension
Runtime:      .NET 8 SDK
Containers:   Docker (opcional para despliegue)
Gestión BD:   Supabase Dashboard + Supabase CLI
```

### Desarrollo Asistido por IA

```
Herramienta:  Claude Code CLI v2.1.32+
Modo:         Agent Teams (experimental)
Flag:         CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1
Archivo base: CLAUDE.md (este archivo)
Orquestador: ORCHESTRATOR.md
Skills:       /skills/ (instrucciones por capa)
```

---

## 3. Arquitectura del Sistema

### Patrón: N-Capas (N-Tier) + MVC

El sistema sigue una arquitectura de N-capas estricta, separada en proyectos independientes dentro de una misma solución. El patrón MVC se aplica en la capa de presentación y el patrón Repositorio en la capa de acceso a datos.

```
SolucionVittal/
├── Vittal.Aplicacion/          ← Frontend MVC (Areas, Controllers, Views, wwwroot)
├── Vittal.API/                 ← Backend Web API (Controllers, Swagger, JWT)
├── Vittal.BLL/                 ← Business Logic Layer (Servicios, Reglas de negocio)
├── Vittal.DAL/                 ← Data Access Layer (Repositorios, Supabase/PostgreSQL)
├── Vittal.Entity/              ← Entidades del dominio (Modelos de BD)
├── Vittal.DTO/                 ← Data Transfer Objects (Request/Response)
├── Vittal.IOC/                 ← Inversión de Control (Inyección de dependencias)
└── Vittal.Utility/             ← Helpers, extensiones, constantes globales
```

### Flujo de datos obligatorio

```
[Vista Razor] → [Controller MVC] → [API Controller] → [BLL Service]
     → [DAL Repository] → [Supabase/PostgreSQL] → [Entity]
     ← [DTO Response] ← [BLL] ← [DAL] ← [PostgreSQL]
```

**Regla absoluta:** Ninguna capa puede saltarse otra. El Controller no llama directamente al DAL. La Vista no llama al BLL. El DAL no contiene lógica de negocio.

### Estructura de un módulo completo (ejemplo: Pacientes)

```
Vittal.Entity/
  └── Paciente.cs                    ← Modelo de base de datos

Vittal.DTO/
  ├── Paciente/PacienteRequestDto.cs ← Datos de entrada (crear/editar)
  └── Paciente/PacienteResponseDto.cs ← Datos de salida (lectura)

Vittal.DAL/
  ├── Interfaces/IPacienteRepository.cs
  └── Repositories/PacienteRepository.cs

Vittal.BLL/
  ├── Interfaces/IPacienteService.cs
  └── Services/PacienteService.cs

Vittal.API/
  └── Controllers/PacienteController.cs  ← Endpoints REST

Vittal.Aplicacion/
  └── Areas/Catalogos/
      ├── Controllers/PacienteController.cs ← Controller MVC
      └── Views/Paciente/
          ├── Index.cshtml
          ├── Create.cshtml
          └── Edit.cshtml

Vittal.IOC/
  └── DependencyInjection.cs  ← Registro de IPacienteService → PacienteService
```

---

## 4. Modelo Multi-Tenant (SaaS)

### Principio fundamental

Cada clínica (tenant) tiene sus datos completamente aislados. El campo `clinica_id` es el discriminador universal de tenant y debe estar presente en **todas** las tablas que contengan datos de negocio.

### Implementación en PostgreSQL / Supabase

```sql
-- Toda tabla de negocio incluye este campo
ALTER TABLE pacientes ADD COLUMN clinica_id UUID NOT NULL REFERENCES clinicas(id);
ALTER TABLE citas     ADD COLUMN clinica_id UUID NOT NULL REFERENCES clinicas(id);
-- etc.

-- Row Level Security habilitado en cada tabla
ALTER TABLE pacientes ENABLE ROW LEVEL SECURITY;

-- Política RLS: cada usuario solo ve su clinica
CREATE POLICY "tenant_isolation" ON pacientes
  USING (clinica_id = (current_setting('app.current_clinica_id'))::UUID);
```

### Flujo de autenticación multi-tenant

```
1. Usuario inicia sesión → Supabase Auth valida credenciales
2. JWT retornado contiene: user_id, clinica_id, perfil_id, permisos[]
3. Cada request al API incluye el JWT en Authorization: Bearer {token}
4. Middleware extrae clinica_id del JWT y lo inyecta como claim
5. DAL usa clinica_id en todas las consultas (RLS lo aplica automáticamente)
```

---

## 4.1 Decisión Arquitectónica — Especialidad por Sala

> **Confirmado 2026-05-12** | Aprobado por @PM

Una clínica puede tener múltiples salas con **distintas especialidades médicas**. Los catálogos de antecedentes y signos vitales se configuran **por sala**, no por clínica.

```
Ejemplo:
  Clínica MedicCore
  ├── Sala 1 = Medicina General → antecedentes: HTA, Diabetes, Cirugía previa
  ├── Sala 2 = Cardiología      → antecedentes: HTA, Diabetes, IAM previo, Tabaquismo
  └── Sala 3 = Dermatología     → antecedentes: Alergias cutáneas, Psoriasis, Acné
```

### Regla de discriminadores

| Campo | Propósito | Aplica en |
|---|---|---|
| `sala_id` | Discriminador de **especialidad** | `tipos_antecedente`, `tipos_signo_vital`, `antecedentes_paciente`, `signos_vitales_hoja` |
| `clinica_id` | Discriminador de **tenant** (RLS) | Todas las tablas de negocio |

**Regla absoluta:** `sala_id` define la especialidad. `clinica_id` define el aislamiento de datos. **Nunca usar `clinica_id` como discriminador de especialidad.**

### Flujo de onboarding de sala (plantillas)

```
Admin crea sala → Selecciona especialidad → Sistema importa plantilla →
Se generan tipos_antecedente y tipos_signo_vital para esa sala →
Admin puede personalizar (agregar/quitar/editar) →
Sala lista para atender pacientes en segundos
```

---

## 5. Módulos del Sistema

### Orden de desarrollo (por prioridad del backlog)

| ID | Historia de Usuario | Módulo | Prioridad | Importancia | Días |
|---|---|---|---|---|---|
| HU01 | Creación de la Base de Datos | Base de Datos | Alta | 100 | 7 |
| HU02 | Acceso al Sistema (Login) | Login | Alta | 95 | 3 |
| HU03 | Gestión de Perfiles | Administración | Alta | 95 | 5 |
| HU04 | Gestión de Usuarios | Administración | Alta | 95 | 6 |
| HU05 | Gestión de Permisos | Administración | Alta | 95 | 7 |
| HU07 | Gestión de Pacientes | Catálogos | Alta | 95 | 6 |
| HU06 | Gestión de Asignar Salas | Administración | Media | 85 | 4 |
| HU08 | Gestión de Medicamentos | Catálogos | Media | 85 | 4 |
| HU09 | Gestión de Clínicas | Catálogos | Media | 85 | 4 |
| HU10 | Gestión de Salas/Áreas | Catálogos | Media | 85 | 4 |
| HU11 | Gestión de Tipos de Cirugías | Catálogos | Media | 85 | 3 |
| HU12 | Gestión de Cirugías | Catálogos | Media | 85 | 3 |
| HU13 | Gestión de Tipos de Diagnósticos | Catálogos | Media | 85 | 3 |
| HU14 | Gestión de Diagnósticos | Catálogos | Media | 85 | 3 |
| HU15 | Gestión de Tratamientos | Catálogos | Media | 85 | 3 |
| HU16 | Gestión de Recomendaciones | Catálogos | Media | 85 | 3 |
| HU17 | Gestión de Exámenes | Catálogos | Media | 85 | 3 |
| **HU-E01** | **ALTER citas: Agregar hora_fin** | **Agenda/BD** | **Alta** | **90** | **0.5** |
| **HU-E02** | **Plantillas de Especialidad + Seed** | **Catálogos Sistema** | **Alta** | **88** | **1** |
| **HU-E03** | **Tipos de Antecedente por Sala** | **Catálogos Médicos** | **Alta** | **88** | **2** |
| **HU-E04** | **Tipos de Signo Vital por Sala** | **Catálogos Médicos** | **Alta** | **88** | **2** |
| **HU-E05** | **Antecedentes del Paciente** | **Expedientes** | **Alta** | **85** | **2** |
| **HU-E06** | **Signos Vitales por Consulta** | **Expedientes** | **Alta** | **85** | **1.5** |
| **HU-E07** | **Constancias Médicas** | **Expedientes** | **Media** | **80** | **2** |
| HU18 | Cola de Espera | Cola Espera | Media | 80 | 8 |
| HU19 | Línea de Tiempo | Línea Tiempo | Media | 80 | 5 |
| HU20 | Gestión de Expedientes | Expedientes | Media | 80 | 28 |
| HU21 | Agenda | Agenda | Media | 80 | 10 |
| HU23 | Dashboard | Dashboard | Media | 80 | 10 |
| HU22 | Reportes | Reportes | Media | 80 | 6 |
| HU23 | Alertas Configurables | Alertas | Media | 70 | 2 |
| **HU-L01** | **Landing Page Informativa** | **Landing** | **Alta** | **90** | **3** |

**Total estimado: ~156 días de desarrollo** *(+11 días por Sprint de Especialidades por Sala)*

### Áreas MVC del Frontend

```
Vittal.Aplicacion/Areas/
├── Landing/         ← HU-L01 (Público — sin auth)
├── Login/           ← HU02
├── Administracion/  ← HU03, HU04, HU05, HU06
├── Catalogos/       ← HU07 al HU17
├── ColaEspera/      ← HU18
├── LineaTiempo/     ← HU19
├── Expedientes/     ← HU20
├── Agenda/          ← HU21
├── Dashboard/       ← HU23
├── Reportes/        ← HU22
└── Alertas/         ← HU23 (alertas configurables)
```

---

## 6. Modelo de Permisos

### Tipos de permisos existentes

El sistema maneja exactamente 3 tipos de permisos por tarea/módulo:

| Permiso | Código | Descripción |
|---|---|---|
| Leer | `READ` | Visualizar listados y registros |
| Crear | `CREATE` | Insertar nuevos registros |
| Actualizar | `UPDATE` | Editar registros existentes |

**No existe permiso de eliminación.** Los registros solo cambian estado de `activo` a `inactivo`.

### Estructura de la verificación de permisos

```csharp
// En cada Controller API — verificar permiso antes de ejecutar
[HttpGet]
[Authorize]
[RequirePermission("pacientes", PermissionType.Read)]
public async Task<IActionResult> GetAll() { ... }

[HttpPost]
[Authorize]
[RequirePermission("pacientes", PermissionType.Create)]
public async Task<IActionResult> Create([FromBody] PacienteRequestDto dto) { ... }
```

### Roles especiales

- **Administrador del sistema:** tiene acceso completo a todos los módulos y permisos sin restricciones.
- **Doctor:** acceso a módulos de su clínica asignada, filtrado por su `doctor_id`.
- **Gerente de Clínica:** acceso a reportes, dashboard y gestión administrativa de su clínica.

---

## 7. Convenciones de Código

### Nomenclatura general

```
Clases C#:         PascalCase         → PacienteService, CitaRepository
Interfaces C#:     IPascalCase        → IPacienteService, ICitaRepository
Métodos C#:        PascalCase         → GetAllAsync, CreatePacienteAsync
Variables C#:      camelCase          → pacienteId, clinicaId
Constantes C#:     UPPER_SNAKE_CASE   → MAX_WAIT_TIME, DEFAULT_PAGE_SIZE
Tablas SQL:        snake_case plural  → pacientes, tipos_cirugias, citas_medicas
Columnas SQL:      snake_case         → clinica_id, fecha_nacimiento, primer_nombre
Archivos .cshtml:  PascalCase         → Index.cshtml, CreatePaciente.cshtml
Archivos .cs:      PascalCase         → PacienteService.cs, ICitaRepository.cs
```

### Estructura obligatoria de una Entity

```csharp
namespace Vittal.Entity;

public class Paciente
{
    public Guid Id { get; set; }                    // Autogenerado por PostgreSQL
    public Guid ClinicaId { get; set; }             // OBLIGATORIO — discriminador tenant
    public Guid DoctorId { get; set; }              // Paciente asignado a un doctor
    public string PrimerNombre { get; set; } = string.Empty;
    public string SegundoNombre { get; set; } = string.Empty;
    public string PrimerApellido { get; set; } = string.Empty;
    public string SegundoApellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;       // "M" | "F"
    public bool Activo { get; set; } = true;               // NUNCA eliminar — solo desactivar
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
}
```

### Estructura obligatoria de un Repository

```csharp
namespace Vittal.DAL.Repositories;

public class PacienteRepository : IPacienteRepository
{
    private readonly IDbConnection _db;

    public PacienteRepository(IDbConnection db)
    {
        _db = db;
    }

    // SIEMPRE filtrar por clinica_id — RLS de Supabase lo refuerza a nivel BD
    public async Task<IEnumerable<Paciente>> GetAllAsync(Guid clinicaId)
    {
        const string sql = @"
            SELECT * FROM pacientes
            WHERE clinica_id = @ClinicaId AND activo = true
            ORDER BY primer_apellido, primer_nombre";

        return await _db.QueryAsync<Paciente>(sql, new { ClinicaId = clinicaId });
    }

    public async Task<Guid> CreateAsync(Paciente paciente)
    {
        const string sql = @"
            INSERT INTO pacientes (clinica_id, doctor_id, primer_nombre, primer_apellido,
                                   email, celular, direccion, sexo, activo, fecha_creacion)
            VALUES (@ClinicaId, @DoctorId, @PrimerNombre, @PrimerApellido,
                    @Email, @Celular, @Direccion, @Sexo, true, NOW())
            RETURNING id";

        return await _db.ExecuteScalarAsync<Guid>(sql, paciente);
    }

    // REGLA: No existe DeleteAsync — solo DeactivateAsync
    public async Task<bool> DeactivateAsync(Guid id, Guid clinicaId)
    {
        const string sql = @"
            UPDATE pacientes SET activo = false, fecha_modificacion = NOW()
            WHERE id = @Id AND clinica_id = @ClinicaId";

        var rows = await _db.ExecuteAsync(sql, new { Id = id, ClinicaId = clinicaId });
        return rows > 0;
    }
}
```

### Estructura obligatoria de un API Controller

```csharp
namespace Vittal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PacientesController : ControllerBase
{
    private readonly IPacienteService _service;
    private readonly ILogger<PacientesController> _logger;

    public PacientesController(IPacienteService service, ILogger<PacientesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Obtiene todos los pacientes activos de la clínica</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PacienteResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var clinicaId = User.GetClinicaId();   // ExtensionMethod del JWT claim
        var result = await _service.GetAllAsync(clinicaId);
        return Ok(result);
    }

    /// <summary>Crea un nuevo paciente</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PacienteResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] PacienteRequestDto dto)
    {
        var clinicaId = User.GetClinicaId();
        var result = await _service.CreateAsync(dto, clinicaId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
```

### Respuestas estándar de la API

```csharp
// Siempre usar este wrapper para respuestas de la API
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

## 8. Base de Datos — Reglas Supabase / PostgreSQL

### Tipos de datos estándar

| Campo | Tipo PostgreSQL | Notas |
|---|---|---|
| IDs | `UUID` | `gen_random_uuid()` como default |
| Textos cortos | `VARCHAR(n)` | Definir longitud explícita |
| Textos largos | `TEXT` | Notas, descripciones, observaciones |
| Fechas | `TIMESTAMPTZ` | Siempre con timezone UTC |
| Booleanos | `BOOLEAN` | Para campo `activo` y flags |
| Decimales | `NUMERIC(10,2)` | Para valores monetarios o dosis |
| Enumerables | `VARCHAR(20)` | Estado, sexo, tipo |

### Campos obligatorios en TODA tabla de negocio

```sql
id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
clinica_id      UUID NOT NULL REFERENCES clinicas(id),
activo          BOOLEAN NOT NULL DEFAULT true,
fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
fecha_modificacion TIMESTAMPTZ
```

### Migración estándar (Supabase CLI)

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_create_pacientes.sql

CREATE TABLE IF NOT EXISTS pacientes (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    doctor_id           UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    primer_nombre       VARCHAR(100) NOT NULL,
    segundo_nombre      VARCHAR(100),
    primer_apellido     VARCHAR(100) NOT NULL,
    segundo_apellido    VARCHAR(100),
    email               VARCHAR(255),
    celular             VARCHAR(20),
    direccion           TEXT,
    sexo                VARCHAR(1) CHECK (sexo IN ('M', 'F')),
    foto_url            TEXT,                    -- Supabase Storage URL
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

-- Índices obligatorios
CREATE INDEX idx_pacientes_clinica_id ON pacientes(clinica_id);
CREATE INDEX idx_pacientes_doctor_id ON pacientes(doctor_id);
CREATE INDEX idx_pacientes_activo ON pacientes(activo);

-- RLS obligatorio
ALTER TABLE pacientes ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation" ON pacientes
    FOR ALL USING (clinica_id = (current_setting('app.current_clinica_id', true))::UUID);

-- Comentarios (en español, obligatorio)
COMMENT ON TABLE pacientes IS 'Registro de todos los pacientes del sistema por clínica';
COMMENT ON COLUMN pacientes.clinica_id IS 'Identificador del tenant (clínica) al que pertenece el paciente';
COMMENT ON COLUMN pacientes.activo IS 'Los pacientes no se eliminan, solo se desactivan';
```

---

## 9. Módulos con Comportamiento Especial

### Cola de Espera (HU18) — Tiempo Real

```
Tecnología:  Supabase Realtime + SignalR
Filtro:      Por doctor y fecha actual (día en transcurso)
Orden:       Por hora de cita ASC
Estados:     Agendada → En espera → En atención → Atendida / Cancelada
Botón:       "Atender" redirige al expediente y saca al paciente de la cola
Vista Admin: Filtro de doctores — ve todas las citas de la clínica
```

### Línea de Tiempo (HU19) — Tracking de Pasos

```
Muestra:    Salas y horas por las que pasó un paciente en el día
Filtro:     Por doctor y fecha actual
Propósito:  Control de tiempos de atención por etapa
```

### Expedientes (HU20) — Módulo Central

```
Estructura: 1 expediente por paciente → N hojas de cita
Hoja de cita contiene:
  - Diagnósticos (con tipo de diagnóstico)
  - Tratamientos y medicamentos (receta)
  - Exámenes y resultados
  - Cirugías (con tipo de cirugía)
  - Recomendaciones
  - Archivos adjuntos (PDF, imágenes → Supabase Storage)
Funciones:  Imprimir receta médica, imprimir epicrisis
            Enviar archivos por correo al paciente
            Mostrar foto del paciente
```

### Alertas Configurables (HU23) — Notificaciones Push

```
Tecnología:  SignalR hub + Supabase Realtime
Trigger:     Paciente excede tiempo de espera configurado en la clínica
Datos:       Área, nombre paciente, hora de cita, hora de llegada, doctor
Visibilidad: Todos los usuarios de la clínica ven las notificaciones
Config:      Tiempo de espera en minutos se define por clínica (HU09)
```

### Agenda (HU21) — Citas Médicas

```
Estados posibles: Agendada | Cancelada | Atendida | En espera | En atención
Requerimientos:   Buscador de pacientes, lugar de la cita, asignación a doctor
Filtro:           Por doctor (doctores ven solo sus citas)
Admin:            Vista global con filtro de doctores
```

---

## 10. Integración Supabase Storage

Para archivos de expedientes (PDFs, imágenes, resultados de exámenes):

```csharp
// Ejemplo de subida a Supabase Storage desde el API
public async Task<string> UploadExpedienteFileAsync(
    IFormFile file,
    Guid pacienteId,
    Guid clinicaId)
{
    var bucketName = "expedientes";
    var path = $"{clinicaId}/{pacienteId}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

    using var stream = file.OpenReadStream();
    var response = await _supabaseClient.Storage
        .From(bucketName)
        .Upload(stream, path, new FileOptions { ContentType = file.ContentType });

    return _supabaseClient.Storage.From(bucketName).GetPublicUrl(path);
}
```

### Bucket de Storage

```
expedientes/         ← Archivos de expedientes médicos (por clinica_id/paciente_id/)
avatares/            ← Fotos de perfil de pacientes y usuarios
```

---

## 11. Configuración del Proyecto

### appsettings.json (estructura base)

```json
{
  "ConnectionStrings": {
    "Supabase": "Host=db.xxxx.supabase.co;Database=postgres;Username=postgres;Password=xxxx;SSL Mode=Require"
  },
  "Supabase": {
    "Url": "https://xxxx.supabase.co",
    "AnonKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "ServiceRoleKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  },
  "Jwt": {
    "Secret": "xxxx",
    "Issuer": "vittal-api",
    "Audience": "vittal-client",
    "ExpirationHours": 8
  },
  "App": {
    "DefaultPageSize": 20,
    "MaxPageSize": 100,
    "Environment": "Development"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Inyección de dependencias (Vittal.IOC)

```csharp
// DependencyInjection.cs — registro de todos los servicios
public static class DependencyInjection
{
    public static IServiceCollection AddVittalServices(this IServiceCollection services)
    {
        // Repositorios
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<ICitaRepository, CitaRepository>();
        // ... todos los repositorios

        // Servicios BLL
        services.AddScoped<IPacienteService, PacienteService>();
        services.AddScoped<ICitaService, CitaService>();
        // ... todos los servicios

        return services;
    }
}
```

---

## 12. Reglas de Negocio Globales

Estas reglas aplican a **todos** los módulos sin excepción:

1. **Nunca eliminar registros.** Solo desactivar (`activo = false`). Aplica a: usuarios, perfiles, pacientes, medicamentos, cirugías, diagnósticos, salas, clínicas y cualquier catálogo.

2. **Todo ID es autogenerado.** Ningún código o ID visible puede ser modificado por el usuario. Se genera en la base de datos (`gen_random_uuid()`).

3. **Multi-tenant obligatorio.** Toda consulta a la BD debe incluir `clinica_id`. Sin excepciones.

4. **Permisos granulares.** Antes de cualquier operación CRUD en el API, verificar que el usuario tenga el permiso correspondiente (`READ`, `CREATE`, `UPDATE`) para el módulo.

5. **Solo el administrador es omnipotente.** Los usuarios con perfil `ADMIN` tienen todos los permisos de todos los módulos de su clínica. No del sistema global.

6. **Filtro por doctor.** Los módulos de Cola de Espera, Expedientes y Agenda muestran solo los datos del doctor autenticado, excepto para usuarios con perfil `ADMIN` o `GERENTE` que ven toda la clínica.

7. **Auditoría mínima obligatoria.** Todo registro tiene `fecha_creacion` y `fecha_modificacion`. En operaciones críticas agregar `creado_por` (UUID del usuario).

8. **Respuestas API consistentes.** Siempre usar `ApiResponse<T>`. Nunca retornar el modelo de Entity directamente — siempre usar DTOs.

9. **Validación en dos niveles.** FluentValidation en el servidor (BLL) y jQuery Validate en el cliente. No confiar solo en validación del lado cliente.

10. **Archivos médicos protegidos.** Los archivos de Supabase Storage están en buckets privados. Las URLs son generadas con tokens de acceso temporal, nunca públicas permanentes.

11. **Especialidad por Sala — discriminador `sala_id`.** Los catálogos médicos dinámicos (`tipos_antecedente`, `tipos_signo_vital`) usan `sala_id` como discriminador de especialidad. El `clinica_id` solo existe para RLS. **Nunca usar `clinica_id` como discriminador de especialidad médica.** Una misma clínica puede tener salas de distintas especialidades. El código nunca tiene campos hardcodeados de especialidades — todo se gestiona dinámicamente a través de los catálogos por sala.

12. **Plantillas de especialidad son globales del sistema.** Las tablas `plantillas_especialidad` y `plantilla_items` NO tienen `clinica_id` — pertenecen al sistema, no a ningún tenant. Solo el Super Admin puede administrarlas. Son el punto de partida para el onboarding de nuevas salas.

---

## 13. Estructura de Carpetas del Repositorio

```
vittal-sistema/
├── CLAUDE.md                    ← Este archivo (Archivo Maestro)
├── ORCHESTRATOR.md              ← Guía del agente PM orquestador
├── README.md                    ← Documentación general del proyecto
├── .gitignore
├── .github/
│   └── workflows/
│       └── ci-cd.yml            ← GitHub Actions pipeline
├── skills/
│   ├── skill-bll.md             ← Instrucciones para generar BLL
│   ├── skill-dal.md             ← Instrucciones para generar DAL
│   ├── skill-controller.md      ← Instrucciones para API Controllers
│   ├── skill-view.md            ← Instrucciones para Vistas Razor
│   └── skill-supabase.md        ← Instrucciones para migraciones SQL
├── supabase/
│   ├── config.toml              ← Configuración Supabase CLI
│   └── migrations/              ← Migraciones SQL versionadas
│       ├── 20240101000000_initial_schema.sql
│       ├── 20240101000001_create_clinicas.sql
│       ├── 20240101000002_create_usuarios.sql
│       └── ...
├── src/
│   ├── Vittal.Aplicacion/       ← Proyecto Frontend MVC
│   ├── Vittal.API/              ← Proyecto Backend Web API
│   ├── Vittal.BLL/              ← Business Logic Layer
│   ├── Vittal.DAL/              ← Data Access Layer
│   ├── Vittal.Entity/           ← Modelos de dominio
│   ├── Vittal.DTO/              ← Data Transfer Objects
│   ├── Vittal.IOC/              ← Inyección de dependencias
│   └── Vittal.Utility/          ← Utilidades compartidas
├── tests/
│   ├── Vittal.BLL.Tests/        ← Unit tests de servicios
│   └── Vittal.API.Tests/        ← Integration tests de endpoints
└── docs/
    ├── historias-de-usuario.md  ← Backlog completo
    ├── arquitectura.md          ← Diagramas de arquitectura
    └── api-docs.md              ← Documentación de endpoints
```

---

## 14. Guía Rápida para Agentes

### Cuando el @PM asigne una tarea de nuevo módulo, el flujo es:

```
@Arquitecto  → Define estructura N-capas del módulo (Entity, DTO, interfaces)
                Espera aprobación del @PM antes de implementar

@IngenieroDatos → Crea migración SQL en supabase/migrations/
                   Implementa Repository (DAL) con Dapper
                   Habilita RLS y crea políticas de tenant

@EspecialistaUI  → Implementa API Controller (Vittal.API)
                    Implementa BLL Service
                    Crea vistas Razor MVC (Vittal.Aplicacion/Areas/)
                    Conecta validaciones cliente

@PM          → Revisa integración, prueba flujo completo
                Actualiza lista de tareas compartida
```

### Checklist de entregable por módulo

Antes de marcar una tarea como completada, verificar:

- [ ] Migración SQL creada en `/supabase/migrations/` con `clinica_id` y RLS
- [ ] Entity creada en `Vittal.Entity/`
- [ ] DTOs de Request y Response creados en `Vittal.DTO/`
- [ ] Interface y Repository creados en `Vittal.DAL/`
- [ ] Interface y Service creado en `Vittal.BLL/`
- [ ] Endpoint documentado con Swagger en `Vittal.API/`
- [ ] Registros agregados en `Vittal.IOC/DependencyInjection.cs`
- [ ] Vistas Razor en el Área correspondiente de `Vittal.Aplicacion/`
- [ ] Validación FluentValidation en BLL
- [ ] Validación jQuery Validate en la vista
- [ ] Verificación de permisos en el Controller API
- [ ] Filtro por `clinica_id` en todas las consultas
- [ ] Campo `activo` respetado (no delete, solo deactivate)

---

## 15. Comandos de Referencia Rápida

```bash
# Ejecutar el proyecto API
dotnet run --project src/Vittal.API

# Ejecutar el proyecto Frontend
dotnet run --project src/Vittal.Aplicacion

# Aplicar migraciones de Supabase
supabase db push

# Crear nueva migración
supabase migration new nombre_de_la_migracion

# Ver estado de migraciones
supabase migration list

# Iniciar Supabase local (desarrollo)
supabase start

# Ejecutar tests
dotnet test

# Activar Agent Teams en Claude Code
export CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1
claude --teammate-mode in-process
```

---

*CLAUDE.md — Vittal v1.0.0 | Última actualización: 2026-05-12*
*Archivo Maestro del proyecto — cargado automáticamente por todos los agentes de Claude Code*
*v1.1 — Decisión arquitectónica: Especialidad por Sala (sala_id). Sprint 3.5 agregado. HU-E01 a HU-E07 incorporadas al backlog.*
