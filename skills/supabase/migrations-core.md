# Supabase Migrations — Core System Tables

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar:** Para crear tablas del sistema core (clínicas, perfiles, usuarios, permisos).
> **Prerequisito:** skills/supabase/SKILL.md

---

## Plantilla Maestra de Migración

```sql
-- =============================================================================
-- Migración: create_[tabla]
-- Descripción: [Descripción en español]
-- Historia de Usuario: HU[XX] — [Nombre]
-- Agente: @IngenieroDatos
-- Fecha: [YYYY-MM-DD]
-- =============================================================================

CREATE TABLE IF NOT EXISTS [nombre_tabla] (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    -- [campos de negocio]
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

-- Comentario obligatorio
COMMENT ON TABLE [nombre_tabla] IS '[Descripción]';
COMMENT ON COLUMN [nombre_tabla].clinica_id IS 'Clínica (tenant) a la que pertenece';
COMMENT ON COLUMN [nombre_tabla].activo IS 'FALSE = desactivado, nunca eliminado';

-- Índices obligatorios
CREATE INDEX IF NOT EXISTS idx_[tabla]_clinica_id ON [nombre_tabla](clinica_id);
CREATE INDEX IF NOT EXISTS idx_[tabla]_activo ON [nombre_tabla](activo);
CREATE INDEX IF NOT EXISTS idx_[tabla]_clinica_activo ON [nombre_tabla](clinica_id, activo);

-- RLS obligatorio
ALTER TABLE [nombre_tabla] ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_[tabla]" ON [nombre_tabla]
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_[tabla]" ON [nombre_tabla]
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON [nombre_tabla] TO authenticated;
```

---

## Tabla: clinicas (tabla raíz — sin clinica_id propio)

```sql
-- Migración: create_clinicas | HU09
CREATE TABLE IF NOT EXISTS clinicas (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre                  VARCHAR(255) NOT NULL,
    direccion               TEXT,
    telefono                VARCHAR(20),
    email                   VARCHAR(255),
    logo_url                TEXT,
    tiempo_espera_minutos   INTEGER NOT NULL DEFAULT 30,
    bd_externa_1            VARCHAR(255),
    bd_externa_2            VARCHAR(255),
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ
);

COMMENT ON TABLE clinicas IS 'Clínicas registradas en el sistema. Cada clínica es un tenant del SaaS Vittal';
COMMENT ON COLUMN clinicas.tiempo_espera_minutos IS 'Minutos máximos antes de alerta de espera';

CREATE INDEX IF NOT EXISTS idx_clinicas_activo ON clinicas(activo);

ALTER TABLE clinicas ENABLE ROW LEVEL SECURITY;

CREATE POLICY "service_role_full_access_clinicas" ON clinicas
    FOR ALL TO service_role USING (true) WITH CHECK (true);

CREATE POLICY "authenticated_read_own_clinica" ON clinicas
    FOR SELECT TO authenticated
    USING (id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

GRANT SELECT ON clinicas TO authenticated;
```

---

## Tabla: perfiles

```sql
-- Migración: create_perfiles | HU03
CREATE TABLE IF NOT EXISTS perfiles (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id  UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre      VARCHAR(100) NOT NULL,
    descripcion TEXT,
    es_admin    BOOLEAN NOT NULL DEFAULT false,
    activo      BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ,
    UNIQUE (clinica_id, nombre)
);

COMMENT ON TABLE perfiles IS 'Perfiles de acceso del sistema. Define el nivel de acceso de los usuarios';
COMMENT ON COLUMN perfiles.es_admin IS 'Si true, acceso completo sin verificar permisos específicos';

CREATE INDEX IF NOT EXISTS idx_perfiles_clinica_id ON perfiles(clinica_id);
CREATE INDEX IF NOT EXISTS idx_perfiles_clinica_activo ON perfiles(clinica_id, activo);

ALTER TABLE perfiles ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_perfiles" ON perfiles
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_perfiles" ON perfiles
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON perfiles TO authenticated;
```

---

## Tabla: usuarios

```sql
-- Migración: create_usuarios | HU04
CREATE TABLE IF NOT EXISTS usuarios (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    perfil_id           UUID NOT NULL REFERENCES perfiles(id) ON DELETE RESTRICT,
    auth_user_id        UUID UNIQUE REFERENCES auth.users(id) ON DELETE SET NULL,
    usuario             VARCHAR(100) NOT NULL,
    nombres             VARCHAR(255) NOT NULL,
    apellidos           VARCHAR(255) NOT NULL,
    email               VARCHAR(255) NOT NULL,
    sexo                VARCHAR(1) CHECK (sexo IN ('M', 'F')),
    direccion           TEXT,
    celular             VARCHAR(20),
    es_doctor           BOOLEAN NOT NULL DEFAULT false,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    UNIQUE (clinica_id, usuario),
    UNIQUE (clinica_id, email)
);

COMMENT ON TABLE usuarios IS 'Usuarios del sistema. Vinculados a Supabase Auth via auth_user_id';
COMMENT ON COLUMN usuarios.auth_user_id IS 'UUID del usuario en Supabase Auth (auth.users)';
COMMENT ON COLUMN usuarios.es_doctor IS 'Si true, aparece como opción en filtros de doctor';

CREATE INDEX IF NOT EXISTS idx_usuarios_clinica_id ON usuarios(clinica_id);
CREATE INDEX IF NOT EXISTS idx_usuarios_auth_user_id ON usuarios(auth_user_id);
CREATE INDEX IF NOT EXISTS idx_usuarios_clinica_activo ON usuarios(clinica_id, activo);
CREATE INDEX IF NOT EXISTS idx_usuarios_es_doctor ON usuarios(clinica_id, es_doctor) WHERE es_doctor = true;

ALTER TABLE usuarios ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_usuarios" ON usuarios
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_usuarios" ON usuarios
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON usuarios TO authenticated;
```

---

## Tabla: permisos + modulos_sistema

```sql
-- Migración: create_permisos | HU05

-- Catálogo de módulos del sistema
CREATE TABLE IF NOT EXISTS modulos_sistema (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clave       VARCHAR(50) NOT NULL UNIQUE,
    nombre      VARCHAR(100) NOT NULL,
    descripcion TEXT,
    activo      BOOLEAN NOT NULL DEFAULT true
);

COMMENT ON TABLE modulos_sistema IS 'Catálogo de módulos del sistema. Solo administradores del sistema pueden modificar';

-- Permisos por perfil y módulo
CREATE TABLE IF NOT EXISTS permisos (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id  UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    perfil_id   UUID NOT NULL REFERENCES perfiles(id) ON DELETE RESTRICT,
    modulo_id   UUID NOT NULL REFERENCES modulos_sistema(id) ON DELETE RESTRICT,
    puede_leer      BOOLEAN NOT NULL DEFAULT false,
    puede_crear     BOOLEAN NOT NULL DEFAULT false,
    puede_actualizar BOOLEAN NOT NULL DEFAULT false,
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modificado_por UUID REFERENCES usuarios(id),
    UNIQUE (clinica_id, perfil_id, modulo_id)
);

COMMENT ON TABLE permisos IS 'Permisos por perfil y módulo. Solo READ, CREATE, UPDATE — no DELETE';

CREATE INDEX IF NOT EXISTS idx_permisos_clinica_id ON permisos(clinica_id);
CREATE INDEX IF NOT EXISTS idx_permisos_perfil_id ON permisos(perfil_id);
CREATE INDEX IF NOT EXISTS idx_permisos_modulo_id ON permisos(modulo_id);

ALTER TABLE permisos ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_permisos" ON permisos
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_permisos" ON permisos
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON permisos TO authenticated;
GRANT SELECT ON modulos_sistema TO authenticated;

-- Seed de módulos del sistema
INSERT INTO modulos_sistema (clave, nombre, descripcion) VALUES
    ('login',          'Acceso al sistema',    'Módulo de autenticación'),
    ('perfiles',       'Gestión de perfiles',  'Administración de perfiles de usuario'),
    ('usuarios',       'Gestión de usuarios',  'Administración de usuarios del sistema'),
    ('permisos',       'Gestión de permisos',  'Asignación de permisos por perfil'),
    ('salas',          'Gestión de salas',     'Asignación de salas a doctores'),
    ('pacientes',      'Gestión de pacientes', 'CRUD de pacientes de la clínica'),
    ('medicamentos',   'Medicamentos',         'Catálogo de medicamentos'),
    ('clinicas',       'Gestión de clínicas',  'Administración de clínicas y sucursales'),
    ('areas',          'Salas y áreas',        'Gestión de salas y áreas de la clínica'),
    ('tipos_cirugia',  'Tipos de cirugías',    'Catálogo de tipos de cirugías'),
    ('cirugias',       'Cirugías',             'Catálogo de cirugías'),
    ('tipos_dx',       'Tipos de diagnósticos','Catálogo de tipos de diagnósticos'),
    ('diagnosticos',   'Diagnósticos',         'Catálogo de diagnósticos'),
    ('tratamientos',   'Tratamientos',         'Catálogo de tratamientos'),
    ('recomendaciones','Recomendaciones',      'Catálogo de recomendaciones médicas'),
    ('examenes',       'Exámenes',             'Catálogo de exámenes médicos'),
    ('cola_espera',    'Cola de espera',       'Visualización de pacientes en espera del día'),
    ('linea_tiempo',   'Línea de tiempo',      'Seguimiento de pacientes paso a paso'),
    ('expedientes',    'Expedientes médicos',  'Historial clínico completo de pacientes'),
    ('agenda',         'Agenda de citas',      'Programación y gestión de citas médicas'),
    ('dashboard',      'Dashboard',            'Visualización de gráficos y métricas'),
    ('reportes',       'Reportes',             'Reportes generales y detallados'),
    ('alertas',        'Alertas configurables','Notificaciones de tiempo de espera excedido')
ON CONFLICT (clave) DO NOTHING;
```

---

## Checklist de Calidad — Migrations Core

### Migración
- [ ] Archivo nombrado con timestamp: `YYYYMMDDHHMMSS_create_[tabla].sql`
- [ ] `id UUID PRIMARY KEY DEFAULT gen_random_uuid()`
- [ ] `clinica_id UUID NOT NULL REFERENCES clinicas(id)` (excepto tabla clinicas)
- [ ] `activo BOOLEAN NOT NULL DEFAULT true`
- [ ] `fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- [ ] `fecha_modificacion TIMESTAMPTZ`
- [ ] Comentarios en español en tabla y columnas principales
- [ ] `UNIQUE` constraints donde corresponde
- [ ] `CHECK` constraints en enumeraciones
- [ ] `ON DELETE RESTRICT` en FKs de negocio

### Índices
- [ ] Índice en `clinica_id`
- [ ] Índice en `activo`
- [ ] Índice compuesto `(clinica_id, activo)`
- [ ] Índices en columnas de filtro frecuente

### RLS
- [ ] `ALTER TABLE [tabla] ENABLE ROW LEVEL SECURITY`
- [ ] Política `clinica_isolation_[tabla]` con FOR ALL
- [ ] Política `service_role_full_access_[tabla]`
- [ ] `GRANT SELECT, INSERT, UPDATE ON [tabla] TO authenticated`
- [ ] **No existe GRANT DELETE**

---

*skills/supabase/migrations-core.md — Vittal v1.0.0*
