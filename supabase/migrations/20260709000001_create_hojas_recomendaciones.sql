-- =============================================================================
-- Migración: Crear tabla public.hojas_recomendaciones
-- Tabla intermedia: relaciona recomendaciones del catálogo con hojas de cita.
-- Historia de Usuario: HU20 — Expedientes (Recomendaciones por hoja de cita)
-- Fecha: 2026-07-09
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Crear tabla hojas_recomendaciones
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.hojas_recomendaciones (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id        UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    recomendacion_id    UUID NOT NULL REFERENCES recomendaciones(id) ON DELETE RESTRICT,
    observaciones       TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

-- -----------------------------------------------------------------------------
-- 2. Comentarios de tabla y columnas (en español)
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.hojas_recomendaciones IS 'Recomendaciones registradas en una hoja de cita médica. Tabla intermedia entre hojas_cita y recomendaciones.';

COMMENT ON COLUMN public.hojas_recomendaciones.id IS 'Identificador único de la recomendación en la hoja de cita (UUID autogenerado)';
COMMENT ON COLUMN public.hojas_recomendaciones.clinica_id IS 'Identificador del tenant (clínica) al que pertenece el registro — discriminador multi-tenant obligatorio';
COMMENT ON COLUMN public.hojas_recomendaciones.hoja_cita_id IS 'Hoja de cita a la que pertenece la recomendación (FK -> hojas_cita)';
COMMENT ON COLUMN public.hojas_recomendaciones.recomendacion_id IS 'Recomendación del catálogo (FK -> recomendaciones)';
COMMENT ON COLUMN public.hojas_recomendaciones.observaciones IS 'Observaciones adicionales sobre la recomendación indicada';
COMMENT ON COLUMN public.hojas_recomendaciones.activo IS 'Indica si el registro está activo (true) o desactivado (false). Nunca se eliminan registros.';
COMMENT ON COLUMN public.hojas_recomendaciones.fecha_creacion IS 'Fecha y hora de creación del registro (UTC)';
COMMENT ON COLUMN public.hojas_recomendaciones.fecha_modificacion IS 'Fecha y hora de la última modificación (UTC), NULL si nunca se modificó';

-- -----------------------------------------------------------------------------
-- 3. Índices obligatorios
-- -----------------------------------------------------------------------------
CREATE INDEX idx_hojas_recomendaciones_clinica_id ON public.hojas_recomendaciones(clinica_id);
CREATE INDEX idx_hojas_recomendaciones_hoja_cita_id ON public.hojas_recomendaciones(hoja_cita_id);
CREATE INDEX idx_hojas_recomendaciones_recomendacion_id ON public.hojas_recomendaciones(recomendacion_id);
CREATE INDEX idx_hojas_recomendaciones_activo ON public.hojas_recomendaciones(activo);

-- -----------------------------------------------------------------------------
-- 4. Row Level Security (RLS) — obligatorio multi-tenant
-- -----------------------------------------------------------------------------
ALTER TABLE public.hojas_recomendaciones ENABLE ROW LEVEL SECURITY;

-- Política de aislamiento por clínica: cada usuario solo ve registros de su clínica
CREATE POLICY "clinica_isolation" ON public.hojas_recomendaciones
    FOR ALL
    USING (clinica_id = (current_setting('app.current_clinica_id', true))::UUID);

-- -----------------------------------------------------------------------------
-- 5. Restricción UNIQUE para evitar duplicados (misma recomendación en misma hoja)
-- -----------------------------------------------------------------------------
ALTER TABLE public.hojas_recomendaciones
    ADD CONSTRAINT uq_hojas_recomendaciones_hoja_recomendacion
    UNIQUE (hoja_cita_id, recomendacion_id);

COMMENT ON CONSTRAINT uq_hojas_recomendaciones_hoja_recomendacion ON public.hojas_recomendaciones
    IS 'Una misma recomendación no puede registrarse dos veces en la misma hoja de cita';
