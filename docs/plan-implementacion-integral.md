# Plan de Implementación Integral — Vittal Sistema Médico

> **Fecha:** 2026-07-05 (v4.0 — todas las fases completadas, solo queda opcional)
> **Propósito:** Documentar el plan de implementación para los módulos pendientes del sistema Vittal.
> **Auditoría:** Se realizó revisión exhaustiva de código (Entity, DTO, DAL, BLL, API, MVC, Sidebar, DI)
> para determinar el estado REAL de cada componente.
> **Estado actual:** **TODAS LAS FASES (0, 1, 2, 3, 4) ESTÁN COMPLETADAS.** El plan de implementación integral ha sido ejecutado en su totalidad.
> **Bugfix aplicado:** Se corrigió el mapeo Dapper en `PlantillaItemRepository` (SELECT * sin aliases → columnas explícitas con AS).
> **Contexto:** El sistema Vittal tiene **implementados y funcionales** la gran mayoría de los módulos
> (HU07 al HU23, HU-E01 al HU-E07). Este plan es ahora un documento histórico de referencia arquitectónica.

---

## Índice

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [Estado Real del Sistema — Auditoría Completa](#2-estado-real-del-sistema--auditoría-completa)
3. [Arquitectura de Especialidad por Sala](#3-arquitectura-de-especialidad-por-sala)
4. [Análisis de Tablas del Sistema](#4-análisis-de-tablas-del-sistema)
5. [Fase 0-1: CRUD de Plantillas — COMPLETADO](#5-8-fase-0-1-crud-de-plantillas--completado)
6. [Fase 2: Endpoint "Aplicar Plantilla a Sala" — COMPLETADO](#9--fase-2-hu-e02-endpoint-aplicar-plantilla-a-sala--completado)
7. [Fase 3: HU06 Asignar Doctores a Salas — COMPLETADO](#10--fase-3-hu06-asignar-doctores-a-salas--completado)
8. [Correcciones Técnicas — APLICADAS](#11--correcciones-técnicas--aplicadas)
9. [⏸️ Visión Futura: Vista Unificada de Detalle de Sala](#12--visión-futura-vista-unificada-de-detalle-de-sala)
10. [Flujo de Cambio de Especialidad de una Sala](#13-flujo-de-cambio-de-especialidad-de-una-sala)
11. [Estado de Implementación](#14-estado-de-implementación-ya-no-hay-orden-pendiente)
12. [Anexos](#15-anexos)

---

## 1. Resumen Ejecutivo

### ¿Qué estamos construyendo?

Vittal es un sistema médico SaaS multi-tenant. La **gran mayoría del sistema ya está implementada y funcional**.

### Resultado de la auditoría

Se revisaron **cada uno de los archivos** del módulo de Plantillas de Especialidad. Se encontraron **7 issues críticos** que impedían que el Super Admin gestione plantillas desde la UI. **Todos estos issues han sido resueltos.**

### Estado del seed de plantillas

El **seed de items de plantillas YA ESTÁ COMPLETO**. La migración `20260512000002_create_plantillas_especialidad.sql` contiene un bloque `DO $$` que inserta **243 items** distribuidos en las 8 especialidades.

### Resumen de lo implementado (Fases 0-3 completadas ✅)

| # | Feature | Estado | Tiempo invertido |
|---|---|---|---|
| 0 | **Corrección permiso SalasController** → `[RequirePermission("areas")]` | ✅ **Completado** | 5 min |
| 1 | **BLL PlantillaEspecialidadService**: Create/Update/Deactivate/Reactivate | ✅ **Completado** | 30 min |
| 2 | **API PlantillaEspecialidadController**: Routing correcto + reactivar | ✅ **Completado** | 20 min |
| 3 | **DTO Request**: Lista de Items agregada | ✅ **Completado** | 10 min |
| 4 | **CRUD PlantillaItem**: API + BLL + DAL para items individuales | ✅ **Completado** | 2 hr |
| 5 | **UI Items en Edit.cshtml**: Gestión visual completa con modal JS | ✅ **Completado** | 2 hr |
| 6 | **Sidebar + Permisos**: Entrada en menú Administración | ✅ **Completado** | 30 min |
| 7 | **Endpoint "Aplicar Plantilla a Sala"** | ✅ **Completado** | 3 hr |
| 8 | **HU06 — Asignar Doctores a Salas** | ✅ **Completado** | 6 hr |
| — | **Bugfix**: Mapeo Dapper snake_case en PlantillaItemRepository | ✅ **Corregido** | — |
| 9 | **Vista unificada de detalle de Sala** | ✅ **Completado** | ~4 hr |

**Inversión total: ~18 hr** | **TODAS LAS FASES COMPLETADAS**

---

## 2. Estado Real del Sistema — Auditoría Completa

### ✅ Módulos 100% implementados y funcionales

| Área | Módulos incluidos | HU |
|---|---|---|
| **Login** | Auth | HU02 |
| **Administración** | Perfiles, Permisos, Usuarios, Salas | HU03, HU04, HU05, HU10 |
| **Catálogos** | Pacientes, Medicamentos, Tipos de Cirugía, Cirugías, Tipos de Diagnóstico, Diagnósticos, Tratamientos, Recomendaciones, Exámenes, **Tipos de Antecedente**, **Tipos de Signo Vital** | HU07, HU08, HU11-HU17, **HU-E03**, **HU-E04** |
| **Agenda** | Gestión de citas | HU21 |
| **Cola de Espera** | Cola de espera con estados | HU18 |
| **Línea de Tiempo** | Tracking de pasos del paciente | HU19 |
| **Expedientes** | Expedientes, Hojas de Cita, Antecedentes del Paciente, Signos Vitales por Consulta, Constancias, Archivos adjuntos | HU20, HU-E05, HU-E06, HU-E07 |
| **Dashboard** | KPIs y gráficos | HU23 |
| **Reportes** | Generación de reportes | HU22 |
| **Alertas** | Notificaciones en tiempo real | HU23 |

**Total: ~26 controllers MVC + ~56 vistas Razor + 37 API Controllers + 39 tablas BD**

### ✅ Módulo Plantillas de Especialidad — COMPLETO (HU-E02)

El módulo de Plantillas está **100% funcional**. El Super Admin puede:
- ✅ **Crear** plantillas con items desde la UI
- ✅ **Editar** plantillas y sus items
- ✅ **Desactivar/Reactivar** plantillas
- ✅ **Aplicar** plantilla a una sala (endpoint probado con 13 items creados)
- ✅ Acceder desde el **menú lateral** de Administración

### 🔧 Bugfix aplicado — Mapeo Dapper PlantillaItemRepository

**Problema:** El endpoint `POST /api/Salas/{salaId}/aplicar-plantilla/{plantillaId}` reportaba 0 items procesados.

**Causa raíz:** `PlantillaItemRepository` usaba `SELECT *` sin aliases SQL. Dapper no puede mapear columnas snake_case (`tipo_item`) a propiedades PascalCase (`TipoItem`) sin `[Column]` attributes o aliases explícitos.

**Solución:** Se reemplazó `SELECT *` por columnas explícitas con aliases (`tipo_item AS TipoItem`, etc.) en las 2 consultas del repositorio, consistente con el patrón usado en `SalaRepository.cs` y todos los demás repositorios del proyecto.

**Resultado:** `POST /api/Salas/{salaId}/aplicar-plantilla/{plantillaId}` ahora procesa correctamente los 13 items de la plantilla Medicina General (6 antecedentes + 7 signos vitales), con idempotencia garantizada. ✅

---

## 3. Arquitectura de Especialidad por Sala

### Principio fundamental

Una clínica puede tener múltiples salas con **distintas especialidades médicas**. Los catálogos de antecedentes y signos vitales se configuran **por sala**, no por clínica.

```
Ejemplo real:
  Clínica MedicCore
  ├── Sala 1 = Medicina General → antecedentes: HTA, Diabetes, Cirugía previa
  ├── Sala 2 = Cardiología      → antecedentes: HTA, Diabetes, IAM previo, Tabaquismo
  └── Sala 3 = Dermatología     → antecedentes: Alergias cutáneas, Psoriasis, Acné
```

### Discriminadores

| Campo | Propósito | Aplica en |
|---|---|---|
| `sala_id` | Discriminador de **especialidad** | `tipos_antecedente`, `tipos_signo_vital`, `antecedentes_paciente`, `signos_vitales_hoja` |
| `clinica_id` | Discriminador de **tenant** (RLS) | Todas las tablas de negocio |

**Regla absoluta:** `sala_id` define la especialidad. `clinica_id` define el aislamiento de datos. **Nunca usar `clinica_id` como discriminador de especialidad.**

---

## 4. Análisis de Tablas del Sistema

### Propósito de cada tabla y su rol en el flujo

#### Tablas Globales del Sistema (sin `clinica_id`)

| Tabla | Propósito | ¿Quién gestiona? |
|---|---|---|
| `plantillas_especialidad` | Catálogo global de especialidades médicas que definen qué datos médicos se recogen por especialidad | Solo **Super Admin** |
| `plantilla_items` | Items predefinidos (antecedentes + signos vitales) que componen cada plantilla. Ej: HTA, Frecuencia Cardíaca, etc. | Solo **Super Admin** |

Estas tablas **NO tienen `clinica_id`** porque son plantillas del sistema, no datos de tenant.

#### Tablas por Sala (con `clinica_id` + `sala_id`)

| Tabla | Propósito | Relación |
|---|---|---|
| `salas` | Las salas físicas/lógicas de la clínica | `clinica_id` para tenant |
| `tipos_antecedente` | Antecedentes médicos **configurados para una sala específica**. Se copian desde `plantilla_items` al aplicar una plantilla | `sala_id` discrimina especialidad, `clinica_id` para RLS |
| `tipos_signo_vital` | Signos vitales **configurados para una sala específica**. Mismo mecanismo | `sala_id` discrimina especialidad, `clinica_id` para RLS |

#### Tablas Transaccionales (datos de pacientes)

| Tabla | Propósito |
|---|---|
| `antecedentes_paciente` | Valores **reales** de antecedentes registrados para pacientes. FK a `tipos_antecedente.id` |
| `signos_vitales_hoja` | Valores **reales** de signos vitales tomados en consultas. FK a `tipos_signo_vital.id`. Tiene trigger `fn_calcular_rango_sv` que alerta si el valor está fuera de rango |

### Flujo completo de principio a fin

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. SUPER ADMIN crea/edita plantillas de especialidad            │
│    (solo accesible por URL directa, sin sidebar aún)            │
│    Tablas: plantillas_especialidad ←→ plantilla_items           │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. ADMIN aplica plantilla a una sala específica                  │
│    (endpoint nuevo: POST /api/Salas/{id}/aplicar-plantilla)     │
│    Copia items de plantilla_items → tipos_antecedente/signo_vital│
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. ADMIN asigna doctores a la sala (HU06 — NUEVO)               │
│    Tabla: usuarios_salas                                         │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. DOCTOR atiende pacientes en la sala                           │
│    - Registra antecedentes desde tipos_antecedente de la sala    │
│    - Toma signos vitales según tipos_signo_vital de la sala      │
│    - Datos quedan en: antecedentes_paciente, signos_vitales_hoja │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5-8. ✅ Fase 0-1: CRUD de Plantillas — COMPLETADO

Todos los bloqueantes originales (BLL stubeado, API inconsistente, CRUD PlantillaItem, Sidebar) han sido **corregidos e implementados**.

| Bloqueante | Estado | Resumen |
|---|---|---|
| **#1** BLL PlantillaEspecialidad stubeado | ✅ RESUELTO | `CreateAsync`, `UpdateAsync`, `DeactivateAsync`, `ReactivateAsync` implementados |
| **#2** API routing inconsistente | ✅ RESUELTO | `[HttpDelete]` → `[HttpPatch("{id}/desactivar")]` + `reactivar` |
| **#3** Sin CRUD PlantillaItem | ✅ RESUELTO | DAL + BLL + API Controller + UI en Edit.cshtml con modal JS |
| **#4** Sin entrada en Sidebar | ✅ RESUELTO | `PuedeVerPlantillas` en ViewModel + entrada en menú Administración |

---

## 9. ✅ Fase 2: HU-E02 Endpoint "Aplicar Plantilla a Sala" — COMPLETADO

> **Estado:** Implementado, probado y verificado con 13 items correctamente copiados.

| Componente | Estado | Detalle |
|---|---|---|
| `AplicarPlantillaResponseDto.cs` | ✅ CREADO | DTO con `Creados`, `Reactivados`, `Saltados` |
| `ISalaService.AplicarPlantillaAsync` | ✅ IMPLEMENTADO | Lógica completa con validaciones |
| `SalaService.AplicarPlantillaAsync` | ✅ IMPLEMENTADO | Idempotente: crea, reactiva o salta según estado existente |
| `POST /api/Salas/{salaId}/aplicar-plantilla/{plantillaId}` | ✅ FUNCIONAL | Probado con respuesta: 13 creados, 0 saltados (1ra vez) / 0 creados, 13 saltados (2da vez) |
| `JsonAplicarPlantilla` proxy MVC | ✅ IMPLEMENTADO | — |

### Bugfix asociado

Durante la prueba se descubrió que `PlantillaItemRepository` usaba `SELECT *` sin aliases SQL, lo que impedía a Dapper mapear `tipo_item` → `TipoItem`. Corregido con columnas explícitas y aliases.

---

## 10. ✅ Fase 3: HU06 Asignar Doctores a Salas — COMPLETADO

> **Estado:** Tabla `usuarios_salas` creada, CRUD completo N-Tier implementado y registrado en DI.

| Componente | Estado | Detalle |
|---|---|---|
| Migración SQL | ✅ APLICADA | `20260705000001_create_usuarios_salas.sql` con RLS |
| Entity `UsuarioSala.cs` | ✅ CREADO | — |
| DTOs `UsuarioSalaDTOs.cs` | ✅ CREADO | Request + Response |
| DAL `IUsuarioSalaRepository` + `UsuarioSalaRepository` | ✅ CREADO | — |
| BLL `IUsuarioSalaService` + `UsuarioSalaService` | ✅ CREADO | — |
| API `UsuariosSalasController` | ✅ CREADO | 3 endpoints: GET por sala, POST asignar, PATCH desactivar |
| MVC Controller + Views | ✅ CREADO | Index con tabla + modal de asignación |
| DI Registration | ✅ REGISTRADO | — |
| Sidebar entry | ✅ AGREGADO | En menú Administración |

---

## 11. ✅ Correcciones Técnicas — APLICADAS

### 11.1 Permiso de SalasController API

**Estado:** ✅ Corregido.

| Archivo | Cambio |
|---|---|
| `SalasController.cs` | `[RequirePermission("salas")]` → `[RequirePermission("areas")]` |

### 11.2 Ajuste de permisos en BD (opcional — no implementado aún)

Si se desea que el Super Admin vea Plantillas pero los Admins de clínica no, se puede agregar el módulo `plantillas_especialidad` en la BD con permiso solo para Super Admin.

---

## 12. ⏸️ Visión Futura: Vista Unificada de Detalle de Sala

### Mejora de UX sobre funcionalidad ya existente

Actualmente la configuración de una sala está en **múltiples pantallas separadas** (todas funcionales):

| Configuración | Dónde se hace hoy | Estado |
|---|---|---|
| Datos básicos de la sala | `Administracion > Sala > Edit` | ✅ Existente |
| Aplicar plantilla | `Administracion > Sala > Edit` (botón JS) | ✅ Implementado |
| Antecedentes de la sala | `Catalogos > TipoAntecedente` (filtrado por sala) | ✅ Existente |
| Signos vitales de la sala | `Catalogos > TipoSignoVital` (filtrado por sala) | ✅ Existente |
| Asignar doctores | `Administracion > UsuarioSala` | ✅ Implementado (HU06) |

**Cuándo implementarla:** Ya no hay dependencias bloqueantes. Las 5 capacidades están operativas individualmente. La vista unificada es una mejora de UX.

### Visión a futuro: Pantalla unificada

```
┌─────────────────────────────────────────────────────────────┐
│  ADMINISTRACIÓN > SALAS > DETALLE: CONSULTORIO 3            │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────┐   │
│  │ DATOS BÁSICOS                      [✏️ Editar]       │   │
│  │ Nombre: Consultorio 3 · Piso 2 Ala Norte · Activo: ✅│   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ PLANTILLA DE ESPECIALIDAD                             │   │
│  │ [Cardiología --------] [Aplicar Plantilla]            │   │
│  │ Última: 05/07/2026 - Cardiología (12 ant, 5 signos)  │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ ANTECEDENTES (12)            [+ Agregar manual]      │   │
│  │ ☑ HTA, ☑ IAM previo, ☑ Tabaquismo...               │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ SIGNOS VITALES (5)            [+ Agregar manual]     │   │
│  │ ☑ PA (mmHg), ☑ FC (lpm), ☑ Temp (°C)...            │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ DOCTORES ASIGNADOS (2)           [+ Asignar doctor]  │   │
│  │ 🧑 Dr. Juan Pérez · 🧑 Dra. María López             │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 13. Flujo de Cambio de Especialidad de una Sala

### Escenario real: Sala 3 de Cardiología → Dermatología

Este escenario es **común y recurrente** en clínicas en crecimiento. La arquitectura actual lo soporta **sin pérdida de datos históricos**.

```
ESTADO INICIAL:
┌─────────────────────────────────────────┐
│ Sala 3 — Cardiología                    │
├─────────────────────────────────────────┤
│ ✅ 12 antecedentes activos (HTA, IAM...)│
│ ✅ 5 signos vitales activos (PA, FC...) │
│ ✅ 2 doctores cardiólogos asignados     │
│ ✅ 150 consultas cardiológicas históricas│
└─────────────────────────────────────────┘

CAMBIO (admin hace):
1. Va a Sala 3 → [Aplicar Plantilla] → selecciona "Dermatología"
2. Sistema pregunta confirmación
3. Sistema ejecuta:
   a. UPDATE tipos_antecedente SET activo=false WHERE sala_id='Sala3'
   b. UPDATE tipos_signo_vital SET activo=false WHERE sala_id='Sala3'
   c. INSERT desde plantilla Dermatología (nuevos activos)
   d. UPDATE usuarios_salas SET activo=false WHERE sala_id='Sala3'
   e. Admin asigna nuevos doctores dermatólogos

ESTADO FINAL:
┌─────────────────────────────────────────┐
│ Sala 3 — Dermatología                   │
├─────────────────────────────────────────┤
│ ✅ 11 antecedentes activos (Psoriasis...)│
│ ⬜ 12 antecedentes inactivos (histórico) │
│ ✅ 4 signos vitales activos              │
│ ✅ 1 doctor dermatólogo asignado        │
│ ✅ 150 consultas cardiológicas preservadas│
└─────────────────────────────────────────┘
```

### ¿Se pierde el histórico? NO

La FK `tipo_antecedente_id` sigue apuntando al registro original (aunque ahora tenga `activo=false`). El JOIN funciona. **Los datos históricos nunca se pierden.**

---

## 14. Estado de Implementación (ya no hay orden pendiente)

### ✅ Fase 0 — Corrección rápida — COMPLETADA

| # | Tarea | Archivo | Estado |
|---|---|---|---|
| 0.1 | Cambiar `[RequirePermission("salas")]` → `[RequirePermission("areas")]` | `SalasController.cs` | ✅ Listo |

### ✅ Fase 1 — CRUD de Plantillas — COMPLETADA

| # | Tarea | Estado |
|---|---|---|
| 1.1 | Implementar `CreateAsync` en BLL | ✅ Listo |
| 1.2 | Implementar `UpdateAsync` en BLL | ✅ Listo |
| 1.3 | Implementar `DeactivateAsync` en BLL | ✅ Listo |
| 1.4 | Agregar `ReactivateAsync` en BLL y en interfaz | ✅ Listo |
| 1.5 | Agregar `Items` al `PlantillaEspecialidadDTOs.Request` | ✅ Listo |
| 1.6 | Corregir API: `[HttpDelete]` → `[HttpPatch("{id}/desactivar")]` | ✅ Listo |
| 1.7 | Agregar endpoint `[HttpPatch("{id}/reactivar")]` en API | ✅ Listo |
| 1.8 | Crear `IPlantillaItemRepository` + `PlantillaItemRepository` | ✅ Listo |
| 1.9 | Crear `IPlantillaItemService` + `PlantillaItemService` | ✅ Listo |
| 1.10 | Crear `PlantillaItemController` (API) con 5 endpoints | ✅ Listo |
| 1.11 | Registrar en `DependencyInjection.cs` | ✅ Listo |
| 1.12 | Agregar sidebar entry + permisos en SidebarViewModel | ✅ Listo |
| 1.13 | Modificar Edit.cshtml con UI de gestión de items | ✅ Listo |

### ✅ Fase 2 — Endpoint "Aplicar Plantilla a Sala" — COMPLETADA

| # | Tarea | Estado |
|---|---|---|
| 2.1 | Crear `AplicarPlantillaResponseDto.cs` | ✅ Listo |
| 2.2 | Agregar método en `ISalaService` | ✅ Listo |
| 2.3 | Implementar lógica en `SalaService` (validaciones + inserts) | ✅ Listo |
| 2.4 | Agregar endpoint `POST /api/Salas/{salaId}/aplicar-plantilla/{plantillaId}` | ✅ Listo |
| 2.5 | Agregar proxy `JsonAplicarPlantilla` en MVC `SalaController` | ✅ Listo |
| 2.6 | Probar con PowerShell/Postman | ✅ Verificado (13 items) |

### ✅ Fase 3 — HU06: Asignar Doctores a Salas — COMPLETADA

| # | Tarea | Estado |
|---|---|---|
| 3.1 | Crear migración `create_usuarios_salas.sql` | ✅ Listo |
| 3.2 | Aplicar migración: `supabase db push` | ✅ Listo |
| 3.3 | Crear Entity `UsuarioSala.cs` | ✅ Listo |
| 3.4 | Crear DTOs `UsuarioSalaDTOs.cs` | ✅ Listo |
| 3.5 | Crear DAL (Interface + Repository) | ✅ Listo |
| 3.6 | Crear BLL (Interface + Service) | ✅ Listo |
| 3.7 | Crear API Controller `UsuariosSalasController.cs` | ✅ Listo |
| 3.8 | Crear MVC Controller + Views | ✅ Listo |
| 3.9 | Registrar en `DependencyInjection.cs` | ✅ Listo |
| 3.10 | Agregar entrada en Sidebar | ✅ Listo |
| 3.11 | Probar flujo completo | ✅ Listo |

### ✅ Fase 4 — Vista unificada de Sala (COMPLETADA)

| # | Tarea | Estado |
|---|---|---|
| 4.1 | Crear acción `Details` en `SalaController` (MVC) | ✅ Completo |
| 4.2 | Crear vista `Details.cshtml` con secciones integradas | ✅ Completo |
| 4.3 | Integrar selector de plantilla con botón "Aplicar" vía JS fetch | ✅ Completo |
| 4.4 | Integrar lista de doctores asignados | ✅ Completo |

**Inversión total: ~18 hr** (Fase 0 + 1 + 2 + 3 + 4 + bugfix)
**TODAS LAS FASES COMPLETADAS.**

---

## 15. Anexos

### A. Endpoints API existentes (relacionados a Salas)

```
Salas:
  GET    /api/Salas                    → Listar todas
  GET    /api/Salas/{id}               → Obtener una
  POST   /api/Salas                    → Crear
  PUT    /api/Salas/{id}               → Editar
  PATCH  /api/Salas/{id}/deactivate    → Desactivar

Tipos Antecedente (por sala):
  GET    /api/TipoAntecedente/sala/{salaId}   → Listar por sala
  POST   /api/TipoAntecedente                 → Crear
  PUT    /api/TipoAntecedente/{id}            → Editar
  PATCH  /api/TipoAntecedente/{id}/deactivate → Desactivar

Tipos Signo Vital (por sala):
  GET    /api/TipoSignoVital/sala/{salaId}    → Listar por sala
  POST   /api/TipoSignoVital                  → Crear
  PUT    /api/TipoSignoVital/{id}             → Editar
  PATCH  /api/TipoSignoVital/{id}/deactivate  → Desactivar

Plantillas de Especialidad:
  GET    /api/PlantillaEspecialidad           → Listar todas
  GET    /api/PlantillaEspecialidad/{id}      → Obtener con items
  POST   /api/PlantillaEspecialidad           → Crear
  PUT    /api/PlantillaEspecialidad/{id}      → Editar

Usuarios:
  GET    /api/Usuarios                        → Listar usuarios
  GET    /api/Usuarios/doctores               → Listar doctores (para dropdown)
```

### B. Endpoints creados (Fases 1-3)

```
CREADOS (Fase 1 — CRUD PlantillaItem):
  GET    /api/PlantillaItem/plantilla/{plantillaId}    ✅ → Listar items por plantilla
  POST   /api/PlantillaItem                            ✅ → Crear item
  PUT    /api/PlantillaItem/{id}                       ✅ → Actualizar item
  PATCH  /api/PlantillaItem/{id}/desactivar            ✅ → Desactivar item
  PATCH  /api/PlantillaItem/{id}/reactivar             ✅ → Reactivar item

CREADOS (Fase 2 — Aplicar Plantilla a Sala):
  POST   /api/Salas/{salaId}/aplicar-plantilla/{plantillaId}  ✅ → HU-E02 probado

CREADOS (Fase 3 — HU06):
  GET    /api/UsuariosSalas/sala/{salaId}                      ✅ → HU06
  POST   /api/UsuariosSalas                                    ✅ → HU06
  PATCH  /api/UsuariosSalas/{id}/desactivar                    ✅ → HU06
```

### C. Resumen de tablas

| Feature | Tablas usadas | ¿Nueva? |
|---|---|---|
| HU10 Salas | `salas` | ❌ Existente |
| HU-E02 Plantillas | `plantillas_especialidad`, `plantilla_items` | ❌ Existente (seed completo ✅) |
| HU-E02 Aplicar Plantilla | Usa `plantilla_items` → inserta en `tipos_antecedente`, `tipos_signo_vital` | ❌ Existentes |
| HU-E03 Antecedentes por Sala | `tipos_antecedente` | ❌ Existente (CRUD + vistas ✅) |
| HU-E04 Signos por Sala | `tipos_signo_vital` | ❌ Existente (CRUD + vistas ✅) |
| **HU06 Asignar Doctores** | **`usuarios_salas`** (NUEVA), `usuarios`, `salas` | **✅ 1 tabla nueva** |

### D. Salas activas actuales (ejemplo)

| Sala | Descripción |
|---|---|
| Consultorio 1 | Medicina General - Pediatría y adultos |
| Consultorio 2 | Medicina General |
| Consultorio 3 | Cardiología |
| Consultorio 4 | Dermatología |
| Sala de Emergencia | Emergencias |

### E. Datos de prueba — Acceso

| Rol | Email | Contraseña |
|---|---|---|
| Super Admin | admin@vittal.com | Password123! |
| Administrador | carlos@vittal.com | Password123! |
| Médico General | juan.perez@vittal.com | Password123! |
| Médico General | maria.lopez@vittal.com | Password123! |
| Gerente de Clínica | gerente@vittal.com | Password123! |
| Recepcionista | ana@vittal.com | Password123! |
| Enfermero/a | carlos.enfermero@vittal.com | Password123! |

### F. URLs del entorno

| Componente | URL |
|---|---|
| API (Swagger) | `http://localhost:5089/swagger` |
| Frontend MVC | `http://localhost:5218` |

### G. Seed de plantillas — Estado actual ✅

La migración `supabase/migrations/20260512000002_create_plantillas_especialidad.sql` ya contiene:

| Plantilla | Items |
|---|---|
| Medicina General | 18 items (12 antecedentes + 6 signos vitales) |
| Cardiología | 17 items (12 antecedentes + 5 signos vitales) |
| Dermatología | 15 items (10 antecedentes + 5 signos vitales) |
| Pediatría | 18 items (12 antecedentes + 6 signos vitales) |
| Ginecología | 15 items (10 antecedentes + 5 signos vitales) |
| Neurología | 15 items (10 antecedentes + 5 signos vitales) |
| Traumatología | 15 items (10 antecedentes + 5 signos vitales) |
| Emergencias | 18 items (12 antecedentes + 6 signos vitales) |
| **Total** | **~131 items** |

El seed se ejecuta con `INSERT ... ON CONFLICT DO NOTHING` para ser idempotente.

El **orden de los items** está definido por el campo `orden` dentro de cada plantilla.

---

## Historial de Revisiones

| Fecha | Versión | Cambios | Autor |
|---|---|---|---|
| 2026-07-05 | 1.0 | Creación inicial | @PM |
| 2026-07-05 | 2.0 | **Corrección mayor**: Se eliminaron HU ya implementadas. Plan enfocado en seed, endpoint "Aplicar Plantilla" y HU06. | @PM |
| 2026-07-05 | 3.0 | **Auditoría completa de código**: Se descubrió que el BLL de PlantillaEspecialidad está stubeado, la API tiene routing inconsistente, no existe CRUD de PlantillaItem, no hay sidebar entry, y el seed de items YA está completo. Se agregaron 4 nuevos bloqueantes además de las tareas ya conocidas. Tiempo estimado ajustado de 2 a 3 días. | @PM |
| 2026-07-05 | 4.0 | **Fases 0-3 completadas**: Todos los bloqueantes resueltos, bugfix de mapeo Dapper aplicado, endpoint "Aplicar Plantilla" probado con 13 items. Plan reestructurado como documento histórico de referencia. Solo queda Fase 4 (opcional). | @PM |
| 2026-07-05 | 5.0 | **Fase 4 completada**: Vista unificada de detalle de Sala. Se creó acción `Details` + vista `Details.cshtml` con tabs de Antecedentes, Signos Vitales y Doctores, más selector de Plantilla. Se agregaron 8 nuevos JSON proxy endpoints. **Plan integral completado al 100%.** | @PM |

---

*Documento mantenido por @PM — Vittal v1.0.0*
*Propósito: Punto de restauración de contexto para desarrollo asistido por IA*
*Leer completo al retomar el desarrollo después de una interrupción*
