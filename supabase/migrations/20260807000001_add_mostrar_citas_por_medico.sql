-- =============================================================================
-- Migración: add_mostrar_citas_por_medico
-- Descripción: Añade el widget "Citas por Médico" (gráfico de barras apiladas
--              segmentado por citas atendidas / pendientes) a la configuración
--              de widgets del dashboard por clínica.
-- Historia de Usuario: HU23 — Dashboard
-- Agente: @IngenieroDatos
-- Fecha: 2026-08-07
-- =============================================================================

ALTER TABLE dashboard_config
    ADD COLUMN IF NOT EXISTS mostrar_citas_por_medico BOOLEAN NOT NULL DEFAULT true;

-- =============================================================================
-- Comentarios
-- =============================================================================
COMMENT ON COLUMN dashboard_config.mostrar_citas_por_medico IS 'Muestra el gráfico apilado de citas por médico (atendidas/pendientes)';

-- =============================================================================
-- Seed: Habilitar el nuevo widget en clínicas existentes
-- =============================================================================
UPDATE dashboard_config
   SET mostrar_citas_por_medico = true,
       fecha_modificacion = NOW()
 WHERE activo = true
   AND mostrar_citas_por_medico IS NOT TRUE;