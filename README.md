# Vittal — Sistema Médico SaaS Multi-Tenant

> Plataforma integral para la gestión de citas, expedientes clínicos y operaciones médicas, diseñada para clínicas oftalmológicas bajo modelo SaaS.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC%20%2B%20Web%20API-512BD4?style=for-the-badge&logo=aspdotnetcore&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Supabase](https://img.shields.io/badge/Supabase-BaaS-3ECF8E?style=for-the-badge&logo=supabase&logoColor=white)](https://supabase.com/)
[![Dapper](https://img.shields.io/badge/ORM-Dapper-008080?style=for-the-badge)](https://github.com/DapperLib/Dapper)
[![Bootstrap](https://img.shields.io/badge/UI-Bootstrap%205.3-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![xUnit](https://img.shields.io/badge/Tests-87%20passing-2D8C3C?style=for-the-badge&logo=xunit&logoColor=white)](https://xunit.net/)
[![License](https://img.shields.io/badge/License-Private-red?style=for-the-badge)](#)

---

## 📋 Descripción

**Vittal** es una plataforma SaaS (Software as a Service) multi-tenant que centraliza toda la operación clínica de centros oftalmológicos: desde la gestión de pacientes y citas médicas, hasta expedientes clínicos completos con diagnósticos, tratamientos, cirugías, exámenes y resultados.

Cada clínica opera con **aislamiento total de datos** mediante Row Level Security (RLS) en PostgreSQL, mientras un **Super Admin Global** puede provisionar nuevas clínicas de forma automatizada.

### Cliente Principal

| Campo | Valor |
|-------|-------|
| **Cliente** | COA — Centro Oftalmológico Avanzado |
| **Tipo** | Clínica oftalmológica multi-sala |
| **Modelo** | SaaS + BaaS (Backend as a Service) |

---

## ✨ Características Principales

### 🏥 Gestión Clínica

| Módulo | Descripción |
|--------|-------------|
| **Pacientes** | Registro completo con datos personales, foto y expediente médico |
| **Agenda** | Programación de citas con estados: Agendada, En espera, En atención, Atendida, Cancelada |
| **Cola de Espera** | Vista en tiempo real del flujo de pacientes del día (SignalR) |
| **Línea de Tiempo** | Tracking de pasos del paciente por sala/área con timer en vivo |
| **Expedientes** | Hojas de cita con diagnósticos, tratamientos, cirugías, exámenes y archivos adjuntos |

### 📊 Analítica y Reportes

| Módulo | Descripción |
|--------|-------------|
| **Dashboard** | KPIs en tiempo real: pacientes del día, citas pendientes, tiempos de espera |
| **Reportes** | 4 tipos de reporte con filtros dinámicos, gráficos Chart.js y exportación CSV |
| **Alertas** | Notificaciones push configurables cuando se exceden tiempos de espera |

### ⚙️ Administración Multi-Tenant

| Módulo | Descripción |
|--------|-------------|
| **Super Admin** | Provisionamiento automatizado de nuevas clínicas con un solo endpoint |
| **Clínicas** | Gestión de tenants con configuración independiente |
| **Usuarios** | Creación, edición y asignación de perfiles y salas |
| **Permisos** | Sistema granular: READ, CREATE, UPDATE por módulo y perfil |
| **Salas** | Configuración de salas con especialidades médicas dinámicas |

### 📚 Catálogos Médicos

- **Medicamentos** · **Tipos de Cirugías** · **Cirugías**
- **Tipos de Diagnósticos** · **Diagnósticos** · **Tratamientos**
- **Recomendaciones** · **Exámenes** · **Plantillas de Especialidad**
- **Antecedentes por Sala** · **Signos Vitales por Sala**
- **Constancias Médicas**

---

## 🏗 Arquitectura

### Patrón: N-Capas (N-Tier) + MVC

Arquitectura estricta con separación de responsabilidades. Ninguna capa puede saltarse otra.

```
┌─────────────────────────────────────────────────────────────────┐
│                    Vittal.Aplicacion (MVC)                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌───────────────┐   │
│  │  Areas/  │  │Controllers│  │  Views/  │  │   wwwroot/    │   │
│  │ (10 áreas)│  │  (26)    │  │ (~95+)   │  │ JS + CSS      │   │
│  └────┬─────┘  └──────────┘  └──────────┘  └───────────────┘   │
└───────┼─────────────────────────────────────────────────────────┘
        │ HTTP
┌───────▼─────────────────────────────────────────────────────────┐
│                    Vittal.API (Web API)                         │
│  ┌──────────────┐  ┌────────────┐  ┌─────────────────────────┐ │
│  │ Controllers  │  │ SignalR    │  │ Auth (JWT + Supabase)   │ │
│  │    (36)      │  │ Hubs (2)   │  │ RequirePermission       │ │
│  └──────┬───────┘  └────────────┘  │ RequireSuperAdmin       │ │
└───────┼───────────────────────────┴───────────────────────────┘
        │
┌───────▼─────────────────────────────────────────────────────────┐
│                    Vittal.BLL (Business Logic)                  │
│  ┌──────────────────┐  ┌──────────────────────────────────────┐ │
│  │ Interfaces (35)  │  │ Services (35)                        │ │
│  │ IService         │  │ ServiceResult<T> + FluentValidation  │ │
│  └────────┬─────────┘  └──────────────────────────────────────┘ │
└─────────┼───────────────────────────────────────────────────────┘
          │
┌─────────▼───────────────────────────────────────────────────────┐
│                    Vittal.DAL (Data Access)                     │
│  ┌──────────────────┐  ┌──────────────────────────────────────┐ │
│  │ Interfaces (36)  │  │ Repositories (36)                    │ │
│  │ IRepository      │  │ Dapper + DbConnectionFactory         │ │
│  └────────┬─────────┘  └──────────────────────────────────────┘ │
└─────────┼───────────────────────────────────────────────────────┘
          │ SQL
┌─────────▼───────────────────────────────────────────────────────┐
│              Supabase (PostgreSQL 15 + BaaS)                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────────────┐  │
│  │ 31 Migs  │  │ RLS      │  │ Realtime │  │ Storage Buckets│  │
│  │ ~38 Tablas│  │ Policies │  │ Channels │  │ expedientes/   │  │
│  └──────────┘  └──────────┘  └──────────┘  │ avatares/      │  │
│                                             └────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Flujo de Datos

```
Vista Razor → MVC Controller → API Controller → BLL Service → DAL Repository → PostgreSQL
             ← DTO Response ← BLL ← DAL ← Entity ← PostgreSQL
```

### Principios Arquitectónicos

| Principio | Implementación |
|-----------|---------------|
| **Multi-Tenant** | `clinica_id` en toda tabla de negocio + RLS policies |
| **Soft Delete** | `activo = false` — nunca DELETE físico |
| **Tenant Isolation** | JWT claims + `TenantMiddleware` + RLS en BD |
| **Super Admin** | `RequireSuperAdminAttribute` con bypass de tenant |
| **Audit Trail** | `fecha_creacion`, `fecha_modificacion` en toda entidad |
| **UUID IDs** | `gen_random_uuid()` autogenerado en PostgreSQL |
| **API Responses** | Wrapper `ApiResponse<T>` consistente en todos los endpoints |
| **Validación** | FluentValidation (server) + jQuery Validate (client) |

---

## 📁 Estructura del Proyecto

```
vittal/
├── src/
│   ├── Vittal.Aplicacion/          # Frontend MVC (10 áreas, ~95 vistas)
│   │   ├── Areas/
│   │   │   ├── Administracion/     # Perfiles, Usuarios, Permisos, Salas
│   │   │   ├── Agenda/             # Citas médicas
│   │   │   ├── Alertas/            # Alertas configurables
│   │   │   ├── Catalogos/          # 12 catálogos médicos
│   │   │   ├── ColaEspera/         # Cola en tiempo real
│   │   │   ├── Dashboard/          # KPIs y gráficos
│   │   │   ├── Expedientes/        # Expediente clínico completo
│   │   │   ├── LineaTiempo/        # Timeline de pacientes
│   │   │   ├── Login/              # Autenticación
│   │   │   └── Reportes/           # Reportes y exportación
│   │   └── wwwroot/                # CSS, JS, assets
│   ├── Vittal.API/                 # Backend Web API (36 controllers + 2 hubs)
│   │   ├── Controllers/
│   │   ├── Hubs/                   # AlertasHub, LineaTiempoHub
│   │   ├── Authorization/          # RequirePermission, RequireSuperAdmin
│   │   ├── Extensions/             # ClaimsPrincipal, ServiceResult
│   │   └── Middleware/             # TenantMiddleware
│   ├── Vittal.BLL/                 # Business Logic Layer
│   │   ├── Interfaces/             # 35 service interfaces
│   │   └── Services/               # 35 service implementations
│   ├── Vittal.DAL/                 # Data Access Layer
│   │   ├── Interfaces/             # 36 repository interfaces
│   │   ├── Repositories/           # 36 repository implementations
│   │   └── Connections/            # DbConnectionFactory
│   ├── Vittal.Entity/              # Domain entities (38 classes)
│   ├── Vittal.DTO/                 # Data Transfer Objects (~75+ files)
│   ├── Vittal.IOC/                 # Dependency Injection (~76 registros)
│   └── Vittal.Utility/             # Shared helpers y extensiones
├── tests/
│   ├── Vittal.BLL.Tests/           # 66 unit tests (xUnit + Moq)
│   └── Vittal.API.Tests/           # 21 integration tests
├── supabase/
│   ├── config.toml
│   └── migrations/                 # 31 migraciones SQL versionadas
├── skills/                         # 29 archivos de instrucciones por capa
├── docs/                           # Documentación del proyecto
├── CLAUDE.md                       # Archivo maestro del proyecto
├── ORCHESTRATOR.md                 # Guía de orquestación de agentes
├── AGENTS.md                       # Reglas para agentes de IA
└── Vittal.sln                      # Solución .NET (10 proyectos)
```

---

## 🚀 Inicio Rápido

### Prerrequisitos

| Herramienta | Versión | Descripción |
|-------------|---------|-------------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0+ | Runtime y SDK |
| [Supabase CLI](https://supabase.com/docs/guides/cli) | Latest | Gestión de BD local |
| [Git](https://git-scm.com/) | 2.40+ | Control de versiones |

### 1. Clonar el repositorio

```bash
git clone <repository-url>
cd vittal
```

### 2. Configurar Supabase (local)

```bash
# Iniciar Supabase local
supabase start

# Aplicar migraciones
supabase db push
```

### 3. Configurar conexión

Crear `appsettings.Development.json` en `Vittal.API/` y `Vittal.Aplicacion/`:

```json
{
  "ConnectionStrings": {
    "Supabase": "Host=localhost;Database=postgres;Username=postgres;Password=<tu-password>;Port=54322"
  },
  "Supabase": {
    "Url": "http://localhost:54321",
    "AnonKey": "<tu-anon-key>",
    "ServiceRoleKey": "<tu-service-role-key>"
  },
  "Jwt": {
    "Secret": "<tu-jwt-secret>",
    "Issuer": "vittal-api",
    "Audience": "vittal-client",
    "ExpirationHours": 8
  }
}
```

### 4. Ejecutar la aplicación

```bash
# API Backend (puerto configurado en launchSettings.json)
dotnet run --project src/Vittal.API

# Frontend MVC (puerto configurado en launchSettings.json)
dotnet run --project src/Vittal.Aplicacion
```

### 5. Acceder al sistema

| Servicio | URL |
|----------|-----|
| **Frontend** | `https://localhost:<port>` |
| **API** | `https://localhost:<port>/api` |
| **Swagger** | `https://localhost:<port>/swagger` |
| **Supabase Studio** | `http://localhost:54323` |

---

## 🧪 Testing

```bash
# Ejecutar todos los tests
dotnet test

# Tests de BLL (66 tests)
dotnet test tests/Vittal.BLL.Tests/

# Tests de API (21 tests)
dotnet test tests/Vittal.API.Tests/

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

### Cobertura actual

| Proyecto | Tests | Framework | Estado |
|----------|-------|-----------|--------|
| Vittal.BLL.Tests | 66 | xUnit + Moq + FluentAssertions | ✅ Passing |
| Vittal.API.Tests | 21 | xUnit + Moq | ✅ Passing |
| **Total** | **87** | | ✅ **All passing** |

---

## 📊 Métricas del Proyecto

| Métrica | Valor |
|---------|-------|
| **Proyectos en solución** | 10 |
| **Migraciones SQL** | 31 |
| **Tablas de negocio** | ~38 + 2 buckets Storage |
| **Entities** | 38 |
| **DTOs** | ~75+ archivos en 36 carpetas |
| **Interfaces DAL** | 36 |
| **Repositorios DAL** | 36 |
| **Servicios BLL** | 35 |
| **Controllers API** | 36 |
| **SignalR Hubs** | 2 |
| **Controllers MVC** | 26 |
| **Áreas MVC** | 10 |
| **Vistas Razor** | ~95+ |
| **Registros DI** | ~76 |
| **Tests unitarios** | 87 |
| **Build** | 0 errores, 0 warnings |
| **Violaciones críticas** | 0 |

---

## 🔐 Modelo de Seguridad

### Autenticación

```
Usuario → Supabase Auth → JWT Token → API Request → Claims Extraídos
```

El JWT contiene: `user_id`, `clinica_id`, `perfil_id`, `permisos[]`, `es_super_admin`

### Autorización

| Filtro | Propósito |
|--------|-----------|
| `[Authorize]` | Requiere JWT válido |
| `[RequirePermission("modulo", PermissionType.Read)]` | Verifica permiso granular |
| `[RequireSuperAdmin]` | Acceso exclusivo de Super Admin Global |

### Aislamiento Multi-Tenant

```
┌─────────────────────────────────────────────┐
│              Tenant Isolation               │
├──────────────┬──────────────────────────────┤
│ Application  │ TenantMiddleware + JWT claims│
│ Database     │ RLS policies (clinica_id)    │
│ API          │ clinicaId extraído del JWT   │
│ Super Admin  │ Bypass de tenant isolation   │
└──────────────┴──────────────────────────────┘
```

### Permisos Granulares

| Permiso | Código | Operaciones |
|---------|--------|-------------|
| Leer | `READ` | GET — Visualizar listados y registros |
| Crear | `CREATE` | POST — Insertar nuevos registros |
| Actualizar | `UPDATE` | PUT/PATCH — Editar registros existentes |

> **No existe eliminación física.** Los registros se desactivan con `activo = false`.

---

## 📡 API Endpoints

La API REST está documentada con **Swagger/OpenAPI 3.0**. Accede a `/swagger` para la documentación interactiva.

### Endpoints principales

| Recurso | GET | POST | PUT/PATCH |
|---------|-----|------|-----------|
| `/api/pacientes` | Listar pacientes | Crear paciente | — |
| `/api/citas` | Listar citas | Crear cita | Actualizar cita |
| `/api/expedientes` | Listar expedientes | Crear expediente | Actualizar |
| `/api/dashboard/data` | KPIs del día | — | — |
| `/api/linea-tiempo/dia` | Timeline del día | Iniciar paso | Finalizar/Saltar |
| `/api/alertas` | Alertas activas | Resolver alerta | — |
| `/api/notificaciones` | Notificaciones | — | Marcar leída |
| `/api/reportes` | Historial reportes | Generar reporte | Exportar CSV |
| `/api/admin/provision` | — | Provisionar clínica | — |

### SignalR Hubs

| Hub | Ruta | Propósito |
|-----|------|-----------|
| `AlertasHub` | `/hubs/alertas` | Notificaciones push de alertas de espera |
| `LineaTiempoHub` | `/hubs/linea-tiempo` | Actualizaciones en vivo del timeline |

---

## 🗄 Base de Datos

### Motor: PostgreSQL 15 (Supabase)

| Característica | Detalle |
|----------------|---------|
| **IDs** | UUID con `gen_random_uuid()` |
| **Tenant** | `clinica_id UUID NOT NULL` en toda tabla de negocio |
| **Soft Delete** | `activo BOOLEAN NOT NULL DEFAULT true` |
| **Timestamps** | `TIMESTAMPTZ` (UTC) |
| **RLS** | Habilitado en todas las tablas de negocio |
| **Migraciones** | 31 migraciones versionadas en `/supabase/migrations/` |

### Campos obligatorios en toda tabla de negocio

```sql
id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
clinica_id          UUID NOT NULL REFERENCES clinicas(id),
activo              BOOLEAN NOT NULL DEFAULT true,
fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
fecha_modificacion  TIMESTAMPTZ
```

### Migraciones

```bash
# Ver estado de migraciones
supabase migration list

# Aplicar migraciones pendientes
supabase db push

# Crear nueva migración
supabase migration new nombre_de_la_migracion
```

---

## 🤖 Desarrollo con Agentes de IA

Este proyecto está configurado para desarrollo orquestado con **Claude Code Agent Teams**:

| Rol | Agente | Responsabilidad |
|-----|--------|-----------------|
| **@PM** | Director de Proyecto | Orquestación, asignación, QA |
| **@Arquitecto** | Arquitecto de Software | Entity, DTO, interfaces, IOC |
| **@IngenieroDatos** | Ingeniero de Datos | Migraciones SQL, RLS, Repositories |
| **@EspecialistaUI** | Especialista UI/UX | BLL Services, API Controllers, Views |

### Archivos de configuración

| Archivo | Propósito |
|---------|-----------|
| `CLAUDE.md` | Archivo maestro con contexto completo del proyecto |
| `ORCHESTRATOR.md` | Guía de orquestación, roles y flujos de trabajo |
| `AGENTS.md` | Reglas y convenciones para agentes de IA |
| `skills/` | 29 archivos de instrucciones por capa técnica |

---

## 📚 Documentación Adicional

| Recurso | Descripción |
|---------|-------------|
| [CLAUDE.md](./CLAUDE.md) | Archivo maestro — contexto, convenciones, reglas de negocio |
| [ORCHESTRATOR.md](./ORCHESTRATOR.md) | Guía de orquestación del equipo de agentes |
| [AGENTS.md](./AGENTS.md) | Reglas de desarrollo para agentes de IA |
| [skills/](./skills/) | Instrucciones detalladas por capa (BLL, DAL, Controller, View, Supabase) |
| [docs/](./docs/) | Documentación adicional del proyecto |
| Swagger UI | Documentación interactiva de la API (al ejecutar el proyecto) |

---

## 📄 Licencia

**Propiedad privada** — Todos los derechos reservados.

Este software es propiedad del **Centro Oftalmológico Avanzado (COA)**. Su uso, distribución y modificación están restringidos a los términos del contrato de desarrollo establecido.

---

## 👥 Equipo de Desarrollo

| Rol | Descripción |
|-----|-------------|
| **Cliente** | COA — Centro Oftalmológico Avanzado |
| **Desarrollo** | Claude Code Agent Teams (IA orquestada) |
| **Arquitectura** | N-Tier + MVC + Repository Pattern |
| **Base de Datos** | Supabase (PostgreSQL 15) |

---

<p align="center">
  <strong>Vittal v1.2.0</strong> — Sistema Médico SaaS Multi-Tenant<br>
  <em>Desarrollado con .NET 8, Supabase y arquitectura N-Tier</em><br>
  <sub>Última actualización: Mayo 2026</sub>
</p>
