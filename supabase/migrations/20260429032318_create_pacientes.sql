-- =============================================================================
-- Migración: create_pacientes
-- Descripción: Registro de pacientes por clínica.
-- Historia de Usuario: HU07 — Gestión de Pacientes
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
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
    foto_url            TEXT,
    observaciones       TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

COMMENT ON TABLE pacientes IS 'Registro de pacientes por clínica. Los pacientes no se eliminan, solo se desactivan';
COMMENT ON COLUMN pacientes.id IS 'Identificador único autogenerado por la base de datos';
COMMENT ON COLUMN pacientes.clinica_id IS 'Clínica (tenant) a la que pertenece el paciente';
COMMENT ON COLUMN pacientes.doctor_id IS 'Doctor al que está asignado el paciente por defecto';
COMMENT ON COLUMN pacientes.foto_url IS 'URL pública de la foto del paciente almacenada en Supabase Storage bucket: avatares';
COMMENT ON COLUMN pacientes.activo IS 'Estado del registro. FALSE = desactivado, nunca eliminado';
COMMENT ON COLUMN pacientes.fecha_creacion IS 'Fecha y hora UTC de creación del registro';
COMMENT ON COLUMN pacientes.fecha_modificacion IS 'Fecha y hora UTC de última modificación';

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
