-- =============================================================================
-- Migración: create_modulos_sistema
-- Descripción: Catálogo de módulos del sistema + seed de 23 módulos base.
-- Historia de Usuario: HU05 — Gestión de Permisos
-- Agente: @IngenieroDatos
-- Fecha: 2026-04-28
-- =============================================================================

CREATE TABLE IF NOT EXISTS modulos_sistema (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clave       VARCHAR(50) NOT NULL UNIQUE,
    nombre      VARCHAR(100) NOT NULL,
    descripcion TEXT,
    activo      BOOLEAN NOT NULL DEFAULT true
);

COMMENT ON TABLE modulos_sistema IS 'Catálogo de módulos del sistema. Solo administradores del sistema pueden modificar';
COMMENT ON COLUMN modulos_sistema.clave IS 'Clave técnica usada en el código para verificar permisos';

-- Seed de los 23 módulos del sistema
INSERT INTO modulos_sistema (clave, nombre, descripcion) VALUES
    ('login',           'Acceso al sistema',        'Módulo de autenticación'),
    ('perfiles',        'Gestión de perfiles',      'Administración de perfiles de usuario'),
    ('usuarios',        'Gestión de usuarios',      'Administración de usuarios del sistema'),
    ('permisos',        'Gestión de permisos',      'Asignación de permisos por perfil'),
    ('salas',           'Gestión de salas',         'Asignación de salas a doctores'),
    ('pacientes',       'Gestión de pacientes',     'CRUD de pacientes de la clínica'),
    ('medicamentos',    'Medicamentos',             'Catálogo de medicamentos'),
    ('clinicas',        'Gestión de clínicas',      'Administración de clínicas y sucursales'),
    ('areas',           'Salas y áreas',            'Gestión de salas y áreas de la clínica'),
    ('tipos_cirugia',   'Tipos de cirugías',        'Catálogo de tipos de cirugías'),
    ('cirugias',        'Cirugías',                 'Catálogo de cirugías'),
    ('tipos_dx',        'Tipos de diagnósticos',    'Catálogo de tipos de diagnósticos'),
    ('diagnosticos',    'Diagnósticos',             'Catálogo de diagnósticos'),
    ('tratamientos',    'Tratamientos',             'Catálogo de tratamientos'),
    ('recomendaciones', 'Recomendaciones',          'Catálogo de recomendaciones médicas'),
    ('examenes',        'Exámenes',                 'Catálogo de exámenes médicos'),
    ('cola_espera',     'Cola de espera',           'Visualización de pacientes en espera del día'),
    ('linea_tiempo',    'Línea de tiempo',          'Seguimiento de pacientes paso a paso'),
    ('expedientes',     'Expedientes médicos',      'Historial clínico completo de pacientes'),
    ('agenda',          'Agenda de citas',          'Programación y gestión de citas médicas'),
    ('dashboard',       'Dashboard',                'Visualización de gráficos y métricas'),
    ('reportes',        'Reportes',                 'Reportes generales y detallados'),
    ('alertas',         'Alertas configurables',    'Notificaciones de tiempo de espera excedido')
ON CONFLICT (clave) DO NOTHING;

GRANT SELECT ON modulos_sistema TO authenticated;
