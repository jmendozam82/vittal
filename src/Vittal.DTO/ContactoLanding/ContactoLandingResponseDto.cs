namespace Vittal.DTO.ContactoLanding;

/// <summary>
/// DTO de salida para contactos de landing (vista admin).
/// Contiene solo los campos necesarios para la administración.
/// Historia de Usuario: HU-L01 — Landing Page Informativa
/// </summary>
public class ContactoLandingResponseDto
{
    /// <summary>Identificador único del contacto</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre completo del contactante</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Correo electrónico del contactante</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Rol del contactante: director, gerente, admin, doctor, otro</summary>
    public string Rol { get; set; } = string.Empty;

    /// <summary>Fecha de creación del contacto (UTC)</summary>
    public DateTime FechaCreacion { get; set; }
}
