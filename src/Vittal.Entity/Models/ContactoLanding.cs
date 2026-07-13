namespace Vittal.Entity.Models;

/// <summary>
/// Contactos recibidos desde el formulario de la landing page.
/// Tabla global del sistema (sin clinica_id) — solo Super Admin gestiona.
/// Excepción a CLAUDE.md §12: no requiere clinica_id al ser global.
/// Historia de Usuario: HU-L01 — Landing Page Informativa
/// </summary>
public class ContactoLanding
{
    // ── Campos primarios ──────────────────────────────────────────

    /// <summary>Identificador único del contacto</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre completo del contactante (requerido, máx. 200 caracteres)</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Correo electrónico del contactante (requerido, formato válido)</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Número de teléfono del contactante (opcional, máx. 20 caracteres)</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Rol del contactante: director, gerente, admin, doctor, otro</summary>
    public string Rol { get; set; } = string.Empty;

    /// <summary>Mensaje del contactante (requerido, máx. 2000 caracteres)</summary>
    public string Mensaje { get; set; } = string.Empty;

    // ── Campos de estado y auditoría ──────────────────────────────

    /// <summary>Indica si el contacto ha sido leído por el admin</summary>
    public bool Leido { get; set; } = false;

    /// <summary>Estado del contacto (activo/inactivo) — nunca se elimina</summary>
    public bool Activo { get; set; } = true;

    /// <summary>Fecha y hora de creación del registro (UTC)</summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha y hora de la última modificación</summary>
    public DateTime? FechaModificacion { get; set; }
}
