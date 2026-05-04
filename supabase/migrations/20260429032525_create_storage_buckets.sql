-- =============================================================================
-- Migración: create_storage_buckets
-- Descripción: Buckets de Supabase Storage y sus políticas de acceso.
-- Historia de Usuario: HU20 — Gestión de Expedientes (archivos médicos)
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

-- Bucket para archivos de expedientes médicos (PRIVADO)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'expedientes',
    'expedientes',
    false,       -- PRIVADO — acceso solo con token temporal
    52428800,    -- 50MB límite por archivo
    ARRAY[
        'application/pdf',
        'image/jpeg',
        'image/png',
        'image/webp',
        'application/msword',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
    ]
)
ON CONFLICT (id) DO NOTHING;

-- Bucket para fotos de pacientes y usuarios (PÚBLICO)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'avatares',
    'avatares',
    true,        -- PÚBLICO — URL accesible directamente
    5242880,     -- 5MB límite
    ARRAY['image/jpeg', 'image/png', 'image/webp']
)
ON CONFLICT (id) DO NOTHING;

-- Política: usuarios autenticados pueden leer archivos de su clínica
CREATE POLICY "clinica_read_expedientes"
ON storage.objects FOR SELECT
TO authenticated
USING (
    bucket_id = 'expedientes'
    AND (storage.foldername(name))[1] = current_setting('app.current_clinica_id', true)
);

-- Política: usuarios autenticados pueden subir archivos a su clínica
CREATE POLICY "clinica_insert_expedientes"
ON storage.objects FOR INSERT
TO authenticated
WITH CHECK (
    bucket_id = 'expedientes'
    AND (storage.foldername(name))[1] = current_setting('app.current_clinica_id', true)
);

-- Política: avatares son de lectura pública
CREATE POLICY "public_read_avatares"
ON storage.objects FOR SELECT
TO public
USING (bucket_id = 'avatares');

-- Política: solo usuarios autenticados pueden subir avatares
CREATE POLICY "authenticated_insert_avatares"
ON storage.objects FOR INSERT
TO authenticated
WITH CHECK (bucket_id = 'avatares');
