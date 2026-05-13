-- =============================================================================
-- Migración: create_antecedentes_paciente
-- Descripción: Valores de antecedentes médicos por paciente y sala.
--              Junction table: expediente × sala × tipo_antecedente → valor.
--              Los antecedentes son estables (no cambian en cada visita).
--              Un paciente tiene UN set de antecedentes por sala, que se
--              consulta en cada visita y se actualiza solo cuando algo cambia.
-- Historia de Usuario: HU-E05
-- Agente: @IngenieroDatos
-- Sprint: 3.5 — Especialidades por Sala
-- Fecha: 2026-05-12
-- Dependencias: create_expedientes (HU20), create_tipos_antecedente (HU-E03)
-- =============================================================================

CREATE TABLE IF NOT EXISTS antecedentes_paciente (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id              UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    expediente_id           UUID NOT NULL REFERENCES expedientes(id) ON DELETE RESTRICT,
    sala_id                 UUID NOT NULL REFERENCES salas(id) ON DELETE RESTRICT,
    tipo_antecedente_id     UUID NOT NULL REFERENCES tipos_antecedente(id) ON DELETE RESTRICT,
    valor                   TEXT NOT NULL,
                            -- 'true' / 'false' para boolean
                            -- '120' para numero
                            -- Texto libre para tipo_dato = 'texto'
                            -- La conversión es responsabilidad de la capa BLL
    fecha_actualizacion     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    actualizado_por         UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    activo                  BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion      TIMESTAMPTZ
);

COMMENT ON TABLE antecedentes_paciente IS
  'Valores de antecedentes médicos de un paciente por sala. '
  'Junction: expediente_id × sala_id × tipo_antecedente_id → valor. '
  'Los antecedentes son datos estables del paciente: se llenan en la primera '
  'consulta y se actualizan solo cuando algo cambia clínicamente. '
  'Si el mismo paciente va a dos salas distintas, tiene antecedentes independientes.';
COMMENT ON COLUMN antecedentes_paciente.valor IS
  'Valor en TEXT para soportar los tres tipos de dato sin columnas múltiples. '
  'boolean → ''true''/''false'' | numero → ''120.5'' | texto → texto libre. '
  'La conversión al tipo correcto la hace la BLL según tipos_antecedente.tipo_dato.';
COMMENT ON COLUMN antecedentes_paciente.sala_id IS
  'Sala en la que se registraron estos antecedentes (especialidad del contexto).';
COMMENT ON COLUMN antecedentes_paciente.fecha_actualizacion IS
  'Última vez que el valor fue modificado (distinto a fecha_creacion del registro).';

-- Índices
CREATE INDEX IF NOT EXISTS idx_ant_pac_clinica      ON antecedentes_paciente(clinica_id);
CREATE INDEX IF NOT EXISTS idx_ant_pac_expediente   ON antecedentes_paciente(expediente_id);
CREATE INDEX IF NOT EXISTS idx_ant_pac_sala         ON antecedentes_paciente(expediente_id, sala_id);

-- Unicidad: un valor por tipo de antecedente por paciente por sala
-- Garantiza que no existan duplicados — la lógica de upsert va en el Repository
CREATE UNIQUE INDEX IF NOT EXISTS uix_antecedentes_paciente
    ON antecedentes_paciente(expediente_id, sala_id, tipo_antecedente_id)
    WHERE activo = true;

-- RLS
ALTER TABLE antecedentes_paciente ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_ant_pac" ON antecedentes_paciente
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_ant_pac" ON antecedentes_paciente
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON antecedentes_paciente TO authenticated;
