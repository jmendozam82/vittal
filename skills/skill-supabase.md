# skill-supabase.md — Skill: Ingeniero de Datos y Persistencia

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar este skill:** Antes de crear cualquier migración SQL, política RLS,
> configuración de Storage, suscripción Realtime o Edge Function en el proyecto Vittal.
> **Prerequisito:** Haber leído CLAUDE.md completo. Este skill asume ese contexto.

---

## 1. Principios Fundamentales

Antes de escribir cualquier línea de SQL o configurar Supabase, internalizar estos principios:

```
1. NUNCA usar DELETE en ninguna tabla de negocio — solo UPDATE activo = false
2. SIEMPRE incluir clinica_id en tablas de negocio — es el discriminador de tenant
3. SIEMPRE habilitar RLS en cada tabla que crees
4. SIEMPRE crear políticas RLS de aislamiento por clinica_id
5. Los IDs son UUID generados por PostgreSQL — nunca secuencias numéricas
6. Todos los timestamps usan TIMESTAMPTZ (con timezone) — nunca TIMESTAMP sin tz
7. Los nombres de tablas y columnas van en snake_case y en español
8. Cada migración es atómica — un solo propósito, reversible si es posible
9. Los comentarios en SQL van en español
10. Supabase CLI es la única herramienta para aplicar migraciones — nunca SQL directo en prod
```

---

## 2. Estructura del Directorio de Migraciones

```
vittal-sistema/
└── supabase/
    ├── config.toml                          ← Configuración del proyecto Supabase
    ├── seed.sql                             ← Datos semilla (opcional, solo dev)
    └── migrations/
        ├── 20240101000001_create_clinicas.sql
        ├── 20240101000002_create_perfiles.sql
        ├── 20240101000003_create_usuarios.sql
        ├── 20240101000004_create_permisos.sql
        ├── 20240101000005_create_salas.sql
        ├── 20240101000006_create_pacientes.sql
        ├── 20240101000007_create_medicamentos.sql
        ├── 20240101000008_create_tipos_cirugia.sql
        ├── 20240101000009_create_cirugias.sql
        ├── 20240101000010_create_tipos_diagnostico.sql
        ├── 20240101000011_create_diagnosticos.sql
        ├── 20240101000012_create_tratamientos.sql
        ├── 20240101000013_create_recomendaciones.sql
        ├── 20240101000014_create_examenes.sql
        ├── 20240101000015_create_citas.sql
        ├── 20240101000016_create_expedientes.sql
        └── 20240101000017_create_hojas_cita.sql
```

### Nomenclatura de archivos de migración

```
Formato:  YYYYMMDDHHMMSS_[accion]_[tabla].sql
Acciones: create | alter | drop | add | remove | seed | index | policy

Ejemplos correctos:
  20240115093000_create_pacientes.sql
  20240116110000_alter_pacientes_add_foto_url.sql
  20240117140000_add_index_citas_fecha.sql
  20240118090000_policy_rls_expedientes.sql

Ejemplos incorrectos:
  pacientes.sql                  ← sin timestamp
  01_create_table.sql            ← sin nombre descriptivo
  CreatePacientes.sql            ← PascalCase incorrecto
```

---

## 3. Plantilla Maestra de Migración

Usar esta plantilla para TODA migración nueva. Copiar y adaptar:

```sql
-- =============================================================================
-- Migración: create_[tabla]
-- Descripción: [Descripción en español de qué hace esta migración]
-- Historia de Usuario: HU[XX] — [Nombre de la HU]
-- Agente: @IngenieroDatos
-- Fecha: [YYYY-MM-DD]
-- =============================================================================

-- -----------------------------------------------------------------------------
-- TABLA PRINCIPAL
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS [nombre_tabla] (

    -- Identificador único
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Discriminador de tenant (OBLIGATORIO en tablas de negocio)
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,

    -- Relaciones con otras entidades
    -- [foreign_key]_id   UUID NOT NULL REFERENCES [tabla](id) ON DELETE RESTRICT,

    -- Campos de negocio
    -- nombre             VARCHAR(255) NOT NULL,
    -- descripcion        TEXT,
    -- estado             VARCHAR(20) NOT NULL DEFAULT 'activo',

    -- Campos de auditoría (OBLIGATORIOS)
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL

);

-- -----------------------------------------------------------------------------
-- COMENTARIOS (en español, obligatorios)
-- -----------------------------------------------------------------------------
COMMENT ON TABLE [nombre_tabla] IS '[Descripción de la tabla]';
COMMENT ON COLUMN [nombre_tabla].id IS 'Identificador único autogenerado por la base de datos';
COMMENT ON COLUMN [nombre_tabla].clinica_id IS 'Clínica (tenant) a la que pertenece el registro';
COMMENT ON COLUMN [nombre_tabla].activo IS 'Estado del registro. FALSE = desactivado, nunca eliminado';
COMMENT ON COLUMN [nombre_tabla].fecha_creacion IS 'Fecha y hora UTC de creación del registro';
COMMENT ON COLUMN [nombre_tabla].fecha_modificacion IS 'Fecha y hora UTC de última modificación';

-- -----------------------------------------------------------------------------
-- ÍNDICES DE RENDIMIENTO
-- -----------------------------------------------------------------------------
-- Índice del discriminador de tenant (siempre primero)
CREATE INDEX IF NOT EXISTS idx_[tabla]_clinica_id
    ON [nombre_tabla](clinica_id);

-- Índice del campo activo (para filtrar registros activos)
CREATE INDEX IF NOT EXISTS idx_[tabla]_activo
    ON [nombre_tabla](activo);

-- Índice compuesto tenant + activo (consulta más frecuente)
CREATE INDEX IF NOT EXISTS idx_[tabla]_clinica_activo
    ON [nombre_tabla](clinica_id, activo);

-- [Agregar índices adicionales según campos de búsqueda frecuente]

-- -----------------------------------------------------------------------------
-- ROW LEVEL SECURITY (RLS)
-- -----------------------------------------------------------------------------
-- Habilitar RLS en la tabla
ALTER TABLE [nombre_tabla] ENABLE ROW LEVEL SECURITY;

-- Política de aislamiento por tenant (todas las operaciones)
CREATE POLICY "clinica_isolation_[tabla]" ON [nombre_tabla]
    FOR ALL
    USING (
        clinica_id = NULLIF(
            current_setting('app.current_clinica_id', true), ''
        )::UUID
    )
    WITH CHECK (
        clinica_id = NULLIF(
            current_setting('app.current_clinica_id', true), ''
        )::UUID
    );

-- Política para el rol de servicio (service_role bypassa RLS para admin del sistema)
CREATE POLICY "service_role_full_access_[tabla]" ON [nombre_tabla]
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

-- -----------------------------------------------------------------------------
-- GRANTS (permisos de acceso)
-- -----------------------------------------------------------------------------
GRANT SELECT, INSERT, UPDATE ON [nombre_tabla] TO authenticated;
-- NOTA: No se otorga DELETE — usar UPDATE activo = false
```

---

## 4. Migraciones de Tablas del Sistema Vittal

### 4.1 Tabla: clinicas (tabla base — sin clinica_id propio)

```sql
-- =============================================================================
-- Migración: create_clinicas
-- Descripción: Tabla raíz del sistema multi-tenant. Cada clínica es un tenant.
-- Historia de Usuario: HU09 — Gestión de Clínicas
-- =============================================================================

CREATE TABLE IF NOT EXISTS clinicas (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre                  VARCHAR(255) NOT NULL,
    direccion               TEXT,
    telefono                VARCHAR(20),
    email                   VARCHAR(255),
    logo_url                TEXT,
    tiempo_espera_minutos   INTEGER NOT NULL DEFAULT 30,
    -- Campos para integración con sistemas externos (HU09)
    bd_externa_1            VARCHAR(255),
    bd_externa_2            VARCHAR(255),
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ
);

COMMENT ON TABLE clinicas IS 'Clínicas registradas en el sistema. Cada clínica es un tenant del SaaS Vittal';
COMMENT ON COLUMN clinicas.tiempo_espera_minutos IS 'Minutos máximos antes de que un paciente genere alerta de espera';
COMMENT ON COLUMN clinicas.bd_externa_1 IS 'Nombre de base de datos externa del sistema 1 relacionado';
COMMENT ON COLUMN clinicas.bd_externa_2 IS 'Nombre de base de datos externa del sistema 2 relacionado';

CREATE INDEX IF NOT EXISTS idx_clinicas_activo ON clinicas(activo);

-- Clinicas NO tiene RLS por clinica_id (es la tabla raíz del tenant)
-- Se protege por service_role únicamente
ALTER TABLE clinicas ENABLE ROW LEVEL SECURITY;

CREATE POLICY "service_role_full_access_clinicas" ON clinicas
    FOR ALL TO service_role USING (true) WITH CHECK (true);

-- El rol authenticated solo puede leer su propia clínica
CREATE POLICY "authenticated_read_own_clinica" ON clinicas
    FOR SELECT TO authenticated
    USING (id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

GRANT SELECT ON clinicas TO authenticated;
```

### 4.2 Tabla: perfiles

```sql
-- =============================================================================
-- Migración: create_perfiles
-- Historia de Usuario: HU03 — Gestión de Perfiles
-- =============================================================================

CREATE TABLE IF NOT EXISTS perfiles (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id  UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre      VARCHAR(100) NOT NULL,
    descripcion TEXT,
    es_admin    BOOLEAN NOT NULL DEFAULT false,
    activo      BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ,

    UNIQUE (clinica_id, nombre)  -- No puede haber dos perfiles con el mismo nombre en la misma clínica
);

COMMENT ON TABLE perfiles IS 'Perfiles de acceso del sistema. Define el nivel de acceso de los usuarios';
COMMENT ON COLUMN perfiles.es_admin IS 'Si true, el usuario tiene acceso completo sin verificar permisos específicos';

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

### 4.3 Tabla: usuarios

```sql
-- =============================================================================
-- Migración: create_usuarios
-- Historia de Usuario: HU04 — Gestión de Usuarios
-- =============================================================================

CREATE TABLE IF NOT EXISTS usuarios (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    perfil_id           UUID NOT NULL REFERENCES perfiles(id) ON DELETE RESTRICT,
    -- Referencia al usuario en Supabase Auth
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
COMMENT ON COLUMN usuarios.es_doctor IS 'Si true, aparece como opción en filtros de doctor en Cola de Espera y Agenda';

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

### 4.4 Tabla: permisos

```sql
-- =============================================================================
-- Migración: create_permisos
-- Historia de Usuario: HU05 — Gestión de Permisos
-- =============================================================================

-- Catálogo de tareas/módulos del sistema (registradas desde BD, no modificables por usuario)
CREATE TABLE IF NOT EXISTS modulos_sistema (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clave       VARCHAR(50) NOT NULL UNIQUE,  -- 'pacientes', 'citas', 'expedientes'
    nombre      VARCHAR(100) NOT NULL,
    descripcion TEXT,
    activo      BOOLEAN NOT NULL DEFAULT true
);

COMMENT ON TABLE modulos_sistema IS 'Catálogo de módulos del sistema. Solo administradores del sistema pueden modificar';
COMMENT ON COLUMN modulos_sistema.clave IS 'Clave técnica usada en el código para verificar permisos';

-- Permisos asignados por perfil y módulo
CREATE TABLE IF NOT EXISTS permisos (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id  UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    perfil_id   UUID NOT NULL REFERENCES perfiles(id) ON DELETE RESTRICT,
    modulo_id   UUID NOT NULL REFERENCES modulos_sistema(id) ON DELETE RESTRICT,
    puede_leer      BOOLEAN NOT NULL DEFAULT false,
    puede_crear     BOOLEAN NOT NULL DEFAULT false,
    puede_actualizar BOOLEAN NOT NULL DEFAULT false,
    -- NOTA: No existe puede_eliminar — el sistema no permite eliminación
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modificado_por UUID REFERENCES usuarios(id),

    UNIQUE (clinica_id, perfil_id, modulo_id)
);

COMMENT ON TABLE permisos IS 'Permisos por perfil y módulo. Solo READ, CREATE, UPDATE — no DELETE';
COMMENT ON COLUMN permisos.puede_leer IS 'Permite visualizar listados y registros del módulo';
COMMENT ON COLUMN permisos.puede_crear IS 'Permite insertar nuevos registros en el módulo';
COMMENT ON COLUMN permisos.puede_actualizar IS 'Permite editar registros existentes en el módulo';

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

-- Seed de módulos del sistema (datos base — no son de ningún tenant)
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

### 4.5 Tabla: pacientes

```sql
-- =============================================================================
-- Migración: create_pacientes
-- Historia de Usuario: HU07 — Gestión de Pacientes
-- =============================================================================

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
    fecha_nacimiento    DATE,
    foto_url            TEXT,                           -- URL de Supabase Storage
    observaciones       TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

COMMENT ON TABLE pacientes IS 'Registro de pacientes por clínica. Los pacientes no se eliminan, solo se desactivan';
COMMENT ON COLUMN pacientes.doctor_id IS 'Doctor al que está asignado el paciente por defecto';
COMMENT ON COLUMN pacientes.foto_url IS 'URL pública de la foto del paciente almacenada en Supabase Storage bucket: avatares';

CREATE INDEX IF NOT EXISTS idx_pacientes_clinica_id ON pacientes(clinica_id);
CREATE INDEX IF NOT EXISTS idx_pacientes_doctor_id ON pacientes(doctor_id);
CREATE INDEX IF NOT EXISTS idx_pacientes_clinica_activo ON pacientes(clinica_id, activo);
CREATE INDEX IF NOT EXISTS idx_pacientes_nombre ON pacientes(clinica_id, primer_apellido, primer_nombre);

ALTER TABLE pacientes ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_pacientes" ON pacientes
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_pacientes" ON pacientes
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON pacientes TO authenticated;
```

### 4.6 Tabla: citas (Agenda — HU21)

```sql
-- =============================================================================
-- Migración: create_citas
-- Historia de Usuario: HU21 — Agenda
-- =============================================================================

CREATE TABLE IF NOT EXISTS citas (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    paciente_id     UUID NOT NULL REFERENCES pacientes(id) ON DELETE RESTRICT,
    doctor_id       UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    sala_id         UUID REFERENCES salas(id) ON DELETE SET NULL,
    fecha_cita      DATE NOT NULL,
    hora_cita       TIME NOT NULL,
    hora_llegada    TIME,
    lugar           VARCHAR(255),
    motivo          TEXT,
    estado          VARCHAR(20) NOT NULL DEFAULT 'agendada'
                    CHECK (estado IN ('agendada', 'en_espera', 'en_atencion', 'atendida', 'cancelada')),
    notas           TEXT,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ,
    creado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por  UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

COMMENT ON TABLE citas IS 'Citas médicas programadas. Estados: agendada, en_espera, en_atencion, atendida, cancelada';
COMMENT ON COLUMN citas.hora_llegada IS 'Hora en que el paciente llegó físicamente a la clínica';
COMMENT ON COLUMN citas.estado IS 'Estado del flujo de la cita: agendada → en_espera → en_atencion → atendida';

CREATE INDEX IF NOT EXISTS idx_citas_clinica_id ON citas(clinica_id);
CREATE INDEX IF NOT EXISTS idx_citas_doctor_id ON citas(doctor_id);
CREATE INDEX IF NOT EXISTS idx_citas_paciente_id ON citas(paciente_id);
CREATE INDEX IF NOT EXISTS idx_citas_fecha ON citas(clinica_id, fecha_cita);
CREATE INDEX IF NOT EXISTS idx_citas_estado ON citas(clinica_id, estado);
-- Índice compuesto para Cola de Espera (consulta más frecuente del módulo)
CREATE INDEX IF NOT EXISTS idx_citas_cola_espera
    ON citas(clinica_id, doctor_id, fecha_cita, hora_cita)
    WHERE estado IN ('agendada', 'en_espera');

ALTER TABLE citas ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_citas" ON citas
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_citas" ON citas
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON citas TO authenticated;
```

### 4.7 Tabla: expedientes y hojas de cita (HU20)

```sql
-- =============================================================================
-- Migración: create_expedientes
-- Historia de Usuario: HU20 — Gestión de Expedientes
-- =============================================================================

-- Un expediente por paciente
CREATE TABLE IF NOT EXISTS expedientes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    paciente_id     UUID NOT NULL REFERENCES pacientes(id) ON DELETE RESTRICT,
    doctor_id       UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    notas_generales TEXT,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ,

    UNIQUE (clinica_id, paciente_id)  -- Solo un expediente por paciente por clínica
);

-- Cada visita médica es una hoja de cita
CREATE TABLE IF NOT EXISTS hojas_cita (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    expediente_id   UUID NOT NULL REFERENCES expedientes(id) ON DELETE RESTRICT,
    cita_id         UUID REFERENCES citas(id) ON DELETE SET NULL,
    doctor_id       UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    fecha_consulta  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    motivo_consulta TEXT,
    notas_consulta  TEXT,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ
);

-- Diagnósticos de una hoja de cita
CREATE TABLE IF NOT EXISTS hoja_diagnosticos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id    UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    diagnostico_id  UUID NOT NULL REFERENCES diagnosticos(id) ON DELETE RESTRICT,
    observaciones   TEXT,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tratamientos/Receta de una hoja de cita
CREATE TABLE IF NOT EXISTS hoja_tratamientos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id    UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    medicamento_id  UUID REFERENCES medicamentos(id) ON DELETE SET NULL,
    tratamiento_id  UUID REFERENCES tratamientos(id) ON DELETE SET NULL,
    dosis           VARCHAR(100),
    frecuencia      VARCHAR(100),
    duracion        VARCHAR(100),
    instrucciones   TEXT,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Cirugías registradas en una hoja de cita
CREATE TABLE IF NOT EXISTS hoja_cirugias (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id  UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    cirugia_id  UUID NOT NULL REFERENCES cirugias(id) ON DELETE RESTRICT,
    fecha_cirugia DATE,
    observaciones TEXT,
    activo      BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Exámenes solicitados en una hoja de cita
CREATE TABLE IF NOT EXISTS hoja_examenes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id    UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    examen_id       UUID NOT NULL REFERENCES examenes(id) ON DELETE RESTRICT,
    resultado       TEXT,
    archivo_url     TEXT,           -- URL de resultado en Supabase Storage
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Archivos adjuntos generales del expediente
CREATE TABLE IF NOT EXISTS expediente_archivos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    expediente_id   UUID NOT NULL REFERENCES expedientes(id) ON DELETE RESTRICT,
    hoja_cita_id    UUID REFERENCES hojas_cita(id) ON DELETE SET NULL,
    nombre_archivo  VARCHAR(255) NOT NULL,
    tipo_mime       VARCHAR(100) NOT NULL,
    storage_path    TEXT NOT NULL,          -- Path en Supabase Storage
    url_publica     TEXT,                   -- URL de acceso (token temporal)
    tamano_bytes    BIGINT,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por      UUID REFERENCES usuarios(id)
);

-- Índices para expedientes
CREATE INDEX IF NOT EXISTS idx_expedientes_clinica ON expedientes(clinica_id);
CREATE INDEX IF NOT EXISTS idx_expedientes_paciente ON expedientes(clinica_id, paciente_id);
CREATE INDEX IF NOT EXISTS idx_hojas_cita_expediente ON hojas_cita(clinica_id, expediente_id);
CREATE INDEX IF NOT EXISTS idx_hojas_cita_fecha ON hojas_cita(clinica_id, fecha_consulta DESC);
CREATE INDEX IF NOT EXISTS idx_expediente_archivos_expediente ON expediente_archivos(clinica_id, expediente_id);

-- RLS para todas las tablas de expedientes
DO $$
DECLARE
    t TEXT;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'expedientes', 'hojas_cita', 'hoja_diagnosticos',
        'hoja_tratamientos', 'hoja_cirugias', 'hoja_examenes', 'expediente_archivos'
    ]
    LOOP
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);

        EXECUTE format(
            'CREATE POLICY "clinica_isolation_%s" ON %I
             FOR ALL
             USING (clinica_id = NULLIF(current_setting(''app.current_clinica_id'', true), '''')::UUID)
             WITH CHECK (clinica_id = NULLIF(current_setting(''app.current_clinica_id'', true), '''')::UUID)',
            t, t
        );

        EXECUTE format(
            'CREATE POLICY "service_role_full_%s" ON %I
             FOR ALL TO service_role USING (true) WITH CHECK (true)',
            t, t
        );

        EXECUTE format('GRANT SELECT, INSERT, UPDATE ON %I TO authenticated', t);
    END LOOP;
END;
$$;
```

---

## 5. Configuración de Supabase Storage

### Buckets requeridos

```sql
-- Ejecutar en el Dashboard de Supabase o via CLI

-- Bucket para archivos de expedientes médicos (privado)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'expedientes',
    'expedientes',
    false,                          -- PRIVADO — acceso solo con token
    52428800,                       -- 50MB límite por archivo
    ARRAY[
        'application/pdf',
        'image/jpeg',
        'image/png',
        'image/webp',
        'application/msword',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
    ]
);

-- Bucket para fotos de pacientes y usuarios (público)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'avatares',
    'avatares',
    true,                           -- PÚBLICO — URL accesible directamente
    5242880,                        -- 5MB límite
    ARRAY['image/jpeg', 'image/png', 'image/webp']
);
```

### Políticas de Storage

```sql
-- Política: usuarios autenticados pueden leer archivos de su clínica
CREATE POLICY "clinica_read_expedientes"
ON storage.objects FOR SELECT
TO authenticated
USING (
    bucket_id = 'expedientes'
    AND (storage.foldername(name))[1] = (
        current_setting('app.current_clinica_id', true)
    )
);

-- Política: usuarios autenticados pueden subir archivos a su clínica
CREATE POLICY "clinica_insert_expedientes"
ON storage.objects FOR INSERT
TO authenticated
WITH CHECK (
    bucket_id = 'expedientes'
    AND (storage.foldername(name))[1] = (
        current_setting('app.current_clinica_id', true)
    )
);

-- Política: avatares son de lectura pública
CREATE POLICY "public_read_avatares"
ON storage.objects FOR SELECT
TO public
USING (bucket_id = 'avatares');

-- Política: solo usuarios autenticados pueden subir avatares
CREATE POLICY "authenticated_insert_avatares"
ON storage.objects FOR INSERT
TO authenticated
WITH CHECK (bucket_id = 'avatares');
```

### Ruta de almacenamiento estándar

```
expedientes/
└── {clinica_id}/
    └── {paciente_id}/
        ├── {uuid}-resultado-exam.pdf
        ├── {uuid}-imagen-ojo.jpg
        └── {uuid}-epicrisis.pdf

avatares/
└── pacientes/
    └── {paciente_id}.jpg
```

---

## 6. Configuración de Supabase Realtime

Para los módulos de Cola de Espera (HU18) y Alertas (HU23):

```sql
-- Habilitar Realtime en tablas de tiempo real
ALTER PUBLICATION supabase_realtime ADD TABLE citas;
ALTER PUBLICATION supabase_realtime ADD TABLE alertas_espera;

-- Tabla para alertas de tiempo de espera
CREATE TABLE IF NOT EXISTS alertas_espera (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    cita_id         UUID NOT NULL REFERENCES citas(id) ON DELETE RESTRICT,
    paciente_id     UUID NOT NULL REFERENCES pacientes(id) ON DELETE RESTRICT,
    doctor_id       UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    sala_id         UUID REFERENCES salas(id),
    hora_cita       TIME NOT NULL,
    hora_llegada    TIME,
    minutos_espera  INTEGER NOT NULL,
    resuelta        BOOLEAN NOT NULL DEFAULT false,
    fecha_alerta    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_resolucion TIMESTAMPTZ
);

COMMENT ON TABLE alertas_espera IS 'Alertas generadas cuando un paciente excede el tiempo de espera configurado en su clínica';

ALTER TABLE alertas_espera ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_alertas" ON alertas_espera
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_alertas" ON alertas_espera
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON alertas_espera TO authenticated;

ALTER PUBLICATION supabase_realtime ADD TABLE alertas_espera;
```

---

## 7. Edge Functions

### Función: verificar-alertas-espera

```typescript
// supabase/functions/verificar-alertas-espera/index.ts
// Se ejecuta cada minuto via scheduled task o llamada desde el API

import { createClient } from 'https://esm.sh/@supabase/supabase-js@2'

Deno.serve(async (_req) => {
  const supabase = createClient(
    Deno.env.get('SUPABASE_URL')!,
    Deno.env.get('SUPABASE_SERVICE_ROLE_KEY')!
  )

  // Obtener todas las clínicas activas con su tiempo de espera configurado
  const { data: clinicas } = await supabase
    .from('clinicas')
    .select('id, tiempo_espera_minutos')
    .eq('activo', true)

  for (const clinica of clinicas ?? []) {
    // Buscar citas en espera que superan el tiempo configurado
    const { data: citasExcedidas } = await supabase
      .from('citas')
      .select(`
        id, paciente_id, doctor_id, sala_id,
        hora_cita, hora_llegada,
        pacientes(primer_nombre, primer_apellido)
      `)
      .eq('clinica_id', clinica.id)
      .eq('fecha_cita', new Date().toISOString().split('T')[0])
      .in('estado', ['en_espera', 'agendada'])
      .not('hora_llegada', 'is', null)

    for (const cita of citasExcedidas ?? []) {
      const llegada = new Date(`1970-01-01T${cita.hora_llegada}`)
      const ahora = new Date()
      const minutosEspera = Math.floor((ahora.getTime() - llegada.getTime()) / 60000)

      if (minutosEspera >= clinica.tiempo_espera_minutos) {
        // Insertar alerta (Realtime la enviará automáticamente a clientes suscritos)
        await supabase.from('alertas_espera').upsert({
          clinica_id: clinica.id,
          cita_id: cita.id,
          paciente_id: cita.paciente_id,
          doctor_id: cita.doctor_id,
          sala_id: cita.sala_id,
          hora_cita: cita.hora_cita,
          hora_llegada: cita.hora_llegada,
          minutos_espera: minutosEspera,
          resuelta: false
        }, { onConflict: 'cita_id' })
      }
    }
  }

  return new Response(JSON.stringify({ ok: true }), {
    headers: { 'Content-Type': 'application/json' }
  })
})
```

---

## 8. Comandos de Referencia Supabase CLI

```bash
# ── Inicialización del proyecto ──────────────────────────────────────────
# Inicializar Supabase en el repositorio (solo una vez)
supabase init

# Configurar proyecto remoto
supabase link --project-ref [tu-project-ref]

# ── Trabajo con migraciones ───────────────────────────────────────────────
# Crear nueva migración
supabase migration new [nombre_descripcion]

# Ver migraciones pendientes
supabase migration list

# Aplicar migraciones al entorno local
supabase db reset

# Aplicar migraciones al entorno remoto (producción)
supabase db push

# Verificar estado de la BD local
supabase status

# ── Desarrollo local ─────────────────────────────────────────────────────
# Iniciar Supabase local (Docker requerido)
supabase start

# Detener Supabase local
supabase stop

# Ver logs de la BD local
supabase db logs

# Acceder a la BD local con psql
supabase db connect

# ── Storage y Functions ───────────────────────────────────────────────────
# Desplegar Edge Function
supabase functions deploy [nombre-funcion]

# Ver logs de una Edge Function
supabase functions logs [nombre-funcion]

# ── Inspección y diagnóstico ─────────────────────────────────────────────
# Generar tipos TypeScript desde el schema (útil para el frontend)
supabase gen types typescript --project-id [tu-project-ref] > src/types/supabase.ts

# Verificar políticas RLS activas
supabase db diff
```

---

## 9. Checklist de Calidad — @IngenieroDatos

Antes de notificar al @PM que el DAL está listo, verificar:

### Migración SQL

- [ ] Archivo nombrado con timestamp correcto: `YYYYMMDDHHMMSS_create_[tabla].sql`
- [ ] Tabla tiene `id UUID PRIMARY KEY DEFAULT gen_random_uuid()`
- [ ] Tabla tiene `clinica_id UUID NOT NULL REFERENCES clinicas(id)`
- [ ] Tabla tiene `activo BOOLEAN NOT NULL DEFAULT true`
- [ ] Tabla tiene `fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- [ ] Tabla tiene `fecha_modificacion TIMESTAMPTZ`
- [ ] Comentarios en español en tabla y columnas principales
- [ ] `UNIQUE` constraints aplicados donde corresponde
- [ ] `CHECK` constraints en campos de enumeración (estado, sexo, etc.)
- [ ] `ON DELETE RESTRICT` en FKs de negocio (nunca CASCADE en prod)

### Índices

- [ ] Índice en `clinica_id`
- [ ] Índice en `activo`
- [ ] Índice compuesto `(clinica_id, activo)`
- [ ] Índices en columnas usadas en filtros frecuentes (`doctor_id`, `fecha_cita`, `estado`)
- [ ] Índice parcial en consultas de Cola de Espera si aplica

### RLS

- [ ] `ALTER TABLE [tabla] ENABLE ROW LEVEL SECURITY`
- [ ] Política `clinica_isolation_[tabla]` creada con FOR ALL
- [ ] Política `service_role_full_access_[tabla]` creada para el rol service_role
- [ ] `GRANT SELECT, INSERT, UPDATE ON [tabla] TO authenticated`
- [ ] **No existe GRANT DELETE**

### Repository (C# / Dapper)

- [ ] Implementa `IPacienteRepository` (o la interfaz del módulo)
- [ ] `GetAllAsync(Guid clinicaId)` filtra por `clinica_id` y `activo = true`
- [ ] `GetByIdAsync(Guid id, Guid clinicaId)` filtra por ambas columnas
- [ ] `CreateAsync` inserta con `clinica_id` del parámetro, no de ningún otro origen
- [ ] `UpdateAsync` incluye `clinica_id` en el WHERE como segundo guard
- [ ] `DeactivateAsync` hace `UPDATE activo = false` — **no DELETE**
- [ ] Todas las queries usan parámetros `@NombreParametro` — **no interpolación de string**
- [ ] Repository registrado en `Vittal.IOC/DependencyInjection.cs`
- [ ] Migración aplicada exitosamente con `supabase db push`

---

## 10. Errores Comunes y Cómo Evitarlos

| Error | Causa | Solución |
|---|---|---|
| `permission denied for table X` | RLS habilitado sin política para `authenticated` | Crear política `clinica_isolation` y GRANT correspondiente |
| `null value in column clinica_id` | Insertar sin proporcionar `clinica_id` | Siempre extraer `clinicaId` del JWT y pasarlo al Repository |
| `violates foreign key constraint` | Referenciar un ID que no existe o es de otro tenant | Validar existencia y tenant antes de insertar |
| Datos de otro tenant visibles | Política RLS incorrecta o `current_setting` no configurado | Verificar que el middleware del API configura `app.current_clinica_id` antes de cada query |
| `migration already applied` | Intentar re-aplicar migración ya existente | Crear una nueva migración con `ALTER TABLE` o `ALTER COLUMN` |
| Query lenta sin índice | Consulta por columnas sin índice | Agregar migración de índice (`YYYYMMDDHHMMSS_add_index_X.sql`) |
| UUID inválido en `gen_random_uuid()` | Extensión `pgcrypto` no habilitada | `CREATE EXTENSION IF NOT EXISTS "pgcrypto"` (Supabase lo incluye por defecto) |

---

*skill-supabase.md — Vittal v1.0.0 | Agente: @IngenieroDatos*
*Para contexto del proyecto: CLAUDE.md | Para coordinación de agentes: ORCHESTRATOR.md*
