-- =============================================================================
-- Migración: alter_citas_add_hora_fin
-- Descripción: Agrega hora_fin a la tabla citas para calcular duración real
--              de consulta, habilitar agenda visual con bloques de tiempo y
--              alimentar reportes de productividad y la Línea de Tiempo (HU19).
-- Historia de Usuario: HU-E01
-- Agente: @IngenieroDatos
-- Sprint: 3.5 — Especialidades por Sala
-- Fecha: 2026-05-12
-- Dependencias: 20260429032414_create_citas.sql
-- =============================================================================

-- Campo nullable para no romper datos existentes.
-- Los registros anteriores quedan con hora_fin = NULL.
ALTER TABLE citas ADD COLUMN IF NOT EXISTS hora_fin TIME;

-- Comentario en español (estándar del proyecto)
COMMENT ON COLUMN citas.hora_fin IS
  'Hora de finalización de la consulta. Permite calcular duración real, '
  'evitar solapamientos en agenda visual y alimentar reportes de productividad. '
  'NULL = consulta en curso o dato no registrado.';

-- Índice compuesto útil para reportes de duración por doctor y fecha
CREATE INDEX IF NOT EXISTS idx_citas_duracion
    ON citas(clinica_id, doctor_id, fecha_cita, hora_cita, hora_fin)
    WHERE hora_fin IS NOT NULL;
