-- =============================================================================
-- Migración: create_alertas_espera
-- Descripción: Alertas de tiempo de espera excedido. Con Supabase Realtime.
-- Historia de Usuario: HU23 — Alertas Configurables
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

CREATE TABLE IF NOT EXISTS alertas_espera (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    cita_id             UUID NOT NULL REFERENCES citas(id) ON DELETE RESTRICT,
    paciente_id         UUID NOT NULL REFERENCES pacientes(id) ON DELETE RESTRICT,
    doctor_id           UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    sala_id             UUID REFERENCES salas(id) ON DELETE SET NULL,
    hora_cita           TIME NOT NULL,
    hora_llegada        TIME,
    minutos_espera      INTEGER NOT NULL,
    resuelta            BOOLEAN NOT NULL DEFAULT false,
    fecha_alerta        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_resolucion    TIMESTAMPTZ
);

COMMENT ON TABLE alertas_espera IS 'Alertas generadas cuando un paciente excede el tiempo de espera configurado en su clínica';
COMMENT ON COLUMN alertas_espera.id IS 'Identificador único autogenerado por la base de datos';
COMMENT ON COLUMN alertas_espera.clinica_id IS 'Clínica (tenant) a la que pertenece la alerta';
COMMENT ON COLUMN alertas_espera.minutos_espera IS 'Minutos que el paciente lleva esperando al momento de generar la alerta';
COMMENT ON COLUMN alertas_espera.resuelta IS 'Indica si la alerta fue atendida. TRUE = resuelta, no genera más notificaciones';
COMMENT ON COLUMN alertas_espera.fecha_resolucion IS 'Fecha y hora en que la alerta fue marcada como resuelta';

CREATE INDEX IF NOT EXISTS idx_alertas_clinica ON alertas_espera(clinica_id);
CREATE INDEX IF NOT EXISTS idx_alertas_cita ON alertas_espera(cita_id);
CREATE INDEX IF NOT EXISTS idx_alertas_doctor ON alertas_espera(clinica_id, doctor_id);
CREATE INDEX IF NOT EXISTS idx_alertas_resuelta ON alertas_espera(clinica_id, resuelta) WHERE resuelta = false;

ALTER TABLE alertas_espera ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_alertas_espera" ON alertas_espera
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_alertas_espera" ON alertas_espera
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON alertas_espera TO authenticated;

-- Habilitar Realtime para las tablas clave (Cola de Espera y Alertas)
ALTER PUBLICATION supabase_realtime ADD TABLE citas;
ALTER PUBLICATION supabase_realtime ADD TABLE alertas_espera;
