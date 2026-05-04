# Supabase — Core Skill

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar:** Antes de crear migraciones SQL, políticas RLS o configurar Supabase.
> **Prerequisito:** Haber leído CLAUDE.md completo.

---

## 1. Principios Fundamentales

```
1. NUNCA usar DELETE — solo UPDATE activo = false
2. SIEMPRE incluir clinica_id en tablas de negocio
3. SIEMPRE habilitar RLS en cada tabla que crees
4. SIEMPRE crear políticas RLS de aislamiento por clinica_id
5. IDs son UUID generados por PostgreSQL — nunca secuencias numéricas
6. Todos los timestamps usan TIMESTAMPTZ (con timezone)
7. Tablas y columnas en snake_case y en español
8. Cada migración es atómica — un solo propósito
9. Comentarios en SQL van en español
10. Supabase CLI es la única herramienta para aplicar migraciones
```

## 2. Estructura de Migraciones

```
supabase/
├── config.toml
├── seed.sql
└── migrations/
    └── YYYYMMDDHHMMSS_[accion]_[tabla].sql
```

### Nomenclatura

```
Formato:  YYYYMMDDHHMMSS_[accion]_[tabla].sql
Acciones: create | alter | drop | add | remove | seed | index | policy

Ejemplos correctos:
  20240115093000_create_pacientes.sql
  20240116110000_alter_pacientes_add_foto_url.sql
  20240117140000_add_index_citas_fecha.sql

Ejemplos incorrectos:
  pacientes.sql          ← sin timestamp
  01_create_table.sql    ← sin nombre descriptivo
  CreatePacientes.sql    ← PascalCase incorrecto
```

## 3. Campos Obligatorios en TODA Tabla de Negocio

```sql
id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
clinica_id      UUID NOT NULL REFERENCES clinicas(id),
activo          BOOLEAN NOT NULL DEFAULT true,
fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
fecha_modificacion TIMESTAMPTZ
```

## 4. Comandos Supabase CLI

```bash
# Inicialización
supabase init
supabase link --project-ref [tu-project-ref]

# Migraciones
supabase migration new [nombre]
supabase migration list
supabase db reset          # Aplicar al local
supabase db push           # Aplicar al remoto (producción)

# Desarrollo local
supabase start
supabase stop
supabase db connect        # psql local
supabase db logs

# Storage y Functions
supabase functions deploy [nombre]
supabase functions logs [nombre]

# Tipos TypeScript
supabase gen types typescript --project-id [ref] > src/types/supabase.ts
```

## 5. Errores Comunes y Soluciones

| Error | Causa | Solución |
|---|---|---|
| `permission denied for table X` | RLS sin política authenticated | Crear `clinica_isolation` + GRANT |
| `null value in column clinica_id` | Insert sin clinica_id | Extraer del JWT, pasar al Repository |
| `violates foreign key` | Referenciar ID inexistente | Validar existencia y tenant antes |
| Datos de otro tenant visibles | Política RLS incorrecta | Verificar `app.current_clinica_id` |
| `migration already applied` | Re-aplicar migración existente | Nueva migración con `ALTER TABLE` |
| Query lenta sin índice | Columnas sin índice | Migración de índice |

---

## 6. Navegación de Sub-skills — Leer según tu tarea

Este archivo contiene los principios generales. **Ahora carga el sub-skill específico para tu tarea:**

| Tu tarea | Sub-skill a cargar |
|---|---|
| Crear migración tablas core (usuarios, perfiles, permisos) | → `skills/supabase/migrations-core.md` |
| Crear migración tablas negocio (pacientes, citas, catálogos) | → `skills/supabase/migrations-business.md` |
| Configurar Storage buckets | → `skills/supabase/storage.md` |
| Configurar Realtime / Alertas | → `skills/supabase/realtime.md` |

---

## Checklist de Calidad — Supabase Core

- [ ] Principio "no DELETE" internalizado
- [ ] clinica_id presente en todas las tablas de negocio
- [ ] RLS habilitado en cada tabla nueva
- [ ] Migración nombrada con timestamp correcto
- [ ] Comentarios en español
- [ ] Supabase CLI usado para aplicar migraciones

---

*skills/supabase/SKILL.md — Vittal v1.0.0*
*Sub-skills: migrations-core.md | migrations-business.md | storage.md | realtime.md*
