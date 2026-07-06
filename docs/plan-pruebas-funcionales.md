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

### 1.6 Plantillas de Especialidad (HU-E02)

- [ ] Ir a: **Administración → Plantillas de Especialidad**
  (`/Administracion/PlantillaEspecialidad`)
- [ ] **Verificar** si existen plantillas por defecto
- [ ] **Crear plantilla** para `Medicina General`:
  - Antecedentes: Hipertensión, Diabetes Mellitus tipo 2, Asma, Cirugía previa, Alergias medicamentosas
  - Signos Vitales: Presión Arterial, Frecuencia Cardíaca, Temperatura, Peso, Talla, Saturación O₂
- [ ] **Crear plantilla** para `Cardiología`:
  - Antecedentes: Hipertensión, Diabetes, IAM previo, Tabaquismo, Dislipidemia, Insuficiencia Cardíaca
  - Signos Vitales: Presión Arterial, Frecuencia Cardíaca, Temperatura, Peso, Saturación O₂, Glucemia capilar
- [ ] **Editar plantilla**: Agregar/quitar items
- [ ] **Verificar** persistencia

---

### 1.7 Onboarding de Sala con Plantilla (HU-E02)

- [ ] Seleccionar una sala (ej: Consultorio 1)
- [ ] **Aplicar plantilla** de Medicina General
- [ ] **Verificar** que se generaron los `Tipos de Antecedente` para la sala
- [ ] Ir a: **Catálogos → Tipos de Antecedente** y filtrar por la sala
- [ ] **Verificar** que los antecedentes de la plantilla se importaron
- [ ] Ir a: **Catálogos → Tipos de Signo Vital** y filtrar por la sala
- [ ] **Verificar** que los signos vitales de la plantilla se importaron
- [ ] Repetir para sala de Cardiología

> **Flujo crítico:** La plantilla debe propagarse correctamente a los catálogos por sala.

---

### 1.8 Tipos de Antecedente por Sala (HU-E03)

- [ ] Ir a: **Catálogos → Tipos de Antecedente** (`/Catalogos/TipoAntecedente`)
- [ ] **Filtrar** por sala "Consultorio 1"
- [ ] **Verificar** que están los importados de la plantilla
- [ ] **Crear** un antecedente adicional manualmente para esta sala:
  - Nombre: `COVID-19 previo`
  - Sala: Consultorio 1
- [ ] **Editar** un antecedente existente
- [ ] **Desactivar** un antecedente (sin uso)
- [ ] **Filtrar** por sala "Consultorio 3 (Cardiología)"
- [ ] **Verificar** que los antecedentes son distintos a los de Medicina General

---

### 1.9 Tipos de Signo Vital por Sala (HU-E04)

- [ ] Ir a: **Catálogos → Tipos de Signo Vital** (`/Catalogos/TipoSignoVital`)
- [ ] **Filtrar** por sala "Consultorio 1"
- [ ] **Verificar** los signos importados de la plantilla
- [ ] **Crear** un signo vital adicional:
  - Nombre: `Perímetro abdominal`
  - Sala: Consultorio 4 (Dermatología) - no aplica pero para probar
  - Unidad: `cm`
- [ ] **Editar** un signo vital
- [ ] **Desactivar** un signo vital

---

## FASE 2 — CATÁLOGOS MÉDICOS

> **Propósito:** Poblar los catálogos base del sistema.

### 2.1 Gestión de Pacientes (HU07)

- [ ] Ir a: **Catálogos → Pacientes** (`/Catalogos/Paciente`)
- [ ] **Crear pacientes de prueba**:

| Nombre | Email | Celular | Sexo | Doctor Asignado |
|---|---|---|---|---|
| Pedro García López | pedro@email.test | 555-0101 | M | Dr. Juan Pérez |
| Ana Martínez Ruiz | ana@email.test | 555-0102 | F | Dr. Juan Pérez |
| Carlos Sánchez Gil | carlos@email.test | 555-0103 | M | Dra. María López |
| Laura Torres Díaz | laura@email.test | 555-0104 | F | Dra. María López |
| Roberto Fernández Paz | roberto@email.test | 555-0105 | M | Dr. Juan Pérez |
| Sofía Ramírez Cruz | sofia@email.test | 555-0106 | F | Dr. Juan Pérez |
| Miguel Ángel Vega | miguel@email.test | 555-0107 | M | Dra. María López |
| Elena Ortiz Maya | elena@email.test | 555-0108 | F | Dr. Juan Pérez |
| Diego Herrera Solís | diego@email.test | 555-0109 | M | Dra. María López |
| Carmen Navarro Ríos | carmen@email.test | 555-0110 | F | Dr. Juan Pérez |

- [ ] **Verificar**: Búsqueda de paciente por nombre, email, celular
- [ ] **Editar paciente**: Cambiar teléfono o dirección
- [ ] **Verificar** duplicados: Intentar crear con mismo email → debe rechazar
- [ ] **Desactivar paciente**: Probar desactivación
- [ ] **Reactivar paciente**
- [ ] **Verificar** que el paciente desactivado NO aparece en búsqueda normal

---

### 2.2 Gestión de Medicamentos (HU08)

- [ ] Ir a: **Catálogos → Medicamentos** (`/Catalogos/Medicamento`)
- [ ] **Crear medicamentos**:

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

- [ ] **Buscar** medicamento por nombre
- [ ] **Editar** medicamento
- [ ] **Desactivar** medicamento

---

### 2.3 Gestión de Tipos de Cirugía (HU11)

- [ ] Ir a: **Catálogos → Tipos de Cirugía** (`/Catalogos/TipoCirugia`)
- [ ] **Crear tipos**:

| Nombre |
|---|
| Cirugía Mayor |
| Cirugía Menor Ambulatoria |
| Cirugía Laparoscópica |
| Cirugía de Emergencia |
| Cirugía Estética |

- [ ] **Buscar** por nombre
- [ ] **Editar / Desactivar**

---

### 2.4 Gestión de Cirugías (HU12)

- [ ] Ir a: **Catálogos → Cirugías** (`/Catalogos/Cirugia`)
- [ ] **Crear cirugías**:

| Nombre | Tipo |
|---|---|
| Apendicectomía | Cirugía Mayor |
| Colecistectomía Laparoscópica | Cirugía Laparoscópica |
| Cesárea | Cirugía Mayor |
| Sutura de herida superficial | Cirugía Menor Ambulatoria |
| Reducción de fractura cerrada | Cirugía Menor Ambulatoria |
| Amigdalectomía | Cirugía Mayor |

- [ ] **Buscar** por nombre
- [ ] **Editar / Desactivar**

---

### 2.5 Gestión de Tipos de Diagnóstico (HU13)

- [ ] Ir a: **Catálogos → Tipos de Diagnóstico** (`/Catalogos/TipoDiagnostico`)
- [ ] **Crear tipos**:

| Nombre |
|---|
| Diagnóstico Principal |
| Diagnóstico Secundario |
| Diagnóstico Diferencial |
| Diagnóstico de Ingreso |
| Diagnóstico de Egreso |

- [ ] **Buscar / Editar / Desactivar**

---

### 2.6 Gestión de Diagnósticos (HU14)

- [ ] Ir a: **Catálogos → Diagnósticos** (`/Catalogos/Diagnostico`)
- [ ] **Crear diagnósticos (CIE-10 simplificado)**:

| Código | Nombre | Tipo |
|---|---|---|
| J00X | Rinitis alérgica aguda | Diagnóstico Principal |
| I10X | Hipertensión esencial | Diagnóstico Principal |
| E11X | Diabetes mellitus tipo 2 | Diagnóstico Principal |
| J459 | Asma no especificada | Diagnóstico Principal |
| A099 | Gastroenteritis de presunto origen infeccioso | Diagnóstico Principal |
| M545 | Lumbago no especificado | Diagnóstico Principal |
| N390 | Infección de vías urinarias | Diagnóstico Principal |
| J069 | Infección aguda no especulada de vías respiratorias | Diagnóstico Principal |

- [ ] **Buscar** por código o nombre
- [ ] **Editar / Desactivar**

---

### 2.7 Gestión de Tratamientos (HU15)

- [ ] Ir a: **Catálogos → Tratamientos** (`/Catalogos/Tratamiento`)
- [ ] **Crear tratamientos**:

| Nombre |
|---|
| Reposo moderado por 7 días |
| Terapia física 3 veces por semana |
| Cambio de vendaje cada 24 horas |
| Dieta blanda por 3 días |
| Aplicación de hielo local cada 6 horas |
| Elevación del miembro afectado |
| Control de signos vitales cada 4 horas |

- [ ] **Buscar / Editar / Desactivar**

---

### 2.8 Gestión de Recomendaciones (HU16)

- [ ] Ir a: **Catálogos → Recomendaciones** (`/Catalogos/Recomendacion`)
- [ ] **Crear recomendaciones**:

| Nombre |
|---|
| Tomar abundante agua |
| Evitar esfuerzos físicos por 48 horas |
| No consumir alcohol durante el tratamiento |
| Regresar a consulta en 7 días |
| Realizar ejercicio moderado 30 min diarios |
| Mantener herida limpia y seca |

- [ ] **Buscar / Editar / Desactivar**

---

### 2.9 Gestión de Exámenes (HU17)

- [ ] Ir a: **Catálogos → Exámenes** (`/Catalogos/Examen`)
- [ ] **Crear exámenes**:

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

- [ ] **Buscar / Editar / Desactivar**

---

## FASE 3 — AGENDA Y CITAS

> **Propósito:** Probar el módulo de agenda médica.

### 3.1 Agenda (HU21) — Creación de Citas

- [ ] Ir a: **Agenda** (`/Agenda/Agenda`)
- [ ] **Verificar** que la agenda carga correctamente
- [ ] **Probar cambio de vistas**: Día, 5 Días, Semana, Mes
- [ ] **Navegación**: Botones Anterior/Hoy/Siguiente
- [ ] **Crear cita** para Pedro García con Dr. Juan Pérez:
  - Fecha: día siguiente
  - Hora: 09:00
  - Sala: Consultorio 1
  - Duración: 30 min
  - Motivo: Control rutinario
- [ ] **Crear cita** para Ana Martínez con Dr. Juan Pérez:
  - Fecha: día siguiente
  - Hora: 09:30
  - Sala: Consultorio 1
  - Motivo: Dolor de cabeza persistente
- [ ] **Crear cita** para Carlos Sánchez con Dra. María López:
  - Fecha: día siguiente
  - Hora: 09:00
  - Sala: Consultorio 2
  - Motivo: Revisión de resultados
- [ ] **Crear cita** para Roberto Fernández con Dr. Juan Pérez:
  - Fecha: día siguiente
  - Hora: 10:00
  - Sala: Consultorio 1
  - Motivo: Dolor lumbar
- [ ] **Crear cita** para Laura Torres con Dra. María López:
  - Fecha: día siguiente
  - Hora: 10:00
  - Sala: Consultorio 2
  - Motivo: Control prenatal
- [ ] **Crear cita** para Sofía Ramírez con Dr. Juan Pérez:
  - Fecha: día siguiente
  - Hora: 10:30
  - Sala: Consultorio 1
  - Motivo: Vacunación
- [ ] **Crear más citas** para llenar la agenda del día (al menos 8-10 citas)

- [ ] **Verificar** que las citas aparecen en la agenda en el horario correcto
- [ ] **Verificar** que se muestran con el color del doctor correspondiente
- [ ] **Verificar** superposición: Intentar crear cita en mismo horario/sala → debe advertir

### 3.2 Agenda — Edición y Cambio de Estado de Citas

- [ ] **Editar cita**: Cambiar hora de una cita existente
- [ ] **Cambiar estado** de una cita a "Cancelada"
- [ ] **Verificar** que la cita cancelada se marca visualmente (tachada/gris)

### 3.3 Agenda — Filtros

- [ ] **Filtrar** por doctor (Dr. Juan Pérez)
- [ ] **Verificar** que solo se ven las citas de ese doctor
- [ ] **Filtrar** por sala (Consultorio 1)
- [ ] **Verificar** que solo se ven citas de esa sala
- [ ] **Quitar filtros** y ver todas

---

## FASE 4 — COLA DE ESPERA Y ATENCIÓN

> **Propósito:** Probar el flujo de atención desde que el paciente llega hasta que es atendido.

### 4.1 Cola de Espera (HU18)

- [ ] Ir a: **Cola de Espera** (`/ColaEspera/ColaEspera`)
- [ ] **Verificar** que la cola carga correctamente
- [ ] **Verificar** que aparecen las citas del día de hoy (pasar algunas citas al día actual si es necesario)
- [ ] **Mover paciente** de "Agendada" a "En espera" → simular que el paciente llegó
- [ ] **Verificar** que la cola se actualiza en tiempo real (si aplica Realtime)
- [ ] **Cambiar estado** de varios pacientes a "En espera"

### 4.2 Línea de Tiempo (HU19)

- [ ] Ir a: **Línea de Tiempo** (`/LineaTiempo/LineaTiempo`)
- [ ] **Verificar** que la línea de tiempo muestra los pasos del paciente
- [ ] Para un paciente en "En espera":
  - **Iniciar consulta** → Cambia a "En atención"
  - **Verificar** que aparece el tiempo transcurrido
- [ ] **Finalizar consulta** → Cambia a "Atendida"
- [ ] **Verificar** que se registró el tiempo total de atención

### 4.3 Atención desde Cola de Espera

- [ ] **Seleccionar paciente** en la cola con estado "En espera"
- [ ] **Click en "Atender"** → Debe redirigir al expediente del paciente
- [ ] **Verificar** el cambio de estado en la cola

---

## FASE 5 — EXPEDIENTES Y HOJAS DE CITA

> **Propósito:** Probar el módulo central del sistema.

### 5.1 Gestión de Expedientes (HU20 — Base)

- [ ] Ir a: **Expedientes** (`/Expedientes/Expedientes`)
- [ ] **Verificar** listado de expedientes (uno por paciente)
- [ ] **Crear expediente** para un paciente que no tenga uno
- [ ] **Buscar** expediente por paciente
- [ ] **Ver detalles** del expediente de Pedro García

### 5.2 Hoja de Cita — Consulta Médica

- [ ] **Seleccionar cita** desde la agenda o desde el expediente
- [ ] **Crear Hoja de Cita** para la consulta

#### 5.2.1 Diagnósticos
- [ ] **Agregar diagnóstico**: Hipertensión esencial (I10X) como Diagnóstico Principal
- [ ] **Agregar diagnóstico secundario**: Diabetes mellitus tipo 2 (E11X)
- [ ] **Verificar** que aparecen en la hoja de cita

#### 5.2.2 Antecedentes del Paciente (HU-E05)
- [ ] Ir a la sección de **Antecedentes** del paciente
- [ ] **Registrar antecedentes**:
  - Hipertensión: Sí, desde 2018, en tratamiento con Losartán
  - Diabetes: Sí, desde 2020, en tratamiento con Metformina
  - Cirugía previa: Apendicectomía en 2015
- [ ] **Editar** un antecedente registrado
- [ ] **Verificar** que los antecedentes se muestran en la hoja de cita

#### 5.2.3 Signos Vitales por Consulta (HU-E06)
- [ ] Ir a la sección de **Signos Vitales** de la hoja de cita
- [ ] **Registrar signos vitales**:
  - Presión Arterial: 130/85 mmHg
  - Frecuencia Cardíaca: 78 lpm
  - Temperatura: 36.8 °C
  - Peso: 75 kg
  - Talla: 170 cm
  - Saturación O₂: 98%
- [ ] **Editar** un signo vital
- [ ] **Verificar** que los signos se guardan correctamente

#### 5.2.4 Tratamientos y Medicamentos (Receta)
- [ ] **Agregar tratamiento**: Reposo moderado por 7 días
- [ ] **Agregar medicamento** a la receta:
  - Losartán 50 mg — 1 tableta cada 12 horas — 30 días
  - Metformina 850 mg — 1 tableta cada 8 horas — 30 días
- [ ] **Verificar** que aparecen en la hoja de cita

#### 5.2.5 Exámenes
- [ ] **Agregar examen**: Biometría Hemática Completa
- [ ] **Agregar examen**: Química Sanguínea
- [ ] **Agregar examen**: Electrocardiograma
- [ ] **Agregar resultados** a un examen (si aplica)

#### 5.2.6 Cirugías
- [ ] **Agregar cirugía** (si aplica al caso)
- [ ] Seleccionar: Colecistectomía Laparoscópica
- [ ] **Verificar** que se registra en la hoja

#### 5.2.7 Recomendaciones
- [ ] **Agregar recomendaciones**:
  - Tomar abundante agua
  - Evitar esfuerzos físicos por 48 horas
  - Regresar a consulta en 7 días
- [ ] **Verificar** en la hoja de cita

### 5.3 Imprimir Receta Médica

- [ ] Desde la hoja de cita, **click en "Imprimir Receta"**
- [ ] **Verificar** que se genera una vista para impresión con:
  - Logo de la clínica
  - Datos del doctor
  - Datos del paciente
  - Medicamentos con dosis y duración
  - Fecha y firma

### 5.4 Constancias Médicas (HU-E07)

- [ ] Ir a: **Expedientes → Constancias** (`/Expedientes/Constancias`)
- [ ] **Crear constancia** para un paciente:
  - Tipo: Constancia de atención
  - Fecha: fecha actual
  - Contenido: "Se hace constar que el paciente fue atendido..."
- [ ] **Ver detalles** de la constancia
- [ ] **Verificar** que se asocia al expediente correcto

### 5.5 Archivos Adjuntos (HU20 — Storage)

- [ ] **Subir archivo** al expediente (PDF o imagen)
- [ ] **Verificar** que aparece en la lista de archivos
- [ ] **Descargar** / Visualizar archivo
- [ ] **Eliminar** archivo
- [ ] **Verificar** Supabase Storage se actualiza

---

## FASE 6 — FLUJO COMPLETO CON SEGUNDO PACIENTE

> **Propósito:** Probar un segundo ciclo completo de atención para validar consistencia.

- [ ] Seleccionar al paciente **Carlos Sánchez** con Dra. María López
- [ ] **Registrar cita** en agenda
- [ ] **Avanzar por cola de espera**: Agendada → En espera → En atención
- [ ] **Crear hoja de cita** para la consulta
- [ ] **Registrar**:
  - Diagnóstico: Lumbago no especificado (M545)
  - Signos vitales completos
  - Tratamiento: Terapia física 3 veces por semana
  - Medicamento: Ibuprofeno 400 mg cada 8 horas por 5 días
  - Recomendación: Evitar esfuerzos físicos por 48 horas
  - Examen: Radiografía de Tórax
- [ ] **Imprimir receta**
- [ ] **Finalizar consulta**

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

| Módulo | HU | Estado |
|---|---|---|
| Clínicas | HU09 | ✅ |
| Perfiles | HU03 | ✅ |
| Permisos | HU05 | ✅ |
| Usuarios | HU04 | ✅ |
| Salas | HU10 | ✅ |
| Plantillas Especialidad | HU-E02 | ◻️ |
| Tipos Antecedente | HU-E03 | ◻️ |
| Tipos Signo Vital | HU-E04 | ◻️ |
| Pacientes | HU07 | ◻️ |
| Medicamentos | HU08 | ◻️ |
| Tipos Cirugía | HU11 | ◻️ |
| Cirugías | HU12 | ◻️ |
| Tipos Diagnóstico | HU13 | ◻️ |
| Diagnósticos | HU14 | ◻️ |
| Tratamientos | HU15 | ◻️ |
| Recomendaciones | HU16 | ◻️ |
| Exámenes | HU17 | ◻️ |
| Agenda | HU21 | ◻️ |
| Cola de Espera | HU18 | ◻️ |
| Línea de Tiempo | HU19 | ◻️ |
| Antecedentes Paciente | HU-E05 | ◻️ |
| Signos Vitales Hoja | HU-E06 | ◻️ |
| Expedientes | HU20 | ◻️ |
| Constancias | HU-E07 | ◻️ |
| Dashboard | HU23 | ◻️ |
| Reportes | HU22 | ◻️ |
| Alertas | HU23 | ◻️ |
| Multi-tenant | Global | ◻️ |
| Permisos | Global | ◻️ |

---

## NOTAS Y OBSERVACIONES

| # | Módulo | Hallazgo | Tipo | Status |
|---|---|---|---|---|
| 1 | | | | |
| 2 | | | | |
| 3 | | | | |
| 4 | | | | |
| 5 | | | | |

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

*Documento generado el 2026-07-01 | Próxima revisión: Al completar cada fase*
*Vittal v1.0.0 — Plan de Pruebas Funcionales*
