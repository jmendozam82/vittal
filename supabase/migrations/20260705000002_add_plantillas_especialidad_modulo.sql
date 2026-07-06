-- Migration: Add plantillas_especialidad module to modulos_sistema
-- Purpose: Restrict Plantillas de Especialidad to Super Admin only
-- Affects: HU-E02 — Super Admin gestiona plantillas, clinic admins no tienen acceso

-- 1. Add the module to modulos_sistema (idempotent)
INSERT INTO modulos_sistema (clave, nombre, descripcion)
VALUES ('plantillas_especialidad', 'Plantillas de Especialidad',
        'Gestión de plantillas de especialidades médicas — solo Super Admin')
ON CONFLICT (clave) DO NOTHING;

-- 2. Grant permission ONLY to users with es_super_admin = true
--    Super Admins bypass the permission check anyway (RequirePermissionAttribute line 41),
--    but this ensures consistency in case the logic changes.
--    Clinic Admins intentionally do NOT get this permission.
INSERT INTO permisos (clinica_id, perfil_id, modulo_id, puede_leer, puede_crear, puede_actualizar, fecha_modificacion, modificado_por)
SELECT
    u.clinica_id,
    u.perfil_id,
    m.id,
    true, true, true,
    NOW(),
    u.id
FROM usuarios u
CROSS JOIN modulos_sistema m
WHERE u.es_super_admin = true
  AND m.clave = 'plantillas_especialidad'
  AND u.activo = true
ON CONFLICT (clinica_id, perfil_id, modulo_id) DO NOTHING;

COMMENT ON TABLE modulos_sistema IS 'Catálogo de módulos del sistema — controla permisos por perfil';
COMMENT ON COLUMN modulos_sistema.clave IS 'Identificador único del módulo del sistema';
