-- =============================================================================
-- Migración: create_notificaciones_usuario
-- Descripción: Estado de lectura individual por notificación y usuario.
--              Modelo estándar de apps: la notificación (mensaje) se crea una
--              sola vez (broadcast por clínica) y cada usuario tiene su propio
--              marcador de leído / fecha_lectura en esta tabla hija.
-- Historia de Usuario: HU23 — Alertas Configurables
-- Agente: @IngenieroDatos
-- Fecha: 2026-08-08
-- =============================================================================

-- =============================================================================
-- 1. Tabla relacional notificación ↔ usuario
-- =============================================================================
CREATE TABLE IF NOT EXISTS notificaciones_usuario (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    notificacion_id     UUID NOT NULL REFERENCES notificaciones(id) ON DELETE CASCADE,
    usuario_id          UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    leida               BOOLEAN NOT NULL DEFAULT false,
    fecha_lectura       TIMESTAMPTZ,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (notificacion_id, usuario_id)
);

-- =============================================================================
-- 2. Índices de rendimiento
-- =============================================================================
CREATE INDEX IF NOT EXISTS idx_notificaciones_usuario_usuario ON notificaciones_usuario(usuario_id);
CREATE INDEX IF NOT EXISTS idx_notificaciones_usuario_usuario_leida ON notificaciones_usuario(usuario_id, leida)
    WHERE leida = false;
CREATE INDEX IF NOT EXISTS idx_notificaciones_usuario_notificacion ON notificaciones_usuario(notificacion_id);

-- =============================================================================
-- 3. Backfill: asignar las notificaciones existentes a los usuarios activos
--    de su clínica. El estado leida heredado de la columna compartida actual.
-- =============================================================================
INSERT INTO notificaciones_usuario (notificacion_id, usuario_id, leida, fecha_lectura, fecha_creacion)
SELECT n.id,
       u.id,
       n.leida,
       n.fecha_lectura,
       n.fecha_creacion
FROM notificaciones n
JOIN usuarios u ON u.clinica_id = n.clinica_id
WHERE n.activo = true
  AND u.activo = true
ON CONFLICT (notificacion_id, usuario_id) DO NOTHING;

-- =============================================================================
-- 4. Row Level Security (RLS) — aislamiento por clínica vía notificaciones
-- =============================================================================
ALTER TABLE notificaciones_usuario ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_notificaciones_usuario" ON notificaciones_usuario
    FOR ALL
    USING (EXISTS (
        SELECT 1 FROM notificaciones n
        WHERE n.id = notificacion_id
          AND n.clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID
    ))
    WITH CHECK (EXISTS (
        SELECT 1 FROM notificaciones n
        WHERE n.id = notificacion_id
          AND n.clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID
    ));

CREATE POLICY "service_role_full_access_notificaciones_usuario" ON notificaciones_usuario
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON notificaciones_usuario TO authenticated;

-- =============================================================================
-- 5. Comentarios
-- =============================================================================
COMMENT ON TABLE notificaciones_usuario IS 'Estado de lectura individual por notificación y usuario (HU23)';
COMMENT ON COLUMN notificaciones_usuario.notificacion_id IS 'Notificación (mensaje) a la que pertenece el estado de lectura';
COMMENT ON COLUMN notificaciones_usuario.usuario_id IS 'Usuario destinatario. Cada usuario tiene su propio marcador de leído';
COMMENT ON COLUMN notificaciones_usuario.leida IS 'TRUE si el usuario ha leído la notificación';
COMMENT ON COLUMN notificaciones_usuario.fecha_lectura IS 'Fecha y hora en que el usuario marcó la notificación como leída';