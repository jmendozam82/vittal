-- =============================================================================
-- Migración: create_tipos_antecedente
-- Descripción: Catálogo de tipos de antecedentes médicos configurado por sala.
--              sala_id = discriminador de especialidad.
--              clinica_id = discriminador de tenant (RLS únicamente).
--              Endpoint especial: POST /api/tipos-antecedente/importar-plantilla/{salaId}/{plantillaId}
-- Historia de Usuario: HU-E03
-- Agente: @IngenieroDatos
-- Sprint: 3.5 — Especialidades por Sala
-- Fecha: 2026-05-12
-- Dependencias: create_plantillas_especialidad (HU-E02), create_salas, create_clinicas
-- =============================================================================

CREATE TABLE IF NOT EXISTS tipos_antecedente (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    sala_id             UUID NOT NULL REFERENCES salas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(150) NOT NULL,
    categoria           VARCHAR(50),
                        -- Valores sugeridos: 'sistemico','ocular','quirurgico',
                        -- 'familiar','alergias','respiratorio','cardiaco',
                        -- 'obstetrico','ginecologico','psiquiatrico','habitos','otro'
    tipo_dato           VARCHAR(20) NOT NULL DEFAULT 'boolean'
                        CHECK (tipo_dato IN ('boolean', 'texto', 'numero')),
    orden               INTEGER NOT NULL DEFAULT 0,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

COMMENT ON TABLE tipos_antecedente IS
  'Catálogo de tipos de antecedentes médicos por sala. '
  'sala_id = discriminador de especialidad (una sala = una especialidad). '
  'clinica_id = tenant isolation (RLS). '
  'El código nunca tiene antecedentes hardcodeados — todo es configurable.';
COMMENT ON COLUMN tipos_antecedente.sala_id IS
  'Sala que define la especialidad de estos antecedentes. '
  'Una clínica puede tener múltiples salas con distintas especialidades.';
COMMENT ON COLUMN tipos_antecedente.clinica_id IS
  'Clínica (tenant) dueña del catálogo. Solo para RLS — no es discriminador de especialidad.';
COMMENT ON COLUMN tipos_antecedente.tipo_dato IS
  'boolean = campo Sí/No | texto = campo de texto libre | numero = valor numérico';
COMMENT ON COLUMN tipos_antecedente.categoria IS
  'Agrupación visual en el formulario. Ej: sistemico, ocular, quirurgico, familiar';
COMMENT ON COLUMN tipos_antecedente.orden IS
  'Controla el orden de aparición en el formulario de antecedentes';

-- Índices
CREATE INDEX IF NOT EXISTS idx_tipos_antecedente_clinica ON tipos_antecedente(clinica_id);
CREATE INDEX IF NOT EXISTS idx_tipos_antecedente_sala    ON tipos_antecedente(sala_id);
CREATE INDEX IF NOT EXISTS idx_tipos_antecedente_activo  ON tipos_antecedente(clinica_id, sala_id, activo);

-- Unicidad: no duplicar nombres en la misma sala
CREATE UNIQUE INDEX IF NOT EXISTS uix_tipos_antecedente_nombre_sala
    ON tipos_antecedente(sala_id, nombre)
    WHERE activo = true;

-- RLS
ALTER TABLE tipos_antecedente ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_tipos_antecedente" ON tipos_antecedente
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_tipos_antecedente" ON tipos_antecedente
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON tipos_antecedente TO authenticated;
