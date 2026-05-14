-- =============================================================================
-- Migración: create_notificaciones
-- Descripción: Historial persistente de notificaciones del sistema.
--              Las notificaciones se muestran en tiempo real en la UI
--              y se almacenan para consulta histórica.
-- Historia de Usuario: HU23 — Alertas Configurables
-- Agente: @IngenieroDatos
-- Fecha: 2026-05-13
-- =============================================================================

CREATE TABLE IF NOT EXISTS notificaciones (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    alerta_id           UUID REFERENCES alertas_espera(id) ON DELETE SET NULL,
    tipo                VARCHAR(30) NOT NULL DEFAULT 'alerta_espera',
    titulo              VARCHAR(200) NOT NULL,
    mensaje             TEXT NOT NULL,
    icono               VARCHAR(50),
    color               VARCHAR(30),
    leida               BOOLEAN NOT NULL DEFAULT false,
    usuario_destino_id  UUID REFERENCES usuarios(id) ON DELETE SET NULL,
    fecha_lectura       TIMESTAMPTZ,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- Índices de rendimiento
-- =============================================================================
CREATE INDEX IF NOT EXISTS idx_notificaciones_clinica ON notificaciones(clinica_id, fecha_creacion DESC);
CREATE INDEX IF NOT EXISTS idx_notificaciones_no_leidas ON notificaciones(clinica_id, leida)
    WHERE leida = false;
CREATE INDEX IF NOT EXISTS idx_notificaciones_tipo ON notificaciones(clinica_id, tipo);
CREATE INDEX IF NOT EXISTS idx_notificaciones_usuario ON notificaciones(usuario_destino_id)
    WHERE usuario_destino_id IS NOT NULL;

-- =============================================================================
-- Row Level Security (RLS)
-- =============================================================================
ALTER TABLE notificaciones ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_notificaciones" ON notificaciones
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_notificaciones" ON notificaciones
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON notificaciones TO authenticated;

-- =============================================================================
-- Realtime: publicar nuevas notificaciones inmediatamente
-- =============================================================================
ALTER PUBLICATION supabase_realtime ADD TABLE IF NOT EXISTS notificaciones;

-- =============================================================================
-- Comentarios
-- =============================================================================
COMMENT ON TABLE notificaciones IS 'Historial persistente de notificaciones del sistema (HU23)';
COMMENT ON COLUMN notificaciones.alerta_id IS 'ID de la alerta relacionada (opcional, para alertas de tiempo de espera)';
COMMENT ON COLUMN notificaciones.tipo IS 'Tipo de notificación: alerta_espera, sistema, recordatorio, informacion, advertencia, exito';
COMMENT ON COLUMN notificaciones.titulo IS 'Título corto de la notificación';
COMMENT ON COLUMN notificaciones.mensaje IS 'Mensaje detallado de la notificación';
COMMENT ON COLUMN notificaciones.icono IS 'Nombre del icono a mostrar (ej: clock, bell, check-circle)';
COMMENT ON COLUMN notificaciones.color IS 'Color de la notificación (ej: warning, danger, success, info)';
COMMENT ON COLUMN notificaciones.leida IS 'TRUE si el usuario ha visto la notificación';
COMMENT ON COLUMN notificaciones.usuario_destino_id IS 'Usuario destino (NULL = todos los usuarios de la clínica)';
COMMENT ON COLUMN notificaciones.fecha_lectura IS 'Fecha y hora en que se leyó la notificación';
