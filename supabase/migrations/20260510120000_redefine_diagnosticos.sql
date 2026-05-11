-- =============================================================================
-- Migración: redefine_diagnosticos
-- Descripción: Redefine la tabla diagnosticos como tabla de unión entre citas
--              y tipos de diagnóstico (HU14). Reemplaza la versión anterior que
--              era un catálogo con nombre/codigo_cie10.
-- Historia de Usuario: HU14 — Gestión de Diagnósticos
-- Agente: @IngenieroDatos
-- Fecha: 2026-05-10
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Eliminar la tabla diagnosticos anterior (catálogo con nombre/codigo_cie10)
--    No hay datos ni dependencias de otras tablas hacia esta.
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS public.diagnosticos CASCADE;

-- -----------------------------------------------------------------------------
-- 2. Crear la nueva tabla diagnosticos (unión cita ↔ tipo_diagnóstico)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.diagnosticos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    cita_id             UUID NOT NULL,                                                         -- FK a citas_medicas (aún no implementada)
    tipo_diagnostico_id UUID NOT NULL REFERENCES tipos_diagnostico(id) ON DELETE RESTRICT,
    descripcion         TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    UNIQUE (clinica_id, cita_id, tipo_diagnostico_id)
);

-- -----------------------------------------------------------------------------
-- 3. Comentarios de tabla y columnas
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.diagnosticos IS 'Diagnósticos asignados a una cita médica, clasificados por tipo de diagnóstico. Cada cita puede tener múltiples diagnósticos de diferentes tipos, pero solo uno por tipo.';
COMMENT ON COLUMN public.diagnosticos.cita_id IS 'Identificador de la cita médica. FK lógica a citas_medicas (pendiente de implementar la tabla)';
COMMENT ON COLUMN public.diagnosticos.tipo_diagnostico_id IS 'Tipo de diagnóstico (ej: Refractivo, Glaucoma). FK a tipos_diagnostico';
COMMENT ON COLUMN public.diagnosticos.descripcion IS 'Descripción detallada del diagnóstico en el contexto de esta cita';

-- -----------------------------------------------------------------------------
-- 4. Índices de rendimiento
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_diagnosticos_clinica_id    ON public.diagnosticos(clinica_id);
CREATE INDEX IF NOT EXISTS idx_diagnosticos_cita_id       ON public.diagnosticos(cita_id);
CREATE INDEX IF NOT EXISTS idx_diagnosticos_tipo_diag_id  ON public.diagnosticos(tipo_diagnostico_id);
CREATE INDEX IF NOT EXISTS idx_diagnosticos_activo        ON public.diagnosticos(activo);

-- -----------------------------------------------------------------------------
-- 5. RLS — Aislamiento multi-tenant por clínica
-- -----------------------------------------------------------------------------
ALTER TABLE public.diagnosticos ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_diagnosticos" ON public.diagnosticos
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_diagnosticos" ON public.diagnosticos
    FOR ALL TO service_role USING (true) WITH CHECK (true);

-- -----------------------------------------------------------------------------
-- 6. Permisos para roles autenticados
-- -----------------------------------------------------------------------------
GRANT SELECT, INSERT, UPDATE ON public.diagnosticos TO authenticated;
