-- =============================================================================
-- Migración: create_salas
-- Descripción: Salas y áreas de atención por clínica.
-- Historia de Usuario: HU06 — Gestión de Salas
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

CREATE TABLE IF NOT EXISTS salas (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(100) NOT NULL,
    descripcion         TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,

    UNIQUE (clinica_id, nombre)
);

COMMENT ON TABLE salas IS 'Salas y áreas de atención médica por clínica';
COMMENT ON COLUMN salas.id IS 'Identificador único autogenerado por la base de datos';
COMMENT ON COLUMN salas.clinica_id IS 'Clínica (tenant) a la que pertenece la sala';
COMMENT ON COLUMN salas.activo IS 'Estado del registro. FALSE = desactivado, nunca eliminado';
COMMENT ON COLUMN salas.fecha_creacion IS 'Fecha y hora UTC de creación del registro';
COMMENT ON COLUMN salas.fecha_modificacion IS 'Fecha y hora UTC de última modificación';

CREATE INDEX IF NOT EXISTS idx_salas_clinica_id ON salas(clinica_id);
CREATE INDEX IF NOT EXISTS idx_salas_clinica_activo ON salas(clinica_id, activo);

ALTER TABLE salas ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_salas" ON salas
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_salas" ON salas
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON salas TO authenticated;
