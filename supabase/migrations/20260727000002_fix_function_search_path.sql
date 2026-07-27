-- =============================================================================
-- Migración: 20260727000002_fix_function_search_path
-- Descripción: Corrección de advertencia de seguridad de Supabase linter.
--              Asignar search_path explícito a la función fn_calcular_rango_sv
--              para prevenir hijacking de búsqueda (mutable search_path).
-- =============================================================================

ALTER FUNCTION public.fn_calcular_rango_sv() SET search_path = public;
