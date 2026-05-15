-- =============================================================================
-- Migración: add_super_admin
-- Descripción: Agrega el flag es_super_admin a la tabla usuarios para el rol
--              de Super Admin Global del sistema Vittal.
--              Los Super Admins pueden crear clínicas, asignar admins,
--              y ver datos de cualquier tenant.
-- Historia de Usuario: HU-SA01 — Super Admin Global
-- Agente: @IngenieroDatos
-- Sprint: Super Admin Global + Provisioning Multi-Clínica
-- Fecha: 2026-05-15
-- Dependencias: 20260429032243_create_usuarios.sql
-- =============================================================================

-- =============================================================================
-- 1. Agregar columna es_super_admin
-- =============================================================================
ALTER TABLE usuarios
    ADD COLUMN IF NOT EXISTS es_super_admin BOOLEAN NOT NULL DEFAULT false;

-- =============================================================================
-- 2. Seed: Marcar usuarios con perfil admin como Super Admin Global
-- =============================================================================
UPDATE usuarios u
SET es_super_admin = true
FROM perfiles p
WHERE u.perfil_id = p.id
  AND p.es_admin = true
  AND u.es_super_admin = false;

-- =============================================================================
-- 3. Comentarios (en español, estándar del proyecto)
-- =============================================================================
COMMENT ON COLUMN usuarios.es_super_admin IS
  'Indica si el usuario es Super Admin Global del sistema Vittal. '
  'Los Super Admins pueden crear clínicas, asignar admins de clínica, '
  'y ver datos de cualquier tenant. No están limitados por clinica_id. '
  'Solo el Super Admin puede acceder a endpoints de /api/admin/ y '
  '/api/clinicas/provisionar.';
