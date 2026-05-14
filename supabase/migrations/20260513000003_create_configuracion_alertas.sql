-- =============================================================================
-- Migración: create_configuracion_alertas
-- Descripción: Configuración de alertas de tiempo de espera por clínica.
--              Relación 1:1 con clinica_id.
--              Define umbrales, sonido y frecuencia de revisión.
-- Historia de Usuario: HU23 — Alertas Configurables
-- Agente: @IngenieroDatos
-- Fecha: 2026-05-13
-- =============================================================================

CREATE TABLE IF NOT EXISTS configuracion_alertas (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id                  UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    tiempo_espera_maximo_minutos INTEGER NOT NULL DEFAULT 30,
    activo                      BOOLEAN NOT NULL DEFAULT true,
    notificacion_sonido         BOOLEAN NOT NULL DEFAULT false,
    intervalo_revision_segundos INTEGER NOT NULL DEFAULT 60,
    fecha_creacion              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion          TIMESTAMPTZ,
    creado_por                  UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por              UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

-- =============================================================================
-- Índices: UNIQUE en clinica_id para relación 1:1
-- =============================================================================
CREATE UNIQUE INDEX IF NOT EXISTS idx_config_alertas_clinica ON configuracion_alertas(clinica_id);
CREATE INDEX IF NOT EXISTS idx_config_alertas_activo ON configuracion_alertas(activo);

-- =============================================================================
-- Row Level Security (RLS)
-- =============================================================================
ALTER TABLE configuracion_alertas ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_config_alertas" ON configuracion_alertas
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_config_alertas" ON configuracion_alertas
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON configuracion_alertas TO authenticated;

-- =============================================================================
-- Comentarios
-- =============================================================================
COMMENT ON TABLE configuracion_alertas IS 'Configuración de alertas de tiempo de espera por clínica (HU23)';
COMMENT ON COLUMN configuracion_alertas.tiempo_espera_maximo_minutos IS 'Umbral en minutos para disparar alerta de espera (default: 30)';
COMMENT ON COLUMN configuracion_alertas.notificacion_sonido IS 'Reproducir sonido al recibir alerta';
COMMENT ON COLUMN configuracion_alertas.intervalo_revision_segundos IS 'Cada cuántos segundos revisar tiempos de espera (default: 60)';
COMMENT ON COLUMN configuracion_alertas.creado_por IS 'Usuario que creó la configuración';
COMMENT ON COLUMN configuracion_alertas.modificado_por IS 'Último usuario que modificó la configuración';
