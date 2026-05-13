-- =============================================================================
-- Migración: create_signos_vitales_hoja
-- Descripción: Signos vitales registrados en cada consulta (hoja de cita).
--              A diferencia de los antecedentes (estables), los signos vitales
--              cambian en cada visita y permiten graficar evolución en el tiempo.
--              El flag fuera_de_rango se calcula automáticamente en trigger.
-- Historia de Usuario: HU-E06
-- Agente: @IngenieroDatos
-- Sprint: 3.5 — Especialidades por Sala
-- Fecha: 2026-05-12
-- Dependencias: create_hojas_cita (HU20), create_tipos_signo_vital (HU-E04)
-- =============================================================================

CREATE TABLE IF NOT EXISTS signos_vitales_hoja (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    hoja_cita_id        UUID NOT NULL REFERENCES hojas_cita(id) ON DELETE RESTRICT,
    sala_id             UUID NOT NULL REFERENCES salas(id) ON DELETE RESTRICT,
    tipo_signo_vital_id UUID NOT NULL REFERENCES tipos_signo_vital(id) ON DELETE RESTRICT,
    valor               NUMERIC(10,4) NOT NULL,
    unidad              VARCHAR(20),               -- Copia de tipos_signo_vital.unidad en el momento del registro
    fuera_de_rango      BOOLEAN NOT NULL DEFAULT false,
                        -- Calculado automáticamente por trigger fn_calcular_rango_sv
                        -- true = valor < valor_min OR valor > valor_max del catálogo
    fecha_hora          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    registrado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

COMMENT ON TABLE signos_vitales_hoja IS
  'Signos vitales registrados por consulta (hoja de cita). '
  'A diferencia de antecedentes (estables), los signos vitales cambian en cada visita. '
  'Permiten graficar evolución: TA de los últimos 12 meses, peso del niño en percentiles, PIO glaucomatosa. '
  'El flag fuera_de_rango es calculado automáticamente al insertar/actualizar.';
COMMENT ON COLUMN signos_vitales_hoja.valor IS
  'Valor numérico del signo vital en la consulta actual.';
COMMENT ON COLUMN signos_vitales_hoja.unidad IS
  'Copia de la unidad al momento del registro. Preserva el dato aunque el catálogo cambie.';
COMMENT ON COLUMN signos_vitales_hoja.fuera_de_rango IS
  'TRUE si valor < tipos_signo_vital.valor_min OR valor > tipos_signo_vital.valor_max. '
  'Calculado automáticamente por trigger. Alimenta alertas al doctor.';
COMMENT ON COLUMN signos_vitales_hoja.sala_id IS
  'Sala donde se realizó la medición (especialidad del contexto clínico).';

-- Índices
CREATE INDEX IF NOT EXISTS idx_sv_hoja_clinica         ON signos_vitales_hoja(clinica_id);
CREATE INDEX IF NOT EXISTS idx_sv_hoja_hoja_cita       ON signos_vitales_hoja(hoja_cita_id);
CREATE INDEX IF NOT EXISTS idx_sv_hoja_sala_tipo       ON signos_vitales_hoja(sala_id, tipo_signo_vital_id);
CREATE INDEX IF NOT EXISTS idx_sv_hoja_fuera_rango     ON signos_vitales_hoja(clinica_id, fuera_de_rango)
    WHERE fuera_de_rango = true;

-- Unicidad: un valor por tipo de signo vital por hoja de cita
CREATE UNIQUE INDEX IF NOT EXISTS uix_sv_hoja
    ON signos_vitales_hoja(hoja_cita_id, tipo_signo_vital_id)
    WHERE activo = true;

-- ---------------------------------------------------------------------------
-- TRIGGER: Calcular fuera_de_rango automáticamente al insertar o actualizar
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_calcular_rango_sv()
RETURNS TRIGGER AS $$
DECLARE
    v_min NUMERIC(10,4);
    v_max NUMERIC(10,4);
BEGIN
    -- Obtener rango normal del catálogo
    SELECT valor_min, valor_max
    INTO v_min, v_max
    FROM tipos_signo_vital
    WHERE id = NEW.tipo_signo_vital_id;

    -- Calcular fuera_de_rango solo si hay límites definidos
    IF v_min IS NOT NULL AND v_max IS NOT NULL THEN
        NEW.fuera_de_rango := (NEW.valor < v_min OR NEW.valor > v_max);
    ELSIF v_min IS NOT NULL THEN
        NEW.fuera_de_rango := (NEW.valor < v_min);
    ELSIF v_max IS NOT NULL THEN
        NEW.fuera_de_rango := (NEW.valor > v_max);
    ELSE
        NEW.fuera_de_rango := false;
    END IF;

    -- Copiar unidad del catálogo si no se proveyó
    IF NEW.unidad IS NULL THEN
        SELECT unidad INTO NEW.unidad
        FROM tipos_signo_vital WHERE id = NEW.tipo_signo_vital_id;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION fn_calcular_rango_sv() IS
  'Trigger que calcula automáticamente si un signo vital está fuera del rango normal. '
  'Compara el valor con valor_min y valor_max del catálogo tipos_signo_vital. '
  'También copia la unidad del catálogo si no fue provista explícitamente.';

CREATE OR REPLACE TRIGGER trg_calcular_rango_sv
    BEFORE INSERT OR UPDATE OF valor, tipo_signo_vital_id
    ON signos_vitales_hoja
    FOR EACH ROW
    EXECUTE FUNCTION fn_calcular_rango_sv();

-- RLS
ALTER TABLE signos_vitales_hoja ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_sv_hoja" ON signos_vitales_hoja
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_sv_hoja" ON signos_vitales_hoja
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON signos_vitales_hoja TO authenticated;
