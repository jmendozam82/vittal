-- =============================================================================
-- Migración: create_constancias
-- Descripción: Sub-módulo del expediente para emitir constancias médicas.
--              (reposo, incapacidad, constancia de atención, referencia a especialista)
--              Vinculadas al expediente y hoja de cita. Imprimibles con membrete.
-- Historia de Usuario: HU-E07
-- Agente: @IngenieroDatos
-- Sprint: 3.5 — Especialidades por Sala
-- Fecha: 2026-05-12
-- Dependencias: create_expedientes (HU20), create_hojas_cita (HU20)
-- =============================================================================

CREATE TABLE IF NOT EXISTS constancias (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    expediente_id       UUID NOT NULL REFERENCES expedientes(id) ON DELETE RESTRICT,
    hoja_cita_id        UUID REFERENCES hojas_cita(id) ON DELETE SET NULL,
                        -- Nullable: puede emitirse una constancia fuera del contexto de una cita
    doctor_id           UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    tipo_constancia     VARCHAR(50) NOT NULL
                        CHECK (tipo_constancia IN (
                            'reposo',
                            'incapacidad',
                            'constancia_atencion',
                            'referencia_especialista',
                            'otro'
                        )),
    contenido           TEXT NOT NULL,              -- Cuerpo del documento (puede incluir HTML básico para impresión)
    fecha_emision       DATE NOT NULL DEFAULT CURRENT_DATE,
    dias_reposo         INTEGER,                    -- Solo para tipo 'reposo' e 'incapacidad'
    especialista_referido VARCHAR(150),             -- Solo para tipo 'referencia_especialista'
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

COMMENT ON TABLE constancias IS
  'Constancias médicas emitidas desde el expediente del paciente. '
  'Tipos: reposo, incapacidad, constancia_atencion, referencia_especialista, otro. '
  'Se vinculan al expediente y opcionalmente a la hoja de cita actual. '
  'Imprimibles con template de membrete de la clínica (vista Print.cshtml independiente). '
  'Auditables y buscables en el historial del expediente.';
COMMENT ON COLUMN constancias.tipo_constancia IS
  'Tipo de documento: reposo=descanso médico | incapacidad=baja laboral | '
  'constancia_atencion=comprobante de visita | referencia_especialista=derivación | otro=libre';
COMMENT ON COLUMN constancias.contenido IS
  'Cuerpo del documento médico. Pre-llenado con datos del paciente y doctor. '
  'El doctor puede editar antes de emitir.';
COMMENT ON COLUMN constancias.dias_reposo IS
  'Número de días de reposo indicados (aplica para tipo reposo e incapacidad).';
COMMENT ON COLUMN constancias.especialista_referido IS
  'Nombre o especialidad del médico al que se refiere (aplica para referencia_especialista).';
COMMENT ON COLUMN constancias.hoja_cita_id IS
  'Hoja de cita en cuyo contexto se emite la constancia. NULL = emitida fuera de consulta.';
COMMENT ON COLUMN constancias.activo IS
  'FALSE = constancia anulada. Nunca se elimina — solo se desactiva.';

-- Índices
CREATE INDEX IF NOT EXISTS idx_constancias_clinica     ON constancias(clinica_id);
CREATE INDEX IF NOT EXISTS idx_constancias_expediente  ON constancias(expediente_id);
CREATE INDEX IF NOT EXISTS idx_constancias_doctor      ON constancias(clinica_id, doctor_id);
CREATE INDEX IF NOT EXISTS idx_constancias_fecha       ON constancias(clinica_id, fecha_emision DESC);
CREATE INDEX IF NOT EXISTS idx_constancias_tipo        ON constancias(clinica_id, tipo_constancia);

-- RLS
ALTER TABLE constancias ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_constancias" ON constancias
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_constancias" ON constancias
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON constancias TO authenticated;
