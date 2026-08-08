-- =============================================================================
-- Migración: deshabilitar_perfil_medico_general
-- Descripción: Opción A — Unifica los médicos de Vittal Clinic Central en el
--              perfil "Doctor" y deshabilita el perfil "Medico General".
--              1) Reasigna juan.perez@vittal.com y maria.lopez@vittal.com al
--                 perfil Doctor (cd80154b-...), que ya tiene permisos limpios
--                 (agenda/cola/expedientes/línea de tiempo con CRUD, catálogos
--                 ocultos).
--              2) Pone en false los permisos del perfil Medico General para
--                 dejar consistencia (el perfil queda deshabilitado).
--              3) Deshabilita el perfil Medico General (activo = false).
--              La tabla `usuarios` se actualiza por perfil_id; NO se toca
--              Supabase Auth (auth.users).
-- Historia de Usuario: HU — Decisión de negocio: solo queda el perfil Doctor
-- Agente: @IngenieroDatos
-- Fecha: 2026-08-06
-- =============================================================================

-- 1) Reasignar usuarios del perfil Medico General → perfil Doctor (Central)
UPDATE public.usuarios
SET perfil_id = 'cd80154b-fd16-4e73-9f85-6db61349f0f9'  -- Perfil Doctor (Central)
WHERE perfil_id = 'e764cf43-d151-4a16-9526-ffa1ad0d2cf9'  -- Perfil Medico General (Central)
  AND clinica_id = '45b513be-a245-45b9-a29f-2cf25fafa5b6'; -- Clínica Central

-- 2) Desactivar los permisos del perfil Medico General (consistencia con perfil inactivo)
UPDATE permisos
SET puede_leer          = false,
    puede_crear         = false,
    puede_actualizar    = false,
    fecha_modificacion  = NOW()
WHERE perfil_id = 'e764cf43-d151-4a16-9526-ffa1ad0d2cf9';  -- Perfil Medico General

-- 3) Deshabilitar el perfil Medico General
UPDATE public.perfiles
SET activo = false,
    fecha_modificacion = NOW()
WHERE id = 'e764cf43-d151-4a16-9526-ffa1ad0d2cf9';  -- Perfil Medico General
