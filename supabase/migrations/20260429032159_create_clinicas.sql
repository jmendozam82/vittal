-- =============================================================================
-- Migración: create_clinicas
-- Descripción: Tabla raíz del sistema multi-tenant. Cada clínica es un tenant.
-- Historia de Usuario: HU09 — Gestión de Clínicas
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

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
COMMENT ON COLUMN clinicas.id IS 'Identificador único autogenerado por la base de datos';
COMMENT ON COLUMN clinicas.tiempo_espera_minutos IS 'Minutos máximos antes de que un paciente genere alerta de espera';
COMMENT ON COLUMN clinicas.bd_externa_1 IS 'Nombre de base de datos externa del sistema 1 relacionado';
COMMENT ON COLUMN clinicas.bd_externa_2 IS 'Nombre de base de datos externa del sistema 2 relacionado';
COMMENT ON COLUMN clinicas.activo IS 'Estado del registro. FALSE = desactivado, nunca eliminado';
COMMENT ON COLUMN clinicas.fecha_creacion IS 'Fecha y hora UTC de creación del registro';
COMMENT ON COLUMN clinicas.fecha_modificacion IS 'Fecha y hora UTC de última modificación';

CREATE INDEX IF NOT EXISTS idx_clinicas_activo ON clinicas(activo);

-- Clinicas NO tiene RLS por clinica_id (es la tabla raíz del tenant)
ALTER TABLE clinicas ENABLE ROW LEVEL SECURITY;

CREATE POLICY "service_role_full_access_clinicas" ON clinicas
    FOR ALL TO service_role USING (true) WITH CHECK (true);

CREATE POLICY "authenticated_read_own_clinica" ON clinicas
    FOR SELECT TO authenticated
    USING (id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

GRANT SELECT ON clinicas TO authenticated;
