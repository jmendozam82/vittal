-- =============================================================================
-- Migración: 20260727000003_drop_public_select_storage_policies
-- Descripción: Corrección de advertencias "public_bucket_allows_listing" del linter.
--              Eliminar las políticas SELECT públicas de los buckets 'avatares' y 'landing'.
--              Los buckets al ser públicos sirven los archivos directamente a través
--              de sus URLs públicas sin requerir políticas SELECT de RLS,
--              y al eliminar las políticas SELECT se previene el listado no autorizado.
-- =============================================================================

DROP POLICY IF EXISTS "public_read_avatares" ON storage.objects;
DROP POLICY IF EXISTS "landing_public_read" ON storage.objects;
