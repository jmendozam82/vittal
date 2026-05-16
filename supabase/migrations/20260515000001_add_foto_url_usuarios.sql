-- =============================================================================
-- Migración: add_foto_url_usuarios
-- Descripción: Agrega columna foto_url a la tabla usuarios para avatar/foto
-- Historia de Usuario: HU03 — Gestión de Perfiles (Mi Perfil)
-- Agente: @IngenieroDatos
-- Fecha: 2026-05-15
-- =============================================================================

-- Agregar columna foto_url para avatar del usuario
ALTER TABLE public.usuarios
ADD COLUMN foto_url TEXT;

COMMENT ON COLUMN public.usuarios.foto_url
    IS 'URL pública del avatar del usuario almacenado en Supabase Storage bucket: avatares';
