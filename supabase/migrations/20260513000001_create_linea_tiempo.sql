-- =============================================================================
-- Migración: create_linea_tiempo
-- Descripción: Seguimiento de pacientes por sala/área durante su visita.
--              Cada paso (Registro, Espera, Consulta, Diagnóstico, Salida)
--              se registra con timestamps de entrada y salida.
-- Historia de Usuario: HU19 — Línea de Tiempo
-- Agente: @IngenieroDatos
-- Fecha: 2026-05-13
-- =============================================================================

CREATE TABLE IF NOT EXISTS linea_tiempo (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    cita_id             UUID NOT NULL REFERENCES citas(id) ON DELETE RESTRICT,
    paciente_id         UUID NOT NULL REFERENCES pacientes(id) ON DELETE RESTRICT,
    sala_id             UUID REFERENCES salas(id) ON DELETE SET NULL,
    nombre_paso         VARCHAR(100) NOT NULL,
    orden               INTEGER NOT NULL CHECK (orden > 0),
    estado              VARCHAR(20) NOT NULL DEFAULT 'pendiente'
                        CHECK (estado IN ('pendiente', 'en_sala', 'completado', 'saltado')),
    hora_llegada        TIME,
    hora_salida         TIME,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

-- =============================================================================
-- Índices de rendimiento
-- =============================================================================
CREATE INDEX IF NOT EXISTS idx_linea_tiempo_cita ON linea_tiempo(cita_id, orden);
CREATE INDEX IF NOT EXISTS idx_linea_tiempo_clinica_fecha ON linea_tiempo(clinica_id, fecha_creacion);
CREATE INDEX IF NOT EXISTS idx_linea_tiempo_paciente ON linea_tiempo(paciente_id);
CREATE INDEX IF NOT EXISTS idx_linea_tiempo_estado ON linea_tiempo(clinica_id, estado);

-- =============================================================================
-- Row Level Security (RLS)
-- =============================================================================
ALTER TABLE linea_tiempo ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_linea_tiempo" ON linea_tiempo
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_linea_tiempo" ON linea_tiempo
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON linea_tiempo TO authenticated;

-- =============================================================================
-- Realtime: publicar cambios de línea de tiempo para la UI en vivo
-- =============================================================================
ALTER PUBLICATION supabase_realtime ADD TABLE IF NOT EXISTS linea_tiempo;

-- =============================================================================
-- Comentarios (español obligatorio)
-- =============================================================================
COMMENT ON TABLE linea_tiempo IS 'Seguimiento de pacientes por sala/área durante su visita (HU19)';
COMMENT ON COLUMN linea_tiempo.nombre_paso IS 'Nombre del paso: Registro, Espera, Consulta, Diagnóstico, Salida';
COMMENT ON COLUMN linea_tiempo.orden IS 'Orden del paso dentro de la secuencia de atención (1, 2, 3...)';
COMMENT ON COLUMN linea_tiempo.estado IS 'Estado del paso: pendiente, en_sala, completado, saltado';
COMMENT ON COLUMN linea_tiempo.hora_llegada IS 'Hora (TIME) en que el paciente inició este paso';
COMMENT ON COLUMN linea_tiempo.hora_salida IS 'Hora (TIME) en que el paciente completó este paso';
COMMENT ON COLUMN linea_tiempo.activo IS 'Los registros de línea de tiempo no se eliminan, solo se desactivan';
