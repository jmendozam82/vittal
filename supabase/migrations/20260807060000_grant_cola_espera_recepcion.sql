-- =============================================================================
-- Migración: grant_cola_espera_recepcion
-- Descripción: Otorga el módulo "cola_espera" al perfil Recepcionista
--              (Opción A — Cola de Espera restringida).
-- La recepcionista ve la cola del día y puede registrar "Llegó" (agendada → en_espera)
-- y "Cancelar" citas. NO aparece el avance clínico: Atender/Completar quedan solo
-- para el personal asistencial (guards en ColaEsperaController — si la llamada llega,
-- el backend devuelve 403).
-- Historia de Usuario: HU18 — Cola de Espera
-- Agente: @IngenieroDatos
-- Fecha: 2026-08-07
-- =============================================================================

-- Asignar modulo cola_espera a los perfiles Recepcionista (todas las clínicas)
-- 1) Si el registro de permiso ya existe (matriz creada al dar de alta el perfil):
--    activarlo con UPDATE.
UPDATE public.permisos pe
SET puede_leer = true, puede_crear = true, puede_actualizar = true, fecha_modificacion = now()
FROM public.perfiles p
JOIN public.modulos_sistema m ON m.clave = 'cola_espera'
WHERE pe.perfil_id = p.id AND pe.modulo_id = m.id
  AND lower(p.nombre) = 'recepcionista';

-- 2) Si NO existe (perfil creado sin matriz), insertarlo
INSERT INTO public.permisos (clinica_id, perfil_id, modulo_id, puede_leer, puede_crear, puede_actualizar, fecha_modificacion)
SELECT p.clinica_id, p.id, m.id, true, true, true, now()
FROM public.perfiles p
JOIN public.modulos_sistema m ON m.clave = 'cola_espera'
WHERE lower(p.nombre) = 'recepcionista'
  AND NOT EXISTS (
      SELECT 1 FROM public.permisos pe
      WHERE pe.perfil_id = p.id AND pe.modulo_id = m.id
  );