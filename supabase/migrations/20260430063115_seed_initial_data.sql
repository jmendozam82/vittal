-- =============================================================================
-- Migración: seed_initial_data
-- Descripción: Datos iniciales del sistema (clínica default, perfil admin, módulos).
-- Historia de Usuario: HU01 — Creación de la Base de Datos
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-30
-- =============================================================================

-- Datos semilla transaccionales: clínica + perfil admin
DO $$
DECLARE
    clinic_id UUID;
    admin_profile_id UUID;
BEGIN
    -- 1. Insertar clínica por defecto (si no existe ya)
    IF NOT EXISTS (SELECT 1 FROM public.clinicas WHERE email = 'admin@vittalclinic.com') THEN
        INSERT INTO public.clinicas (nombre, direccion, telefono, email)
        VALUES ('Vittal Clinic Central', 'Av. Principal 123', '555-0100', 'admin@vittalclinic.com')
        RETURNING id INTO clinic_id;

        -- 2. Insertar perfil administrador para la clínica
        INSERT INTO public.perfiles (clinica_id, nombre, descripcion, es_admin)
        VALUES (clinic_id, 'Super Administrador', 'Acceso total al sistema', true)
        RETURNING id INTO admin_profile_id;
    END IF;
END $$;
