-- =============================================================================
-- Migración: create_contactos_landing
-- Descripción: Tabla para contactos recibidos desde el formulario de la landing
--              page. Tabla GLOBAL del sistema — sin clinica_id (excepción CLAUDE.md §12).
--              Solo el Super Admin puede gestionar estos contactos.
--              Incluye bucket de Storage para imágenes de la landing.
-- Historia de Usuario: HU-L01 — Landing Page Informativa
-- Agente: @IngenieroDatos
-- Sprint: 7 — Landing Page Informativa
-- Fecha: 2026-07-11
-- Dependencias: Ninguna
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Tabla: contactos_landing
-- Almacena los mensajes enviados desde el formulario de contacto de la landing.
-- NOTA: Sin clinica_id — tabla global del sistema (excepción CLAUDE.md §12).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS contactos_landing (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre_completo     VARCHAR(200) NOT NULL,
    email               VARCHAR(255) NOT NULL,
    telefono            VARCHAR(20),
    rol                 VARCHAR(50) NOT NULL
                        CHECK (rol IN ('director', 'gerente', 'admin', 'doctor', 'otro')),
    mensaje             TEXT,
    leido               BOOLEAN NOT NULL DEFAULT false,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

-- ---------------------------------------------------------------------------
-- Comentarios (español, obligatorio)
-- ---------------------------------------------------------------------------
COMMENT ON TABLE contactos_landing IS
  'Contactos recibidos desde el formulario de la landing page. '
  'Tabla global del sistema — sin clinica_id. Solo Super Admin gestiona.';
COMMENT ON COLUMN contactos_landing.id IS 'Identificador único del contacto (UUID autogenerado)';
COMMENT ON COLUMN contactos_landing.nombre_completo IS 'Nombre completo del contactante (requerido, máx. 200 caracteres)';
COMMENT ON COLUMN contactos_landing.email IS 'Correo electrónico del contactante (requerido, formato válido)';
COMMENT ON COLUMN contactos_landing.telefono IS 'Número de teléfono del contactante (opcional, máx. 20 caracteres)';
COMMENT ON COLUMN contactos_landing.rol IS 'Rol del contactante: director, gerente, admin, doctor, otro';
COMMENT ON COLUMN contactos_landing.mensaje IS 'Mensaje enviado desde el formulario de contacto';
COMMENT ON COLUMN contactos_landing.leido IS 'Indica si el contacto ha sido leído por el administrador';
COMMENT ON COLUMN contactos_landing.activo IS 'Los contactos no se eliminan, solo se desactivan';
COMMENT ON COLUMN contactos_landing.fecha_creacion IS 'Fecha y hora de creación del registro (UTC)';
COMMENT ON COLUMN contactos_landing.fecha_modificacion IS 'Fecha y hora de la última modificación (UTC)';

-- ---------------------------------------------------------------------------
-- Índices de rendimiento
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_contactos_landing_email ON contactos_landing(email);
CREATE INDEX IF NOT EXISTS idx_contactos_landing_activo ON contactos_landing(activo);
CREATE INDEX IF NOT EXISTS idx_contactos_landing_fecha ON contactos_landing(fecha_creacion);
CREATE INDEX IF NOT EXISTS idx_contactos_landing_leido ON contactos_landing(leido);

-- ---------------------------------------------------------------------------
-- RLS (Row Level Security)
-- La tabla NO tiene clinica_id. RLS protege los datos del sistema.
-- Solo service_role puede acceder (usado por el API backend).
-- ---------------------------------------------------------------------------
ALTER TABLE contactos_landing ENABLE ROW LEVEL SECURITY;

CREATE POLICY "service_role_only" ON contactos_landing
    FOR ALL USING (auth.role() = 'service_role');

-- ---------------------------------------------------------------------------
-- Bucket de Storage: landing
-- Almacena imágenes de la landing page (logos, screenshots, icons, etc.)
-- Bucket PÚBLICO de lectura — las imágenes se muestran sin autenticación.
-- Escritura restringida a service_role (solo admin puede subir imágenes).
-- ---------------------------------------------------------------------------

-- Crear bucket (idempotente)
INSERT INTO storage.buckets (id, name, public)
VALUES ('landing', 'landing', true)
ON CONFLICT (id) DO NOTHING;

-- Política: lectura pública para todos (las imágenes se ven en la landing)
CREATE POLICY "landing_public_read" ON storage.objects
    FOR SELECT USING (bucket_id = 'landing');

-- Política: solo service_role puede subir imágenes
CREATE POLICY "landing_service_role_insert" ON storage.objects
    FOR INSERT WITH CHECK (
        bucket_id = 'landing'
        AND auth.role() = 'service_role'
    );

-- Política: solo service_role puede eliminar imágenes
CREATE POLICY "landing_service_role_delete" ON storage.objects
    FOR DELETE USING (
        bucket_id = 'landing'
        AND auth.role() = 'service_role'
    );
