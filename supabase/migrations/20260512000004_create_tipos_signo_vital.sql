-- =============================================================================
-- Migración: create_tipos_signo_vital
-- Descripción: Catálogo de tipos de signos vitales configurado por sala.
--              sala_id = discriminador de especialidad.
--              clinica_id = discriminador de tenant (RLS únicamente).
--              valor_min / valor_max definen el rango normal → base de alertas.
-- Historia de Usuario: HU-E04
-- Agente: @IngenieroDatos
-- Sprint: 3.5 — Especialidades por Sala
-- Fecha: 2026-05-12
-- Dependencias: create_plantillas_especialidad (HU-E02), create_salas, create_clinicas
-- =============================================================================

CREATE TABLE IF NOT EXISTS tipos_signo_vital (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    sala_id             UUID NOT NULL REFERENCES salas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(150) NOT NULL,
    unidad              VARCHAR(20),               -- 'mmHg', 'bpm', 'kg', 'm', 'cm', '%', '°C', etc.
    valor_min           NUMERIC(10,4),             -- Rango normal mínimo → alerta si valor < min
    valor_max           NUMERIC(10,4),             -- Rango normal máximo → alerta si valor > max
    orden               INTEGER NOT NULL DEFAULT 0,
    es_obligatorio      BOOLEAN NOT NULL DEFAULT false,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

COMMENT ON TABLE tipos_signo_vital IS
  'Catálogo de signos vitales por sala. Los signos vitales varían radicalmente '
  'por especialidad: Cardiología mide TA/FC, Oftalmología mide PIO/Agudeza Visual, '
  'Pediatría mide peso/talla con percentiles. sala_id = discriminador de especialidad. '
  'clinica_id = tenant isolation (RLS únicamente).';
COMMENT ON COLUMN tipos_signo_vital.sala_id IS
  'Sala que define qué signos vitales se miden. Discriminador de especialidad.';
COMMENT ON COLUMN tipos_signo_vital.clinica_id IS
  'Clínica (tenant). Solo para RLS — no es discriminador de especialidad.';
COMMENT ON COLUMN tipos_signo_vital.unidad IS
  'Unidad de medida del signo vital. Ej: mmHg, bpm, kg, °C, %, D (dioptrías)';
COMMENT ON COLUMN tipos_signo_vital.valor_min IS
  'Límite inferior del rango normal. Si el valor registrado es menor, se activa fuera_de_rango.';
COMMENT ON COLUMN tipos_signo_vital.valor_max IS
  'Límite superior del rango normal. Si el valor registrado es mayor, se activa fuera_de_rango.';
COMMENT ON COLUMN tipos_signo_vital.es_obligatorio IS
  'Si true, la hoja de cita no puede guardarse sin registrar este signo vital.';

-- Índices
CREATE INDEX IF NOT EXISTS idx_tipos_sv_clinica ON tipos_signo_vital(clinica_id);
CREATE INDEX IF NOT EXISTS idx_tipos_sv_sala    ON tipos_signo_vital(sala_id);
CREATE INDEX IF NOT EXISTS idx_tipos_sv_activo  ON tipos_signo_vital(clinica_id, sala_id, activo);

-- Unicidad: no duplicar nombres en la misma sala
CREATE UNIQUE INDEX IF NOT EXISTS uix_tipos_sv_nombre_sala
    ON tipos_signo_vital(sala_id, nombre)
    WHERE activo = true;

-- RLS
ALTER TABLE tipos_signo_vital ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_tipos_sv" ON tipos_signo_vital
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_tipos_sv" ON tipos_signo_vital
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON tipos_signo_vital TO authenticated;
