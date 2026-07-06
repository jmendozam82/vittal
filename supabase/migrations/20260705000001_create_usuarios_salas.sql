-- =============================================================================
-- Migracion: create_usuarios_salas
-- Descripcion: Tabla de asignacion de doctores a salas.
--              Un doctor puede estar en multiples salas.
--              Una sala puede tener multiples doctores.
--              UNIQUE(usuario_id, sala_id) evita duplicados.
--              Baja logica con activo = false.
-- Historia de Usuario: HU06 — Asignar Doctores a Salas
-- Agente: @IngenieroDatos
-- Sprint: 3.5 — Especialidades por Sala
-- Fecha: 2026-07-05
-- Dependencias: create_usuarios, create_salas, create_clinicas
-- =============================================================================

CREATE TABLE IF NOT EXISTS usuarios_salas (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id          UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    sala_id             UUID NOT NULL REFERENCES salas(id) ON DELETE RESTRICT,
    clinica_id          UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,
    UNIQUE(usuario_id, sala_id)
);

COMMENT ON TABLE usuarios_salas IS
  'Asignacion de doctores a salas. Un doctor puede estar en multiples salas. '
  'Controla que doctores pueden atender en cada sala segun su especialidad.';
COMMENT ON COLUMN usuarios_salas.usuario_id IS 'Doctor asignado a la sala (FK a usuarios)';
COMMENT ON COLUMN usuarios_salas.sala_id IS 'Sala a la que se asigna el doctor (FK a salas)';
COMMENT ON COLUMN usuarios_salas.clinica_id IS 'Tenant (clinica) al que pertenece la asignacion';
COMMENT ON COLUMN usuarios_salas.activo IS 'Si false, el doctor ya no esta asignado a la sala (baja logica)';

-- Indices
CREATE INDEX IF NOT EXISTS idx_usuarios_salas_usuario ON usuarios_salas(usuario_id);
CREATE INDEX IF NOT EXISTS idx_usuarios_salas_sala ON usuarios_salas(sala_id);
CREATE INDEX IF NOT EXISTS idx_usuarios_salas_clinica ON usuarios_salas(clinica_id);
CREATE INDEX IF NOT EXISTS idx_usuarios_salas_activo ON usuarios_salas(clinica_id, activo);

-- RLS
ALTER TABLE usuarios_salas ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_usuarios_salas" ON usuarios_salas
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_access_usuarios_salas" ON usuarios_salas
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON usuarios_salas TO authenticated;

-- =============================================================================
-- Agregar módulos faltantes al sistema de permisos
-- usuarios_salas, tipos_antecedente y tipos_signo_vital se crearon como tablas
-- pero nunca se registraron en modulos_sistema, lo que impedía asignar permisos.
-- =============================================================================
INSERT INTO modulos_sistema (clave, nombre, descripcion) VALUES
    ('usuarios_salas', 'Asignar Doctores a Salas', 'Asignación de doctores a salas por especialidad'),
    ('tipos_antecedente', 'Tipos de Antecedente', 'Catálogo de tipos de antecedentes médicos por sala'),
    ('tipos_signo_vital', 'Tipos de Signo Vital', 'Catálogo de tipos de signos vitales por sala')
ON CONFLICT (clave) DO NOTHING;
