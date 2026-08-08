-- =============================================================================
-- Migración: doctor_perfil_ocultar_catalogos
-- Descripción: Oculta del sidebar del perfil "Doctor" los módulos de catálogo
--              que solo deben usarse internamente (hoja de cita) pero no
--              administrarse desde el perfil. Se revierten los permisos
--              (puede_leer, puede_crear, puede_actualizar = false) para:
--              cirugias, diagnosticos, examenes, medicamentos,
--              recomendaciones, tratamientos.
--              El Doctor conserva: agenda, cola_espera, expedientes,
--              dashboard, pacientes (lectura) y login.
-- Historia de Usuario: HU — Perfil Doctor: ocultar catálogos del sidebar
-- Agente: @IngenieroDatos
-- Fecha: 2026-08-06
-- =============================================================================

UPDATE permisos p
SET puede_leer          = false,
    puede_crear         = false,
    puede_actualizar    = false,
    fecha_modificacion  = NOW()
FROM modulos_sistema ms
WHERE ms.id = p.modulo_id
  AND p.perfil_id  = 'c9106459-1752-432b-bfd9-c173aefb3963'  -- Perfil Doctor
  AND p.clinica_id = '218c92ce-7604-49cb-b1ae-ec1af45cb8d6'  -- Clínica Managua
  AND ms.clave IN (
        'cirugias',
        'diagnosticos',
        'examenes',
        'medicamentos',
        'recomendaciones',
        'tratamientos'
  );
