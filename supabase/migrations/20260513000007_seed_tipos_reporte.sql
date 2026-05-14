-- =============================================================================
-- Migración: seed_tipos_reporte
-- Descripción: Catálogo global de tipos de reporte disponibles en el sistema.
--              Esta tabla NO tiene clinica_id — es un catálogo del sistema
--              administrado por Super Admin. Todos los tenants la consultan.
-- Historia de Usuario: HU22 — Reportes
-- Agente: @IngenieroDatos
-- Fecha: 2026-05-13
-- =============================================================================

-- Tabla de sistema: tipos de reporte disponibles (global, sin clinica_id)
CREATE TABLE IF NOT EXISTS tipos_reporte (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clave           VARCHAR(50) NOT NULL UNIQUE,
    nombre          VARCHAR(200) NOT NULL,
    descripcion     TEXT,
    icono           VARCHAR(50) DEFAULT 'bi-file-earmark-barGraph',
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- Seed data: tipos de reporte predefinidos
-- =============================================================================
INSERT INTO tipos_reporte (clave, nombre, descripcion, icono) VALUES
    ('pacientes_por_dia', 'Pacientes por día', 'Cantidad de pacientes atendidos por día en un rango de fechas', 'bi-people'),
    ('citas_por_estado', 'Citas por estado', 'Distribución de citas por estado (agendada, atendida, cancelada)', 'bi-pie-chart'),
    ('doctores_mas_activos', 'Doctores más activos', 'Top doctores con más citas atendidas en el período', 'bi-trophy'),
    ('tiempo_promedio_espera', 'Tiempo promedio de espera', 'Tiempo promedio que los pacientes esperan para ser atendidos', 'bi-clock-history')
ON CONFLICT (clave) DO NOTHING;

-- =============================================================================
 -- Índices
-- =============================================================================
CREATE INDEX IF NOT EXISTS idx_tipos_reporte_activo ON tipos_reporte(activo);

-- =============================================================================
-- Row Level Security (RLS): SOLO LECTURA para usuarios autenticados
-- =============================================================================
ALTER TABLE tipos_reporte ENABLE ROW LEVEL SECURITY;

CREATE POLICY "tipos_reporte_lectura" ON tipos_reporte
    FOR SELECT USING (true);

GRANT SELECT ON tipos_reporte TO authenticated;

-- =============================================================================
-- Comentarios
-- =============================================================================
COMMENT ON TABLE tipos_reporte IS 'Catálogo de tipos de reporte disponibles en el sistema (catálogo global, sin clinica_id)';
COMMENT ON COLUMN tipos_reporte.clave IS 'Identificador único del tipo de reporte (ej: pacientes_por_dia)';
COMMENT ON COLUMN tipos_reporte.nombre IS 'Nombre descriptivo visible en la UI';
COMMENT ON COLUMN tipos_reporte.icono IS 'Clase Bootstrap Icons para el icono representativo';
