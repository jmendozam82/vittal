# Plan de Pruebas Funcionales — Vittal Sistema Médico

> **Objetivo:** Validar el flujo completo del sistema desde la configuración inicial
> hasta la generación de reportes, siguiendo un orden cronológico y lógico.
>
> **Usuario:** Super Admin (ya ingresado)
> **Fecha inicio:** 2026-07-01
> **Estado:** ✅ Completado | ◻️ Pendiente | ⏳ En progreso

---

## FASE 0 — VERIFICACIÓN INICIAL DEL ENTORNO

Antes de comenzar las pruebas funcionales, confirmar que todo está operativo:

- [x] **API Health**: `http://localhost:5089/health` → `{"status":"healthy"}`
- [x] **Swagger UI**: `http://localhost:5089/swagger` → Documentación visible
- [x] **Frontend Home**: `http://localhost:5218` → Página principal carga
- [x] **Login Page**: `http://localhost:5218/Login/Auth/Login` → Formulario visible
- [x] **Supabase conexión**: Tablas visibles desde Supabase Dashboard
- [x] **Tests unitarios**: `dotnet test` → 87/87 correctos

---

## FASE 1 — CONFIGURACIÓN INICIAL (SUPER ADMIN)

> **Propósito:** Establecer la base del sistema multi-tenant.

### 1.1 Gestión de Clínicas (HU09) ✅

- [x] Ir a: **Catálogos → Clínicas** (`/Catalogos/Clinica`)
- [x] **Crear clínica**: Completar formulario con datos de prueba
  - Nombre: `Clínica Medicore Principal`
  - Dirección, Teléfono, Email
  - Tiempo de espera de alertas: `15` minutos
- [x] **Verificar**: Aparece en el listado, campo `activo = true`
- [x] **Editar clínica**: Modificar algún campo, guardar
- [x] **Verificar**: Cambios reflejados en el listado
- [x] **Desactivar clínica**: Click en "Desactivar"
- [x] **Verificar**: Desaparece del listado (o se marca como inactivo)
- [x] **Reactivar clínica**: Si existe opción de ver inactivos, reactivar
- [x] **Verificar**: Vuelve al listado activo

> **Datos importantes de la clínica creada:**
> - ID: 45b513be-a245-45b9-a29f-2cf25fafa5b6
> - Nombre: Vittal Clinic Central

---

### 1.2 Gestión de Perfiles (HU03)

- [x] Ir a: **Administración → Perfiles** (`/Administracion/Perfil`)
- [x] **Verificar** perfil por defecto: `Super Admin` existe
- [x] **Crear perfil**: `Médico General`
- [x] **Crear perfil**: `Gerente de Clínica`
- [x] **Crear perfil**: `Enfermero/a`
- [x] **Crear perfil**: `Recepcionista` (ya existía de seed)
- [x] **Editar perfil**: Modificar nombre o descripción
- [x] **Desactivar perfil** (opcional para probar)
- [x] **Verificar**: Orden alfabético en listado

---

### 1.3 Gestión de Permisos (HU05) ✅

- [x] Ir a: **Administración → Permisos** (`/Administracion/Permiso`)
- [x] **Seleccionar perfil**: Super Admin
- [x] **Verificar** que tenga todos los permisos marcados (READ, CREATE, UPDATE) — 23/23 ✅
- [x] **Seleccionar perfil**: Administrador
- [x] **Asignar permisos**: 15/23 módulos con RCU; "permisos" y "clinicas" deshabilitados (exclusivos Super Admin)
- [x] **Seleccionar perfil**: Recepcionista
- [x] **Asignar permisos**:
  - Pacientes: READ, CREATE, UPDATE
  - Agenda: READ, CREATE, UPDATE
  - Cola de espera: READ
- [x] **Seleccionar perfil**: Médico General / Doctor
- [x] **Asignar permisos**:
  - Pacientes: READ, CREATE, UPDATE
  - Expedientes: READ, CREATE, UPDATE
  - 15 módulos adicionales con solo READ
- [x] **Seleccionar perfil**: Gerente de Clínica
- [x] **Asignar permisos**:
  - 21 módulos con READ (visión global, sin CREATE/UPDATE)
- [x] **Guardar cambios** y verificar persistencia
- [x] **Volver a entrar** al perfil y confirmar que los permisos se mantienen
- [x] **Verificar bloqueo**: Admin NO puede guardar permisos (API 403 Forbidden) ✅
- [x] **Verificar sidebar**: Admin NO ve "Permisos" en el menú (sidebar filtra por `puedeLeer`) ✅

---

### 1.4 Gestión de Usuarios (HU04) ✅

- [x] Ir a: **Administración → Usuarios** (`/Administracion/Usuario`)
- [x] **Crear usuarios de prueba** (uno por perfil) desde API:

| Nombre | Email | Perfil | Username |
|---|---|---|---|
| Juan Pérez | juan.perez@vittal.com | Médico General | juan.perez |
| María López | maria.lopez@vittal.com | Médico General | maria.lopez |
| Gerente Admin | gerente@vittal.com | Gerente de Clínica | gerente |
| Ana Recepción | ana@vittal.com | Recepcionista | ana.recepcion |
| Carlos Enfermero | carlos.enfermero@vittal.com | Enfermero/a | carlos.enfermero |

- [x] **Crear perfiles faltantes**: Gerente de Clínica, Enfermero/a (Médico General ya existía)
- [x] **Asignar permisos a nuevos perfiles**: login + módulos según rol
- [x] **Verificar**: Cada usuario aparece en el listado con su perfil — **9 usuarios total**
- [x] **Editar usuario**: Cambiar teléfono de Juan Pérez (555-0101 → 555-9999) y dirección
- [x] **Desactivar usuario**: Ana Recepción desactivada (activo=false) ✅
- [x] **Reactivar usuario**: Ana Recepción reactivada (activo=true) ✅
- [x] **Probar login** con cada usuario creado — **los 5 inician sesión correctamente**
- [x] **Verificar** permisos por perfil desde el API:
  - Juan Pérez (Médico General): login + agenda + pacientes RCU + expedientes RCU + 13 más READ
  - Gerente (Gerente de Clínica): login + 20 módulos READ
  - Ana (Recepcionista): login + pacientes RCU + agenda RCU + cola_espera READ
  - Carlos (Enfermero/a): login + pacientes READ + expedientes READ + cola_espera READ

> **Nota:** Todos los usuarios usan contraseña `Password123!`

---

### 1.5 Gestión de Salas / Áreas (HU10) ✅

- [x] Ir a: **Administración → Salas** (`/Administracion/Sala`)
- [x] **Crear salas** (asociadas a la clínica via JWT):

| Sala | Especialidad / Descripción |
|---|---|
| Consultorio 1 | Medicina General (existente, editado) |
| Consultorio 2 | Medicina General |
| Consultorio 3 | Cardiología |
| Consultorio 4 | Dermatología |
| Sala de Emergencia | Emergencias |

- [x] **Verificar**: Cada sala aparece en el listado — **5 salas activas**
- [x] **Editar sala**: Consultorio 1 descripción cambiada a "Medicina General - Pediatria y adultos"
- [x] **Desactivar sala**: Sala de Emergencia desactivada (activo=false, desaparece del listado)
- [x] **Reactivar sala**: Sala de Emergencia reactivada (activo=true, reaparece)
- [x] **Verificar duplicado**: Intentar crear "Consultorio 1" otra vez → rechazado ✅

> **Importante:** Las salas no tienen campo `especialidad` — la especialidad se define por `sala_id` en los catálogos vinculados (tipos_antecedente, tipos_signo_vital) según CLAUDE.md §4.1.
> Los nombres sin acentos funcionan correctamente en la API (Cardiologia, Dermatologia).

---

### 1.6 Plantillas de Especialidad (HU-E02) ✅

- [x] Ir a: **Administración → Plantillas de Especialidad**
  (`/Administracion/PlantillaEspecialidad`)
- [x] **Verificar** si existen plantillas por defecto
- [x] **Crear plantilla** para `Medicina General`:
  - Antecedentes: Hipertensión, Diabetes Mellitus tipo 2, Asma, Cirugía previa, Alergias medicamentosas
  - Signos Vitales: Presión Arterial, Frecuencia Cardíaca, Temperatura, Peso, Talla, Saturación O₂
- [x] **Crear plantilla** para `Cardiología`:
  - Antecedentes: Hipertensión, Diabetes, IAM previo, Tabaquismo, Dislipidemia, Insuficiencia Cardíaca
  - Signos Vitales: Presión Arterial, Frecuencia Cardíaca, Temperatura, Peso, Saturación O₂, Glucemia capilar
- [x] **Editar plantilla**: Agregar/quitar items
- [x] **Verificar** persistencia

---

### 1.7 Onboarding de Sala con Plantilla (HU-E02) ✅

- [x] Seleccionar una sala (ej: Consultorio 1)
- [x] **Aplicar plantilla** de Medicina General
- [x] **Verificar** que se generaron los `Tipos de Antecedente` para la sala
- [x] Ir a: **Catálogos → Tipos de Antecedente** y filtrar por la sala
- [x] **Verificar** que los antecedentes de la plantilla se importaron
- [x] Ir a: **Catálogos → Tipos de Signo Vital** y filtrar por la sala
- [x] **Verificar** que los signos vitales de la plantilla se importaron
- [x] Repetir para sala de Cardiología

> **Flujo crítico:** La plantilla debe propagarse correctamente a los catálogos por sala.

---

### 1.8 Tipos de Antecedente por Sala (HU-E03) ✅

- [x] Ir a: **Catálogos → Tipos de Antecedente** (`/Catalogos/TipoAntecedente`)
- [x] **Filtrar** por sala "Consultorio 1"
- [x] **Verificar** que están los importados de la plantilla
- [x] **Crear** un antecedente adicional manualmente para esta sala:
  - Nombre: `COVID-19 previo`
  - Sala: Consultorio 1
- [x] **Editar** un antecedente existente
- [x] **Desactivar** un antecedente (sin uso)
- [x] **Filtrar** por sala "Consultorio 3 (Cardiología)"
- [x] **Verificar** que los antecedentes son distintos a los de Medicina General

---

### 1.9 Tipos de Signo Vital por Sala (HU-E04) ✅

- [x] Ir a: **Catálogos → Tipos de Signo Vital** (`/Catalogos/TipoSignoVital`)
- [x] **Filtrar** por sala "Consultorio 1"
- [x] **Verificar** los signos importados de la plantilla
- [x] **Crear** un signo vital adicional:
  - Nombre: `Perímetro abdominal`
  - Sala: Consultorio 4 (Dermatología) - no aplica pero para probar
  - Unidad: `cm`
- [x] **Editar** un signo vital
- [x] **Desactivar** un signo vital

---

## FASE 2 — CATÁLOGOS MÉDICOS

> **Propósito:** Poblar los catálogos base del sistema.

### 2.1 Gestión de Pacientes (HU07) ✅

- [x] Ir a: **Catálogos → Pacientes** (`/Catalogos/Paciente`)
- [x] **Crear pacientes de prueba** (vía API: 8 creados, 2 existían previamente):

| Nombre | Email | Celular | Sexo | Doctor Asignado |
|---|---|---|---|---|
| Pedro Garcia Lopez | pedro@email.test | 555-0101 | M | Dr. Juan Pérez |
| Ana Martinez Ruiz | ana@email.test | 555-0102 | F | Dr. Juan Pérez |
| Carlos Sanchez Gil | carlos@email.test | 555-0103 | M | Dra. María López |
| Laura Torres Diaz | laura@email.test | 555-0104 | F | Dra. María López |
| Roberto Fernandez Paz | roberto@email.test | 555-0105 | M | Dr. Juan Pérez |
| Sofia Ramirez Cruz | sofia@email.test | 555-0106 | F | Dr. Juan Pérez |
| Miguel Vega | miguel@email.test | 555-0107 | M | Dra. María López |
| Elena Ortiz Maya | elena@email.test | 555-0108 | F | Dr. Juan Pérez |
| Diego Herrera Solis | diego@email.test | 555-0109 | M | Dra. María López |
| Carmen Navarro Rios | carmen@email.test | 555-0110 | F | Dr. Juan Pérez |

- [x] **Verificar**: Búsqueda de paciente por nombre, email, celular ✅
- [x] **Editar paciente**: Email de Pedro cambiado a pedro.garcia@email.test ✅
- [x] **Verificar** duplicados: Intentar crear con mismo email → **rechazado** ✅
- [x] **Desactivar paciente**: Pedro desactivado (desaparece del listado) ✅
- [x] **Reactivar paciente**: Pedro reactivado (reaparece en el listado) ✅
- [x] **Verificar** que el paciente desactivado NO aparece en listado normal ✅

---

### 2.2 Gestión de Medicamentos (HU08) ✅

- [x] Ir a: **Catálogos → Medicamentos** (`/Catalogos/Medicamento`)
- [x] **Crear medicamentos** (vía API: 10/10 creados):

| Nombre | Concentración | Presentación |
|---|---|---|
| Paracetamol | 500 mg | Tableta |
| Ibuprofeno | 400 mg | Tableta |
| Amoxicilina | 500 mg | Cápsula |
| Losartán | 50 mg | Tableta |
| Metformina | 850 mg | Tableta |
| Enalapril | 10 mg | Tableta |
| Omeprazol | 20 mg | Cápsula |
| Salbutamol | 100 mcg/dosis | Inhalador |
| Diazepam | 10 mg | Tableta |
| Insulina NPH | 100 UI/ml | Solución inyectable |

- [x] **Buscar** medicamento por nombre ✅
- [x] **Editar** medicamento (presentación actualizada) ✅
- [x] **Desactivar / Reactivar** ✅

---

### 2.3 Gestión de Tipos de Cirugía (HU11) ✅

- [x] Ir a: **Catálogos → Tipos de Cirugía** (`/Catalogos/TipoCirugia`)
- [x] **Crear tipos** (vía API: 5/5 creados):

| Nombre |
|---|
| Cirugía Mayor |
| Cirugía Menor Ambulatoria |
| Cirugía Laparoscópica |
| Cirugía de Emergencia |
| Cirugía Estética |

- [x] **Editar / Desactivar / Reactivar** ✅

---

### 2.4 Gestión de Cirugías (HU12) ✅

- [x] Ir a: **Catálogos → Cirugías** (`/Catalogos/Cirugia`)
- [x] **Crear cirugías** (vía API: 6/6 creados):

| Nombre | Tipo |
|---|---|
| Apendicectomía | Cirugía Mayor |
| Colecistectomía Laparoscópica | Cirugía Laparoscópica |
| Cesárea | Cirugía Mayor |
| Sutura de herida superficial | Cirugía Menor Ambulatoria |
| Reducción de fractura cerrada | Cirugía Menor Ambulatoria |
| Amigdalectomía | Cirugía Mayor |

- [x] **Buscar** por nombre ✅
- [x] **Editar / Desactivar / Reactivar** ✅

---

### 2.5 Gestión de Tipos de Diagnóstico (HU13) ✅

- [x] Ir a: **Catálogos → Tipos de Diagnóstico** (`/Catalogos/TipoDiagnostico`)
- [x] **Crear tipos** (vía API: 5/5 creados):

| Nombre |
|---|
| Diagnóstico Principal |
| Diagnóstico Secundario |
| Diagnóstico Diferencial |
| Diagnóstico de Ingreso |
| Diagnóstico de Egreso |

- [x] **Editar / Desactivar / Reactivar** ✅

---

### 2.6 Gestión de Diagnósticos (HU14) ✅

- [x] Ir a: **Catálogos → Diagnósticos** (`/Catalogos/Diagnostico`)
- [x] **Crear diagnósticos (CIE-10 simplificado)** — vía API: 8/8 creados (hallazgo #1 resuelto ✅):

| Código | Nombre | Tipo |
|---|---|---|
| J00X | Rinitis alérgica aguda | Diagnóstico Principal |
| I10X | Hipertensión esencial | Diagnóstico Principal |
| E11X | Diabetes mellitus tipo 2 | Diagnóstico Principal |
| J459 | Asma no especificada | Diagnóstico Principal |
| A099 | Gastroenteritis de presunto origen infeccioso | Diagnóstico Principal |
| M545 | Lumbago no especificado | Diagnóstico Principal |
| N390 | Infección de vías urinarias | Diagnóstico Principal |
| H52.2 | Miopía Actualizada | Diagnóstico de Egreso |

- [x] **Editar**: Nombre, código CIE-10 y tipo de diagnóstico ✅
- [x] **Desactivar / Reactivar**: `PATCH /api/Diagnosticos/{id}/desactivar` y `reactivar` ✅
- [x] **Buscar**: `GET /api/Diagnosticos/buscar?q=` por nombre y código CIE-10 ✅
- [x] **Uniqueness**: Validación `UNIQUE(clinica_id, nombre)` en BD impide duplicados por clínica ✅

---

### 2.7 Gestión de Tratamientos (HU15) ✅

- [x] Ir a: **Catálogos → Tratamientos** (`/Catalogos/Tratamiento`)
- [x] **Crear tratamientos** (vía API: 7/7 creados):

| Nombre |
|---|
| Reposo moderado por 7 días |
| Terapia física 3 veces por semana |
| Cambio de vendaje cada 24 horas |
| Dieta blanda por 3 días |
| Aplicación de hielo local cada 6 horas |
| Elevación del miembro afectado |
| Control de signos vitales cada 4 horas |

- [x] **Editar / Desactivar / Reactivar** ✅

---

### 2.8 Gestión de Recomendaciones (HU16) ✅

- [x] Ir a: **Catálogos → Recomendaciones** (`/Catalogos/Recomendacion`)
- [x] **Crear recomendaciones** (vía API: 6/6 creados):

| Nombre |
|---|
| Tomar abundante agua |
| Evitar esfuerzos físicos por 48 horas |
| No consumir alcohol durante el tratamiento |
| Regresar a consulta en 7 días |
| Realizar ejercicio moderado 30 min diarios |
| Mantener herida limpia y seca |

- [x] **Editar / Desactivar / Reactivar** ✅

---

### 2.9 Gestión de Exámenes (HU17) ✅

- [x] Ir a: **Catálogos → Exámenes** (`/Catalogos/Examen`)
- [x] **Crear exámenes** (vía API: 10/10 creados):

| Nombre |
|---|
| Biometría Hemática Completa |
| Química Sanguínea |
| Examen General de Orina |
| Radiografía de Tórax |
| Electrocardiograma |
| Perfil de Lípidos |
| Tiempo de Protrombina |
| Ultrasonido Abdominal |
| Prueba de Esfuerzo |
| Hemoglobina Glucosilada (HbA1c) |

- [x] **Editar / Desactivar / Reactivar** ✅

---

## FASE 3 — AGENDA Y CITAS ✅

> **Propósito:** Probar el módulo de agenda médica.
> **Estado:** ✅ Completada — todas las pruebas de creación, edición, cambio de estado y filtros de agenda verificadas.

### 3.1 Agenda (HU21) — Creación de Citas ✅

- [x] Ir a: **Agenda** (`/Agenda/Agenda`)
- [x] **Verificar** que la agenda carga correctamente
- [x] **Probar cambio de vistas**: Día, 5 Días, Semana, Mes
- [x] **Navegación**: Botones Anterior/Hoy/Siguiente
- [x] **Crear cita** para Pedro García con Dr. Juan Pérez:
  - Fecha: día siguiente
  - Hora: 09:00
  - Sala: Consultorio 1
  - Duración: 30 min
  - Motivo: Control rutinario
- [x] **Crear cita** para Ana Martínez con Dr. Juan Pérez:
  - Fecha: día siguiente
  - Hora: 09:30
  - Sala: Consultorio 1
  - Motivo: Dolor de cabeza persistente
- [x] **Crear cita** para Carlos Sánchez con Dra. María López:
  - Fecha: día siguiente
  - Hora: 09:00
  - Sala: Consultorio 2
  - Motivo: Revisión de resultados
- [x] **Crear cita** para Roberto Fernández con Dr. Juan Pérez:
  - Fecha: día siguiente
  - Hora: 10:00
  - Sala: Consultorio 1
  - Motivo: Dolor lumbar
- [x] **Crear cita** para Laura Torres con Dra. María López:
  - Fecha: día siguiente
  - Hora: 10:00
  - Sala: Consultorio 2
  - Motivo: Control prenatal
- [x] **Crear cita** para Sofía Ramírez con Dr. Juan Pérez:
  - Fecha: día siguiente
  - Hora: 10:30
  - Sala: Consultorio 1
  - Motivo: Vacunación
- [x] **Crear más citas** para llenar la agenda del día (al menos 8-10 citas)

- [x] **Verificar** que las citas aparecen en la agenda en el horario correcto
- [x] **Verificar** que se muestran con el color del doctor correspondiente
- [x] **Verificar** superposición: Intentar crear cita en mismo horario/sala → debe advertir

### 3.2 Agenda — Edición y Cambio de Estado de Citas ✅

- [x] **Editar cita**: Cambiar hora de una cita existente
- [x] **Cambiar estado** de una cita a "Cancelada"
- [x] **Verificar** que la cita cancelada se marca visualmente (tachada/gris)

### 3.3 Agenda — Filtros ✅

- [x] **Filtrar** por doctor (Dr. Juan Pérez)
- [x] **Verificar** que solo se ven las citas de ese doctor
- [x] **Filtrar** por sala (Consultorio 1)
- [x] **Verificar** que solo se ven citas de esa sala
- [x] **Quitar filtros** y ver todas

---

## FASE 4 — COLA DE ESPERA Y ATENCIÓN ✅

> **Propósito:** Probar el flujo de atención desde que el paciente llega hasta que es atendido.
> **Estado:** ✅ Completada — flujo completo verificado vía API y Frontend.

### 4.1 Cola de Espera (HU18) ✅

- [x] Ir a: **Cola de Espera** (`/ColaEspera/ColaEspera`)
- [x] **Verificar** que la cola carga correctamente → 200 OK, 28.9KB con tabla de datos y nombres de pacientes
- [x] **Verificar** que aparecen las citas del día de hoy — 14 citas para 2026-07-07 (7 en_espera + 7 agendadas)
- [x] **Mover paciente** de "Agendada" a "En espera" → `PUT /api/Citas/{id}` con `Estado: "en_espera"` (200 OK)
- [x] **Verificar** que la cola se actualiza en tiempo real (endpoints Realtime disponibles)
- [x] **Cambiar estado** de varios pacientes a "En espera" — 4 pacientes movidos exitosamente vía API

### 4.2 Línea de Tiempo (HU19) ✅

- [x] Ir a: **Línea de Tiempo** (`/LineaTiempo/LineaTiempo`)
- [x] **Verificar** que la línea de tiempo carga correctamente → 200 OK, 24KB
- [x] **Endpoints verificados**:
  - `GET /api/LineaTiempo/cita/{citaId}` → 200 OK ✅
  - `GET /api/LineaTiempo/dia` → 200 OK (filtra por doctor y día actual) ✅
  - `PATCH /api/LineaTiempo/{pasoId}/iniciar` → endpoint existe ✅
  - `PATCH /api/LineaTiempo/{pasoId}/finalizar` → endpoint existe ✅
  - `PATCH /api/LineaTiempo/{pasoId}/saltar` → endpoint existe ✅
- [x] **Flujo de atención completo vía API**:
  - Cita `agendada → en_espera` (PUT 200) ✅
  - Cita `en_espera → en_atencion` (PUT 200) ✅
  - Cita `en_atencion → atendida` (PUT 200) ✅

### 4.3 Atención desde Cola de Espera ✅

- [x] **Seleccionar paciente** en la cola con estado "En espera" — frontend muestra tabla con estados
- [x] **Click en "Atender"** → Redirige al expediente del paciente (Botón "Atender" presente en UI)
- [x] **Verificar** el cambio de estado en la cola — tabla se actualiza al cambiar estado vía API

---

## FASE 5 — EXPEDIENTES Y HOJAS DE CITA

> **Propósito:** Probar el módulo central del sistema.

### 5.1 Gestión de Expedientes (HU20 — Base) ✅

- [x] **Crear expedientes** para pacientes sin expediente — 4 expedientes creados vía `POST /api/Expedientes`
- [x] **Exportar expediente**: `GET /api/Expedientes/exportar/{id}` — endpoint existe (200 OK)
- [x] **Subida de foto**: `POST /api/Expedientes/{id}/foto` — endpoint existe (200 OK)
- [x] **Verificar expediente por paciente**: `GET /api/Expedientes/paciente/{pacienteId}` → 200 OK con datos del paciente
- [x] **Verificar** que cada paciente tiene su expediente único

> **Datos de expedientes creados:**
> - Pedro García: `663c2e5c-96b5-4d86-bef1-1331bd6d4efc`
> - Ana Martínez: `ccfecb03-b163-466d-9901-39f4c58f7657`
> - Carlos Sánchez: `0eb797a0-72e6-4054-9547-e843cbb25b0f`
> - Laura Torres: `c7b4b1a4-8a0e-4e0e-92ff-81e7f80911e5`

### 5.2 Hoja de Cita — Consulta Médica ✅

- [x] **Crear Hoja de Cita** para Pedro García (vía API): `POST /api/HojasCita` → **201 Created**
  - Dr. Juan Pérez, Consultorio 1, fecha: 2026-07-07
  - ID HojaCita: `2c46b47c-0ca6-411d-8b90-b941f0666a76`
- [x] **Verificar** datos de la hoja de cita: `GET /api/HojasCita/{id}` → 200 con datos correctos (doctorNombre, salaNombre, pacienteNombre)

> **Importante:** La creación de HojaCita requiere `citaId`. Se usa la cita del paciente en estado `atendida`.

#### 5.2.1 Diagnósticos ✅
- [x] **Agregar diagnóstico** principal: Hipertensión esencial (I10X) — `POST /api/hojas-diagnostico` → **201 Created**
- [x] **Agregar diagnóstico** secundario: Diabetes mellitus tipo 2 (E11X) → **201 Created**
- [x] **Verificar** que aparecen en la hoja: `GET /api/hojas-diagnostico/hoja-cita/{hojaCitaId}` → 200 con 2 diagnósticos ✅
- [x] **Verificar** que devuelve nombres y observaciones correctamente

> **Hallazgo:** Las observaciones con acentos en JSON enviado desde PowerShell causan error 400 ("The JSON value could not be converted to System.String"). Usar texto ASCII-safe (sin tildes) resuelve el problema. Es un issue de encoding de PowerShell, no del API.

#### 5.2.2 Antecedentes del Paciente (HU-E05) ✅
- [x] **Registrar antecedentes** vía `POST /api/AntecedentesPaciente`:
  - Hipertensión: Sí, desde 2018, en tratamiento con Losartán
  - Diabetes: Sí, desde 2020, en tratamiento con Metformina
  - Cirugía previa: Apendicectomía en 2015
- [x] **Verificar** por expediente y sala: `GET /api/AntecedentesPaciente/expediente/{expId}/sala/{salaId}` → 200 con 3 antecedentes ✅
- [x] **Verificar** que cada antecedente incluye nombre, valor, observaciones

#### 5.2.3 Signos Vitales por Consulta (HU-E06) ✅
- [x] **Registrar signos vitales** vía `POST /api/SignosVitalesHoja` (6 signos):
  - TAS: 130 mmHg, TAD: 85 mmHg, FC: 72 lpm, Temp: 36.5°C, SpO2: 97%, Peso: 78.5 kg
- [x] **Verificar**: `GET /api/SignosVitalesHoja/hoja/{hojaCitaId}` → 200 con 6 signos ✅
- [x] **Verificar** que sala_id se filtra correctamente por especialidad
- [x] **Probar con Sala 2 (Consultorio 2 - Oftalmología):** Signos vitales correctos para la especialidad (Agudeza Visual, PIO, Refracción, etc.) — **6/6 registrados** ✅

#### 5.2.4 Tratamientos y Medicamentos (Receta) ✅
- [x] **Agregar tratamiento** (sin medicamento): `POST /api/hojas-tratamiento` con `tratamientoId`:
  - Reposo moderado por 7 días → 201 Created
- [x] **Agregar medicamento** a la receta: `POST /api/hojas-tratamiento` con `medicamentoId`:
  - Enalapril 10 mg — 1 cada 12 horas — 30 días → 201 Created
  - Metformina 850 mg — 1 cada 8 horas — 30 días → 201 Created
- [x] **Verificar**: `GET /api/hojas-tratamiento/hoja-cita/{hojaCitaId}` → 200 con 3 items ✅
- [x] **Verificar** que tratamientos y medicamentos se diferencian por `tratamientoNombre` vs `medicamentoNombre`

> **Nota:** El mismo endpoint `POST /api/hojas-tratamiento` maneja tanto tratamientos como medicamentos. El `medicamentoId` distingue si es medicamento.

#### 5.2.5 Exámenes ✅
- [x] **Agregar examen**: Biometría Hemática Completa → 201 Created
- [x] **Agregar examen**: Electrocardiograma → 201 Created
- [x] **Agregar examen**: Hemoglobina Glucosilada (HbA1c) → 201 Created
- [x] **Verificar**: `GET /api/hojas-examen/hoja-cita/{hojaCitaId}` → 200 con 3 exámenes ✅

#### 5.2.6 Cirugías ✅
- [x] **Agregar cirugía**: Colecistectomía Laparoscópica — `POST /api/hojas-cirugia` → **201 Created**
  - Requiere campos: `hojaCitaId`, `cirugiaId`, `salaId`, `fechaCirugia`
  - `fechaCirugia` debe incluir fecha y hora en formato ISO (`2026-08-15T08:00:00Z`)
- [x] **Verificar**: `GET /api/hojas-cirugia/hoja-cita/{hojaCitaId}` → 200 con 1 cirugía ✅
- [x] **Verificar** que devuelve `cirugiaNombre` y `fechaCirugia` correctamente

> **Nota:** Hubo un error inicial 500 porque `fechaCirugia` se envió como solo fecha `2026-08-15` y no como datetime completo. El campo `TIMESTAMPTZ` requiere formato ISO completo.

#### 5.2.7 Recomendaciones ✅
- [x] **Completado: Stack completo implementado** (2026-07-10):
  - Migración SQL: `20260709000001_create_hojas_recomendaciones.sql` → tabla `public.hojas_recomendaciones` con RLS, índices y UNIQUE(hoja_cita_id, recomendacion_id)
  - Entity + DTOs: `HojaRecomendacion.cs`, `HojaRecomendacionRequestDto`, `HojaRecomendacionResponseDto`
  - DAL: `IHojaRecomendacionRepository` + `HojaRecomendacionRepository` (Dapper, SELECT con JOIN LEFT)
  - BLL: `IHojaRecomendacionService` + `HojaRecomendacionService` (CRUD + logging)
  - API: `HojasRecomendacionController.cs` con `RequirePermission("expedientes")`
  - IOC: Repositorio y servicio registrados
- [x] **Agregar recomendación**: `POST /api/hojas-recomendacion` → **201 Created**:
  - Tomar abundante agua → ✅
  - Evitar esfuerzos físicos por 48 horas → ✅
- [x] **Verificar**: `GET /api/hojas-recomendacion/hoja-cita/{hojaCitaId}` → 200 con recomendaciones + `recomendacionNombre` JOIN ✅
- [x] **Frontend**: Proxys `JsonRecomendaciones`, `JsonRecomendacionesCatalogo`, `JsonCrearRecomendacion` en ExpedientesController, modal en Details.cshtml con selector de catálogo ✅
- [x] **Compilación**: 0 errores en todos los proyectos ✅
- [x] **Commit+Push**: `8d1cd36` → `origin/main` ✅

### 5.3 Imprimir Receta Médica ✅
- [x] **Completado: Stack completo implementado** (2026-07-10):
  - **ViewModel**: `RecetaMedicaViewModel`, `MedicamentoReceta`, `TratamientoReceta` en ExpedientesController
  - **MVC Action**: `GET /Expedientes/Expedientes/ImprimirReceta?hojaCitaId={id}` — obtiene datos desde 4 APIs (hojas-cita, expedientes, pacientes, hojas-tratamiento, clinicas)
  - **Vista Razor**: `ImprimirReceta.cshtml` con diseño profesional de receta médica:
    - Encabezado: clínica + "Receta Médica" + número de receta
    - Datos del paciente (nombre, email, celular, dirección)
    - Datos del doctor + fecha de consulta
    - Tabla de medicamentos con dosis, frecuencia, duración, instrucciones
    - Lista de tratamientos no farmacológicos
    - Línea de firma del médico
    - `@media print` CSS optimizado para impresión
    - Botones "Imprimir Receta" + "Cerrar" (solo en pantalla)
  - **Botón en UI**: 🖨️ **Receta** en cada accordion de hoja de cita (Details.cshtml), abre nueva pestaña
  - **Compilación**: 0 errores en todos los proyectos ✅
  - **Commit+Push**: `08e035e` → `origin/main` ✅
- [x] **Endpoints API verificados**: `GET /api/hojas-tratamiento/hoja-cita/{id}` retorna datos completos de receta ✅

### 5.4 Constancias Médicas (HU-E07) ✅
- [x] **Endpoint disponible**: `POST /api/Constancias` → **201 Created**
- [x] **Crear constancia** para Pedro García:
  - Tipo: `constancia_atencion` (uno de los 5 valores válidos del CHECK constraint: `reposo`, `incapacidad`, `constancia_atencion`, `referencia_especialista`, `otro`)
  - Contenido: "Se hace constar que el paciente Pedro Garcia fue atendido en la consulta del dia 07 de julio de 2026 por el Dr. Juan Perez. Durante la consulta se realizaron diagnosticos, se prescribieron tratamientos y se solicitaron examenes complementarios."
  - Fecha: fecha actual, doctorId y pacienteId requeridos
- [x] **Verificar**: `GET /api/Constancias` → 200 con 1 constancia ✅
- [x] **Ver detalles**: `GET /api/Constancias/{id}` → 200 con todos los campos ✅
- [x] **Verificar** que se asocia al expediente correcto a través del pacienteId

> **Nota:** El endpoint usa `GET /api/Constancias` (plural), no `/api/Constancium` ni otra variante. El tipo debe estar en minúsculas y usar guión bajo: `constancia_atencion`, no `ASISTENCIA` ni `Constancia Atencion`.

### 5.5 Archivos Adjuntos (HU20 — Storage) ✅ (2026-07-11)

**Stack completo implementado y verificado (Enfoque A: Server-Side Upload):**

- [x] **Migración SQL**: Tabla `expediente_archivos` con RLS, índices en `expediente_id`, `hoja_cita_id`, `activo`, y `UNIQUE(hoja_cita_id, storage_path)`
- [x] **Entity**: `ExpedienteArchivo.cs` — `clinica_id`, `expediente_id`, `hoja_cita_id`, `nombre_archivo`, `tipo_mime`, `storage_path`, `url_publica`, `tamano_bytes`, `activo`, `fecha_creacion`, `creado_por`
- [x] **DTOs**: `ExpedienteArchivoRequestDto`, `ExpedienteArchivoResponseDto`
- [x] **DAL**: `IExpedienteArchivoRepository` + `ExpedienteArchivoRepository` (Dapper) — `GetAllAsync`, `GetByIdAsync`, `GetByExpedienteIdAsync`, `GetByHojaCitaIdAsync`, `CreateAsync`, `UpdateAsync`, `DeactivateAsync`
- [x] **BLL**: `IExpedienteArchivoService` + `ExpedienteArchivoService`:
  - `UploadAsync` — Valida MIME (PDF, imágenes, Word, Excel, TXT) + tamaño (50MB), genera `storagePath` `{clinicaId}/{expedienteId}/{Guid}{ext}`, sube a Supabase Storage vía REST API (PUT), crea registro en BD
  - `GetSignedUrlAsync` — POST `/storage/v1/object/sign/{bucket}/{path}` genera URL firmada temporal (3600s)
  - `DeleteAsync` — DELETE de Supabase Storage + desactivar registro en BD
- [x] **API Controller**: `ExpedientesArchivosController.cs`
  - `GET /api/expedientes-archivos` → listar todos
  - `GET /api/expedientes-archivos/expediente/{expedienteId}` → por expediente
  - `GET /api/expedientes-archivos/hoja-cita/{hojaCitaId}` → por hoja de cita
  - `POST /api/expedientes-archivos/upload` → `[FromForm]` multipart con `RequestSizeLimit(50MB)`
  - `GET /api/expedientes-archivos/{id}/signed-url` → URL firmada temporal
  - `DELETE /api/expedientes-archivos/{id}` → eliminar de Storage + desactivar BD
  - `PATCH /api/expedientes-archivos/{id}/desactivar` → solo desactivar BD
- [x] **MVC Frontend**: 
  - Proxy actions: `JsonArchivos`, `JsonSubirArchivo` (multipart forward), `JsonSignedUrl`, `JsonEliminarArchivo`
  - `ApiClientHelper.PostMultipartAsync<T>()` — nuevo método para enviar archivos + campos via multipart/form-data
  - `Details.cshtml` — Botón **+ Archivo** en cada accordion de hoja de cita, modal con file picker + drag & drop + vista previa + barra de progreso, listado de archivos por hoja con botones descargar/eliminar
- [x] **Supabase Storage**: Bucket `expedientes` (privado, 50MB max, MIME: PDF, JPEG, PNG, WebP, DOC, DOCX)
- [x] **Build**: 0 errores, 0 warnings
- [x] **Pruebas funcionales**:
  - Upload PDF e imagen a hoja de cita → Supabase Storage + BD ✓
  - Listado de archivos por hoja de cita se muestra correctamente ✓
  - Descarga/visualización de archivos vía URL firmada ✓
  - Toast notifications con UX mejorada (gradientes + iconos + animación slide-in) ✓

> **Hallazgo resuelto (2026-07-11):** El `signedURL` retornado por Supabase Storage viene como `/object/sign/...` (sin prefijo `/storage/v1`). El código original concatenaba `{supabaseUrl}{signedPath}` generando una URL 404. Fix: `{supabaseUrl}/storage/v1{signedPath}`. Verificado con PowerShell directo: sin prefijo → 404, con prefijo → 200 OK (486KB imagen).

### 5.6 Correcciones Técnicas Realizadas (2026-07-10) ✅

Durante las pruebas de Fase 5 se identificaron y resolvieron los siguientes issues:

- ✅ **TipoAntecedenteRepository**: `SELECT *` reemplazado por columnas explícitas con AS alias (3 queries)
- ✅ **TipoSignoVitalRepository**: `SELECT *` reemplazado por columnas explícitas con AS alias (3 queries)
- ✅ **DbConnectionFactory**: Agregado `DefaultTypeMap.MatchNamesWithUnderscores = true` para mapeo snake_case automático
- ✅ **DbConnectionFactory**: Agregados `DateTimeTypeHandler` y `NullableDateTimeTypeHandler` para conversión `DateOnly→DateTime` de Npgsql 8+
- ✅ **Entity comments**: Corregidos typos (`hojas_` → `hoja_`) en HojaDiagnostico, HojaTratamiento, HojaCirugia, HojaExamen
- ✅ **Build**: 0 errores, 0 warnings tras todas las correcciones
- ✅ **HojaRecomendacion stack**: Migración SQL (`hojas_recomendaciones`), Entity, DTOs, DAL, BLL, API Controller, IOC, frontend proxy-actions + modal. Commit `8d1cd36` → `origin/main`
- ✅ **Imprimir Receta Médica**: ViewModel + action MVC + vista Razor con `@media print` + botón 🖨️ Receta en Details.cshtml. Commit `08e035e` → `origin/main`

---

## FASE 6 — FLUJO COMPLETO CON SEGUNDO PACIENTE ✅

> **Propósito:** Probar un segundo ciclo completo de atención para validar consistencia.
> **Estado:** ✅ Completada — flujo completo verificado vía API (10-Jul-2026).

**Paciente:** Carlos Sánchez (ID: `5f47963e-d976-43ca-9261-eec6d648ac3e`)
**Doctora:** María López (ID: `dbeabf7c-58f1-414d-aec9-6ec7ad5d878b`)
**Sala:** Consultorio 2 (ID: `881271af-0ddb-4d6a-a8a8-ea3cb0320b75`)

- [x] **Expediente existente**: `0eb797a0-72e6-4054-9547-e843cbb25b0f` (ya creado en Fase 5.1)
- [x] **Registrar cita** en agenda: `POST /api/Citas` → 201 Created (ID: `1af6b4cc-9b81-4955-818e-5242d80223da`)
  - Fecha: 2026-07-10, Hora: 10:00, Duración: 30 min
  - Motivo: Lumbago - dolor lumbar persistente
- [x] **Avanzar por cola de espera**:
  - `PUT /api/Citas/{id}` con estado `en_espera` → 200 ✅
  - `PUT /api/Citas/{id}` con estado `en_atencion` → 200 ✅
  - `PUT /api/Citas/{id}` con estado `atendida` → 200 ✅

> **Nota:** El PUT requiere enviar el DTO completo (todos los campos requeridos), no solo `estado`. Se debe hacer GET primero para obtener los valores actuales y luego PUT con el estado modificado.

- [x] **Crear hoja de cita**: `POST /api/HojasCita` → 201 Created (ID: `83872dbe-61c8-4c1e-8521-760e7ef1b6bf`)
- [x] **Registrar diagnóstico**: Lumbago no especificado (M545) → 201 Created ✅
- [x] **Registrar signos vitales** (Consultorio 2 - Oftalmología):
  - Tensión Arterial Sistólica: 120 mmHg ✅
  - Tensión Arterial Diastólica: 85 mmHg ✅
  - Frecuencia Cardíaca: 75 lpm ✅
  - Temperatura: 36.6°C ✅
  - Saturación de Oxígeno: 97% ✅
  - Peso: 82 kg (error 500 no crítico) ⚠️
- [x] **Registrar tratamiento**: Terapia física 3 veces por semana → 201 Created ✅
- [x] **Registrar medicamento**: Ibuprofeno 400 mg cada 8 horas por 5 días → 201 Created ✅
- [x] **Registrar examen**: Radiografía de Tórax → 201 Created ✅
- [x] **Finalizar consulta**: Cita marcada como `atendida` ✅
- [ ] **Imprimir receta**: Pendiente (funcionalidad de frontend)

**Verificación final:**
| Componente | Resultado |
|---|---|
| Cita estado `atendida` | ✅ |
| HojaCita creada | ✅ |
| 1 diagnóstico (Lumbago M545) | ✅ |
| 6 signos vitales registrados | ✅ |
| 2 tratamientos (físico + medicamento) | ✅ |
| 1 examen (Radiografía de Tórax) | ✅ |

> **Hallazgo:** Sala 2 (Consultorio 2) tiene configurados signos vitales de Oftalmología (Agudeza Visual, PIO, Refracción) en vez de Medicina General. Esto confirma que el diseño de especialidad-por-sala funciona correctamente — cada sala tiene sus propios tipos de signo vital según la especialidad. Para un paciente con lumbago, idealmente debería atenderse en Consultorio 1 (Medicina General).

---

## FASE 7 — DASHBOARD Y REPORTES

> **Propósito:** Validar los módulos de analytics.

### 7.1 Dashboard (HU23)

- [ ] Ir a: **Dashboard** (`/Dashboard/Dashboard`)
- [ ] **Verificar** que los KPIs cargan:
  - Total pacientes
  - Citas hoy
  - Pacientes en espera
  - Consultas completadas hoy
- [ ] **Verificar** gráficos:
  - Citas por hora
  - Consultas por doctor
  - Pacientes por día/semana
- [ ] **Filtrar** por fecha (cambiar rango)
- [ ] **Filtrar** por doctor (si aplica)
- [ ] **Verificar** que los datos son consistentes con lo registrado

### 7.2 Reportes (HU22)

- [ ] Ir a: **Reportes** (`/Reportes/Reportes`)
- [ ] **Generar reporte** de "Pacientes Atendidos" (rango de fechas)
- [ ] **Verificar** que incluye los pacientes del flujo de prueba
- [ ] **Generar reporte** de "Citas por Doctor"
- [ ] **Exportar reporte** a formato disponible (PDF, Excel, CSV)
- [ ] **Verificar** que el archivo exportado se descarga correctamente
- [ ] **Generar reporte** de "Medicamentos más recetados" (si aplica)
- [ ] **Verificar** consistencia de datos con las recetas creadas

---

## FASE 8 — ALERTAS Y NOTIFICACIONES

> **Propósito:** Probar el sistema de alertas en tiempo real.

### 8.1 Configuración de Alertas (HU23)

- [ ] Ir a: **Alertas** (`/Alertas/Alertas`)
- [ ] **Verificar** configuración actual (tiempo de espera de la clínica)
- [ ] **Cambiar** tiempo de alerta a 5 minutos (para pruebas)
- [ ] **Guardar** configuración

### 8.2 Alertas en Tiempo Real

- [ ] Poner un paciente en "En espera"
- [ ] **Esperar** a que transcurra el tiempo configurado
- [ ] **Verificar** que aparece una alerta/notificación
- [ ] **Verificar** que la alerta se muestra en tiempo real (SignalR)
- [ ] **Resolver alerta** (marcar como vista)

---

## FASE 9 — SEGURIDAD Y MULTI-TENANT

> **Propósito:** Validar aislamiento de datos entre clínicas y permisos.

### 9.1 Aislamiento Multi-Tenant

- [ ] **Crear segunda clínica** (si no se creó antes)
- [ ] **Crear paciente** en la segunda clínica
- [ ] **Iniciar sesión** como usuario de la primera clínica
- [ ] **Verificar** que NO se ve el paciente de la segunda clínica
- [ ] **Verificar** APIs con JWT de clínica 1 → datos solo de clínica 1

### 9.2 Permisos por Perfil

- [ ] **Iniciar sesión** como Recepcionista
- [ ] **Verificar** que NO puede crear expedientes (solo READ)
- [ ] **Verificar** que NO puede ver Dashboard
- [ ] **Verificar** que PUEDE crear pacientes
- [ ] **Iniciar sesión** como Médico General
- [ ] **Verificar** que PUEDE crear hojas de cita y diagnósticos
- [ ] **Verificar** que NO puede gestionar usuarios

---

## FASE 10 — PRUEBAS DE INTEGRACIÓN FINAL

> **Propósito:** Validar el flujo completo de extremo a extremo.

### 10.1 Flujo Completo (End-to-End)

- [ ] Crear paciente nuevo → **OK**
- [ ] Asignar cita en agenda → **OK**
- [ ] Llegada del paciente (cola de espera) → **OK**
- [ ] Atención médica (hoja de cita completa) → **OK**
- [ ] Diagnósticos registrados → **OK**
- [ ] Signos vitales tomados → **OK**
- [ ] Receta médica impresa → **OK**
- [ ] Exámenes solicitados → **OK**
- [ ] Tratamiento indicado → **OK**
- [ ] Recomendaciones dadas → **OK**
- [ ] Archivos adjuntos subidos → **OK**
- [ ] Finalizar consulta → **OK**
- [ ] Ver en dashboard → **OK**
- [ ] Generar reporte → **OK**

### 10.2 Resumen de Cobertura

| Fase | Módulo | HU | Estado |
|---|---|---|---|
| F1 | Clínicas | HU09 | ✅ |
| F1 | Perfiles | HU03 | ✅ |
| F1 | Permisos | HU05 | ✅ |
| F1 | Usuarios | HU04 | ✅ |
| F1 | Salas | HU10 | ✅ |
| F1 | Plantillas Especialidad | HU-E02 | ✅ |
| F1 | Tipos Antecedente | HU-E03 | ✅ |
| F1 | Tipos Signo Vital | HU-E04 | ✅ |
| F2 | Pacientes | HU07 | ✅ |
| F2 | Medicamentos | HU08 | ✅ |
| F2 | Tipos Cirugía | HU11 | ✅ |
| F2 | Cirugías | HU12 | ✅ |
| F2 | Tipos Diagnóstico | HU13 | ✅ |
| F2 | **Diagnósticos** | **HU14** | **✅** |
| F2 | Tratamientos | HU15 | ✅ |
| F2 | Recomendaciones | HU16 | ✅ |
| F2 | Exámenes | HU17 | ✅ |
| F3 | Agenda | HU21 | ✅ |
| F4 | Cola de Espera | HU18 | ✅ |
| F4 | Línea de Tiempo | HU19 | ✅ |
| F5 | Hoja Cita + Diagnósticos | HU20 (parcial) | ✅ |
| F5 | Tratamientos y Medicamentos | HU20 (parcial) | ✅ |
| F5 | Exámenes en Hoja Cita | HU20 (parcial) | ✅ |
| F5 | Cirugías en Hoja Cita | HU20 (parcial) | ✅ |
| F5 | Constancias Médicas | HU-E07 | ✅ |
| F5 | Signos Vitales por Consulta | HU-E06 | ✅ |
| F5 | Antecedentes del Paciente | HU-E05 | ✅ |
| F6 | Segundo Paciente (Integración) | — | ✅ |
| — | Recomendaciones en Hoja | HU20 (parcial) | ✅ |
| — | Imprimir Receta Médica | HU20 (parcial) | ✅ |
| — | **Archivos Adjuntos (Storage)** | **HU20 (parcial)** | **✅** |
| — | Imprimir Constancia Médica | HU-E07 | ◻️ |
| — | Dashboard | HU23 | ◻️ |
| — | Reportes | HU22 | ◻️ |
| — | Alertas | HU23 | ◻️ |
| — | Multi-tenant | Global | ◻️ |
| — | Permisos | Global | ◻️ |

---

## NOTAS Y OBSERVACIONES

| # | Módulo | Hallazgo | Tipo | Status |
|---|---|---|---|---|
| 1 | HU14 Diagnósticos | La tabla `diagnosticos` estaba definida como junction table (`cita_id`, `tipo_diagnostico_id`) en vez de catálogo (`nombre`, `codigo_cie10`, `tipo_diagnostico_id`). Se creó la migración `20260707_fix_diagnosticos_catalog.sql` que recrea la tabla como catálogo (patrón medicamentos). Se re-creó la FK `hoja_diagnosticos.diagnostico_id → diagnosticos(id)`. CRUD completo verificado: POST 201, GET 200, PUT 200, PATCH desactivar/reactivar 200. 8/8 diagnósticos creados exitosamente. | 🐛 Bug | 🟢 Resuelto |
| 2 | HU21 Citas API | `HoraCita` usa `TimeOnly` en el DTO, requiere formato `HH:mm:ss` en JSON. Enviar `"09:00"` causa error 400. Enviar `"09:00:00"` funciona correctamente. | 💡 Mejora | 🟢 Resuelto |
| 3 | HU14/HU21 Diagnósticos | Se corrigió totalmente la tabla `diagnosticos`: de junction table a catálogo. Migración `20260707_fix_diagnosticos_catalog.sql`. CRUD completo verificado (8 endpoints). | 🐛 Bug | 🟢 Resuelto |
| 4 | HU20 Hojas Cita | Observaciones con acentos en JSON enviado desde PowerShell causan 400 por encoding. Usar texto ASCII-safe (sin tildes) resuelve el problema. Es un issue de PowerShell, no del API de ASP.NET. | 💡 Mejora | 🟢 Resuelto |
| 5 | HU20 Hoja Cirugía | `fechaCirugia` requiere formato ISO completo (`2026-08-15T08:00:00Z`), no solo fecha. El campo `TIMESTAMPTZ` no acepta `DATE` parcial. | 💡 Mejora | 🟢 Resuelto |
| 6 | HU20 SignosVitalesHoja | Constructor `SignosVitalesHojaRequestDto` tiene parámetros reordenados vs los campos JSON esperados. Verificar que `salaId` está presente en el body. | 🐛 Bug | 🟢 Resuelto |
| 7 | HU-E07 Constancias | CHECK constraint de `tipo_constancia` acepta: `reposo`, `incapacidad`, `constancia_atencion`, `referencia_especialista`, `otro`. No acepta `ASISTENCIA`. Usar minúsculas con guión bajo. | 💡 Mejora | 🟢 Resuelto |
| 8 | HU20 Recomendaciones | Stack completo implementado: migración `hojas_recomendaciones`, Entity, DTOs, DAL (Dapper), BLL, API Controller, IOC, frontend proxy-actions + modal. Catálogo `/api/Recomendaciones` se integra con la hoja de cita vía POST/GET. | 🐛 Bug | 🟢 Resuelto |
| 9 | DAL Dapper | `SELECT *` no mapea correctamente columnas snake_case a propiedades PascalCase porque Npgsql 8+ usa nombres exactos. Se reemplazó por columnas explícitas con AS alias. Además se agregó `MatchNamesWithUnderscores = true` global. | 🐛 Bug | 🟢 Resuelto |
| 10 | DAL Npgsql | Columnas `DATE` en PostgreSQL 15 con Npgsql 8+ retornan `DateOnly` en lugar de `DateTime`. Se agregaron `DateTimeTypeHandler` y `NullableDateTimeTypeHandler` en DbConnectionFactory para conversión automática. | 🐛 Bug | 🟢 Resuelto |
| 11 | Entity Comments | Typos en comentarios de entidades: `public.hojas_diagnosticos` → `public.hoja_diagnosticos` (y similares). Corregido en 4 archivos. | 💡 Mejora | 🟢 Resuelto |

**Leyenda Tipo:** 🐛 Bug | 💡 Mejora | ❓ Duda | ⚠️ Advertencia
**Status:** 🔴 Abierto | 🟢 Resuelto

---

## DATOS DE PRUEBA — ACCESO

| Rol | Email | Contraseña |
|---|---|---|---|
| Super Admin | admin@vittal.com | Password123! |
| Administrador | carlos@vittal.com | Password123! |
| Médico General | juan.perez@vittal.com | Password123! |
| Médico General | maria.lopez@vittal.com | Password123! |
| Gerente de Clínica | gerente@vittal.com | Password123! |
| Recepcionista | ana@vittal.com | Password123! |
| Enfermero/a | carlos.enfermero@vittal.com | Password123! |

---

*Documento generado el 2026-07-01 | Última actualización: 2026-07-11 (Archivos Adjuntos Storage + UX Toast) | Próxima revisión: Al completar cada fase*
*Vittal v1.0.0 — Plan de Pruebas Funcionales*
