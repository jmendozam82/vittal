-- =============================================================================
-- Migración: create_dashboard_config
-- Descripción: Configuración de widgets del dashboard por clínica.
--              Relación 1:1 con clinica_id.
--              Define qué gráficos y KPIs se muestran y el layout.
-- Historia de Usuario: HU23 — Dashboard
-- Agente: @IngenieroDatos
-- Fecha: 2026-05-13
-- =============================================================================

CREATE TABLE IF NOT EXISTS dashboard_config (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id                      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    mostrar_pacientes_del_dia       BOOLEAN NOT NULL DEFAULT true,
    mostrar_citas_pendientes        BOOLEAN NOT NULL DEFAULT true,
    mostrar_pacientes_en_espera     BOOLEAN NOT NULL DEFAULT true,
    mostrar_tiempo_promedio_espera  BOOLEAN NOT NULL DEFAULT true,
    mostrar_grafico_citas_por_hora  BOOLEAN NOT NULL DEFAULT true,
    mostrar_ultimas_alertas         BOOLEAN NOT NULL DEFAULT true,
    layout                          VARCHAR(20) NOT NULL DEFAULT 'default'
                                    CHECK (layout IN ('default', 'compact', 'expanded')),
    activo                          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion              TIMESTAMPTZ
);

-- =============================================================================
-- Índices: UNIQUE en clinica_id para relación 1:1
-- =============================================================================
CREATE UNIQUE INDEX IF NOT EXISTS idx_dashboard_config_clinica ON dashboard_config(clinica_id);

-- =============================================================================
-- Row Level Security (RLS)
-- =============================================================================
ALTER TABLE dashboard_config ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_dashboard_config" ON dashboard_config
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_dashboard_config" ON dashboard_config
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON dashboard_config TO authenticated;

-- =============================================================================
-- Comentarios
-- =============================================================================
COMMENT ON TABLE dashboard_config IS 'Configuración de widgets del dashboard por clínica (HU23)';
COMMENT ON COLUMN dashboard_config.mostrar_pacientes_del_dia IS 'Muestra el widget de pacientes del día';
COMMENT ON COLUMN dashboard_config.mostrar_citas_pendientes IS 'Muestra el widget de citas pendientes';
COMMENT ON COLUMN dashboard_config.mostrar_pacientes_en_espera IS 'Muestra el widget de pacientes en espera';
COMMENT ON COLUMN dashboard_config.mostrar_tiempo_promedio_espera IS 'Muestra el widget de tiempo promedio de espera';
COMMENT ON COLUMN dashboard_config.mostrar_grafico_citas_por_hora IS 'Muestra el gráfico de citas por hora';
COMMENT ON COLUMN dashboard_config.mostrar_ultimas_alertas IS 'Muestra el widget de últimas alertas';
COMMENT ON COLUMN dashboard_config.layout IS 'Layout del dashboard: default, compact, expanded';

-- =============================================================================
-- Seed: Insertar configuración por defecto para clínicas existentes
-- =============================================================================
INSERT INTO dashboard_config (clinica_id, activo, fecha_creacion)
SELECT id, true, NOW() FROM clinicas WHERE activo = true
ON CONFLICT (clinica_id) DO NOTHING;
