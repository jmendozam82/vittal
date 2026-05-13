-- =============================================================================
-- Migración: create_plantillas_especialidad
-- Descripción: Plantillas globales del sistema para onboarding de salas.
--              Al crear una sala con una especialidad, el admin puede importar
--              la plantilla correspondiente como punto de partida configurable.
--              IMPORTANTE: Sin clinica_id — datos globales del sistema (no tenant).
--              Solo el Super Admin puede administrar estas tablas.
-- Historia de Usuario: HU-E02
-- Agente: @IngenieroDatos
-- Sprint: 3.5 — Especialidades por Sala
-- Fecha: 2026-05-12
-- Dependencias: Ninguna
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Tabla: plantillas_especialidad
-- Catálogo global de especialidades médicas del sistema.
-- Sin clinica_id — pertenece al sistema, no a ningún tenant.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS plantillas_especialidad (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre              VARCHAR(100) NOT NULL UNIQUE,
    descripcion         TEXT,
    icono               VARCHAR(50),                -- Nombre de icono Bootstrap/FontAwesome
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

COMMENT ON TABLE plantillas_especialidad IS
  'Catálogo global de especialidades médicas del sistema. '
  'Sin clinica_id — pertenece al sistema, no a ningún tenant. '
  'Solo el Super Admin puede administrarlas.';
COMMENT ON COLUMN plantillas_especialidad.nombre IS 'Nombre único de la especialidad médica';
COMMENT ON COLUMN plantillas_especialidad.icono IS 'Clase CSS del icono representativo (Bootstrap Icons)';

-- ---------------------------------------------------------------------------
-- Tabla: plantilla_items
-- Ítems (antecedentes y signos vitales) asociados a cada plantilla.
-- Sin clinica_id — igual que la tabla padre.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS plantilla_items (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    plantilla_id        UUID NOT NULL REFERENCES plantillas_especialidad(id) ON DELETE CASCADE,
    tipo_item           VARCHAR(20) NOT NULL
                        CHECK (tipo_item IN ('antecedente', 'signo_vital')),
    nombre              VARCHAR(150) NOT NULL,
    categoria           VARCHAR(50),                -- 'sistemico','ocular','quirurgico','familiar','alergias','otro'
    tipo_dato           VARCHAR(20) NOT NULL DEFAULT 'boolean'
                        CHECK (tipo_dato IN ('boolean', 'texto', 'numero')),
    unidad              VARCHAR(20),                -- Solo para signo_vital: 'mmHg', 'kg', 'bpm', etc.
    valor_min           NUMERIC(10,4),              -- Rango normal mínimo (para alertas)
    valor_max           NUMERIC(10,4),              -- Rango normal máximo (para alertas)
    es_obligatorio      BOOLEAN NOT NULL DEFAULT false,
    orden               INTEGER NOT NULL DEFAULT 0,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE plantilla_items IS
  'Ítems de cada plantilla de especialidad: antecedentes y signos vitales predefinidos. '
  'Al importar una plantilla a una sala, estos ítems generan registros en '
  'tipos_antecedente y tipos_signo_vital para esa sala.';
COMMENT ON COLUMN plantilla_items.tipo_item IS 'antecedente = tipos_antecedente | signo_vital = tipos_signo_vital';
COMMENT ON COLUMN plantilla_items.tipo_dato IS 'boolean = Sí/No | texto = Campo libre | numero = Valor numérico';
COMMENT ON COLUMN plantilla_items.valor_min IS 'Valor mínimo normal del signo vital (para alertas de rango)';
COMMENT ON COLUMN plantilla_items.valor_max IS 'Valor máximo normal del signo vital (para alertas de rango)';

CREATE INDEX IF NOT EXISTS idx_plantilla_items_plantilla ON plantilla_items(plantilla_id);
CREATE INDEX IF NOT EXISTS idx_plantilla_items_tipo ON plantilla_items(plantilla_id, tipo_item);

-- ---------------------------------------------------------------------------
-- SEED: 8 especialidades médicas iniciales
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    v_medicina_general  UUID;
    v_oftalmologia      UUID;
    v_cardiologia       UUID;
    v_pediatria         UUID;
    v_dermatologia      UUID;
    v_traumatologia     UUID;
    v_ginecologia       UUID;
    v_psiquiatria       UUID;
BEGIN
    INSERT INTO plantillas_especialidad (nombre, descripcion, icono) VALUES
        ('Medicina General',  'Consulta médica general y atención primaria',           'bi-heart-pulse'),
        ('Oftalmología',      'Especialidad en enfermedades y cirugía ocular',         'bi-eye'),
        ('Cardiología',       'Especialidad cardiovascular',                            'bi-activity'),
        ('Pediatría',         'Atención médica a niños y adolescentes',                'bi-person-hearts'),
        ('Dermatología',      'Especialidad de piel, cabello y uñas',                  'bi-bandaid'),
        ('Traumatología',     'Especialidad en huesos, articulaciones y músculos',     'bi-clipboard2-pulse'),
        ('Ginecología',       'Salud reproductiva femenina',                           'bi-gender-female'),
        ('Psiquiatría',       'Especialidad en salud mental y trastornos psiquiátricos','bi-brain')
    RETURNING id INTO v_medicina_general;

    -- Re-fetch IDs por nombre para claridad
    SELECT id INTO v_medicina_general  FROM plantillas_especialidad WHERE nombre = 'Medicina General';
    SELECT id INTO v_oftalmologia      FROM plantillas_especialidad WHERE nombre = 'Oftalmología';
    SELECT id INTO v_cardiologia       FROM plantillas_especialidad WHERE nombre = 'Cardiología';
    SELECT id INTO v_pediatria         FROM plantillas_especialidad WHERE nombre = 'Pediatría';
    SELECT id INTO v_dermatologia      FROM plantillas_especialidad WHERE nombre = 'Dermatología';
    SELECT id INTO v_traumatologia     FROM plantillas_especialidad WHERE nombre = 'Traumatología';
    SELECT id INTO v_ginecologia       FROM plantillas_especialidad WHERE nombre = 'Ginecología';
    SELECT id INTO v_psiquiatria       FROM plantillas_especialidad WHERE nombre = 'Psiquiatría';

    -- -----------------------------------------------------------------------
    -- MEDICINA GENERAL — Antecedentes
    -- -----------------------------------------------------------------------
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, orden) VALUES
        (v_medicina_general, 'antecedente', 'Diabetes Mellitus',         'sistemico',   'boolean', 1),
        (v_medicina_general, 'antecedente', 'Hipertensión Arterial',     'sistemico',   'boolean', 2),
        (v_medicina_general, 'antecedente', 'Asma',                      'respiratorio','boolean', 3),
        (v_medicina_general, 'antecedente', 'Alergias medicamentosas',   'alergias',    'texto',   4),
        (v_medicina_general, 'antecedente', 'Cirugías previas',          'quirurgico',  'texto',   5),
        (v_medicina_general, 'antecedente', 'Antecedentes familiares',   'familiar',    'texto',   6);
    -- MEDICINA GENERAL — Signos Vitales
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, unidad, valor_min, valor_max, es_obligatorio, orden) VALUES
        (v_medicina_general, 'signo_vital', 'Tensión Arterial Sistólica',  NULL, 'numero', 'mmHg', 90,  140, true,  1),
        (v_medicina_general, 'signo_vital', 'Tensión Arterial Diastólica', NULL, 'numero', 'mmHg', 60,  90,  true,  2),
        (v_medicina_general, 'signo_vital', 'Frecuencia Cardíaca',         NULL, 'numero', 'bpm',  60,  100, true,  3),
        (v_medicina_general, 'signo_vital', 'Temperatura',                 NULL, 'numero', '°C',   36,  37.5,false, 4),
        (v_medicina_general, 'signo_vital', 'Saturación de Oxígeno',       NULL, 'numero', '%',    95,  100, false, 5),
        (v_medicina_general, 'signo_vital', 'Peso',                        NULL, 'numero', 'kg',   NULL,NULL,false, 6),
        (v_medicina_general, 'signo_vital', 'Talla',                       NULL, 'numero', 'm',    NULL,NULL,false, 7);

    -- -----------------------------------------------------------------------
    -- OFTALMOLOGÍA — Antecedentes
    -- -----------------------------------------------------------------------
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, orden) VALUES
        (v_oftalmologia, 'antecedente', 'Glaucoma',                    'ocular',    'boolean', 1),
        (v_oftalmologia, 'antecedente', 'Miopía',                      'ocular',    'boolean', 2),
        (v_oftalmologia, 'antecedente', 'Hipermetropía',               'ocular',    'boolean', 3),
        (v_oftalmologia, 'antecedente', 'Astigmatismo',                'ocular',    'boolean', 4),
        (v_oftalmologia, 'antecedente', 'Cirugía ocular previa',       'quirurgico','texto',   5),
        (v_oftalmologia, 'antecedente', 'Uso de lentes de contacto',   'ocular',    'boolean', 6),
        (v_oftalmologia, 'antecedente', 'Cataratas',                   'ocular',    'boolean', 7),
        (v_oftalmologia, 'antecedente', 'Retina (desprendimiento/alt.)','ocular',   'boolean', 8),
        (v_oftalmologia, 'antecedente', 'Diabetes (ocular)',           'sistemico', 'boolean', 9),
        (v_oftalmologia, 'antecedente', 'Alergias oculares',           'alergias',  'texto',   10);
    -- OFTALMOLOGÍA — Signos Vitales
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, unidad, valor_min, valor_max, es_obligatorio, orden) VALUES
        (v_oftalmologia, 'signo_vital', 'Agudeza Visual OD (sin corrección)', NULL, 'texto', NULL, NULL, NULL, true,  1),
        (v_oftalmologia, 'signo_vital', 'Agudeza Visual OI (sin corrección)', NULL, 'texto', NULL, NULL, NULL, true,  2),
        (v_oftalmologia, 'signo_vital', 'Agudeza Visual OD (con corrección)', NULL, 'texto', NULL, NULL, NULL, false, 3),
        (v_oftalmologia, 'signo_vital', 'Agudeza Visual OI (con corrección)', NULL, 'texto', NULL, NULL, NULL, false, 4),
        (v_oftalmologia, 'signo_vital', 'PIO OD (Presión Intraocular)',        NULL, 'numero', 'mmHg', 10, 21, true,  5),
        (v_oftalmologia, 'signo_vital', 'PIO OI (Presión Intraocular)',        NULL, 'numero', 'mmHg', 10, 21, true,  6),
        (v_oftalmologia, 'signo_vital', 'Refracción OD (Esfera)',              NULL, 'numero', 'D',  NULL, NULL,false, 7),
        (v_oftalmologia, 'signo_vital', 'Refracción OI (Esfera)',              NULL, 'numero', 'D',  NULL, NULL,false, 8);

    -- -----------------------------------------------------------------------
    -- CARDIOLOGÍA — Antecedentes
    -- -----------------------------------------------------------------------
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, orden) VALUES
        (v_cardiologia, 'antecedente', 'Hipertensión Arterial',         'sistemico', 'boolean', 1),
        (v_cardiologia, 'antecedente', 'Diabetes Mellitus',             'sistemico', 'boolean', 2),
        (v_cardiologia, 'antecedente', 'Tabaquismo',                    'habitos',   'boolean', 3),
        (v_cardiologia, 'antecedente', 'Infarto Agudo de Miocardio',    'cardiaco',  'boolean', 4),
        (v_cardiologia, 'antecedente', 'Arritmia previa',               'cardiaco',  'boolean', 5),
        (v_cardiologia, 'antecedente', 'Insuficiencia Cardíaca',        'cardiaco',  'boolean', 6),
        (v_cardiologia, 'antecedente', 'Dislipidemia',                  'sistemico', 'boolean', 7),
        (v_cardiologia, 'antecedente', 'Antecedentes familiares cardíacos','familiar','texto',  8);
    -- CARDIOLOGÍA — Signos Vitales
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, unidad, valor_min, valor_max, es_obligatorio, orden) VALUES
        (v_cardiologia, 'signo_vital', 'Tensión Arterial Sistólica',  NULL, 'numero', 'mmHg', 90,  140, true,  1),
        (v_cardiologia, 'signo_vital', 'Tensión Arterial Diastólica', NULL, 'numero', 'mmHg', 60,  90,  true,  2),
        (v_cardiologia, 'signo_vital', 'Frecuencia Cardíaca',         NULL, 'numero', 'bpm',  60,  100, true,  3),
        (v_cardiologia, 'signo_vital', 'Saturación de Oxígeno',       NULL, 'numero', '%',    95,  100, true,  4),
        (v_cardiologia, 'signo_vital', 'Peso',                        NULL, 'numero', 'kg',   NULL,NULL, false, 5),
        (v_cardiologia, 'signo_vital', 'IMC',                         NULL, 'numero', 'kg/m²',NULL,NULL, false, 6);

    -- -----------------------------------------------------------------------
    -- PEDIATRÍA — Antecedentes
    -- -----------------------------------------------------------------------
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, orden) VALUES
        (v_pediatria, 'antecedente', 'Prematuridad',                  'perinatal', 'boolean', 1),
        (v_pediatria, 'antecedente', 'Semanas de gestación al nacer', 'perinatal', 'numero',  2),
        (v_pediatria, 'antecedente', 'Vacunas al día',                'inmunologico','boolean',3),
        (v_pediatria, 'antecedente', 'Vacunas pendientes',            'inmunologico','texto',  4),
        (v_pediatria, 'antecedente', 'Alergias',                      'alergias',  'texto',   5),
        (v_pediatria, 'antecedente', 'Lactancia materna',             'perinatal', 'boolean', 6),
        (v_pediatria, 'antecedente', 'Hospitalizaciones previas',     'quirurgico','texto',   7);
    -- PEDIATRÍA — Signos Vitales
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, unidad, valor_min, valor_max, es_obligatorio, orden) VALUES
        (v_pediatria, 'signo_vital', 'Peso',                  NULL, 'numero', 'kg',   NULL, NULL, true,  1),
        (v_pediatria, 'signo_vital', 'Talla',                 NULL, 'numero', 'cm',   NULL, NULL, true,  2),
        (v_pediatria, 'signo_vital', 'Temperatura',           NULL, 'numero', '°C',   36,   37.5, true,  3),
        (v_pediatria, 'signo_vital', 'Frecuencia Cardíaca',   NULL, 'numero', 'bpm',  NULL, NULL, true,  4),
        (v_pediatria, 'signo_vital', 'Frecuencia Respiratoria',NULL,'numero', 'rpm',  NULL, NULL, false, 5),
        (v_pediatria, 'signo_vital', 'Saturación de Oxígeno', NULL, 'numero', '%',    95,   100,  false, 6),
        (v_pediatria, 'signo_vital', 'Perímetro Cefálico',    NULL, 'numero', 'cm',   NULL, NULL, false, 7);

    -- -----------------------------------------------------------------------
    -- DERMATOLOGÍA — Antecedentes básicos
    -- -----------------------------------------------------------------------
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, orden) VALUES
        (v_dermatologia, 'antecedente', 'Psoriasis',              'dermatologico','boolean', 1),
        (v_dermatologia, 'antecedente', 'Dermatitis atópica',     'dermatologico','boolean', 2),
        (v_dermatologia, 'antecedente', 'Rosácea',                'dermatologico','boolean', 3),
        (v_dermatologia, 'antecedente', 'Alergias cutáneas',      'alergias',     'texto',   4),
        (v_dermatologia, 'antecedente', 'Exposición solar crónica','habitos',     'boolean', 5),
        (v_dermatologia, 'antecedente', 'Lesiones malignas previas','quirurgico', 'texto',   6);

    -- -----------------------------------------------------------------------
    -- TRAUMATOLOGÍA — Antecedentes básicos
    -- -----------------------------------------------------------------------
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, orden) VALUES
        (v_traumatologia, 'antecedente', 'Fracturas previas',        'traumatologico','texto',   1),
        (v_traumatologia, 'antecedente', 'Cirugías ortopédicas',     'quirurgico',    'texto',   2),
        (v_traumatologia, 'antecedente', 'Osteoporosis',             'sistemico',     'boolean', 3),
        (v_traumatologia, 'antecedente', 'Artritis / Artrosis',      'inflamatorio',  'boolean', 4),
        (v_traumatologia, 'antecedente', 'Uso de implantes',         'quirurgico',    'boolean', 5),
        (v_traumatologia, 'antecedente', 'Actividad física habitual','habitos',       'texto',   6);
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, unidad, valor_min, valor_max, es_obligatorio, orden) VALUES
        (v_traumatologia, 'signo_vital', 'Tensión Arterial Sistólica',  NULL, 'numero', 'mmHg', 90, 140, false, 1),
        (v_traumatologia, 'signo_vital', 'Frecuencia Cardíaca',         NULL, 'numero', 'bpm',  60, 100, false, 2),
        (v_traumatologia, 'signo_vital', 'Peso',                        NULL, 'numero', 'kg',  NULL,NULL, false, 3);

    -- -----------------------------------------------------------------------
    -- GINECOLOGÍA — Antecedentes básicos
    -- -----------------------------------------------------------------------
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, orden) VALUES
        (v_ginecologia, 'antecedente', 'Gestas (número de embarazos)', 'obstetrico', 'numero', 1),
        (v_ginecologia, 'antecedente', 'Partos / Cesáreas',           'obstetrico', 'texto',  2),
        (v_ginecologia, 'antecedente', 'Abortos',                      'obstetrico', 'numero', 3),
        (v_ginecologia, 'antecedente', 'Fecha última menstruación',    'ginecologico','texto', 4),
        (v_ginecologia, 'antecedente', 'Anticonceptivos actuales',     'ginecologico','texto', 5),
        (v_ginecologia, 'antecedente', 'Patología uterina previa',     'ginecologico','texto', 6),
        (v_ginecologia, 'antecedente', 'Menopausia',                   'ginecologico','boolean',7);

    -- -----------------------------------------------------------------------
    -- PSIQUIATRÍA — Antecedentes básicos
    -- -----------------------------------------------------------------------
    INSERT INTO plantilla_items (plantilla_id, tipo_item, nombre, categoria, tipo_dato, orden) VALUES
        (v_psiquiatria, 'antecedente', 'Diagnóstico psiquiátrico previo','psiquiatrico','texto',  1),
        (v_psiquiatria, 'antecedente', 'Hospitalizaciones psiquiátricas','psiquiatrico','boolean',2),
        (v_psiquiatria, 'antecedente', 'Intentos de autolesión',         'psiquiatrico','boolean',3),
        (v_psiquiatria, 'antecedente', 'Medicación psiquiátrica actual', 'psiquiatrico','texto',  4),
        (v_psiquiatria, 'antecedente', 'Antecedentes familiares psiquiátricos','familiar','texto',5),
        (v_psiquiatria, 'antecedente', 'Consumo de sustancias',          'habitos',     'texto',  6);

END $$;
