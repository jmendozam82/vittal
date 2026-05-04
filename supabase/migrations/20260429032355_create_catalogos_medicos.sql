-- =============================================================================
-- Migración: create_catalogos_medicos
-- Descripción: Todos los catálogos médicos del sistema (tipos_cirugia, cirugias,
--              tipos_diagnostico, diagnosticos, tratamientos, recomendaciones, examenes)
-- Historia de Usuario: HU10-HU17
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

-- -----------------------------------------------------------------------------
-- tipos_cirugia (HU10)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tipos_cirugia (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(255) NOT NULL,
    descripcion         TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    UNIQUE (clinica_id, nombre)
);
COMMENT ON TABLE tipos_cirugia IS 'Catálogo de tipos de cirugías (ej: Catarata, LASIK, Pterigión) por clínica';

-- -----------------------------------------------------------------------------
-- cirugias (HU11)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS cirugias (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    tipo_cirugia_id     UUID NOT NULL REFERENCES tipos_cirugia(id) ON DELETE RESTRICT,
    nombre              VARCHAR(255) NOT NULL,
    descripcion         TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    UNIQUE (clinica_id, nombre)
);
COMMENT ON TABLE cirugias IS 'Catálogo de cirugías específicas, clasificadas por tipo_cirugia';

-- -----------------------------------------------------------------------------
-- tipos_diagnostico (HU12)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tipos_diagnostico (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(255) NOT NULL,
    descripcion         TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    UNIQUE (clinica_id, nombre)
);
COMMENT ON TABLE tipos_diagnostico IS 'Catálogo de tipos de diagnóstico (ej: Refractivo, Glaucoma, Retina) por clínica';

-- -----------------------------------------------------------------------------
-- diagnosticos (HU13)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS diagnosticos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    tipo_diagnostico_id UUID NOT NULL REFERENCES tipos_diagnostico(id) ON DELETE RESTRICT,
    nombre              VARCHAR(255) NOT NULL,
    descripcion         TEXT,
    codigo_cie10        VARCHAR(10),
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    UNIQUE (clinica_id, nombre)
);
COMMENT ON TABLE diagnosticos IS 'Catálogo de diagnósticos médicos, clasificados por tipo_diagnostico. Incluye código CIE-10';
COMMENT ON COLUMN diagnosticos.codigo_cie10 IS 'Código de la Clasificación Internacional de Enfermedades versión 10';

-- -----------------------------------------------------------------------------
-- tratamientos (HU14)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tratamientos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(255) NOT NULL,
    descripcion         TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    UNIQUE (clinica_id, nombre)
);
COMMENT ON TABLE tratamientos IS 'Catálogo de tratamientos médicos disponibles por clínica';

-- -----------------------------------------------------------------------------
-- recomendaciones (HU15)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS recomendaciones (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(255) NOT NULL,
    descripcion         TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    UNIQUE (clinica_id, nombre)
);
COMMENT ON TABLE recomendaciones IS 'Catálogo de recomendaciones médicas predefinidas para incluir en expedientes';

-- -----------------------------------------------------------------------------
-- examenes (HU16)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS examenes (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(255) NOT NULL,
    descripcion         TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    UNIQUE (clinica_id, nombre)
);
COMMENT ON TABLE examenes IS 'Catálogo de exámenes médicos que pueden ser solicitados en una consulta';

-- -----------------------------------------------------------------------------
-- Índices de todos los catálogos
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_tipos_cirugia_clinica ON tipos_cirugia(clinica_id, activo);
CREATE INDEX IF NOT EXISTS idx_cirugias_clinica ON cirugias(clinica_id, activo);
CREATE INDEX IF NOT EXISTS idx_cirugias_tipo ON cirugias(tipo_cirugia_id);
CREATE INDEX IF NOT EXISTS idx_tipos_diagnostico_clinica ON tipos_diagnostico(clinica_id, activo);
CREATE INDEX IF NOT EXISTS idx_diagnosticos_clinica ON diagnosticos(clinica_id, activo);
CREATE INDEX IF NOT EXISTS idx_diagnosticos_tipo ON diagnosticos(tipo_diagnostico_id);
CREATE INDEX IF NOT EXISTS idx_tratamientos_clinica ON tratamientos(clinica_id, activo);
CREATE INDEX IF NOT EXISTS idx_recomendaciones_clinica ON recomendaciones(clinica_id, activo);
CREATE INDEX IF NOT EXISTS idx_examenes_clinica ON examenes(clinica_id, activo);

-- -----------------------------------------------------------------------------
-- RLS para todos los catálogos médicos (patrón único)
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    t TEXT;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'tipos_cirugia', 'cirugias', 'tipos_diagnostico', 'diagnosticos',
        'tratamientos', 'recomendaciones', 'examenes'
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
