# Supabase Migrations — Business Tables

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar:** Para crear tablas de negocio (pacientes, citas, expedientes, catálogos).
> **Prerequisito:** skills/supabase/SKILL.md, skills/supabase/migrations-core.md

---

## Tabla: pacientes (HU07)

```sql
-- Migración: create_pacientes | HU07
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
    foto_url            TEXT,
    observaciones       TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

COMMENT ON TABLE pacientes IS 'Registro de pacientes por clínica. Los pacientes no se eliminan, solo se desactivan';
COMMENT ON COLUMN pacientes.doctor_id IS 'Doctor al que está asignado el paciente por defecto';
COMMENT ON COLUMN pacientes.foto_url IS 'URL de la foto del paciente en Supabase Storage bucket: avatares';

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

---

## Tabla: citas (HU21)

```sql
-- Migración: create_citas | HU21
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

COMMENT ON TABLE citas IS 'Citas médicas. Estados: agendada, en_espera, en_atencion, atendida, cancelada';
COMMENT ON COLUMN citas.hora_llegada IS 'Hora en que el paciente llegó físicamente a la clínica';
COMMENT ON COLUMN citas.estado IS 'Estado del flujo: agendada → en_espera → en_atencion → atendida';

CREATE INDEX IF NOT EXISTS idx_citas_clinica_id ON citas(clinica_id);
CREATE INDEX IF NOT EXISTS idx_citas_doctor_id ON citas(doctor_id);
CREATE INDEX IF NOT EXISTS idx_citas_paciente_id ON citas(paciente_id);
CREATE INDEX IF NOT EXISTS idx_citas_fecha ON citas(clinica_id, fecha_cita);
CREATE INDEX IF NOT EXISTS idx_citas_estado ON citas(clinica_id, estado);
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

---

## Tablas: expedientes y hojas de cita (HU20)

```sql
-- Migración: create_expedientes | HU20

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
    UNIQUE (clinica_id, paciente_id)
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
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ
);

-- Tratamientos/Receta
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
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ
);

-- Cirugías en una hoja de cita
CREATE TABLE IF NOT EXISTS hoja_cirugias (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id  UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    cirugia_id  UUID NOT NULL REFERENCES cirugias(id) ON DELETE RESTRICT,
    fecha_cirugia DATE,
    observaciones TEXT,
    activo      BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ
);

-- Exámenes solicitados
CREATE TABLE IF NOT EXISTS hoja_examenes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id    UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    examen_id       UUID NOT NULL REFERENCES examenes(id) ON DELETE RESTRICT,
    resultado       TEXT,
    archivo_url     TEXT,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMPTZ
);

-- Archivos adjuntos del expediente
CREATE TABLE IF NOT EXISTS expediente_archivos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    expediente_id   UUID NOT NULL REFERENCES expedientes(id) ON DELETE RESTRICT,
    hoja_cita_id    UUID REFERENCES hojas_cita(id) ON DELETE SET NULL,
    nombre_archivo  VARCHAR(255) NOT NULL,
    tipo_mime       VARCHAR(100) NOT NULL,
    storage_path    TEXT NOT NULL,
    url_publica     TEXT,
    tamano_bytes    BIGINT,
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por      UUID REFERENCES usuarios(id)
);

-- Índices
CREATE INDEX IF NOT EXISTS idx_expedientes_clinica ON expedientes(clinica_id);
CREATE INDEX IF NOT EXISTS idx_expedientes_paciente ON expedientes(clinica_id, paciente_id);
CREATE INDEX IF NOT EXISTS idx_hojas_cita_expediente ON hojas_cita(clinica_id, expediente_id);
CREATE INDEX IF NOT EXISTS idx_hojas_cita_fecha ON hojas_cita(clinica_id, fecha_consulta DESC);
CREATE INDEX IF NOT EXISTS idx_expediente_archivos_expediente ON expediente_archivos(clinica_id, expediente_id);

-- RLS para todas las tablas de expedientes (bloque atómico)
DO $$
DECLARE t TEXT;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'expedientes', 'hojas_cita', 'hoja_diagnosticos',
        'hoja_tratamientos', 'hoja_cirugias', 'hoja_examenes', 'expediente_archivos'
    ]
    LOOP
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);
        EXECUTE format(
            'CREATE POLICY "clinica_isolation_%s" ON %I FOR ALL
             USING (clinica_id = NULLIF(current_setting(''app.current_clinica_id'', true), '''')::UUID)
             WITH CHECK (clinica_id = NULLIF(current_setting(''app.current_clinica_id'', true), '''')::UUID)',
            t, t);
        EXECUTE format(
            'CREATE POLICY "service_role_full_%s" ON %I FOR ALL TO service_role USING (true) WITH CHECK (true)',
            t, t);
        EXECUTE format('GRANT SELECT, INSERT, UPDATE ON %I TO authenticated', t);
    END LOOP;
END; $$;
```

---

## Checklist de Calidad — Migrations Business

### Catálogos (pacientes, medicamentos, salas, etc.)
- [ ] Patrón estándar: plantilla de migrations-core.md aplicada
- [ ] CHECK constraints en campos de enumeración (sexo, estado)
- [ ] UNIQUE constraints en campos de unicidad por clínica
- [ ] FK con `ON DELETE RESTRICT` para datos críticos
- [ ] FK con `ON DELETE SET NULL` para datos opcionales

### Expedientes
- [ ] UNIQUE (clinica_id, paciente_id) en expedientes
- [ ] Transacciones para crear expediente + primera hoja de cita
- [ ] RLS aplicado a las 7 tablas del expediente
- [ ] Índice en fecha_consulta DESC para orden cronológico

### Citas
- [ ] CHECK constraint en estados válidos
- [ ] Índice parcial para Cola de Espera (estado IN agendada/en_espera)
- [ ] Índice compuesto (clinica_id, fecha_cita)

---

*skills/supabase/migrations-business.md — Vittal v1.0.0*
