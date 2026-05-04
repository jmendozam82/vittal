-- =============================================================================
-- Migración: create_perfiles
-- Descripción: Perfiles de acceso del sistema por clínica.
-- Historia de Usuario: HU03 — Gestión de Perfiles
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

CREATE TABLE IF NOT EXISTS perfiles (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(100) NOT NULL,
    descripcion         TEXT,
    es_admin            BOOLEAN NOT NULL DEFAULT false,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,

    UNIQUE (clinica_id, nombre)
);

COMMENT ON TABLE perfiles IS 'Perfiles de acceso del sistema. Define el nivel de acceso de los usuarios';
COMMENT ON COLUMN perfiles.id IS 'Identificador único autogenerado por la base de datos';
COMMENT ON COLUMN perfiles.clinica_id IS 'Clínica (tenant) a la que pertenece el perfil';
COMMENT ON COLUMN perfiles.es_admin IS 'Si true, el usuario tiene acceso completo sin verificar permisos específicos';
COMMENT ON COLUMN perfiles.activo IS 'Estado del registro. FALSE = desactivado, nunca eliminado';
COMMENT ON COLUMN perfiles.fecha_creacion IS 'Fecha y hora UTC de creación del registro';
COMMENT ON COLUMN perfiles.fecha_modificacion IS 'Fecha y hora UTC de última modificación';

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
