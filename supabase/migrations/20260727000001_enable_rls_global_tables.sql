-- =============================================================================
-- Migración: 20260727000001_enable_rls_global_tables
-- Descripción: Habilitar Row Level Security (RLS) en tablas globales del sistema
--              que fueron detectadas sin RLS por el linter de Supabase:
--              - modulos_sistema
--              - plantillas_especialidad
--              - plantilla_items
-- =============================================================================

-- 1. Módulos del Sistema
ALTER TABLE public.modulos_sistema ENABLE ROW LEVEL SECURITY;

CREATE POLICY "modulos_sistema_read" ON public.modulos_sistema
    FOR SELECT USING (true);

-- 2. Plantillas de Especialidad
ALTER TABLE public.plantillas_especialidad ENABLE ROW LEVEL SECURITY;

CREATE POLICY "plantillas_especialidad_read" ON public.plantillas_especialidad
    FOR SELECT USING (true);

-- 3. Ítems de Plantilla
ALTER TABLE public.plantilla_items ENABLE ROW LEVEL SECURITY;

CREATE POLICY "plantilla_items_read" ON public.plantilla_items
    FOR SELECT USING (true);
