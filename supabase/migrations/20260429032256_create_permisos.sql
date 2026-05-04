-- =============================================================================
-- Migración: create_permisos
-- Descripción: Permisos granulares por perfil y módulo (solo READ, CREATE, UPDATE).
-- Historia de Usuario: HU05 — Gestión de Permisos
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

CREATE TABLE IF NOT EXISTS permisos (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    perfil_id           UUID NOT NULL REFERENCES perfiles(id) ON DELETE RESTRICT,
    modulo_id           UUID NOT NULL REFERENCES modulos_sistema(id) ON DELETE RESTRICT,
    puede_leer          BOOLEAN NOT NULL DEFAULT false,
    puede_crear         BOOLEAN NOT NULL DEFAULT false,
    puede_actualizar    BOOLEAN NOT NULL DEFAULT false,
    -- NOTA: No existe puede_eliminar — el sistema no permite eliminación
    fecha_modificacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modificado_por      UUID REFERENCES usuarios(id),

    UNIQUE (clinica_id, perfil_id, modulo_id)
);

COMMENT ON TABLE permisos IS 'Permisos por perfil y módulo. Solo READ, CREATE, UPDATE — no DELETE';
COMMENT ON COLUMN permisos.id IS 'Identificador único autogenerado por la base de datos';
COMMENT ON COLUMN permisos.clinica_id IS 'Clínica (tenant) a la que pertenece el permiso';
COMMENT ON COLUMN permisos.puede_leer IS 'Permite visualizar listados y registros del módulo';
COMMENT ON COLUMN permisos.puede_crear IS 'Permite insertar nuevos registros en el módulo';
COMMENT ON COLUMN permisos.puede_actualizar IS 'Permite editar registros existentes en el módulo';

CREATE INDEX IF NOT EXISTS idx_permisos_clinica_id ON permisos(clinica_id);
CREATE INDEX IF NOT EXISTS idx_permisos_perfil_id ON permisos(perfil_id);
CREATE INDEX IF NOT EXISTS idx_permisos_modulo_id ON permisos(modulo_id);
-- Índice compuesto para la consulta más frecuente: "¿puede este perfil leer este módulo?"
CREATE INDEX IF NOT EXISTS idx_permisos_perfil_modulo ON permisos(clinica_id, perfil_id, modulo_id);

ALTER TABLE permisos ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_permisos" ON permisos
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_permisos" ON permisos
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON permisos TO authenticated;
