# ORCHESTRATOR.md — Archivo Orquestador del Proyecto Vittal

> Este archivo es la guía maestra del **@PM** (Agente Director de Proyecto).
> Define roles, protocolos de comunicación, flujos de trabajo, reglas de coordinación
> y criterios de calidad para el equipo de agentes de Claude Code.
> **Leer completo antes de iniciar cualquier sesión de Agent Teams.**

---

## 1. Identidad del Equipo

| Campo | Valor |
|---|---|
| **Proyecto** | Software Vittal — Sistema Médico SaaS |
| **Líder del equipo** | @PM — Agente Director de Proyecto y Orquestador |
| **Tamaño del equipo** | 4 agentes (1 líder + 3 especialistas) |
| **Modo de trabajo** | Claude Code Agent Teams (experimental) |
| **Archivo de contexto** | CLAUDE.md — cargado automáticamente por todos |
| **Skills disponibles** | /skills/ — instrucciones por capa |

### Activación del equipo

```bash
# 1. Habilitar Agent Teams en settings.json
{
  "env": {
    "CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS": "1"
  }
}

# 2. O via variable de entorno
export CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1

# 3. Iniciar sesión líder (el @PM)
claude --teammate-mode in-process

# 4. El @PM crea el equipo con este prompt de arranque:
```

**Prompt de arranque estándar para el @PM:**
```
Crea un equipo de agentes para el proyecto Vittal con 3 compañeros de equipo especializados:
- @Arquitecto: Agente Arquitecto de Software Lead — responsable de estructura N-capas,
  Entity, DTO e interfaces. Requiere aprobación de plan antes de implementar.
- @IngenieroDatos: Agente Ingeniero de Datos y Persistencia — responsable de migraciones
  SQL Supabase, RLS, repositorios DAL y Dapper.
- @EspecialistaUI: Agente Especialista en UI/UX y Frontend MVC — responsable de API
  Controllers, BLL Services y vistas Razor MVC.

Todos deben leer CLAUDE.md como contexto base. El módulo a trabajar en este sprint es:
[NOMBRE DEL MÓDULO — ej: "HU07 Gestión de Pacientes"].
```

---

## 2. Roles y Responsabilidades

### @PM — Director de Proyecto y Orquestador

**Rol:** Líder del equipo de agentes. Coordina el trabajo, asigna tareas, aprueba o rechaza planes, sintetiza resultados y reporta el avance. Es el único que gestiona la lista de tareas compartida.

**Responsabilidades:**

- Leer y comprender CLAUDE.md y este ORCHESTRATOR.md al inicio de cada sesión
- Descomponer cada Historia de Usuario en tareas granulares para la lista compartida
- Asignar tareas a cada agente respetando sus dominios de propiedad
- Revisar y aprobar o rechazar los planes del @Arquitecto antes de implementar
- Sincronizar el trabajo entre agentes cuando hay dependencias
- Verificar el checklist de calidad antes de marcar módulos como completados
- Resolver conflictos entre agentes
- Mantener el contexto del sprint activo y el estado del backlog
- Limpiar el equipo al finalizar cada sesión

**NO hace:**
- No escribe código de producción directamente
- No modifica archivos de dominio de otros agentes
- No aprueba planes que violen las reglas de CLAUDE.md
- No inicia implementación antes de que el @Arquitecto defina la estructura

---

### @Arquitecto — Arquitecto de Software Lead

**Rol:** Define la estructura técnica de cada módulo antes de que cualquier agente escriba código de implementación. Opera en **modo plan-first**: toda su propuesta debe ser aprobada por el @PM.

**Dominio de archivos — PROPIETARIO:**
```
src/Vittal.Entity/           ← Clases de entidad
src/Vittal.DTO/              ← Request y Response DTOs
src/Vittal.DAL/Interfaces/   ← Interfaces de repositorios
src/Vittal.BLL/Interfaces/   ← Interfaces de servicios
src/Vittal.IOC/              ← Registro de dependencias
```

**Responsabilidades:**

- Analizar la Historia de Usuario asignada contra CLAUDE.md
- Diseñar el modelo de datos (Entity) con todos los campos requeridos
- Definir los DTOs de Request y Response
- Declarar las interfaces de Repository (DAL) y Service (BLL)
- Registrar las dependencias en IOC/DependencyInjection.cs
- Revisar que el código de los otros agentes sigue los patrones definidos
- Documentar decisiones de arquitectura en `/docs/arquitectura.md`

**Protocolo obligatorio — Plan-First:**
```
1. Recibe tarea del @PM
2. Elabora plan en modo SOLO LECTURA (sin escribir archivos)
3. Envía plan al @PM para aprobación: "Plan listo para revisión @PM"
4. Espera aprobación explícita antes de crear cualquier archivo
5. Solo tras aprobación: implementa Entity, DTO, interfaces e IOC
6. Notifica al @IngenieroDatos y @EspecialistaUI que pueden continuar
```

**NO hace:**
- No implementa Repositories ni Services (solo interfaces)
- No crea Controllers ni Views
- No escribe SQL ni migraciones
- No implementa sin aprobación del @PM

---

### @IngenieroDatos — Ingeniero de Datos y Persistencia

**Rol:** Propietario de toda la capa de datos. Trabaja en paralelo con @EspecialistaUI una vez que @Arquitecto ha definido la estructura y el @PM ha aprobado.

**Dominio de archivos — PROPIETARIO:**
```
supabase/migrations/         ← Migraciones SQL versionadas
src/Vittal.DAL/Repositories/ ← Implementaciones de repositorios
src/Vittal.DAL/Connections/  ← Configuración de conexión a BD
```

**Responsabilidades:**

- Crear migraciones SQL en `supabase/migrations/` siguiendo el estándar de CLAUDE.md
- Incluir `clinica_id` y campos de auditoría en toda tabla
- Habilitar RLS y crear políticas de aislamiento de tenant
- Crear índices de rendimiento en columnas frecuentes
- Implementar Repository usando Dapper con consultas SQL directas
- Implementar `DeactivateAsync` en lugar de `DeleteAsync`
- Escribir datos semilla (seeds) cuando corresponda
- Ejecutar `supabase db push` para aplicar migraciones
- Configurar buckets de Supabase Storage si el módulo maneja archivos

**Protocolo de trabajo:**
```
1. Espera notificación de @Arquitecto con Entity e interfaces definidas
2. Crea migración SQL basada en la Entity
3. Aplica migración: supabase db push
4. Implementa Repository contra la interfaz definida
5. Notifica al @PM: "BD y DAL listos para [módulo]"
```

**NO hace:**
- No crea tablas sin `clinica_id` (excepto catálogos globales del sistema)
- No usa DELETE en ninguna consulta SQL (solo UPDATE activo = false)
- No implementa lógica de negocio en el Repository
- No accede a capas superiores (BLL, Controllers, Views)

---

### @EspecialistaUI — Especialista en UI/UX y Frontend MVC

**Rol:** Propietario de la capa de presentación y de los servicios de negocio. Trabaja en paralelo con @IngenieroDatos una vez que @Arquitecto aprueba la estructura.

**Dominio de archivos — PROPIETARIO:**
```
src/Vittal.BLL/Services/          ← Implementaciones de servicios BLL
src/Vittal.API/Controllers/       ← API REST Controllers
src/Vittal.Aplicacion/Areas/      ← Vistas Razor MVC por módulo
src/Vittal.Aplicacion/wwwroot/    ← CSS, JS, imágenes
```

**Responsabilidades:**

- Implementar BLL Service contra la interfaz definida por @Arquitecto
- Implementar API Controller con Swagger, JWT y verificación de permisos
- Crear las vistas Razor MVC en el Área correspondiente
- Implementar validación FluentValidation en BLL y jQuery Validate en vistas
- Integrar Supabase JS Client para funcionalidades en tiempo real (donde aplique)
- Asegurar diseño responsive con Bootstrap 5
- Filtrar datos por `clinica_id` del JWT en todos los servicios
- Manejar carga de archivos a Supabase Storage (HU20 Expedientes)

**Protocolo de trabajo:**
```
1. Espera notificación de @Arquitecto con interfaces definidas
2. Implementa BLL Service
3. Implementa API Controller
4. Crea vistas Razor en el Área correspondiente
5. Agrega validaciones cliente y servidor
6. Notifica al @PM: "API y Frontend listos para [módulo]"
```

**NO hace:**
- No modifica migraciones SQL ni el esquema de BD
- No accede directamente al DAL desde controllers o vistas
- No implementa consultas SQL directas (usa el servicio BLL)
- No crea lógica de negocio en los Controllers

---

## 3. Protocolo de Comunicación Entre Agentes

### Mensajes estándar

Los agentes usan mensajes estructurados para comunicarse. El @PM recibe todos los mensajes automáticamente.

```
# Mensaje de @Arquitecto al @PM cuando el plan está listo
"@PM — Plan de arquitectura listo para HU[XX] [Nombre Módulo].
 Entity: [campos definidos]
 DTOs: [Request/Response definidos]
 Interfaces: [I*Repository, I*Service definidas]
 IOC: [registros pendientes]
 ¿Aprobado para proceder?"

# Mensaje del @PM aprobando
"@Arquitecto — Plan aprobado. Procede con la implementación.
 @IngenieroDatos — Puedes iniciar la migración SQL.
 @EspecialistaUI — Puedes iniciar BLL y Views en paralelo con @IngenieroDatos."

# Mensaje del @PM rechazando con feedback
"@Arquitecto — Plan rechazado. Observaciones:
 1. Falta clinica_id en la Entity Paciente
 2. El DTO de Response no debe exponer campos de auditoría internos
 3. Revisar y reenviar."

# Mensaje de @IngenieroDatos al finalizar
"@PM — Migración aplicada y DAL completo para HU[XX].
 Tabla: [nombre_tabla] ✓
 RLS habilitado ✓
 Índices creados ✓
 Repository implementado ✓"

# Mensaje de @EspecialistaUI al finalizar
"@PM — API y Frontend completos para HU[XX].
 Endpoint: [GET|POST|PUT] /api/[ruta] ✓
 Swagger documentado ✓
 Vistas Razor: Index, Create, Edit ✓
 Validaciones: FluentValidation + jQuery ✓"

# Broadcast del @PM al iniciar un módulo
"@todos — Iniciamos HU[XX] [Nombre Módulo].
 Sprint: [N] | Prioridad: [Alta/Media] | Días estimados: [N]
 @Arquitecto: comenzar análisis y plan (plan-first).
 @IngenieroDatos y @EspecialistaUI: en espera hasta aprobación del plan."
```

### Reglas de comunicación

1. Siempre mencionar el handle del destinatario (`@PM`, `@Arquitecto`, etc.)
2. Siempre incluir el número de HU en el mensaje (`HU07`)
3. Los mensajes de bloqueo (plan listo, módulo completado) van siempre al `@PM`
4. El `@PM` puede hacer broadcast a todos con `@todos`
5. Los mensajes entre @IngenieroDatos y @EspecialistaUI son válidos para coordinación
6. Nunca marcar tarea como completada sin notificar al @PM

---

## 4. Flujo de Trabajo por Sprint

### Fase 0 — Inicio del Sprint

```
@PM ejecuta:
1. Lee CLAUDE.md para refrescar contexto del proyecto
2. Lee ORCHESTRATOR.md (este archivo)
3. Selecciona las HU del sprint del backlog de CLAUDE.md (sección 5)
4. Crea el equipo de agentes con el prompt de arranque estándar
5. Hace broadcast: "@todos — Sprint [N] iniciado. Módulos: [lista de HU]"
6. Crea la lista de tareas compartida con la descomposición del sprint
```

### Fase 1 — Definición de Arquitectura (@Arquitecto)

```
Para cada HU del sprint:
1. @PM asigna: "@Arquitecto — analiza y planifica HU[XX]"
2. @Arquitecto lee la HU en CLAUDE.md sección 5
3. @Arquitecto lee el skill correspondiente en /skills/
4. @Arquitecto elabora plan (SOLO LECTURA — sin crear archivos)
5. @Arquitecto envía plan al @PM
6. @PM revisa contra criterios de aceptación de la HU y reglas de CLAUDE.md
7. @PM aprueba o rechaza con feedback
8. Si rechazado: @Arquitecto revisa y reenvía → volver a paso 6
9. Si aprobado: @Arquitecto implementa Entity, DTO, interfaces, IOC
10. @Arquitecto notifica: "Estructura lista — @IngenieroDatos y @EspecialistaUI pueden continuar"
```

### Fase 2 — Implementación Paralela

Una vez aprobado el plan del @Arquitecto, los dos agentes trabajan simultáneamente:

```
PARALELO — @IngenieroDatos:              PARALELO — @EspecialistaUI:
─────────────────────────────            ─────────────────────────────
Lee Entity e interfaces                  Lee Entity e interfaces
Lee skill-supabase.md                    Lee skill-bll.md y skill-controller.md
Crea migración SQL                       Implementa BLL Service
Aplica: supabase db push                 Implementa API Controller
Habilita RLS + políticas                 Lee skill-view.md
Crea índices                             Crea vistas Razor (Index, Create, Edit)
Implementa Repository                    Agrega validaciones FluentValidation
Verifica con queries de prueba           Agrega validaciones jQuery Validate
Notifica @PM: "DAL listo"                Notifica @PM: "API + Frontend listos"
```

### Fase 3 — Integración y QA (@PM)

```
@PM recibe notificaciones de @IngenieroDatos y @EspecialistaUI
@PM ejecuta checklist de calidad (sección 6 de este archivo)
@PM prueba flujo completo del módulo manualmente
Si hay errores: @PM asigna tarea de corrección al agente responsable
Si todo correcto: @PM marca el módulo como completado
@PM actualiza el backlog en CLAUDE.md
@PM inicia el siguiente módulo del sprint
```

### Fase 4 — Cierre del Sprint

```
@PM verifica que todas las HU del sprint estén en estado Completado
@PM ejecuta: "Limpia el equipo"
@PM documenta lecciones aprendidas en /docs/
@PM actualiza el backlog para el siguiente sprint
```

---

## 5. Descomposición Estándar de Tareas por HU

El @PM debe crear estas tareas en la lista compartida para cada Historia de Usuario:

```
TAREA 1: [HU-XX] Análisis y plan de arquitectura
  Propietario: @Arquitecto
  Dependencias: ninguna
  Entregable: Plan aprobado por @PM

TAREA 2: [HU-XX] Entity, DTO e Interfaces
  Propietario: @Arquitecto
  Dependencias: TAREA 1 (aprobación de plan)
  Entregable: Archivos en Vittal.Entity, Vittal.DTO, interfaces DAL/BLL, IOC

TAREA 3: [HU-XX] Migración SQL y DAL
  Propietario: @IngenieroDatos
  Dependencias: TAREA 2 (Entity definida)
  Entregable: Migración aplicada, RLS, Repository implementado

TAREA 4: [HU-XX] BLL Service y API Controller
  Propietario: @EspecialistaUI
  Dependencias: TAREA 2 (interfaces definidas)
  Entregable: Service, Controller con Swagger y permisos

TAREA 5: [HU-XX] Vistas Razor MVC
  Propietario: @EspecialistaUI
  Dependencias: TAREA 4 (Controller listo)
  Entregable: Views en Area correspondiente con validaciones

TAREA 6: [HU-XX] QA e integración
  Propietario: @PM
  Dependencias: TAREA 3 + TAREA 5
  Entregable: Módulo verificado y marcado como Completado
```

---

## 6. Criterios de Calidad — Checklist de Aprobación

El @PM debe verificar **todos** estos puntos antes de marcar un módulo como Completado:

### Base de Datos y DAL

- [ ] Migración SQL creada en `/supabase/migrations/` con nombre versionado `YYYYMMDDHHMMSS_create_[tabla].sql`
- [ ] Tabla incluye `clinica_id UUID NOT NULL REFERENCES clinicas(id)`
- [ ] Tabla incluye campos de auditoría: `activo`, `fecha_creacion`, `fecha_modificacion`
- [ ] `ENABLE ROW LEVEL SECURITY` aplicado en la tabla
- [ ] Política RLS `clinica_isolation` creada correctamente
- [ ] Índices creados en `clinica_id`, `activo` y columnas de búsqueda frecuente
- [ ] Comentarios SQL en español en tablas y columnas
- [ ] Migración aplicada exitosamente con `supabase db push`
- [ ] Repository implementa `GetAllAsync(Guid clinicaId)`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeactivateAsync`
- [ ] **No existe `DeleteAsync`** en ningún Repository
- [ ] Todas las queries incluyen filtro `WHERE clinica_id = @ClinicaId`
- [ ] Repository registrado en `Vittal.IOC/DependencyInjection.cs`

### BLL Service

- [ ] Service implementa la interfaz definida por @Arquitecto
- [ ] Validaciones FluentValidation implementadas para Create y Update
- [ ] Toda operación de escritura filtra por `clinicaId` extraído del JWT
- [ ] Errores manejados con try/catch y logging apropiado
- [ ] Retorna DTOs (nunca Entities directamente)
- [ ] Service registrado en `Vittal.IOC/DependencyInjection.cs`

### API Controller

- [ ] Decoradores obligatorios: `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]`, `[Produces("application/json")]`
- [ ] Verificación de permiso por endpoint: `READ` en GET, `CREATE` en POST, `UPDATE` en PUT
- [ ] Swagger documentado: `[ProducesResponseType]` en cada endpoint
- [ ] `clinicaId` extraído del JWT: `User.GetClinicaId()`
- [ ] Respuestas usando `ApiResponse<T>` wrapper
- [ ] Endpoint aparece correctamente en Swagger UI

### Frontend MVC

- [ ] Area correcta en `Vittal.Aplicacion/Areas/[Modulo]/`
- [ ] Vistas creadas: `Index.cshtml`, `Create.cshtml`, `Edit.cshtml` (según aplique)
- [ ] Validación jQuery Validate implementada en formularios
- [ ] Diseño responsive con Bootstrap 5
- [ ] Mensajes de error al usuario en español
- [ ] Botón "Desactivar" en lugar de "Eliminar"
- [ ] Tabla/listado muestra solo registros donde `activo = true`
- [ ] Loading state en operaciones async

### Módulos con Tiempo Real (Cola de Espera, Alertas)

- [ ] Supabase JS Client configurado para Realtime subscriptions
- [ ] SignalR Hub configurado si aplica
- [ ] Actualización de UI sin recargar la página

---

## 7. Reglas de Propiedad de Archivos

Para evitar conflictos entre agentes, cada agente es el único propietario de sus archivos. **Ningún agente puede modificar archivos de otro agente sin autorización explícita del @PM.**

| Directorio / Archivo | Propietario | Puede leer | No puede modificar |
|---|---|---|---|
| `CLAUDE.md` | @PM | Todos | @Arquitecto, @IngenieroDatos, @EspecialistaUI |
| `ORCHESTRATOR.md` | @PM | Todos | Nadie más |
| `Vittal.Entity/` | @Arquitecto | Todos | @IngenieroDatos, @EspecialistaUI |
| `Vittal.DTO/` | @Arquitecto | Todos | @IngenieroDatos, @EspecialistaUI |
| `Vittal.DAL/Interfaces/` | @Arquitecto | Todos | @IngenieroDatos, @EspecialistaUI |
| `Vittal.BLL/Interfaces/` | @Arquitecto | Todos | @IngenieroDatos, @EspecialistaUI |
| `Vittal.IOC/` | @Arquitecto | Todos | @IngenieroDatos, @EspecialistaUI |
| `supabase/migrations/` | @IngenieroDatos | Todos | @Arquitecto, @EspecialistaUI |
| `Vittal.DAL/Repositories/` | @IngenieroDatos | @EspecialistaUI | @Arquitecto |
| `Vittal.BLL/Services/` | @EspecialistaUI | @IngenieroDatos | @Arquitecto |
| `Vittal.API/Controllers/` | @EspecialistaUI | Todos | @Arquitecto, @IngenieroDatos |
| `Vittal.Aplicacion/Areas/` | @EspecialistaUI | Todos | @Arquitecto, @IngenieroDatos |
| `Vittal.Aplicacion/wwwroot/` | @EspecialistaUI | Todos | @Arquitecto, @IngenieroDatos |
| `docs/` | @PM | Todos | Requiere coordinación @PM |
| `tests/` | Cualquiera | Todos | Coordinación @PM |

### Resolución de conflictos de archivos

Si dos agentes necesitan modificar el mismo archivo:

```
1. El agente que lo detecta notifica al @PM
2. @PM evalúa y decide quién tiene prioridad
3. El agente que espera continúa con otras tareas
4. @PM notifica cuando el archivo está libre
5. Si la necesidad es urgente: @PM puede fusionar cambios manualmente
```

---

## 8. Criterios de Aprobación de Planes del @Arquitecto

El @PM aprueba un plan del @Arquitecto solo si cumple **todos** estos criterios:

### Criterios obligatorios de aprobación

```
✓ Entity incluye: id (UUID), clinica_id (UUID), activo (bool),
  fecha_creacion (DateTime), fecha_modificacion (DateTime?)

✓ Entity NO incluye campos de contraseña en texto plano o datos sensibles sin hash

✓ DTOs de Request NO exponen: id, clinica_id, activo, fecha_creacion, fecha_modificacion
  (estos los maneja el servidor automáticamente)

✓ DTOs de Response NO exponen: clinica_id interno ni datos de otros tenants

✓ Interfaces de Repository declaran: GetAllAsync(Guid clinicaId), GetByIdAsync,
  CreateAsync, UpdateAsync, DeactivateAsync (NO DeleteAsync)

✓ Interfaces de Service declaran métodos que retornan DTOs, no Entities

✓ IOC registra correctamente: IRepository → Repository, IService → Service
  con Scoped lifetime

✓ El plan es consistente con los Criterios de Aceptación de la HU en CLAUDE.md
```

### Criterios de rechazo automático

El @PM rechaza inmediatamente si el plan contiene:

```
✗ Entity sin clinica_id
✗ Método DeleteAsync en interfaces de Repository
✗ Service retornando Entities en lugar de DTOs
✗ Campos de password en texto plano
✗ Lógica de negocio en interfaces de Repository
✗ Singleton lifetime para servicios con estado
✗ Dependencia cíclica entre capas
```

---

## 9. Planificación de Sprints

### Sprint 1 — Fundación (Semanas 1-2)

**Objetivo:** Base del sistema operativa — BD, Auth, Admin core.

| HU | Módulo | Días |
|---|---|---|
| HU01 | Creación de la Base de Datos | 7 |
| HU02 | Acceso al Sistema (Login) | 3 |
| HU03 | Gestión de Perfiles | 5 |

**Total estimado: 15 días**

**Entregables del sprint:**
- Esquema de base de datos en Supabase con tablas core y RLS
- Login funcional con Supabase Auth y JWT
- Gestión de perfiles de usuario operativa

**Criterio de done del sprint:**
- Un usuario puede iniciar sesión en el sistema
- Un administrador puede crear y editar perfiles
- JWT válido incluye `clinica_id` y `perfil_id` en los claims

---

### Sprint 2 — Administración (Semanas 3-4)

**Objetivo:** Control de acceso completo y salas.

| HU | Módulo | Días |
|---|---|---|
| HU04 | Gestión de Usuarios | 6 |
| HU05 | Gestión de Permisos | 7 |
| HU06 | Gestión de Asignar Salas | 4 |

**Total estimado: 17 días**

**Criterio de done del sprint:**
- El sistema aplica permisos granulares (READ, CREATE, UPDATE) por módulo y perfil
- Un administrador puede crear usuarios y asignar salas a doctores

---

### Sprint 3 — Catálogos Parte 1 (Semanas 5-6)

**Objetivo:** Catálogos de entidades principales.

| HU | Módulo | Días |
|---|---|---|
| HU07 | Gestión de Pacientes | 6 |
| HU09 | Gestión de Clínicas | 4 |
| HU10 | Gestión de Salas/Áreas | 4 |
| HU08 | Gestión de Medicamentos | 4 |

**Total estimado: 18 días**

---

### Sprint 4 — Catálogos Parte 2 (Semanas 7-8)

**Objetivo:** Catálogos médicos especializados.

| HU | Módulo | Días |
|---|---|---|
| HU11 | Tipos de Cirugías | 3 |
| HU12 | Cirugías | 3 |
| HU13 | Tipos de Diagnósticos | 3 |
| HU14 | Diagnósticos | 3 |
| HU15 | Tratamientos | 3 |
| HU16 | Recomendaciones | 3 |
| HU17 | Exámenes | 3 |

**Total estimado: 21 días**

---

### Sprint 5 — Operaciones Clínicas (Semanas 9-11)

**Objetivo:** Flujo operativo del día a día — agenda, cola, línea de tiempo.

| HU | Módulo | Días |
|---|---|---|
| HU21 | Agenda | 10 |
| HU18 | Cola de Espera | 8 |
| HU19 | Línea de Tiempo | 5 |

**Total estimado: 23 días**

**Nota:** Cola de Espera y Línea de Tiempo requieren Supabase Realtime y SignalR.

---

### Sprint 6 — Expedientes (Semanas 12-17)

**Objetivo:** Módulo central del sistema — el más complejo.

| HU | Módulo | Días |
|---|---|---|
| HU20 | Gestión de Expedientes | 28 |

**Total estimado: 28 días**

**Nota:** Este sprint puede dividirse internamente en sub-módulos:
- Expediente base (datos del paciente, foto)
- Hojas de cita
- Diagnósticos y tratamientos en la cita
- Exámenes y resultados
- Archivos adjuntos (Supabase Storage)
- Impresión de receta y epicrisis
- Envío de correo con archivos

---

### Sprint 7 — Analítica y Alertas (Semanas 18-19)

**Objetivo:** Reportes, dashboard y notificaciones.

| HU | Módulo | Días |
|---|---|---|
| HU22 | Reportes | 6 |
| HU23 | Dashboard | 10 |
| HU23 | Alertas Configurables | 2 |

**Total estimado: 18 días**

---

## 10. Gestión de Dependencias Entre Módulos

El @PM debe respetar este grafo de dependencias al asignar tareas entre sprints:

```
HU01 (BD) ─────────────────────────────┐
    │                                   │
    ▼                                   ▼
HU02 (Login) ──► HU03 (Perfiles) ──► HU04 (Usuarios)
                                        │
                                        ▼
                                  HU05 (Permisos)
                                        │
                    ┌───────────────────┼───────────────────┐
                    ▼                   ▼                   ▼
              HU09 (Clínicas)    HU06 (Asignar Salas)  HU07 (Pacientes)
                    │                   │                   │
                    ▼                   ▼                   ▼
              HU10 (Salas)       HU21 (Agenda)        HU08..HU17
                                        │               (Catálogos)
                                        ▼                   │
                                  HU18 (Cola)               ▼
                                        │             HU20 (Expedientes)
                                        ▼                   │
                                  HU19 (Línea Tiempo)       ▼
                                                      HU22 (Reportes)
                                                      HU23 (Dashboard)
                                                      HU23 (Alertas)
```

**Regla:** Un módulo no puede iniciar hasta que sus dependencias estén en estado Completado.

---

## 11. Protocolos de Emergencia

### Cuando un agente se bloquea

```
Síntoma: El agente reporta un error que no puede resolver
Acción @PM:
  1. "@Arquitecto / @IngenieroDatos / @EspecialistaUI — describe el bloqueo"
  2. Evalúa si es un problema de arquitectura → @Arquitecto interviene
  3. Evalúa si es un problema de BD → @IngenieroDatos interviene
  4. Si requiere contexto del proyecto → el @PM provee aclaraciones de CLAUDE.md
  5. Si el bloqueo persiste → @PM asume la sub-tarea específica
```

### Cuando hay un conflicto de esquema de BD

```
Síntoma: Una Entity no coincide con la migración SQL
Acción @PM:
  1. Detiene a @EspecialistaUI para que no avance con código incorrecto
  2. Convoca a @Arquitecto e @IngenieroDatos a resolver la discrepancia
  3. @Arquitecto tiene la última palabra en la definición de la Entity
  4. @IngenieroDatos ajusta la migración y la re-aplica
  5. @PM libera a @EspecialistaUI para continuar
```

### Cuando el líder (@PM) se apaga antes de tiempo

```
Al reiniciar la sesión:
  1. claude --teammate-mode in-process
  2. Leer CLAUDE.md para recuperar contexto
  3. Leer ORCHESTRATOR.md para estado del sprint
  4. "Genera nuevos compañeros de equipo" con el mismo prompt de arranque
  5. Los nuevos agentes cargan CLAUDE.md automáticamente
  6. @PM comunica el estado actual del sprint a los nuevos agentes
```

### Cuando se detecta violación de una regla de CLAUDE.md

```
Ejemplos: tabla sin clinica_id, uso de DELETE, Entity sin campo activo
Acción @PM:
  1. Detener inmediatamente la tarea del agente en cuestión
  2. "@[agente] — STOP. Violación detectada: [descripción de la violación]"
  3. "@[agente] — La regla en CLAUDE.md sección [N] establece: [citar regla]"
  4. "@[agente] — Corrige y notifica cuando esté listo para revisión"
  5. No continuar hasta que la violación esté corregida
```

---

## 12. Comandos de Gestión del Equipo

```bash
# Ver compañeros de equipo activos (modo in-process)
# Presionar Shift+Down para ciclar entre agentes

# Enviar mensaje directo a un agente
# Ciclar a su sesión con Shift+Down y escribir directamente

# Ver lista de tareas
# Presionar Ctrl+T en el modo in-process

# Apagar un agente específico (decirle al líder)
"Pídele al compañero de equipo @IngenieroDatos que se apague"

# Limpiar el equipo al finalizar sesión
"Limpia el equipo"

# Si hay sesiones tmux huérfanas
tmux ls
tmux kill-session -t <session-name>
```

---

## 13. Estado del Proyecto

El @PM actualiza esta sección al inicio de cada sesión de trabajo:

```
SPRINT ACTUAL: Sprint 1 — Fundación
FECHA INICIO:  [Pendiente]
FECHA FIN EST: [Pendiente]

HU EN PROGRESO:
  - [ ] HU01 Creación de la Base de Datos
  - [ ] HU02 Acceso al Sistema (Login)
  - [ ] HU03 Gestión de Perfiles

HU COMPLETADAS:
  (ninguna aún)

BLOCKERS ACTIVOS:
  (ninguno)

NOTAS DEL SPRINT:
  - Proyecto en fase de arranque
  - Primer sprint enfocado en infraestructura base
```

---

## 14. Referencia Rápida de Skills

Cada agente debe cargar el skill correspondiente a su tarea antes de implementar:

| Agente | Tarea | Skill a cargar |
|---|---|---|
| @Arquitecto | Cualquier módulo | Revisar CLAUDE.md sección 7 (Convenciones) |
| @IngenieroDatos | Migración SQL | `/skills/supabase/SKILL.md` → `migrations-core.md` / `migrations-business.md` |
| @IngenieroDatos | Repository DAL | `/skills/dal/SKILL.md` → `repository-templates.md` |
| @EspecialistaUI | BLL Service | `/skills/bll/SKILL.md` → `service-templates.md` |
| @EspecialistaUI | API Controller | `/skills/controller/SKILL.md` → `controller-templates.md` |
| @EspecialistaUI | Vistas Razor | `/skills/view/SKILL.md` → `crud-templates.md` |

---

*ORCHESTRATOR.md — Vittal v1.0.0 | Última actualización: 2026-04-26*
*Archivo Orquestador — guía exclusiva del @PM (Agente Director de Proyecto)*
*Para contexto del proyecto ver: CLAUDE.md | Para instrucciones por capa ver: /skills/*
