-- =============================================================================
-- Migración: create_expedientes
-- Descripción: Expedientes médicos, hojas de cita y todas las sub-tablas clínicas.
-- Historia de Usuario: HU20 — Gestión de Expedientes
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

-- Un expediente por paciente por clínica
CREATE TABLE IF NOT EXISTS expedientes (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    paciente_id         UUID NOT NULL REFERENCES pacientes(id) ON DELETE RESTRICT,
    doctor_id           UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    notas_generales     TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,

    UNIQUE (clinica_id, paciente_id)
);
COMMENT ON TABLE expedientes IS 'Expediente médico de un paciente. Uno por paciente por clínica. Contiene todas las hojas de cita.';
COMMENT ON COLUMN expedientes.clinica_id IS 'Clínica (tenant) a la que pertenece el expediente';

-- Cada visita médica es una hoja de cita
CREATE TABLE IF NOT EXISTS hojas_cita (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    expediente_id       UUID NOT NULL REFERENCES expedientes(id) ON DELETE RESTRICT,
    cita_id             UUID REFERENCES citas(id) ON DELETE SET NULL,
    doctor_id           UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    fecha_consulta      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    motivo_consulta     TEXT,
    notas_consulta      TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);
COMMENT ON TABLE hojas_cita IS 'Registro de una consulta médica específica. Pertenece a un expediente.';

-- Diagnósticos de una hoja de cita
CREATE TABLE IF NOT EXISTS hoja_diagnosticos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id        UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    diagnostico_id      UUID NOT NULL REFERENCES diagnosticos(id) ON DELETE RESTRICT,
    observaciones       TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);
COMMENT ON TABLE hoja_diagnosticos IS 'Diagnósticos médicos registrados en una hoja de cita específica';

-- Tratamientos/Receta de una hoja de cita
CREATE TABLE IF NOT EXISTS hoja_tratamientos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id        UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    medicamento_id      UUID REFERENCES medicamentos(id) ON DELETE SET NULL,
    tratamiento_id      UUID REFERENCES tratamientos(id) ON DELETE SET NULL,
    dosis               VARCHAR(100),
    frecuencia          VARCHAR(100),
    duracion            VARCHAR(100),
    instrucciones       TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);
COMMENT ON TABLE hoja_tratamientos IS 'Tratamientos y medicamentos prescritos en una hoja de cita';

-- Cirugías registradas en una hoja de cita
CREATE TABLE IF NOT EXISTS hoja_cirugias (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id        UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    cirugia_id          UUID NOT NULL REFERENCES cirugias(id) ON DELETE RESTRICT,
    fecha_cirugia       DATE,
    observaciones       TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);
COMMENT ON TABLE hoja_cirugias IS 'Cirugías realizadas o programadas, registradas en una hoja de cita';

-- Exámenes solicitados en una hoja de cita
CREATE TABLE IF NOT EXISTS hoja_examenes (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id        UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    examen_id           UUID NOT NULL REFERENCES examenes(id) ON DELETE RESTRICT,
    resultado           TEXT,
    archivo_url         TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);
COMMENT ON TABLE hoja_examenes IS 'Exámenes solicitados y sus resultados, asociados a una hoja de cita';
COMMENT ON COLUMN hoja_examenes.archivo_url IS 'URL del archivo de resultado almacenado en Supabase Storage bucket: expedientes';

-- Archivos adjuntos generales del expediente
CREATE TABLE IF NOT EXISTS expediente_archivos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    expediente_id       UUID NOT NULL REFERENCES expedientes(id) ON DELETE RESTRICT,
    hoja_cita_id        UUID REFERENCES hojas_cita(id) ON DELETE SET NULL,
    nombre_archivo      VARCHAR(255) NOT NULL,
    tipo_mime           VARCHAR(100) NOT NULL,
    storage_path        TEXT NOT NULL,
    url_publica         TEXT,
    tamano_bytes        BIGINT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    creado_por          UUID REFERENCES usuarios(id)
);
COMMENT ON TABLE expediente_archivos IS 'Archivos adjuntos (PDF, imágenes) de un expediente médico';
COMMENT ON COLUMN expediente_archivos.storage_path IS 'Ruta del archivo en Supabase Storage: {clinica_id}/{paciente_id}/{uuid}-archivo';
COMMENT ON COLUMN expediente_archivos.url_publica IS 'URL de acceso temporal con token — nunca URL pública permanente';

-- Índices
CREATE INDEX IF NOT EXISTS idx_expedientes_clinica ON expedientes(clinica_id);
CREATE INDEX IF NOT EXISTS idx_expedientes_paciente ON expedientes(clinica_id, paciente_id);
CREATE INDEX IF NOT EXISTS idx_hojas_cita_expediente ON hojas_cita(clinica_id, expediente_id);
CREATE INDEX IF NOT EXISTS idx_hojas_cita_fecha ON hojas_cita(clinica_id, fecha_consulta DESC);
CREATE INDEX IF NOT EXISTS idx_hoja_diagnosticos_hoja ON hoja_diagnosticos(hoja_cita_id);
CREATE INDEX IF NOT EXISTS idx_hoja_tratamientos_hoja ON hoja_tratamientos(hoja_cita_id);
CREATE INDEX IF NOT EXISTS idx_hoja_cirugias_hoja ON hoja_cirugias(hoja_cita_id);
CREATE INDEX IF NOT EXISTS idx_hoja_examenes_hoja ON hoja_examenes(hoja_cita_id);
CREATE INDEX IF NOT EXISTS idx_expediente_archivos ON expediente_archivos(clinica_id, expediente_id);

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
