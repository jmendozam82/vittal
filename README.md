# Vittal — Sistema Médico SaaS Multi-Tenant

> Plataforma integral de gestión clínica para centros médicos, diseñada bajo modelo SaaS con aislamiento total de datos por tenant.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC_%2B_Web_API-512BD4?style=for-the-badge&logo=aspdotnetcore&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Supabase](https://img.shields.io/badge/Supabase-BaaS-3ECF8E?style=for-the-badge&logo=supabase&logoColor=white)](https://supabase.com/)
[![Dapper](https://img.shields.io/badge/ORM-Dapper-008080?style=for-the-badge)](https://github.com/DapperLib/Dapper)
[![Bootstrap](https://img.shields.io/badge/UI-Bootstrap_5.3-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![License](https://img.shields.io/badge/Licencia-Privada-red?style=for-the-badge)](#licencia)

---

## Descripción

**Vittal** es una plataforma **SaaS (Software as a Service) multi-tenant** que centraliza toda la operación clínica de centros médicos: gestión de pacientes, agenda de citas, expedientes clínicos completos, diagnósticos, tratamientos, cirugías, reportes analíticos y notificaciones en tiempo real.

Cada clínica opera con **aislamiento total de datos** mediante Row Level Security (RLS) en PostgreSQL. La plataforma está diseñada para ser adoptada por múltiples clínicas como servicio y expone su API como BaaS para integraciones externas.

---

## Características del Sistema

### Gestión Clínica

| Módulo | Descripción |
|--------|-------------|
| **Pacientes** | Registro completo con datos personales, documento de identificación y foto |
| **Agenda** | Gestión de citas médicas con estados: Agendada, En espera, En atención, Atendida, Cancelada |
| **Cola de Espera** | Vista en tiempo real del flujo de pacientes del día |
| **Línea de Tiempo** | Seguimiento de pasos del paciente por sala con control de tiempos |
| **Expedientes Clínicos** | Hojas de cita con diagnósticos, tratamientos, cirugías, exámenes, recomendaciones y archivos adjuntos |
| **Constancias Médicas** | Generación de constancias a partir del expediente del paciente |

### Catálogos Médicos

| Catálogo | Descripción |
|----------|-------------|
| **Medicamentos** | Catálogo de medicamentos utilizados en tratamientos |
| **Tipos de Cirugías / Cirugías** | Clasificación y registro de procedimientos quirúrgicos |
| **Tipos de Diagnósticos / Diagnósticos** | Clasificación diagnóstica por sala y especialidad |
| **Tratamientos** | Protocolos de tratamiento por diagnóstico |
| **Recomendaciones** | Indicaciones médicas estandarizadas |
| **Exámenes** | Registro de exámenes clínicos y resultados |
| **Antecedentes por Sala** | Catálogo de antecedentes configurado por especialidad |
| **Signos Vitales por Sala** | Tipos de signos vitales según la especialidad de la sala |
| **Plantillas de Especialidad** | Plantillas globales del sistema para onboarding rápido de nuevas salas |

### Analítica y Reportes

| Módulo | Descripción |
|--------|-------------|
| **Dashboard** | Indicadores clave en tiempo real: pacientes del día, citas pendientes, tiempos de espera |
| **Reportes** | Reportes con filtros dinámicos y exportación |
| **Alertas Configurables** | Notificaciones automáticas cuando se exceden tiempos de espera por clínica |

### Administración Multi-Tenant

| Módulo | Descripción |
|--------|-------------|
| **Clínicas** | Gestión de tenants con configuración independiente por organización |
| **Salas / Áreas** | Configuración de salas con especialidades médicas dinámicas |
| **Usuarios** | Creación, edición y asignación de perfiles y salas |
| **Perfiles** | Definición de roles y alcance de acceso |
| **Permisos** | Control granular de permisos por módulo y perfil |
| **Provisionamiento** | Creación automatizada de nuevas clínicas (Super Admin) |

### Landing Page Pública

Página informativa para prospectos y futuros socios de la plataforma, con formulario de contacto integrado.

---

## Arquitectura

### Patrón: N-Capas (N-Tier) + MVC

La solución implementa una arquitectura de N-capas estricta con separación de responsabilidades entre proyectos independientes. El patrón MVC se aplica en la capa de presentación y el patrón Repositorio en el acceso a datos.

```
┌──────────────────────────────────────────────────────────────┐
│                   Vittal.Aplicacion (MVC)                    │
│          11 Áreas · Controllers MVC · Vistas Razor           │
└───────────────────────────┬──────────────────────────────────┘
                            │ HTTP / JSON
┌───────────────────────────▼──────────────────────────────────┐
│                    Vittal.API (Web API)                      │
│       42 Controllers REST · 2 Hubs SignalR · Middleware      │
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│               Vittal.BLL (Business Logic Layer)              │
│              40 Interfaces · 40 Services · 41 Validators     │
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│               Vittal.DAL (Data Access Layer)                 │
│           40 Interfaces · 39 Repositories · Dapper           │
└───────────────────────────┬──────────────────────────────────┘
                            │ SQL (Npgsql)
┌───────────────────────────▼──────────────────────────────────┐
│              Supabase — PostgreSQL 15 + BaaS                 │
│     42 Migraciones · RLS · Realtime · Storage Buckets        │
└──────────────────────────────────────────────────────────────┘
```

### Flujo de Datos

```
Vista Razor → Controller MVC → Controller API → BLL Service → DAL Repository → PostgreSQL
             ← DTO Response  ← BLL            ← DAL         ← Entity        ← PostgreSQL
```

**Regla fundamental:** Ninguna capa puede saltarse otra. El Controller no llama directamente al DAL. La Vista no accede al BLL. El DAL no contiene lógica de negocio.

### Principios de Diseño

| Principio | Descripción |
|-----------|-------------|
| **Multi-Tenant** | `clinica_id` en toda tabla de negocio + Row Level Security |
| **Soft Delete** | Los registros se desactivan (`activo = false`), nunca se eliminan físicamente |
| **Aislamiento de Datos** | Cada tenant opera en un espacio de datos completamente aislado |
| **Especialidad por Sala** | Catálogos médicos configurados por sala, no por clínica (discriminador `sala_id`) |
| **Auditoría** | `fecha_creacion`, `fecha_modificacion` en toda entidad del sistema |
| **UUIDs autogenerados** | Ningún ID puede ser asignado manualmente — se genera en la base de datos |
| **Respuestas consistentes** | Wrapper `ApiResponse<T>` estándar en todos los endpoints REST |
| **Validación en dos niveles** | FluentValidation en servidor + jQuery Validate en cliente |

---

## Estructura del Proyecto

```
vittal/
├── src/
│   ├── Vittal.Aplicacion/          # Frontend MVC — 11 Áreas, vistas Razor
│   │   └── Areas/
│   │       ├── Landing/            # Página pública informativa
│   │       ├── Login/              # Autenticación
│   │       ├── Administracion/     # Usuarios, Perfiles, Permisos, Salas
│   │       ├── Catalogos/          # 12 catálogos médicos
│   │       ├── Expedientes/        # Expediente clínico completo
│   │       ├── Agenda/             # Gestión de citas
│   │       ├── ColaEspera/         # Cola de pacientes en tiempo real
│   │       ├── LineaTiempo/        # Timeline de atención del día
│   │       ├── Dashboard/          # KPIs e indicadores
│   │       ├── Reportes/           # Generación y exportación de reportes
│   │       └── Alertas/            # Configuración de alertas
│   │
│   ├── Vittal.API/                 # Backend Web API REST
│   │   ├── Controllers/            # 42 controllers documentados con Swagger
│   │   ├── Hubs/                   # SignalR: AlertasHub, LineaTiempoHub
│   │   ├── Authorization/          # Atributos de control de acceso
│   │   ├── Middleware/             # Middleware de contexto de tenant
│   │   ├── Extensions/             # Extensiones de claims y resultados
│   │   └── Services/               # Servicios de infraestructura y background
│   │
│   ├── Vittal.BLL/                 # Capa de Lógica de Negocio
│   │   ├── Interfaces/             # 40 interfaces de servicios
│   │   ├── Services/               # 40 implementaciones
│   │   └── Validators/             # 41 validadores FluentValidation
│   │
│   ├── Vittal.DAL/                 # Capa de Acceso a Datos
│   │   ├── Interfaces/             # 40 interfaces de repositorios
│   │   ├── Repositories/           # 39 implementaciones con Dapper
│   │   ├── Context/                # Fábrica de conexiones y type handlers
│   │   └── Exceptions/             # Excepciones de acceso a datos
│   │
│   ├── Vittal.Entity/              # Entidades del dominio (40 modelos)
│   ├── Vittal.DTO/                 # Data Transfer Objects (Request / Response)
│   ├── Vittal.IOC/                 # Registro centralizado de dependencias
│   └── Vittal.Utility/             # Utilidades compartidas: ServiceResult, PermissionType
│
├── tests/
│   ├── Vittal.BLL.Tests/           # Tests unitarios de servicios (xUnit + Moq)
│   └── Vittal.API.Tests/           # Tests de integración de endpoints
│
├── supabase/
│   ├── config.toml                 # Configuración Supabase CLI
│   └── migrations/                 # 42 migraciones SQL versionadas
│
├── skills/                         # Instrucciones técnicas por capa
├── docs/                           # Documentación técnica del proyecto
├── Dockerfile.api                  # Imagen Docker para el API
├── Dockerfile.web                  # Imagen Docker para el Frontend
└── Vittal.sln                      # Solución .NET — 8 proyectos
```

---

## Requisitos Previos

| Herramienta | Versión | Uso |
|-------------|---------|-----|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0 o superior | Runtime y compilación |
| [Supabase CLI](https://supabase.com/docs/guides/cli) | Última estable | Gestión de base de datos y migraciones |
| [Git](https://git-scm.com/) | 2.40 o superior | Control de versiones |
| [Visual Studio Code](https://code.visualstudio.com/) | Última | IDE recomendado con C# Dev Kit |

---

## Configuración del Entorno de Desarrollo

### 1. Clonar el repositorio

```bash
git clone <repository-url>
cd vittal
```

### 2. Inicializar la base de datos local

```bash
# Iniciar los servicios de Supabase en modo local
supabase start

# Aplicar todas las migraciones al entorno local
supabase db push
```

### 3. Configurar variables de entorno

Crear el archivo `appsettings.Development.json` dentro de `src/Vittal.API/` con las credenciales del entorno local de Supabase. Consultar con el equipo el archivo de ejemplo `.env.example` o la guía en `docs/configuracion.md`.

> **Importante:** No incluir credenciales reales en el repositorio. Utilizar únicamente las claves del entorno local de Supabase (`supabase start` las genera automáticamente).

### 4. Ejecutar los proyectos

```bash
# Levantar el API Backend
dotnet run --project src/Vittal.API

# Levantar el Frontend MVC (en otra terminal)
dotnet run --project src/Vittal.Aplicacion
```

### 5. Verificar los servicios

Una vez en ejecución, los servicios estarán disponibles según la configuración de `launchSettings.json` de cada proyecto:

| Servicio | Descripción |
|----------|-------------|
| **Frontend** | Interfaz web del sistema |
| **API REST** | Backend con documentación Swagger en `/swagger` |
| **Supabase Studio** | Administrador de base de datos (entorno local) |

---

## Tests

```bash
# Ejecutar la suite completa de tests
dotnet test

# Ejecutar únicamente tests unitarios de BLL
dotnet test tests/Vittal.BLL.Tests/

# Ejecutar únicamente tests de integración de API
dotnet test tests/Vittal.API.Tests/

# Generar reporte de cobertura de código
dotnet test --collect:"XPlat Code Coverage"
```

| Suite | Framework | Módulos cubiertos |
|-------|-----------|-------------------|
| `Vittal.BLL.Tests` | xUnit + Moq + FluentAssertions | Admin, AlertaEspera, Cita, Dashboard, Expediente, LineaTiempo, Paciente, Reporte, Usuario |
| `Vittal.API.Tests` | xUnit + Moq | Integration tests de endpoints |

---

## Base de Datos

### Motor: PostgreSQL 15 vía Supabase

| Característica | Implementación |
|----------------|---------------|
| **Identificadores** | UUID generado automáticamente por la base de datos |
| **Aislamiento de tenant** | Campo `clinica_id` en toda tabla de negocio |
| **Borrado lógico** | Campo `activo BOOLEAN` — los registros nunca se eliminan físicamente |
| **Timestamps** | `TIMESTAMPTZ` en UTC para todas las fechas |
| **Seguridad a nivel fila** | Row Level Security habilitado en todas las tablas |
| **Migraciones** | 42 migraciones versionadas en `/supabase/migrations/` |

### Gestión de migraciones

```bash
# Consultar el estado de migraciones aplicadas
supabase migration list

# Aplicar migraciones pendientes al entorno conectado
supabase db push

# Crear una nueva migración
supabase migration new <nombre_descriptivo>
```

---

## API REST

La API está completamente documentada con **Swagger / OpenAPI 3.0**. Al ejecutar el proyecto en modo desarrollo, acceder a `/swagger` para la documentación interactiva con todos los endpoints, esquemas de request/response y posibilidad de pruebas directas.

### Recursos principales

| Área | Recursos expuestos |
|------|--------------------|
| **Autenticación** | Login, refresh, perfil propio |
| **Administración** | Clínicas, usuarios, perfiles, permisos, salas |
| **Catálogos** | Pacientes, medicamentos, cirugías, diagnósticos, tratamientos, exámenes, recomendaciones |
| **Especialidades** | Plantillas, tipos de antecedente, tipos de signo vital |
| **Expedientes** | Expedientes, hojas de cita, archivos adjuntos, constancias |
| **Operaciones** | Citas, cola de espera, línea de tiempo, alertas, notificaciones |
| **Analítica** | Dashboard, reportes |
| **Landing** | Formulario de contacto |

### Tiempo Real (SignalR)

| Hub | Propósito |
|-----|-----------|
| **AlertasHub** | Notificaciones push cuando se exceden tiempos de espera configurados |
| **LineaTiempoHub** | Actualizaciones en vivo del timeline de atención del día |

---

## Modelo de Seguridad

El sistema implementa múltiples capas de seguridad complementarias:

- **Autenticación:** JWT emitido por Supabase Auth, validado en cada request
- **Autorización granular:** Permisos de lectura, creación y actualización verificados por módulo y perfil
- **Aislamiento de tenant:** Cada request opera únicamente sobre los datos del tenant autenticado
- **Seguridad a nivel de base de datos:** Row Level Security como segunda línea de defensa
- **Sin eliminación física:** Los datos nunca se borran, solo se desactivan — garantizando trazabilidad completa
- **Auditoría:** Registro de creación y modificación en cada entidad del sistema

> Para detalles de configuración de seguridad, consultar la documentación interna en `docs/`.

---

## Métricas del Sistema

| Métrica | Valor actual |
|---------|-------------|
| Proyectos en solución | 8 |
| Migraciones SQL | 42 |
| Entidades de dominio | 40 |
| Interfaces DAL | 40 |
| Repositorios DAL | 39 (+1 interfaz base genérica) |
| Interfaces BLL | 40 |
| Servicios BLL | 40 |
| Validadores FluentValidation | 41 |
| Controllers API REST | 42 |
| Hubs SignalR | 2 |
| Áreas MVC | 11 |
| Historias de usuario completadas | 29 / 29 |
| Estado del build | ✅ Sin errores ni warnings |
| Violaciones arquitectónicas | 0 |

---

## Documentación

| Recurso | Descripción |
|---------|-------------|
| `docs/CLAUDE.md` | Archivo maestro del proyecto — contexto, arquitectura y reglas de negocio |
| `docs/ORCHESTRATOR.md` | Guía de orquestación del equipo de desarrollo |
| `docs/AGENTS.md` | Convenciones y reglas de desarrollo |
| `docs/configuracion.md` | Guía de configuración del entorno |
| `skills/` | Instrucciones técnicas por capa (BLL, DAL, Controller, View, Supabase) |
| Swagger UI | Documentación interactiva de la API (disponible al ejecutar el proyecto) |

---

## Despliegue con Docker

El repositorio incluye `Dockerfile.api` y `Dockerfile.web` para construir imágenes de contenedor del API y del Frontend respectivamente.

Consultar la documentación de despliegue en `docs/` para instrucciones de configuración de variables de entorno en producción.

---

## Licencia

**Propiedad privada — Todos los derechos reservados.**

Este software es propiedad de **MedicCore**. Su uso, distribución y modificación están restringidos a los términos del contrato de desarrollo vigente. Queda prohibida su reproducción total o parcial sin autorización expresa y por escrito.

---

<p align="center">
  <strong>Vittal v1.0.0</strong> — Sistema Médico SaaS Multi-Tenant<br>
  <em>Construido con .NET 8 · Supabase · PostgreSQL 15 · Arquitectura N-Tier</em><br>
  <sub>Última actualización: Julio 2026</sub>
</p>
