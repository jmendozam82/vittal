-- ============================================================================
-- Migración: Agregar campos de horario de atención a clínicas
-- Historia de Usuario: HU09 — Gestión de Clínicas (Fase B: Horarios)
-- Fecha: 2026-07-11
-- ============================================================================

-- Agregar columnas de horario de atención
ALTER TABLE public.clinicas
    ADD COLUMN IF NOT EXISTS horario_apertura  TIME,
    ADD COLUMN IF NOT EXISTS horario_cierre    TIME,
    ADD COLUMN IF NOT EXISTS dias_atencion     VARCHAR(100);

-- Comentarios (en español, obligatorio)
COMMENT ON COLUMN public.clinicas.horario_apertura IS 'Hora de apertura de la clínica (formato HH:mm). NULL = sin restricción.';
COMMENT ON COLUMN public.clinicas.horario_cierre IS 'Hora de cierre de la clínica (formato HH:mm). NULL = sin restricción.';
COMMENT ON COLUMN public.clinicas.dias_atencion IS 'Días de atención separados por coma. Ej: L,M,MI,J,V. NULL = sin restricción.';
