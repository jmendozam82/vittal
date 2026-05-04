-- =============================================================================
-- Migración: create_citas
-- Descripción: Citas médicas programadas. Estados del flujo de atención.
-- Historia de Usuario: HU21 — Agenda
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

CREATE TABLE IF NOT EXISTS citas (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    paciente_id         UUID NOT NULL REFERENCES pacientes(id) ON DELETE RESTRICT,
    doctor_id           UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    sala_id             UUID REFERENCES salas(id) ON DELETE SET NULL,
    fecha_cita          DATE NOT NULL,
    hora_cita           TIME NOT NULL,
    hora_llegada        TIME,
    lugar               VARCHAR(255),
    motivo              TEXT,
    estado              VARCHAR(20) NOT NULL DEFAULT 'agendada'
                        CHECK (estado IN ('agendada', 'en_espera', 'en_atencion', 'atendida', 'cancelada')),
    notas               TEXT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    modificado_por      UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

COMMENT ON TABLE citas IS 'Citas médicas programadas. Estados: agendada, en_espera, en_atencion, atendida, cancelada';
COMMENT ON COLUMN citas.id IS 'Identificador único autogenerado por la base de datos';
COMMENT ON COLUMN citas.clinica_id IS 'Clínica (tenant) a la que pertenece la cita';
COMMENT ON COLUMN citas.hora_llegada IS 'Hora en que el paciente llegó físicamente a la clínica';
COMMENT ON COLUMN citas.estado IS 'Estado del flujo de la cita: agendada → en_espera → en_atencion → atendida';
COMMENT ON COLUMN citas.activo IS 'Estado del registro. FALSE = desactivado, nunca eliminado';
COMMENT ON COLUMN citas.fecha_creacion IS 'Fecha y hora UTC de creación del registro';
COMMENT ON COLUMN citas.fecha_modificacion IS 'Fecha y hora UTC de última modificación';

CREATE INDEX IF NOT EXISTS idx_citas_clinica_id ON citas(clinica_id);
CREATE INDEX IF NOT EXISTS idx_citas_doctor_id ON citas(doctor_id);
CREATE INDEX IF NOT EXISTS idx_citas_paciente_id ON citas(paciente_id);
CREATE INDEX IF NOT EXISTS idx_citas_fecha ON citas(clinica_id, fecha_cita);
CREATE INDEX IF NOT EXISTS idx_citas_estado ON citas(clinica_id, estado);
-- Índice compuesto para Cola de Espera (consulta más frecuente del módulo)
CREATE INDEX IF NOT EXISTS idx_citas_cola_espera
    ON citas(clinica_id, doctor_id, fecha_cita, hora_cita)
    WHERE estado IN ('agendada', 'en_espera');

ALTER TABLE citas ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_citas" ON citas
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_citas" ON citas
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON citas TO authenticated;
