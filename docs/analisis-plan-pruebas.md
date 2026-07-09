# Análisis del Plan de Pruebas Funcionales — Vittal Sistema Médico

> **Evaluación general:** El plan está bien estructurado y en buen camino. Las Fases 0–2 están completadas correctamente. Hay un bug identificado (HU14) y las Fases 3–10 están pendientes. Se detectan algunas brechas que conviene atender antes de marcar el plan como "completo".

---

## ✅ Fortalezas Identificadas

### Estructura del Plan

El plan sigue la lógica correcta para un sistema SaaS multi-tenant:

| Aspecto | Evaluación |
|---------|------------|
| **Secuencia lógica** | ✅ Correcta — setup → catálogos → operaciones → reportes → seguridad |
| **Cobertura de HUs** | ✅ Todas las HUs del backlog están contempladas |
| **Datos de prueba** | ✅ Datos realistas y coherentes (CIE-10, pacientes, médicos) |
| **Flujo multi-doctor** | ✅ Se prueban 2 doctores con pacientes distintos |
| **Pruebas de seguridad** | ✅ Incluye validación de permisos por perfil y aislamiento multi-tenant |
| **Casos negativos** | ✅ Duplicados, 403 Forbidden, intentos de superposición en agenda |
| **Criterio de éxito** | ✅ Definido claramente (✅ / ◻️ / ⚠️) |

### Fases 0–2: Correctamente Completadas

- **Entorno operativo** verificado (API healthy, Swagger, Supabase conectado, 87/87 unit tests)
- **Multi-tenant base** correcta: clínica creada con ID real, sala_id como discriminador de especialidad respetado
- **Permisos** verificados con casos positivos y negativos (403 Forbidden en Admin sin permiso)
- **Plantillas por sala** aplicadas correctamente (Medicina General → Consultorio 1, Cardiología → Consultorio 3)
- **Catálogos médicos** bien cubiertos: medicamentos, cirugías, diagnósticos, tratamientos, exámenes, recomendaciones

---

## 🐛 Bug Encontrado — HU14 (Diagnósticos) — Requiere Atención Inmediata

> [!CAUTION]
> **Hallazgo #1 es crítico.** Los diagnósticos se usan en las hojas de cita (Fase 5). Si solo 1 de 8 diagnósticos puede crearse, el flujo del expediente clínico estará bloqueado con datos insuficientes.

**Diagnóstico probable del bug:**

El error *"Ya existe un diagnóstico de ese tipo asignado a esta cita"* sugiere que la validación de unicidad en `DiagnosticoService` está comparando `tipoDiagnosticoId` contra la entidad **`diagnosticos_hoja`** (hojas de cita) en lugar del **catálogo** `diagnosticos`. 

```csharp
// 🔴 Posible código erróneo en DiagnosticoService.cs:
var existente = await _repo.GetByTipoDiagnosticoAsync(dto.TipoDiagnosticoId, citaId);
// ↑ Esta lógica es para "no duplicar diagnóstico tipo X en una sola cita"
// ↑ NO debe aplicarse al catálogo global de diagnósticos

// ✅ La validación correcta para el CATÁLOGO debe ser:
var existente = await _repo.GetByCodigoAsync(dto.Codigo, clinicaId);
// Solo rechazar si el CÓDIGO CIE-10 ya existe en el catálogo de esa clínica
```

**Acción recomendada:** Revisar `Vittal.BLL/Services/DiagnosticoService.cs` y separar la lógica de validación de unicidad:
- **Catálogo de Diagnósticos**: unicidad por `código` dentro de la `clinica_id`
- **Diagnósticos de Hoja de Cita**: unicidad por `tipo_diagnostico_id` dentro de una `hoja_cita_id`

---

## ⚠️ Brechas y Observaciones por Fase

### Fase 3 — Agenda (Pendiente)

> [!WARNING]
> La Fase 3 es prerequisito bloqueante para Fases 4, 5, 6 y 10. No puede avanzarse sin citas creadas.

Puntos específicos a verificar en las pruebas:

- **Conflicto de horario**: Verificar que el sistema rechaza dos citas en la misma sala+hora (no solo advierte — debe bloquear)
- **`hora_fin`**: Verificar que HU-E01 (ALTER citas: agregar hora_fin) está aplicada — sin esto, la duración de la cita no se persiste
- **Filtro por doctor**: El médico solo debe ver sus citas; el admin ve todas
- **Estados iniciales**: Las citas deben crearse en estado `Agendada`

### Fase 4 — Cola de Espera (Pendiente)

> [!IMPORTANT]
> La cola de espera usa **Supabase Realtime**. Probar desde dos navegadores/pestañas simultáneas para validar el tiempo real.

- Verificar que el estado de la cita cambia en tiempo real en ambas pestañas
- Verificar que solo aparecen citas del **día actual** (no históricas)
- Verificar el flujo completo de estados: `Agendada → En espera → En atención → Atendida`

### Fase 5 — Expedientes (Pendiente — Módulo Central Crítico)

> [!IMPORTANT]
> Esta es la fase más compleja. Se recomienda probarla primero con el bug de HU14 **resuelto**.

Puntos específicos no contemplados en el plan actual:

| Escenario faltante | Por qué es importante |
|---|---|
| Intentar acceder al expediente de un paciente de otra clínica directamente por URL | Prueba de aislamiento multi-tenant en el módulo más crítico |
| Crear una 2da hoja de cita para el mismo paciente | Verificar que el historial se acumula correctamente (1 expediente → N hojas) |
| Validar que los signos vitales usan los `tipos_signo_vital` de la **sala** de la cita | Prueba del discriminador `sala_id` en contexto real |
| Validar que los antecedentes usan los `tipos_antecedente` de la **sala** | Ídem anterior |

### Fase 7 — Dashboard y Reportes (Pendiente)

- **Sin datos reales, el dashboard mostrará ceros.** El plan asume que el flujo E2E estará completo antes — secuencia correcta.
- Verificar que los filtros del dashboard respetan `clinica_id` (un admin de clínica 1 no debe ver métricas de clínica 2)

### Fase 8 — Alertas (Pendiente)

- El tiempo mínimo configurable debe ser validado (¿acepta `1 minuto` para pruebas?)
- Verificar que la alerta se dispara via **SignalR**, no solo como notificación local
- Verificar que al resolver la alerta, desaparece de la vista de todos los usuarios de la clínica

### Fase 9 — Multi-Tenant y Seguridad (Pendiente)

> [!IMPORTANT]
> Esta fase es fundamental para un SaaS. Se recomienda no saltarla aunque el tiempo sea corto.

**Escenarios adicionales recomendados:**

```
Escenario A — Acceso por URL directa:
  1. Login como usuario de Clínica A
  2. Obtener el UUID de un paciente de Clínica B (de Supabase Dashboard)
  3. Acceder directamente: /Catalogos/Paciente/Details/{uuid-clinica-b}
  Esperado: 404 o redireccionamiento — NO debe mostrar datos de otra clínica

Escenario B — Manipulación de JWT:
  1. Decodificar el JWT (jwt.io)
  2. Verificar que clinica_id está presente en los claims
  3. Intentar llamar al API con un JWT manipulado
  Esperado: 401 Unauthorized

Escenario C — RLS a nivel de BD:
  Verificar en Supabase Dashboard que las políticas RLS están activas
  en: pacientes, citas, expedientes, hojas_cita, signos_vitales_hoja
```

---

## 📋 Escenarios Faltantes (No contemplados en el plan)

Los siguientes casos de prueba **no están en el plan** pero son importantes para un SaaS médico:

| # | Escenario | Fase Sugerida | Prioridad |
|---|---|---|---|
| A | Sesión expirada: qué pasa cuando el JWT expira mientras el médico está en medio de una consulta | 9 | 🔴 Alta |
| B | Carga concurrente: 2 médicos atienden a sus pacientes simultáneamente sin interferencia | 10 | 🔴 Alta |
| C | Hoja de cita sin diagnóstico: ¿puede cerrarse? ¿hay validación mínima requerida? | 5 | 🟡 Media |
| D | Paciente sin expediente en la cola: ¿el sistema lo crea automáticamente al atender? | 4 | 🟡 Media |
| E | Buscar paciente con caracteres especiales (ñ, acentos) en nombre | 2 | 🟡 Media |
| F | Subir archivo al expediente con extensión no permitida (`.exe`) | 5.5 | 🟡 Media |
| G | Imprimir receta cuando hay 0 medicamentos asignados | 5.3 | 🟢 Baja |
| H | Super Admin ve datos de todas las clínicas vs Admin solo ve la suya | 1/9 | 🔴 Alta |

---

## 📊 Resumen de Estado del Plan

```
Fases completadas:     F0, F1, F2 (parcial — HU14 con bug)
Fases pendientes:      F3, F4, F5, F6, F7, F8, F9, F10
Bug crítico abierto:   1 (HU14 — Diagnósticos)
Cobertura actual:      ~35% del plan
```

### Progreso por tipo de prueba

| Tipo | Estado |
|------|--------|
| Configuración / Setup | ✅ 100% |
| Catálogos base | ✅ 95% (HU14 con bug) |
| Flujo de citas / agenda | ◻️ 0% |
| Flujo clínico (expedientes) | ◻️ 0% |
| Tiempo real / alertas | ◻️ 0% |
| Seguridad / multi-tenant | ◻️ 0% |
| Analytics / reportes | ◻️ 0% |

---

## 🎯 Recomendaciones de Prioridad

### Inmediato (antes de continuar)
1. **🔴 Corregir bug HU14** — DiagnosticoService validación de unicidad
2. Crear los 7 diagnósticos CIE-10 restantes para tener catálogo completo

### Próximos pasos en orden
3. **F3** — Crear las citas en la agenda (prerequisito para todo lo demás)
4. **F4** — Probar cola de espera con Realtime desde 2 pestañas
5. **F5** — Flujo completo de hoja de cita (módulo central)
6. **F6** — Flujo completo con segundo paciente / segundo médico
7. **F9** — Pruebas de seguridad multi-tenant (no omitir)
8. **F7 + F8** — Dashboard, reportes y alertas
9. **F10** — Integración E2E final

---

## ✅ Veredicto Final

> [!NOTE]
> **El plan es sólido, bien estructurado y alineado con la arquitectura SaaS multi-tenant definida en CLAUDE.md.** La secuencia de fases es la correcta para este tipo de sistema médico. Las Fases 0, 1 y 2 completadas demuestran que la base del sistema funciona correctamente.
>
> El único riesgo inmediato es el bug en HU14 que puede bloquear las pruebas de expedientes (Fase 5). El resto del plan puede ejecutarse en secuencia sin cambios mayores.
>
> Se recomienda agregar los 8 escenarios faltantes identificados (Sección "Escenarios Faltantes") antes de declarar las pruebas funcionales como completas.

---

*Análisis generado: 2026-07-07 | Vittal v1.0.0 — Revisión del Plan de Pruebas Funcionales*
