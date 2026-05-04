-- =============================================================================
-- Migración: create_usuarios
-- Descripción: Usuarios del sistema, vinculados a Supabase Auth.
-- Historia de Usuario: HU04 — Gestión de Usuarios
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

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
    creado_por          UUID,
    modificado_por      UUID,

    UNIQUE (clinica_id, usuario),
    UNIQUE (clinica_id, email)
);

COMMENT ON TABLE usuarios IS 'Usuarios del sistema. Vinculados a Supabase Auth via auth_user_id';
COMMENT ON COLUMN usuarios.id IS 'Identificador único autogenerado por la base de datos';
COMMENT ON COLUMN usuarios.clinica_id IS 'Clínica (tenant) a la que pertenece el usuario';
COMMENT ON COLUMN usuarios.auth_user_id IS 'UUID del usuario en Supabase Auth (auth.users)';
COMMENT ON COLUMN usuarios.es_doctor IS 'Si true, aparece como opción en filtros de doctor en Cola de Espera y Agenda';
COMMENT ON COLUMN usuarios.activo IS 'Estado del registro. FALSE = desactivado, nunca eliminado';
COMMENT ON COLUMN usuarios.fecha_creacion IS 'Fecha y hora UTC de creación del registro';
COMMENT ON COLUMN usuarios.fecha_modificacion IS 'Fecha y hora UTC de última modificación';

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
