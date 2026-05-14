-- =============================================================================
-- Migración: create_reportes
-- Descripción: Tablas para reportes generados en el sistema.
--              reportes: almacena los reportes generados con su contenido JSON.
--              reporte_parametros: guarda los filtros usados al generar.
-- Historia de Usuario: HU22 — Reportes
-- Agente: @IngenieroDatos
-- Fecha: 2026-05-13
-- =============================================================================

CREATE TABLE IF NOT EXISTS reportes (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    nombre              VARCHAR(200) NOT NULL,
    tipo                VARCHAR(50) NOT NULL,
    descripcion         TEXT,
    formato             VARCHAR(10) NOT NULL DEFAULT 'json'
                        CHECK (formato IN ('json', 'csv', 'pdf')),
    contenido_json      JSONB NOT NULL DEFAULT '[]',
    fecha_inicio        DATE NOT NULL,
    fecha_fin           DATE NOT NULL,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    creado_por          UUID REFERENCES usuarios(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS reporte_parametros (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reporte_id          UUID NOT NULL REFERENCES reportes(id) ON DELETE CASCADE,
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    clave               VARCHAR(100) NOT NULL,
    valor               TEXT NOT NULL,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- Índices de rendimiento
-- =============================================================================
CREATE INDEX IF NOT EXISTS idx_reportes_clinica ON reportes(clinica_id, fecha_creacion DESC);
CREATE INDEX IF NOT EXISTS idx_reportes_tipo ON reportes(clinica_id, tipo);
CREATE INDEX IF NOT EXISTS idx_reporte_parametros_reporte ON reporte_parametros(reporte_id);
CREATE INDEX IF NOT EXISTS idx_reporte_parametros_clinica ON reporte_parametros(clinica_id);

-- =============================================================================
-- Row Level Security (RLS) — reportes
-- =============================================================================
ALTER TABLE reportes ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_reportes" ON reportes
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_reportes" ON reportes
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON reportes TO authenticated;

-- =============================================================================
-- Row Level Security (RLS) — reporte_parametros
-- =============================================================================
ALTER TABLE reporte_parametros ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_reporte_parametros" ON reporte_parametros
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_reporte_parametros" ON reporte_parametros
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON reporte_parametros TO authenticated;

-- =============================================================================
-- Comentarios
-- =============================================================================
COMMENT ON TABLE reportes IS 'Reportes generados en el sistema (HU22)';
COMMENT ON COLUMN reportes.nombre IS 'Nombre descriptivo del reporte';
COMMENT ON COLUMN reportes.tipo IS 'Tipo: pacientes_por_dia, citas_por_estado, doctores_mas_activos, tiempo_promedio_espera';
COMMENT ON COLUMN reportes.formato IS 'Formato de exportación: json, csv, pdf';
COMMENT ON COLUMN reportes.contenido_json IS 'Datos del reporte en formato JSON';
COMMENT ON COLUMN reportes.fecha_inicio IS 'Fecha inicio del rango del reporte';
COMMENT ON COLUMN reportes.fecha_fin IS 'Fecha fin del rango del reporte';

COMMENT ON TABLE reporte_parametros IS 'Parámetros utilizados al generar un reporte';
COMMENT ON COLUMN reporte_parametros.reporte_id IS 'ID del reporte al que pertenecen los parámetros';
COMMENT ON COLUMN reporte_parametros.clave IS 'Nombre del parámetro (ej: doctor_id, sala_id, estado)';
COMMENT ON COLUMN reporte_parametros.valor IS 'Valor del parámetro';
