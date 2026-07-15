-- Agregar campos de documento de identificación a usuarios y pacientes
-- HU Mejora: Documento de Identificación Obligatorio para Nicaragua

-- =============================================
-- TABLA: usuarios
-- =============================================
ALTER TABLE usuarios
  ADD COLUMN tipo_documento_identificacion VARCHAR(2),
  ADD COLUMN numero_documento_identificacion VARCHAR(30);

-- Comentarios
COMMENT ON COLUMN usuarios.tipo_documento_identificacion IS 'Tipo de documento: CC=Cédula Ciudadanía, CR=Cédula Residente, PA=Pasaporte';
COMMENT ON COLUMN usuarios.numero_documento_identificacion IS 'Número único de documento de identificación por clínica';

-- =============================================
-- TABLA: pacientes
-- =============================================
ALTER TABLE pacientes
  ADD COLUMN tipo_documento_identificacion VARCHAR(2),
  ADD COLUMN numero_documento_identificacion VARCHAR(30);

-- Comentarios
COMMENT ON COLUMN pacientes.tipo_documento_identificacion IS 'Tipo de documento: CC=Cédula Ciudadanía, CR=Cédula Residente, PA=Pasaporte';
COMMENT ON COLUMN pacientes.numero_documento_identificacion IS 'Número único de documento de identificación por clínica';

-- =============================================
-- ÍNDICES ÚNICOS POR CLÍNICA (tenant-scoped uniqueness)
-- Partial index: only enforces uniqueness for non-NULL values
-- This allows existing records without document to remain
-- =============================================
CREATE UNIQUE INDEX idx_usuarios_num_documento_clinica
  ON usuarios(clinica_id, numero_documento_identificacion)
  WHERE numero_documento_identificacion IS NOT NULL;

CREATE UNIQUE INDEX idx_pacientes_num_documento_clinica
  ON pacientes(clinica_id, numero_documento_identificacion)
  WHERE numero_documento_identificacion IS NOT NULL;

-- =============================================
-- ÍNDICES DE BÚSQUEDA
-- =============================================
CREATE INDEX idx_usuarios_tipo_documento ON usuarios(tipo_documento_identificacion);
CREATE INDEX idx_pacientes_tipo_documento ON pacientes(tipo_documento_identificacion);
