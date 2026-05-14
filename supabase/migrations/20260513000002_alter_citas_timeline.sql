-- =============================================================================
-- Migración: alter_citas_timeline
-- Descripción: Agrega columnas de control de línea de tiempo a la tabla citas.
--              hora_fin_atencion: marca el fin de la atención médica.
--              linea_tiempo_activo_id: referencia al paso activo actual.
-- Historia de Usuario: HU19 — Línea de Tiempo, HU-E01 — hora_fin
-- Agente: @IngenieroDatos
-- Fecha: 2026-05-13
-- =============================================================================

ALTER TABLE citas
    ADD COLUMN IF NOT EXISTS hora_fin_atencion TIME,
    ADD COLUMN IF NOT EXISTS linea_tiempo_activo_id UUID REFERENCES linea_tiempo(id) ON DELETE SET NULL;

-- =============================================================================
-- Índices para las nuevas columnas
-- =============================================================================
CREATE INDEX IF NOT EXISTS idx_citas_linea_tiempo_activo ON citas(linea_tiempo_activo_id)
    WHERE linea_tiempo_activo_id IS NOT NULL;

-- =============================================================================
-- Comentarios
-- =============================================================================
COMMENT ON COLUMN citas.hora_fin_atencion IS 'Hora en que finalizó la atención médica (HU-E01)';
COMMENT ON COLUMN citas.linea_tiempo_activo_id IS 'ID del paso activo actual en la línea de tiempo (HU19)';
