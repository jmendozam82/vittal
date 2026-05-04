-- =============================================================================
-- Migración: create_medicamentos
-- Descripción: Catálogo de medicamentos por clínica.
-- Historia de Usuario: HU08 — Catálogo de Medicamentos
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

CREATE TABLE IF NOT EXISTS medicamentos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(255) NOT NULL,
    descripcion         TEXT,
    concentracion       VARCHAR(100),
    unidad_medida       VARCHAR(50),
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,

    UNIQUE (clinica_id, nombre)
);

COMMENT ON TABLE medicamentos IS 'Catálogo de medicamentos disponibles por clínica para prescripción en expedientes';
COMMENT ON COLUMN medicamentos.id IS 'Identificador único autogenerado por la base de datos';
COMMENT ON COLUMN medicamentos.clinica_id IS 'Clínica (tenant) a la que pertenece el medicamento';
COMMENT ON COLUMN medicamentos.concentracion IS 'Concentración del medicamento (ej: 500mg, 10mg/ml)';
COMMENT ON COLUMN medicamentos.unidad_medida IS 'Unidad de medida para dosificación (ej: mg, ml, gotas)';
COMMENT ON COLUMN medicamentos.activo IS 'Estado del registro. FALSE = desactivado, nunca eliminado';
COMMENT ON COLUMN medicamentos.fecha_creacion IS 'Fecha y hora UTC de creación del registro';
COMMENT ON COLUMN medicamentos.fecha_modificacion IS 'Fecha y hora UTC de última modificación';

CREATE INDEX IF NOT EXISTS idx_medicamentos_clinica_id ON medicamentos(clinica_id);
CREATE INDEX IF NOT EXISTS idx_medicamentos_clinica_activo ON medicamentos(clinica_id, activo);
CREATE INDEX IF NOT EXISTS idx_medicamentos_nombre ON medicamentos(clinica_id, nombre);

ALTER TABLE medicamentos ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_medicamentos" ON medicamentos
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_medicamentos" ON medicamentos
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON medicamentos TO authenticated;
