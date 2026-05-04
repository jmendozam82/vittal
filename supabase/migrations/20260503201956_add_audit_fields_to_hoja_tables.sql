-- =============================================================================
-- Migración: add_audit_fields_to_hoja_tables
-- Descripción: Agrega columnas activo y fecha_modificacion a tablas hoja_*
--              si no existen (corrección de migración previa).
-- Historia de Usuario: HU20 — Gestión de Expedientes
-- Agente: @IngenieroDatos
-- Fecha: 2026-05-03
-- =============================================================================

-- hoja_diagnosticos
ALTER TABLE hoja_diagnosticos ADD COLUMN IF NOT EXISTS activo BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE hoja_diagnosticos ADD COLUMN IF NOT EXISTS fecha_modificacion TIMESTAMPTZ;

-- hoja_tratamientos
ALTER TABLE hoja_tratamientos ADD COLUMN IF NOT EXISTS activo BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE hoja_tratamientos ADD COLUMN IF NOT EXISTS fecha_modificacion TIMESTAMPTZ;

-- hoja_cirugias
ALTER TABLE hoja_cirugias ADD COLUMN IF NOT EXISTS activo BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE hoja_cirugias ADD COLUMN IF NOT EXISTS fecha_modificacion TIMESTAMPTZ;

-- hoja_examenes
ALTER TABLE hoja_examenes ADD COLUMN IF NOT EXISTS activo BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE hoja_examenes ADD COLUMN IF NOT EXISTS fecha_modificacion TIMESTAMPTZ;

-- Índices para los nuevos campos
CREATE INDEX IF NOT EXISTS idx_hoja_diagnosticos_activo ON hoja_diagnosticos(activo);
CREATE INDEX IF NOT EXISTS idx_hoja_tratamientos_activo ON hoja_tratamientos(activo);
CREATE INDEX IF NOT EXISTS idx_hoja_cirugias_activo ON hoja_cirugias(activo);
CREATE INDEX IF NOT EXISTS idx_hoja_examenes_activo ON hoja_examenes(activo);
