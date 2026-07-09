-- =============================================================================
-- Migración: fix_diagnosticos_catalog
-- Descripción: Corrige la tabla diagnosticos que fue incorrectamente definida
--              como junction table (cita ↔ tipo_diagnóstico). La restaura como
--              catálogo (tipo medicamentos) con nombre, código CIE-10 y tipo.
-- Historia de Usuario: HU14 — Gestión de Diagnósticos
-- Issue: Hallazgo #1 — Plan de Pruebas Funcionales Fase 5
-- Agente: @IngenieroDatos
-- Fecha: 2026-07-07
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Eliminar la tabla junction anterior (CASCADE elimina FK hoja_diagnosticos)
--    No hay datos críticos — proyecto en desarrollo.
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS public.diagnosticos CASCADE;

-- -----------------------------------------------------------------------------
-- 2. Crear la nueva tabla diagnosticos como catálogo (patrón medicamentos)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.diagnosticos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(255) NOT NULL,
    codigo_cie10        VARCHAR(20),
    tipo_diagnostico_id UUID NOT NULL REFERENCES tipos_diagnostico(id) ON DELETE RESTRICT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,

    UNIQUE (clinica_id, nombre)
);

-- -----------------------------------------------------------------------------
-- 3. Re-crear FK en hoja_diagnosticos.diagnostico_id → diagnosticos(id)
--    (fue eliminada por el CASCADE del DROP)
-- -----------------------------------------------------------------------------
ALTER TABLE public.hoja_diagnosticos
    ADD CONSTRAINT fk_hoja_diagnosticos_diagnostico
    FOREIGN KEY (diagnostico_id) REFERENCES public.diagnosticos(id) ON DELETE RESTRICT;

-- -----------------------------------------------------------------------------
-- 4. Comentarios de tabla y columnas
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.diagnosticos IS 'Catálogo de diagnósticos por clínica, clasificados por tipo de diagnóstico. Cada diagnóstico tiene un nombre y código CIE-10 opcional.';
COMMENT ON COLUMN public.diagnosticos.nombre IS 'Nombre del diagnóstico (ej: Miopía, Glaucoma de ángulo abierto)';
COMMENT ON COLUMN public.diagnosticos.codigo_cie10 IS 'Código CIE-10 del diagnóstico (ej: H52.1, H40.1)';
COMMENT ON COLUMN public.diagnosticos.tipo_diagnostico_id IS 'FK al tipo de diagnóstico (Refractivo, Glaucoma, etc.)';
COMMENT ON COLUMN public.diagnosticos.activo IS 'Los diagnósticos no se eliminan, solo se desactivan';

-- -----------------------------------------------------------------------------
-- 5. Índices de rendimiento
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_diagnosticos_clinica_id        ON public.diagnosticos(clinica_id);
CREATE INDEX IF NOT EXISTS idx_diagnosticos_clinica_activo    ON public.diagnosticos(clinica_id, activo);
CREATE INDEX IF NOT EXISTS idx_diagnosticos_nombre            ON public.diagnosticos(clinica_id, nombre);
CREATE INDEX IF NOT EXISTS idx_diagnosticos_codigo_cie10      ON public.diagnosticos(codigo_cie10);
CREATE INDEX IF NOT EXISTS idx_diagnosticos_tipo_diagnostico  ON public.diagnosticos(tipo_diagnostico_id);

-- -----------------------------------------------------------------------------
-- 6. RLS — Aislamiento multi-tenant por clínica
-- -----------------------------------------------------------------------------
ALTER TABLE public.diagnosticos ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_diagnosticos" ON public.diagnosticos
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_diagnosticos" ON public.diagnosticos
    FOR ALL TO service_role USING (true) WITH CHECK (true);

-- -----------------------------------------------------------------------------
-- 7. Permisos para roles autenticados
-- -----------------------------------------------------------------------------
GRANT SELECT, INSERT, UPDATE ON public.diagnosticos TO authenticated;
